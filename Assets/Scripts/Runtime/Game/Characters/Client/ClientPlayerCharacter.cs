using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Tolik.RemakeSoF.Runtime.Game.Camera;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Owner-Client-Controller für Player-Character.
    /// Client-Side Prediction: Input wird lokal angewendet (responsiv),
    /// dann an Server gesendet zur Validierung.
    /// Auf Remote-Clients: nur CapsuleCollider für Kollision.
    /// </summary>
    [RequireComponent(typeof(NetworkedPlayerCharacter))]
    public class ClientPlayerCharacter : MonoBehaviour
    {
        [SerializeField]
        private NetworkedPlayerCharacter m_NetworkedPlayerCharacter;

        [SerializeField]
        private CharacterController m_CharacterController;

        [SerializeField]
        private CapsuleCollider m_CapsuleCollider;

        /// <summary>
        /// Root-GameObject des Kamera-Setups (CameraManager, Main Camera, etc.).
        /// Wird bei Remote-Clients komplett deaktiviert, damit keine doppelten
        /// Kameras/AudioListeners existieren.
        /// </summary>
        [SerializeField]
        private GameObject m_CameraRoot;

        /// <summary>
        /// AimCameraController auf dem Player-Prefab. Steuert Kamera-Rotation (Yaw/Pitch).
        /// Wird nur für den Owner aktiviert.
        /// </summary>
        [SerializeField]
        private AimCameraController m_AimCameraController;

        /// <summary>
        /// CameraSwitcher auf dem Player-Prefab. Umschalter zwischen First-Person und
        /// Third-Person Kamera. Wird nur für den Owner aktiviert.
        /// </summary>
        [SerializeField]
        private CameraSwitcher m_CameraSwitcher;

        /// <summary>
        /// SkinHandler-Referenz für das OnVisualInstantiated-Event.
        /// Wird benötigt um Yaw/Pitch/CameraTarget nach Visual-Instanziierung zu finden.
        /// </summary>
        [SerializeField]
        private ClientCharacterSkinHandler m_SkinHandler;

        /// <summary>
        /// YawTarget-Transform. Wird zur Laufzeit nach Visual-Instanziierung gesetzt.
        /// Bestimmt die Blickrichtung (Yaw) und Bewegungsrichtung.
        /// </summary>
        private Transform m_YawTarget;

        [SerializeField]
        private float m_MoveSpeed = 7.5f;

        [SerializeField]
        private float m_WalkSpeedMultiplier = 0.5f;

        [SerializeField]
        private float m_JumpForce = 5f;

        [SerializeField]
        private float m_Gravity = -15f;

        /// <summary>
        /// Aktuelle vertikale Geschwindigkeit (Gravity/Jump).
        /// </summary>
        private float m_VerticalVelocity;

        /// <summary>
        /// Auto-generierte Input Actions (AvatarActions.inputactions).
        /// </summary>
        private AvatarActions m_AvatarActions;

        /// <summary>
        /// Gecachter Zugriff auf die Player-Action-Map.
        /// </summary>
        private AvatarActions.PlayerActions m_PlayerActions;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // AvatarActions instanziieren (noch nicht aktiviert)
            m_AvatarActions = new AvatarActions();
            m_PlayerActions = m_AvatarActions.Player;

            // Alles deaktiviert bis OnNetworkSpawn entscheidet, ob Owner oder Remote
            m_CapsuleCollider.enabled = false;
            m_CharacterController.enabled = false;

            // Kamera-Setup deaktiviert bis Owner-Entscheidung
            m_CameraRoot.SetActive(false);
            m_AimCameraController.enabled = false;
            m_CameraSwitcher.enabled = false;

            m_NetworkedPlayerCharacter.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        private void OnDestroy()
        {
            m_NetworkedPlayerCharacter.OnNetworkSpawnHook -= OnNetworkSpawn;

            // Visual-Event abmelden
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated -= OnVisualInstantiated;
            }

            // TogglePauseMenu Callback entfernen
            m_PlayerActions.TogglePauseMenu.performed -= OnMenuToggle;

            // Input Actions aufräumen
            m_AvatarActions?.Dispose();
            m_AvatarActions = null;
        }

        /// <summary>
        /// Wird aufgerufen wenn der NetworkObject gespawnt wird.
        /// Owner: aktiviert Input + CharacterController.
        /// Remote: aktiviert CapsuleCollider für Physik-Kollision.
        /// </summary>
        private void OnNetworkSpawn()
        {
            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                // Remote-Client: nur CapsuleCollider für Kollision
                enabled = false;
                m_CapsuleCollider.enabled = true;
                return;
            }

            // Owner: Player Action Map aktivieren
            m_PlayerActions.Enable();

            // TogglePauseMenu per Callback statt PlayerInput-SendMessage
            m_PlayerActions.TogglePauseMenu.performed += OnMenuToggle;

            // CharacterController NACH Server-Position aktivieren
            // (sonst überschreibt CC die synchronisierte Position)
            m_CharacterController.enabled = true;

            // Kamera-Setup nur für Owner aktivieren (verhindert doppelte Camera/AudioListener)
            m_CameraRoot.SetActive(true);
            m_AimCameraController.enabled = true;
            m_CameraSwitcher.enabled = true;

            // Auf Visual-Instanziierung lauschen (Yaw/Pitch/CameraTarget werden dort gefunden)
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated += OnVisualInstantiated;
            }

            // GameModel updaten
            GameApplication.Instance.Model.PlayerCharacter = this;

            Debug.Log("[ClientPlayerCharacter] Owner: Input + Prediction aktiv");
        }

        private void Update()
        {
            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                return;
            }

            SyncCharacterRotation();
            HandleMovementInput();
            HandleActionInput();
        }

        /// <summary>
        /// Callback wenn das Visual-Prefab instanziiert wurde.
        /// Sucht Yaw, Pitch und CameraTarget per Name und verdrahtet Kamera-Controller.
        /// </summary>
        private void OnVisualInstantiated(GameObject visualInstance)
        {
            Transform yaw = FindDeepChild(visualInstance.transform, "Yaw");
            Transform pitch = FindDeepChild(visualInstance.transform, "Pitch");
            Transform cameraTarget = FindDeepChild(visualInstance.transform, "CameraTarget");

            if (yaw == null || pitch == null)
            {
                Debug.LogWarning("[ClientPlayerCharacter] Yaw oder Pitch nicht im Visual gefunden!");
                return;
            }

            // Lokale Referenz für Bewegungsrichtung + Body-Rotation
            m_YawTarget = yaw;

            // AimCameraController verdrahten
            m_AimCameraController.SetTargets(yaw, pitch);

            // CameraSwitcher: Follow-Targets setzen
            m_CameraSwitcher.SetAimCamFollowTarget(pitch);

            if (cameraTarget != null)
            {
                m_CameraSwitcher.SetFirstPersonFollowTarget(cameraTarget);
            }
            else
            {
                Debug.LogWarning("[ClientPlayerCharacter] CameraTarget nicht im Visual gefunden, First-Person-Kamera hat kein Follow-Target.");
            }

            Debug.Log("[ClientPlayerCharacter] Kamera-Targets verdrahtet (Yaw/Pitch/CameraTarget)");
        }

        /// <summary>
        /// Rekursive Tiefensuche nach einem Child-Transform mit dem angegebenen Namen.
        /// </summary>
        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Synchronisiert die Character-Body-Rotation mit dem YawTarget.
        /// Damit dreht sich das Character-Model in Blickrichtung (nur Yaw).
        /// Nach dem Setzen der Body-Rotation wird die YawTarget-Weltrotation
        /// wiederhergestellt, damit die Cinemachine-Kamera-Hierarchie konsistent bleibt.
        /// </summary>
        private void SyncCharacterRotation()
        {
            if (m_YawTarget == null)
            {
                return;
            }

            float yaw = m_YawTarget.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // YawTarget-Weltrotation wiederherstellen (Parent-Rotation hat sich geändert)
            m_YawTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// Client-Side Prediction: Bewegung lokal anwenden und an Server senden.
        /// </summary>
        private void HandleMovementInput()
        {
            if (m_CharacterController == null || !m_CharacterController.enabled || m_YawTarget == null)
            {
                return;
            }

            // Input lesen
            Vector2 moveInput = m_PlayerActions.Move.ReadValue<Vector2>();
            bool isWalking = m_PlayerActions.Walk.IsPressed();
            bool jumpPressed = m_PlayerActions.Jump.WasPressedThisFrame();

            // Bewegungsrichtung relativ zum YawTarget (Kamera-Blickrichtung)
            Vector3 moveDirection = m_YawTarget.right * moveInput.x + m_YawTarget.forward * moveInput.y;
            float speed = m_MoveSpeed * (isWalking ? m_WalkSpeedMultiplier : 1f);

            // Gravity
            if (m_CharacterController.isGrounded)
            {
                m_VerticalVelocity = -2f; // Leicht negativ, damit isGrounded stabil bleibt

                if (jumpPressed)
                {
                    m_VerticalVelocity = m_JumpForce;
                }
            }
            else
            {
                m_VerticalVelocity += m_Gravity * Time.deltaTime;
            }

            // Prediction: Bewegung LOKAL anwenden (sofort responsiv)
            Vector3 movement = (moveDirection * speed + Vector3.up * m_VerticalVelocity) * Time.deltaTime;
            m_CharacterController.Move(movement);

            // An Server senden: Predicted Position + deltaTime zur Validierung
            m_NetworkedPlayerCharacter.SendPredictedMovement(transform.position, transform.rotation, Time.deltaTime);
        }

        /// <summary>
        /// Aktionen (Attack, etc.) an Server senden.
        /// </summary>
        private void HandleActionInput()
        {
            if (m_PlayerActions.Attack.WasPressedThisFrame())
            {
                m_NetworkedPlayerCharacter.RequestAttack();
            }
        }

        /// <summary>
        /// Toggle Menü-Sichtbarkeit (AvatarActions TogglePauseMenu Callback).
        /// </summary>
        private void OnMenuToggle(InputAction.CallbackContext context)
        {
            GameApplication.Instance.Broadcast(new MenuToggleEvent());
        }

        /// <summary>
        /// Aktiviere oder deaktiviere Gameplay-Inputs und Kamera-Controller.
        /// Wird vom Menü-System aufgerufen (Pause/Resume).
        /// TogglePauseMenu bleibt immer aktiv, damit ESC auch im Menü funktioniert.
        /// </summary>
        public void SetInputsActive(bool active)
        {
            if (active)
            {
                m_PlayerActions.Enable();
            }
            else
            {
                // Alle Actions deaktivieren, dann TogglePauseMenu gezielt re-aktivieren
                m_PlayerActions.Disable();
                m_PlayerActions.TogglePauseMenu.Enable();
            }

            // Kamera-Controller ein-/ausschalten (verhindert Mausbewegung im Menü)
            if (m_AimCameraController != null)
            {
                m_AimCameraController.enabled = active;
            }

            if (m_CameraSwitcher != null)
            {
                m_CameraSwitcher.enabled = active;
            }

            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
    }
}
