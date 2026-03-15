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

            // YawTarget und PitchTarget aktualisieren (falls bereits gesetzt)
            if (m_YawTarget != null)
            {
                m_YawTarget.rotation = Quaternion.Euler(0f, m_Yaw, 0f);
            }

            if (m_PitchTarget != null)
            {
                m_PitchTarget.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
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
        /// Fuegt einen View-Punch hinzu (SoF2 kickAngles).
        /// Modifiziert Pitch und Yaw permanent — der Spieler muss mit der Maus gegenlenken.
        /// Pitch-Kick ist positiv nach oben (Waffe kickt hoch), Yaw-Kick seitwärts.
        /// </summary>
        public void AddViewPunch(float pitchKick, float yawKick)
        {
            m_Pitch -= pitchKick;
            m_Pitch = Mathf.Clamp(m_Pitch, m_PitchMin, m_PitchMax);
            m_Yaw += yawKick;
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
