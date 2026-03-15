using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tolik.RemakeSoF.Runtime.Game.Camera
{
    /// <summary>
    /// Steuert die Kamera-Rotation (Yaw/Pitch) basierend auf Maus-/Gamepad-Input.
    /// Rotiert YawTarget (horizontal) und PitchTarget (vertikal) für Cinemachine-Kameras.
    /// </summary>
    public class AimCameraController : MonoBehaviour
    {
        /// <summary>
        /// YawTarget-Transform. Wird zur Laufzeit via <see cref="SetTargets"/> gesetzt,
        /// da es erst nach Visual-Instanziierung existiert.
        /// </summary>
        private Transform m_YawTarget;

        /// <summary>
        /// PitchTarget-Transform. Wird zur Laufzeit via <see cref="SetTargets"/> gesetzt.
        /// </summary>
        private Transform m_PitchTarget;

        [SerializeField]
        private InputActionReference m_LookInput;

        [SerializeField]
        private InputActionReference m_SwitchShoulderInput;

        [SerializeField]
        private float m_MouseSensitivity = 0.05f;

        [SerializeField]
        private float m_GamepadSensitivity = 0.5f;

        [SerializeField]
        private float m_Sensitivity = 1.5f;

        [SerializeField]
        private float m_PitchMin = -80f;

        [SerializeField]
        private float m_PitchMax = 80f;

        [SerializeField]
        private float m_ShoulderSwitchSpeed = 5f;

        /// <summary>
        /// Referenz auf CinemachineThirdPersonFollow für Shoulder-Switch.
        /// </summary>
        private CinemachineThirdPersonFollow m_AimCam;

        /// <summary>
        /// Aktueller Yaw-Winkel (horizontal).
        /// </summary>
        private float m_Yaw;

        /// <summary>
        /// Aktueller Pitch-Winkel (vertikal).
        /// </summary>
        private float m_Pitch;

        /// <summary>
        /// Zielwert für CameraSide (Shoulder-Switch).
        /// </summary>
        private float m_TargetCameraSide;

        /// <summary>
        /// Akkumulierter Look-Input zwischen FixedUpdate-Aufrufen.
        /// </summary>
        private Vector2 m_AccumulatedLookInput = Vector2.zero;

        // ===== SoF2 Kick-Angles (akkumulierter View-Punch mit Dual-Mode Decay) =====

        /// <summary>Zeitfenster in Sekunden um "feuert noch" zu erkennen (kein RPC-based weaponstate).</summary>
        private const float KICK_FIRING_WINDOW = 0.15f;

        /// <summary>Linearer Decay waehrend Feuer: 0.01°/ms = 10°/s (SoF2: degreesCorrectedPerMSecond).</summary>
        private const float KICK_LINEAR_DECAY_RATE = 10f;

        /// <summary>Exponentieller Decay-Faktor wenn nicht feuert (SoF2: 0.3).</summary>
        private const float KICK_EXPONENTIAL_FACTOR = 0.3f;

        /// <summary>Basis-Millisekunden fuer den exponentiellen Faktor (SoF2: 50ms).</summary>
        private const float KICK_EXPONENTIAL_BASE_MS = 50f;

        /// <summary>Deadzone: Snap auf 0 wenn innerhalb ±0.05° (SoF2).</summary>
        private const float KICK_DEADZONE = 0.05f;

        /// <summary>Akkumulierter Kick-Pitch-Offset in Grad (decayed per-frame Richtung 0).</summary>
        private float m_KickPitch;

        /// <summary>Akkumulierter Kick-Yaw-Offset in Grad (decayed per-frame Richtung 0).</summary>
        private float m_KickYaw;

        /// <summary>Zeitpunkt des letzten Kick-Eingangs (fuer Firing-Detection).</summary>
        private float m_KickTime;

        // ===== Explosion Camera Shake =====

        /// <summary>Aktuelle Shake-Intensitaet (nimmt ueber Zeit ab).</summary>
        private float m_ShakeIntensity;

        /// <summary>Shake-Dauer in Sekunden.</summary>
        private float m_ShakeDuration;

        /// <summary>Vergangene Shake-Zeit.</summary>
        private float m_ShakeElapsed;

        /// <summary>Aktueller Shake-Offset auf Pitch.</summary>
        private float m_ShakePitchOffset;

        /// <summary>Aktueller Shake-Offset auf Yaw.</summary>
        private float m_ShakeYawOffset;

        private void Awake()
        {
            m_AimCam = GetComponent<CinemachineThirdPersonFollow>();
            m_TargetCameraSide = m_AimCam.CameraSide;
        }

        private void Start()
        {
            m_LookInput.asset.Enable();
        }

        private void OnEnable()
        {
            m_SwitchShoulderInput.action.Enable();
            m_SwitchShoulderInput.action.performed += OnSwitchShoulder;
        }

        private void OnDisable()
        {
            m_SwitchShoulderInput.action.Disable();
            m_SwitchShoulderInput.action.performed -= OnSwitchShoulder;
        }

        /// <summary>
        /// Callback: Schulterseite wechseln.
        /// </summary>
        private void OnSwitchShoulder(InputAction.CallbackContext context)
        {
            m_TargetCameraSide = m_AimCam.CameraSide < 0.5f ? 1f : 0f;
        }

        private void Update()
        {
            Vector2 look = m_LookInput.action.ReadValue<Vector2>();

            if (Mouse.current != null && Mouse.current.delta.IsActuated())
            {
                m_AccumulatedLookInput += look * m_MouseSensitivity;
            }
            else if (Gamepad.current != null && Gamepad.current.rightStick.IsActuated())
            {
                m_AccumulatedLookInput += look * m_GamepadSensitivity * Time.deltaTime;
            }

            // Rotation sofort in Update anwenden — Cinemachine liest in LateUpdate,
            // braucht daher jeden Frame frische Werte fuer stutter-freie Kamera.
            // FixedUpdate wuerde nur 50Hz liefern → sichtbares Stottern.
            if (m_AccumulatedLookInput.sqrMagnitude > 0.0001f)
            {
                m_Yaw += m_AccumulatedLookInput.x * m_Sensitivity;
                m_Pitch -= m_AccumulatedLookInput.y * m_Sensitivity;
                m_Pitch = Mathf.Clamp(m_Pitch, m_PitchMin, m_PitchMax);

                m_AccumulatedLookInput = Vector2.zero;
            }

            // SoF2 Kick-Angles: Dual-Mode Decay (PM_Weapon_UpdateKickAngles)
            // Feuert noch → langsamer linearer Decay (10°/s)
            // Nicht mehr feuern → schneller exponentieller Decay (smooth Ruecklauf)
            float kickDt = Time.deltaTime;
            bool isFiring = (Time.time - m_KickTime) < KICK_FIRING_WINDOW;

            if (isFiring)
            {
                // SoF2: linearer Decay waehrend Feuer (0.01°/ms = 10°/s)
                float correction = KICK_LINEAR_DECAY_RATE * kickDt;
                m_KickPitch = Mathf.MoveTowards(m_KickPitch, 0f, correction);
                m_KickYaw = Mathf.MoveTowards(m_KickYaw, 0f, correction);
            }
            else
            {
                // SoF2: exponentieller Decay wenn nicht feuert
                // VectorScale(kickAngles, 1.0 - (0.3 * msec/50))
                float dtMs = kickDt * 1000f;
                float scale = 1f - (KICK_EXPONENTIAL_FACTOR * (dtMs / KICK_EXPONENTIAL_BASE_MS));
                scale = Mathf.Max(scale, 0f);
                m_KickPitch *= scale;
                m_KickYaw *= scale;

                // Deadzone: Snap auf 0 innerhalb ±0.05° (SoF2)
                if (Mathf.Abs(m_KickPitch) < KICK_DEADZONE)
                {
                    m_KickPitch = 0f;
                }

                if (Mathf.Abs(m_KickYaw) < KICK_DEADZONE)
                {
                    m_KickYaw = 0f;
                }
            }

            float kickPitchOffset = m_KickPitch;
            float kickYawOffset = m_KickYaw;

            // Explosion Camera Shake: zufaellige Richtungs-Offsets mit Decay
            float shakePitch = 0f;
            float shakeYaw = 0f;

            if (m_ShakeElapsed < m_ShakeDuration && m_ShakeIntensity > 0f)
            {
                m_ShakeElapsed += Time.deltaTime;
                float shakeRatio = 1f - Mathf.Clamp01(m_ShakeElapsed / m_ShakeDuration);
                float currentIntensity = m_ShakeIntensity * shakeRatio;

                // Perlin-Noise fuer organisches Wackeln (unterschiedliche Frequenzen fuer Pitch/Yaw)
                float noiseTime = Time.time * 25f;
                m_ShakePitchOffset = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * 2f * currentIntensity;
                m_ShakeYawOffset = (Mathf.PerlinNoise(0f, noiseTime + 100f) - 0.5f) * 2f * currentIntensity;

                shakePitch = m_ShakePitchOffset;
                shakeYaw = m_ShakeYawOffset;
            }

            // YawTarget und PitchTarget aktualisieren (Basis + Kick-Overlay + Shake)
            if (m_YawTarget != null)
            {
                m_YawTarget.rotation = Quaternion.Euler(0f, m_Yaw + kickYawOffset + shakeYaw, 0f);
            }

            if (m_PitchTarget != null)
            {
                float effectivePitch = Mathf.Clamp(m_Pitch + kickPitchOffset + shakePitch, m_PitchMin, m_PitchMax);
                m_PitchTarget.localRotation = Quaternion.Euler(effectivePitch, 0f, 0f);
            }

            // Shoulder-Switch interpolieren (visuell, nicht physik-kritisch)
            m_AimCam.CameraSide = Mathf.Lerp(m_AimCam.CameraSide, m_TargetCameraSide, Time.deltaTime * m_ShoulderSwitchSpeed);
        }

        /// <summary>
        /// Setzt die Yaw- und Pitch-Targets zur Laufzeit, nachdem das Visual instanziiert wurde.
        /// Initialisiert die aktuellen Yaw/Pitch-Werte basierend auf der YawTarget-Rotation.
        /// </summary>
        public void SetTargets(Transform yawTarget, Transform pitchTarget)
        {
            m_YawTarget = yawTarget;
            m_PitchTarget = pitchTarget;

            if (m_YawTarget != null)
            {
                Vector3 angles = m_YawTarget.rotation.eulerAngles;
                m_Yaw = angles.y;
                m_Pitch = angles.x;
            }
        }

        /// <summary>
        /// Fuegt einen View-Punch hinzu (SoF2 PM_Weapon_AddKickAngles).
        /// Kick wird auf den akkumulierten Offset addiert.
        /// Bei Schnellfeuer haeuft sich der Kick an (langsamer Decay waehrend Feuer).
        /// Beim Aufhoeren: schneller exponentieller Ruecklauf (smooth).
        /// Pitch-Kick ist positiv nach oben (Waffe kickt hoch), Yaw-Kick seitwaerts.
        /// </summary>
        public void AddViewPunch(float pitchKick, float yawKick)
        {
            m_KickPitch -= pitchKick;
            m_KickYaw += yawKick;
            m_KickTime = Time.time;

            // SoF2 Clamp: maximal 180° (kickPitch > 180000 in SoF2)
            m_KickPitch = Mathf.Clamp(m_KickPitch, -180f, 180f);
        }

        /// <summary>
        /// Setzt Yaw/Pitch basierend auf einer Kamera-Forward-Richtung.
        /// </summary>
        public void SetYawPitchFromCameraForward(Transform cameraTransform)
        {
            Vector3 flatForward = cameraTransform.forward;
            flatForward.y = 0;

            if (flatForward.sqrMagnitude < 0.001f)
            {
                return;
            }

            m_Yaw = Quaternion.LookRotation(flatForward).eulerAngles.y;

            if (m_YawTarget != null)
            {
                m_YawTarget.rotation = Quaternion.Euler(0f, m_Yaw, 0f);
            }

            if (m_PitchTarget != null)
            {
                m_PitchTarget.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        /// <summary>
        /// Fuegt einen Explosions-Kamera-Shake hinzu (proximity-basiert).
        /// Intensity in Grad (SoF2 bounce), Duration in Sekunden.
        /// Bei mehreren gleichzeitigen Shakes wird der staerkere behalten.
        /// </summary>
        public void AddExplosionShake(float intensity, float duration)
        {
            // Staerkeren Shake bevorzugen (nicht addieren, sonst uebertreibt es)
            if (intensity > m_ShakeIntensity * (1f - Mathf.Clamp01(m_ShakeElapsed / Mathf.Max(m_ShakeDuration, 0.01f))))
            {
                m_ShakeIntensity = intensity;
                m_ShakeDuration = duration;
                m_ShakeElapsed = 0f;
            }
        }
    }
}
