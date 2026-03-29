using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tolik.RemakeSoF.Runtime.Game.Camera
{
    /// <summary>
    /// Umschalter zwischen First-Person und Third-Person Cinemachine-Kameras.
    /// Wechselt per Input-Action die Kamera-Prioritaeten.
    /// Konfiguriert die First-Person-Kamera automatisch mit HardLockToTarget
    /// und RotateWithFollowTarget fuer korrekte Positionierung/Rotation.
    /// Erzwingt sofortigen Kamera-Wechsel (Cut, kein Blend).
    /// </summary>
    public class CameraSwitcher : MonoBehaviour
    {
        [SerializeField]
        private CinemachineCamera m_AimCam;

        [SerializeField]
        private CinemachineCamera m_FirstPersonCam;

        /// <summary>
        /// CinemachineBrain auf der Main Camera.
        /// Wird benoetigt um DefaultBlend auf Cut zu setzen (sofortiger Wechsel).
        /// </summary>
        [SerializeField]
        private CinemachineBrain m_Brain;

        [SerializeField]
        private bool m_StartFirstPerson = true;

        /// <summary>
        /// Aktueller Kameramodus.
        /// </summary>
        private bool m_IsFirstPerson;

        /// <summary>
        /// Event: wird bei jedem Kamera-Modus-Wechsel gefeuert.
        /// Parameter: true = First Person, false = Third Person.
        /// </summary>
        public event Action<bool> OnCameraModeChanged;

        /// <summary>
        /// Aktueller Kameramodus (true = First Person).
        /// </summary>
        public bool IsFirstPerson => m_IsFirstPerson;

        /// <summary>
        /// Input-Actions-Instanz fuer Kamera-Umschaltung.
        /// </summary>
        private AvatarActions m_InputActions;

        private void Awake()
        {
            m_InputActions = new AvatarActions();

            // Auto-find CinemachineBrain falls nicht im Inspector gesetzt
            if (m_Brain == null)
            {
                // Versuch 1: Camera.main
                if (UnityEngine.Camera.main != null)
                {
                    m_Brain = UnityEngine.Camera.main.GetComponent<CinemachineBrain>();
                }

                // Versuch 2: Szenen-Suche
                if (m_Brain == null)
                {
                    m_Brain = FindAnyObjectByType<CinemachineBrain>();
                }

                if (m_Brain == null)
                {
                    Debug.LogWarning("[CameraSwitcher] CinemachineBrain nicht gefunden! Instant-Blend deaktiviert.");
                }
                else
                {
                    Debug.Log($"[CameraSwitcher] CinemachineBrain auto-found auf: {m_Brain.gameObject.name}");
                }
            }

            ConfigureFirstPersonPipeline();
            ConfigureInstantBlend();

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
        /// Konfiguriert die First-Person CinemachineCamera Pipeline:
        /// - Entfernt ALLE bestehenden Pipeline-Komponenten (Body, Aim, Noise, etc.)
        /// - Fuegt CinemachineHardLockToTarget hinzu (Position = exakt am Follow-Target)
        /// - Fuegt CinemachineRotateWithFollowTarget hinzu (Rotation = Follow-Target-Rotation)
        /// Wichtig: Pipeline-Cache verwendet nur die ERSTE Komponente pro Stage.
        /// Alte Aim-Komponenten (z.B. RotationComposer) wuerden unsere RotateWithFollowTarget verdecken.
        /// </summary>
        private void ConfigureFirstPersonPipeline()
        {
            if (m_FirstPersonCam == null)
            {
                return;
            }

            // ALLE bestehenden Pipeline-Komponenten entfernen.
            // CinemachineCamera cached nur die erste Komponente pro Stage —
            // wenn eine alte Aim-Komponente existiert, wird unsere RotateWithFollowTarget ignoriert.
            CinemachineComponentBase[] existingComponents = m_FirstPersonCam.GetComponents<CinemachineComponentBase>();
            for (int i = existingComponents.Length - 1; i >= 0; i--)
            {
                Debug.Log($"[CameraSwitcher] Entferne alte Pipeline-Komponente: {existingComponents[i].GetType().Name} (Stage={existingComponents[i].Stage})");
                DestroyImmediate(existingComponents[i]);
            }

            // Body: HardLockToTarget — Kamera-Position = exakt am Follow-Target (Pitch-Bone)
            CinemachineHardLockToTarget hardLock = m_FirstPersonCam.gameObject.AddComponent<CinemachineHardLockToTarget>();
            hardLock.Damping = 0f;

            // Aim: RotateWithFollowTarget — Kamera-Rotation = Follow-Target-Rotation (Pitch-Bone)
            CinemachineRotateWithFollowTarget rotateFollow = m_FirstPersonCam.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
            rotateFollow.Damping = 0f;

            Debug.Log("[CameraSwitcher] First-Person Pipeline konfiguriert (HardLock + RotateWithFollow)");
        }

        /// <summary>
        /// Setzt den CinemachineBrain DefaultBlend auf Cut (sofortiger Wechsel ohne Blend).
        /// CinemachineBrain Default ist EaseInOut 2s — viel zu langsam fuer FPS-Umschaltung.
        /// </summary>
        private void ConfigureInstantBlend()
        {
            if (m_Brain != null)
            {
                m_Brain.DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Styles.Cut, 0f);
            }
        }

        /// <summary>
        /// Setzt die Cinemachine-Prioritaeten je nach Modus.
        /// Erzwingt auch erneut den Instant-Blend (Sicherheitsnetz falls Brain spaet gefunden).
        /// </summary>
        private void SetCameraMode(bool firstPerson)
        {
            // Sicherheitsnetz: Brain spaet suchen falls noch null
            if (m_Brain == null)
            {
                if (UnityEngine.Camera.main != null)
                {
                    m_Brain = UnityEngine.Camera.main.GetComponent<CinemachineBrain>();
                }
                if (m_Brain == null)
                {
                    m_Brain = FindAnyObjectByType<CinemachineBrain>();
                }
            }

            // Instant-Blend erzwingen (jeder Switch)
            if (m_Brain != null)
            {
                m_Brain.DefaultBlend = new CinemachineBlendDefinition(
                    CinemachineBlendDefinition.Styles.Cut, 0f);
            }

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

            OnCameraModeChanged?.Invoke(firstPerson);
        }
    }
}
