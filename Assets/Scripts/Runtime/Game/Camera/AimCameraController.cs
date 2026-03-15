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

        // ===== SoF2 Kick-Angles (temporaerer View-Overlay mit Decay) =====

        /// <summary>Decay-Dauer fuer Kick-Angles in Sekunden (SoF2: ~200ms).</summary>
        private const float KICK_DECAY_TIME = 0.2f;

        /// <summary>Aktueller Kick-Pitch-Offset (decayed ueber Zeit zurueck auf 0).</summary>
        private float m_KickPitch;

        /// <summary>Aktueller Kick-Yaw-Offset (decayed ueber Zeit zurueck auf 0).</summary>
        private float m_KickYaw;

        /// <summary>Initiale Kick-Pitch-Staerke bei letztem Schuss (fuer linearen Decay).</summary>
        private float m_KickPitchStart;

        /// <summary>Initiale Kick-Yaw-Staerke bei letztem Schuss (fuer linearen Decay).</summary>
        private float m_KickYawStart;

        /// <summary>Zeitpunkt des letzten Kicks (fuer Decay-Berechnung).</summary>
        private float m_KickTime;

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

            // SoF2 Kick-Angles: linearer Decay ueber KICK_DECAY_TIME
            float kickPitchOffset = 0f;
            float kickYawOffset = 0f;
            float timeSinceKick = Time.time - m_KickTime;

            if (timeSinceKick < KICK_DECAY_TIME)
            {
                float ratio = 1f - (timeSinceKick / KICK_DECAY_TIME);
                kickPitchOffset = m_KickPitchStart * ratio;
                kickYawOffset = m_KickYawStart * ratio;
            }

            // YawTarget und PitchTarget aktualisieren (Basis + Kick-Overlay)
            if (m_YawTarget != null)
            {
                m_YawTarget.rotation = Quaternion.Euler(0f, m_Yaw + kickYawOffset, 0f);
            }

            if (m_PitchTarget != null)
            {
                float effectivePitch = Mathf.Clamp(m_Pitch + kickPitchOffset, m_PitchMin, m_PitchMax);
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
        /// Fuegt einen View-Punch hinzu (SoF2 kick_angles).
        /// Kick wird als temporaerer Overlay auf die View angewendet und decayed
        /// linear ueber KICK_DECAY_TIME (~200ms) zurueck auf Null.
        /// Bei Schnellfeuer addieren sich Kicks auf den aktuellen Restwert.
        /// Pitch-Kick ist positiv nach oben (Waffe kickt hoch), Yaw-Kick seitwaerts.
        /// </summary>
        public void AddViewPunch(float pitchKick, float yawKick)
        {
            // Bei Schnellfeuer: aktuellen Restwert als Basis nehmen
            float timeSinceKick = Time.time - m_KickTime;
            float remaining = 0f;

            if (timeSinceKick < KICK_DECAY_TIME)
            {
                remaining = 1f - (timeSinceKick / KICK_DECAY_TIME);
            }

            // Neuen Kick auf verbleibenden Kick addieren
            m_KickPitchStart = (m_KickPitchStart * remaining) - pitchKick;
            m_KickYawStart = (m_KickYawStart * remaining) + yawKick;
            m_KickTime = Time.time;
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
    }
}
