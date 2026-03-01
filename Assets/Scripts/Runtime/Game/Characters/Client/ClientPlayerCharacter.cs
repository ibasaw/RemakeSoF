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

        /// <summary>
        /// PelvisTarget-Transform fuer Legs-Ausrichtung basierend auf Bewegungsrichtung.
        /// Wird zur Laufzeit nach Visual-Instanziierung gesetzt.
        /// </summary>
        private Transform m_PelvisTarget;

        /// <summary>
        /// Lower Lumbar Bone-Transform (unterer Torso).
        /// Wird zur Laufzeit nach Visual-Instanziierung gesetzt.
        /// </summary>
        private Transform m_LowerLumbar;

        /// <summary>
        /// Upper Lumbar Bone-Transform (oberer Torso).
        /// Wird zur Laufzeit nach Visual-Instanziierung gesetzt.
        /// </summary>
        private Transform m_UpperLumbar;

        /// <summary>
        /// Pitch-Target-Transform fuer Lumbar LookAt-Berechnung.
        /// Wird zur Laufzeit nach Visual-Instanziierung gesetzt.
        /// </summary>
        private Transform m_PitchTarget;

        /// <summary>
        /// Gesmoothed Legs-Forward Vektor (Slerp-basiert, nie sprunghaft).
        /// </summary>
        private Vector3 m_SmoothedLegsForward = Vector3.forward;

        /// <summary>
        /// Index der letzten Bewegungsrichtung (0-7, SoF2 PM_SetMovementDir Logik).
        /// </summary>
        private int m_LastMoveDirIndex;

        [Header("Pelvis / Legs Facing")]
        [SerializeField]
        private float m_LegsYawOffsetDegrees = 90f;

        [SerializeField]
        private float[] m_IdleYawByDir = new float[8] { 112f, 45f, 68f, 68f, 112f, 180f, 180f, 90f };

        [SerializeField]
        private float m_BaseLegsRotationSmooth = 8f;

        [SerializeField]
        private float m_TorsoFollowYawInfluence = 65f;

        [Header("Lumbar Yaw Offsets")]
        [SerializeField]
        private float m_UpperLumbarYawOffset = -5f;

        [SerializeField]
        private float m_LowerLumbarYawOffset = 0f;

        [SerializeField]
        private float m_LumbarYawSmooth = 8f;

        [Header("Lumbar Pitch Offsets")]
        [SerializeField]
        private float m_UpperLumbarPitchOffset = 0f;

        [SerializeField]
        private float m_LowerLumbarPitchOffset = 0f;

        [SerializeField]
        private float m_LumbarPitchSmooth = 8f;

        [Header("Lean / Strafe")]
        [SerializeField]
        private float m_RollLeanDegrees = 25f;

        [SerializeField]
        private float m_PitchLeanDegrees = 20f;

        [SerializeField]
        private float m_LeanSmooth = 8f;

        [SerializeField]
        private float m_StrafeYawDegrees = 12f;

        [Header("Movement Direction Idle Offsets")]
        [SerializeField]
        private int[] m_MovementOffsets = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

        [SerializeField]
        private float m_MovementOffsetSmooth = 6f;

        /// <summary>
        /// Skeleton-Offset Rotation (90 Grad Y-Korrektur fuer SoF2-Modelle).
        /// </summary>
        private static readonly Quaternion s_SkeletonOffset = Quaternion.Euler(0f, 90f, 0f);

        // Gesmoothed Lean-State (x=Roll, y=Pitch)
        private Vector2 m_CurrentLeanAngles;

        // Gesmoothed Lumbar Yaw Offsets
        private float m_CurrentUpperLumbarYaw;
        private float m_CurrentLowerLumbarYaw;

        // Gesmoothed Lumbar Pitch Offsets
        private float m_CurrentUpperLumbarPitch;
        private float m_CurrentLowerLumbarPitch;

        // Gesmoothed Idle-Offset basierend auf letzter Bewegungsrichtung
        private float m_CurrentMovementIdleOffset;

        [SerializeField]
        private float m_MoveSpeed = 7.5f;

        [SerializeField]
        private float m_WalkSpeedMultiplier = 0.5f;

        [SerializeField]
        private float m_JumpForce = 5f;

        [SerializeField]
        private float m_Gravity = -15f;

        [Header("Animation Smoothing")]
        [SerializeField]
        private float m_AnimParamSmooth = 10f;

        /// <summary>
        /// Smoothed Horizontal-Input fuer Animator (Blending).
        /// </summary>
        private float m_AnimHorizontal;

        /// <summary>
        /// Smoothed Vertical-Input fuer Animator (Blending).
        /// </summary>
        private float m_AnimVertical;

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
            UpdateAnimationState();
        }

        /// <summary>
        /// LateUpdate: Pelvis-Rotation nach Animator-Evaluation anwenden.
        /// Muss in LateUpdate passieren, da der Animator in Update/LateUpdate die Bone-Rotationen setzt
        /// und unsere manuelle Korrektur sonst ueberschrieben wird.
        /// </summary>
        private void LateUpdate()
        {
            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                return;
            }

            UpdatePelvisRotation();
            UpdateLumbarRotation();
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
            Transform pelvis = FindDeepChild(visualInstance.transform, "pelvis");
            Transform lowerLumbar = FindDeepChild(visualInstance.transform, "lower_lumbar");
            Transform upperLumbar = FindDeepChild(visualInstance.transform, "upper_lumbar");
            Transform cranium = FindDeepChild(visualInstance.transform, "cranium");
            Transform modelRoot = FindDeepChild(visualInstance.transform, "model_root");
            Transform rightHandBolt = FindDeepChild(visualInstance.transform, "rhang_tag_bone");
            Transform leftHandBolt = FindDeepChild(visualInstance.transform, "lhand_tag_bone");
            Transform rightFoot = FindDeepChild(visualInstance.transform, "rtarsal");
            Transform leftFoot = FindDeepChild(visualInstance.transform, "ltarsal");


            if (yaw == null || pitch == null)
            {
                Debug.LogWarning("[ClientPlayerCharacter] Yaw oder Pitch nicht im Visual gefunden!");
                return;
            }

            // Lokale Referenz fuer Bewegungsrichtung + Body-Rotation
            m_YawTarget = yaw;
            m_PitchTarget = pitch;
            m_PelvisTarget = pelvis;
            m_LowerLumbar = lowerLumbar;
            m_UpperLumbar = upperLumbar;

            // SmoothedLegsForward initialisieren auf aktuelle Blickrichtung
            Vector3 initialForward = yaw.forward;
            initialForward.y = 0f;
            if (initialForward.sqrMagnitude > 0.001f)
            {
                m_SmoothedLegsForward = initialForward.normalized;
            }

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

            if (m_PelvisTarget != null)
            {
                Debug.Log("[ClientPlayerCharacter] Pelvis-Target gefunden für Idle-Ausrichtung");
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
                    m_NetworkedPlayerCharacter.RequestJumpTrigger();
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
        /// Berechnet den aktuellen Animation-State aus Input und CharacterController-Daten.
        /// Schreibt den State in die NetworkVariable auf NetworkedPlayerCharacter,
        /// damit Remote-Clients die Animation synchron sehen.
        /// Smoothing-Werte (Horizontal/Vertical) werden fuer natuerliches Blending geglaettet.
        /// </summary>
        private void UpdateAnimationState()
        {
            if (m_CharacterController == null || !m_CharacterController.enabled)
            {
                return;
            }

            Vector2 moveInput = m_PlayerActions.Move.ReadValue<Vector2>();
            bool isWalking = m_PlayerActions.Walk.IsPressed();

            // Input-Werte glaetten fuer fluessiges Animator-Blending
            float animT = Mathf.Clamp01(m_AnimParamSmooth * Time.deltaTime);
            m_AnimHorizontal = Mathf.Lerp(m_AnimHorizontal, moveInput.x, animT);
            m_AnimVertical = Mathf.Lerp(m_AnimVertical, moveInput.y, animT);

            // Horizontale Geschwindigkeit berechnen (ohne Y-Komponente)
            Vector3 horizontalVelocity = m_CharacterController.velocity;
            horizontalVelocity.y = 0f;
            float speed = horizontalVelocity.magnitude;
            bool isMoving = speed > 0.01f;

            NetworkAnimationState state = new()
            {

                Speed = speed,
                Horizontal = m_AnimHorizontal,
                Vertical = m_AnimVertical,
                IsMoving = isMoving,
                IsGrounded = m_CharacterController.isGrounded,
                IsWalking = isWalking

            };

            // In NetworkVariable schreiben + lokal auf Animator anwenden
            m_NetworkedPlayerCharacter.WriteAnimationState(state);
        }

        /// <summary>
        /// SoF2 PM_SetMovementDir Logik: berechnet den Direction-Index (0-7) aus Raw-Input.
        /// 0=Forward, 1=Forward-Right, 2=Right, 3=Back-Right, 4=Back, 5=Back-Left, 6=Left, 7=Forward-Left.
        /// </summary>
        private static int ComputeMovementDir(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f)
            {
                return 0;
            }

            float forwardmove = move.y;
            float rightmove = move.x;

            if (rightmove == 0 && forwardmove > 0) return 0;
            if (rightmove < 0 && forwardmove > 0) return 1;
            if (rightmove < 0 && forwardmove == 0) return 2;
            if (rightmove < 0 && forwardmove < 0) return 3;
            if (rightmove == 0 && forwardmove < 0) return 4;
            if (rightmove > 0 && forwardmove < 0) return 5;
            if (rightmove > 0 && forwardmove == 0) return 6;
            if (rightmove > 0 && forwardmove > 0) return 7;

            return 0;
        }

        /// <summary>
        /// Pelvis/Legs Rotation analog zu MyPlayerControllerCustom.LateUpdate.
        /// Verwendet smoothedLegsForward (Slerp), legsYawOffset beim Movement,
        /// idleYawByDir im Idle, und Torso-Follow wenn der Spieler stillsteht.
        /// Muss in LateUpdate aufgerufen werden (nach Animator-Pass).
        /// </summary>
        private void UpdatePelvisRotation()
        {
            if (m_PelvisTarget == null || m_YawTarget == null)
            {
                return;
            }

            // Kamera-Forward/Right auf XZ-Ebene
            Vector3 fwd = m_YawTarget.forward;
            Vector3 rgt = m_YawTarget.right;
            fwd.y = 0f;
            rgt.y = 0f;
            fwd.Normalize();
            rgt.Normalize();

            Vector2 moveInput = m_PlayerActions.Move.ReadValue<Vector2>();
            bool hasInput = moveInput.sqrMagnitude > 0.0001f;

            if (hasInput)
            {
                // Legs-Richtung aus Input berechnen (SoF2-Stil: bei Rueckwaertsbewegung spiegeln)
                float forwardComp = Mathf.Abs(moveInput.y);
                float effectiveX = (moveInput.y < 0f) ? -moveInput.x : moveInput.x;
                Vector3 inputDir = fwd * forwardComp + rgt * effectiveX;

                if (inputDir.sqrMagnitude > 0.0001f)
                {
                    Vector3 desiredFlat = inputDir;
                    desiredFlat.y = 0f;
                    float t = Mathf.Clamp01(m_BaseLegsRotationSmooth * Time.deltaTime);
                    m_SmoothedLegsForward = Vector3.Slerp(m_SmoothedLegsForward, desiredFlat.normalized, t);
                }

                // Direction-Index aus Raw-Input (SoF2 PM_SetMovementDir)
                m_LastMoveDirIndex = ComputeMovementDir(moveInput);
            }
            else
            {
                // Idle: Legs drehen sich langsam Richtung Kamera (Torso-Follow)
                float yawDelta = Vector3.SignedAngle(m_SmoothedLegsForward, fwd, Vector3.up);
                float torsoDrivenT = Mathf.Clamp01(m_TorsoFollowYawInfluence * Time.deltaTime * (Mathf.Abs(yawDelta) / 90f));
                if (torsoDrivenT > 0f)
                {
                    m_SmoothedLegsForward = Vector3.Slerp(m_SmoothedLegsForward, fwd, torsoDrivenT);
                }
            }

            // legsLook aus gesmoothtem Forward
            Quaternion legsLook = Quaternion.LookRotation(m_SmoothedLegsForward, Vector3.up);

            if (!hasInput)
            {
                // Idle: idleYawByDir Offset anwenden
                int idleDir = Mathf.Clamp(m_LastMoveDirIndex, 0, 7);
                Quaternion idleOffset = Quaternion.Euler(0f, m_IdleYawByDir[idleDir], 0f);
                m_PelvisTarget.rotation = legsLook * idleOffset;
            }
            else
            {
                // Movement: legsYawOffset anwenden (Skeleton-Korrektur)
                Quaternion moveOffset = Quaternion.Euler(0f, m_LegsYawOffsetDegrees, 0f);
                m_PelvisTarget.rotation = legsLook * moveOffset;
            }
        }

        /// <summary>
        /// Lumbar-Rotation analog zu MyPlayerControllerCustom.LateUpdate.
        /// LowerLumbar und UpperLumbar schauen zum LookAtPoint (PitchTarget forward),
        /// mit Lean (Roll/Pitch bei Bewegung), Strafe-Yaw-Twist, Lumbar-Yaw/Pitch-Offsets,
        /// und Movement-Idle-Offset fuer den oberen Torso.
        /// Muss in LateUpdate aufgerufen werden (nach Animator-Pass).
        /// </summary>
        private void UpdateLumbarRotation()
        {
            if (m_YawTarget == null)
            {
                return;
            }

            if (m_LowerLumbar == null && m_UpperLumbar == null)
            {
                return;
            }

            // LookAt-Punkt berechnen (PitchTarget forward * 100m)
            Vector3 lookAtPoint;
            if (m_PitchTarget != null)
            {
                lookAtPoint = m_PitchTarget.position + m_PitchTarget.forward * 100f;
            }
            else
            {
                lookAtPoint = m_YawTarget.position + m_YawTarget.forward * 100f;
            }

            Vector2 moveInput = m_PlayerActions.Move.ReadValue<Vector2>();
            bool hasInput = moveInput.sqrMagnitude > 0.0001f;

            // Lean-Winkel berechnen (Roll bei Seitwärtsbewegung, Pitch bei Vorwärts/Rückwärts)
            Vector2 targetLeanAngles = Vector2.zero;
            if (hasInput)
            {
                targetLeanAngles.x = -moveInput.x * m_RollLeanDegrees;
                targetLeanAngles.y = moveInput.y * m_PitchLeanDegrees;
            }

            float leanT = Mathf.Clamp01(m_LeanSmooth * Time.deltaTime);
            m_CurrentLeanAngles = Vector2.Lerp(m_CurrentLeanAngles, targetLeanAngles, leanT);

            // Lumbar Yaw Offsets smoothen
            float yawSmoothT = Mathf.Clamp01(m_LumbarYawSmooth * Time.deltaTime);
            m_CurrentUpperLumbarYaw = Mathf.Lerp(m_CurrentUpperLumbarYaw, m_UpperLumbarYawOffset, yawSmoothT);
            m_CurrentLowerLumbarYaw = Mathf.Lerp(m_CurrentLowerLumbarYaw, m_LowerLumbarYawOffset, yawSmoothT);

            // Lumbar Pitch Offsets smoothen
            float pitchSmoothT = Mathf.Clamp01(m_LumbarPitchSmooth * Time.deltaTime);
            m_CurrentUpperLumbarPitch = Mathf.Lerp(m_CurrentUpperLumbarPitch, m_UpperLumbarPitchOffset, pitchSmoothT);
            m_CurrentLowerLumbarPitch = Mathf.Lerp(m_CurrentLowerLumbarPitch, m_LowerLumbarPitchOffset, pitchSmoothT);

            // Movement-based Idle Offset berechnen (nur im Idle)
            float targetMovementOffset = 0f;
            if (!hasInput && m_LastMoveDirIndex >= 0 && m_LastMoveDirIndex < m_MovementOffsets.Length)
            {
                targetMovementOffset = m_MovementOffsets[m_LastMoveDirIndex];
            }

            float movementSmoothT = Mathf.Clamp01(m_MovementOffsetSmooth * Time.deltaTime);
            m_CurrentMovementIdleOffset = Mathf.Lerp(m_CurrentMovementIdleOffset, targetMovementOffset, movementSmoothT);

            // Lower Lumbar rotieren
            if (m_LowerLumbar != null)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - m_LowerLumbar.position, transform.up);
                float strafeYaw = moveInput.x * m_StrafeYawDegrees;
                Quaternion leanRotation = Quaternion.Euler(
                    m_CurrentLeanAngles.y + m_CurrentLowerLumbarPitch,
                    strafeYaw + m_CurrentLowerLumbarYaw,
                    m_CurrentLeanAngles.x
                );
                m_LowerLumbar.rotation = lookRotation * leanRotation * s_SkeletonOffset;
            }

            // Upper Lumbar rotieren (reduzierter Strafe-Yaw, reduzierter Lean)
            if (m_UpperLumbar != null)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookAtPoint - m_UpperLumbar.position, transform.up);
                float strafeYawUpper = moveInput.x * (m_StrafeYawDegrees * 0.6f);
                float totalYawOffset = strafeYawUpper + m_CurrentUpperLumbarYaw + m_CurrentMovementIdleOffset;
                Quaternion leanRotation = Quaternion.Euler(
                    m_CurrentLeanAngles.y * 0.7f + m_CurrentUpperLumbarPitch,
                    totalYawOffset,
                    m_CurrentLeanAngles.x * 0.7f
                );
                m_UpperLumbar.rotation = lookRotation * leanRotation * s_SkeletonOffset;
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
