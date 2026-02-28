using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
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

        [SerializeField]
        private Transform m_CameraFollow;

        [SerializeField]
        private float m_MoveSpeed = 7.5f;

        [SerializeField]
        private float m_WalkSpeedMultiplier = 0.5f;

        [SerializeField]
        private float m_JumpForce = 5f;

        [SerializeField]
        private float m_Gravity = -15f;

        [SerializeField]
        private float m_MouseSensitivity = 2f;

        /// <summary>
        /// Aktuelle vertikale Geschwindigkeit (Gravity/Jump).
        /// </summary>
        private float m_VerticalVelocity;

        /// <summary>
        /// Vertikaler Kamerawinkel.
        /// </summary>
        private float m_CameraPitch;

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

            m_NetworkedPlayerCharacter.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        private void OnDestroy()
        {
            m_NetworkedPlayerCharacter.OnNetworkSpawnHook -= OnNetworkSpawn;

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

            // Kamera-Follow setzen
            CinemachineCamera cinemachineVirtualCamera = FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineVirtualCamera != null)
            {
                cinemachineVirtualCamera.Follow = m_CameraFollow;
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

            HandleLookInput();
            HandleMovementInput();
            HandleActionInput();
        }

        /// <summary>
        /// Mausbewegung für Kamera-Rotation (lokal, sofort responsiv).
        /// </summary>
        private void HandleLookInput()
        {
            Vector2 lookDelta = m_PlayerActions.Look.ReadValue<Vector2>();

            // Horizontale Rotation auf Character
            transform.Rotate(Vector3.up, lookDelta.x * m_MouseSensitivity);

            // Vertikale Rotation auf Kamera (clamped)
            m_CameraPitch -= lookDelta.y * m_MouseSensitivity;
            m_CameraPitch = Mathf.Clamp(m_CameraPitch, -80f, 80f);

            if (m_CameraFollow != null)
            {
                m_CameraFollow.localRotation = Quaternion.Euler(m_CameraPitch, 0f, 0f);
            }
        }

        /// <summary>
        /// Client-Side Prediction: Bewegung lokal anwenden und an Server senden.
        /// </summary>
        private void HandleMovementInput()
        {
            if (m_CharacterController == null || !m_CharacterController.enabled)
            {
                return;
            }

            // Input lesen
            Vector2 moveInput = m_PlayerActions.Move.ReadValue<Vector2>();
            bool isWalking = m_PlayerActions.Walk.IsPressed();
            bool jumpPressed = m_PlayerActions.Jump.WasPressedThisFrame();

            // Bewegungsrichtung relativ zur Blickrichtung
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
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
        /// Aktiviere oder deaktiviere Inputs.
        /// </summary>
        public void SetInputsActive(bool active)
        {
            if (active)
            {
                m_PlayerActions.Enable();
            }
            else
            {
                m_PlayerActions.Disable();
            }

            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
    }
}
