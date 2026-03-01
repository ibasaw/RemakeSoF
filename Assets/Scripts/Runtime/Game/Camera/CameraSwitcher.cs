using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tolik.RemakeSoF.Runtime.Game.Camera
{
    /// <summary>
    /// Umschalter zwischen First-Person und Third-Person Cinemachine-Kameras.
    /// Wechselt per Input-Action die Kamera-Prioritäten.
    /// </summary>
    public class CameraSwitcher : MonoBehaviour
    {
        [SerializeField]
        private CinemachineCamera m_AimCam;

        [SerializeField]
        private CinemachineCamera m_FirstPersonCam;

        [SerializeField]
        private bool m_StartFirstPerson = true;

        /// <summary>
        /// Aktueller Kameramodus.
        /// </summary>
        private bool m_IsFirstPerson;

        /// <summary>
        /// Input-Actions-Instanz für Kamera-Umschaltung.
        /// </summary>
        private AvatarActions m_InputActions;

        private void Awake()
        {
            m_InputActions = new AvatarActions();

            // Initialer Kameramodus
            m_IsFirstPerson = m_StartFirstPerson;
            SetCameraMode(m_IsFirstPerson);
        }

        private void OnEnable()
        {
            m_InputActions.Enable();
            m_InputActions.Player.SwitchCamera.performed += OnCameraSwitched;
        }

        private void OnDisable()
        {
            m_InputActions.Player.SwitchCamera.performed -= OnCameraSwitched;
            m_InputActions.Disable();
        }

        /// <summary>
        /// Callback: Kameramodus umschalten.
        /// </summary>
        private void OnCameraSwitched(InputAction.CallbackContext ctx)
        {
            m_IsFirstPerson = !m_IsFirstPerson;
            SetCameraMode(m_IsFirstPerson);

            Debug.Log($"[CameraSwitcher] Kamera gewechselt: {(m_IsFirstPerson ? "First Person" : "Third Person")}");
        }

        /// <summary>
        /// Setzt das Follow-Target der First-Person-Kamera zur Laufzeit.
        /// Wird aufgerufen nachdem das CameraTarget im Visual existiert.
        /// </summary>
        public void SetFirstPersonFollowTarget(Transform target)
        {
            if (m_FirstPersonCam != null)
            {
                m_FirstPersonCam.Follow = target;
            }
        }

        /// <summary>
        /// Setzt das Follow-Target der Third-Person/Aim-Kamera zur Laufzeit.
        /// Wird aufgerufen nachdem das PitchTarget im Visual existiert.
        /// </summary>
        public void SetAimCamFollowTarget(Transform target)
        {
            if (m_AimCam != null)
            {
                m_AimCam.Follow = target;
            }
        }

        /// <summary>
        /// Setzt die Cinemachine-Prioritäten je nach Modus.
        /// </summary>
        private void SetCameraMode(bool firstPerson)
        {
            if (firstPerson)
            {
                m_FirstPersonCam.Priority = 20;
                m_AimCam.Priority = 10;
            }
            else
            {
                m_AimCam.Priority = 20;
                m_FirstPersonCam.Priority = 10;
            }
        }
    }
}
