using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Tolik.RemakeSoF.Runtime.Game.Camera;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
/**
    * Owner-Client-Controller für Player-Character.
    * SoF2/Quake3-Style manuelle Physik: Velocity-basierte Bewegung mit CapsuleCasts.
    * Client-Side Prediction: Physik wird lokal angewendet (responsiv),
    * dann an Server gesendet zur Validierung.
    * Auf Remote-Clients: nur CapsuleCollider fuer Kollision.
    Ja, das ist jetzt sehr nah am Original:

Physik auf Framerate — Q3/SoF2 ließ PM_Move pro Client-Frame laufen (nicht auf fixem Tick). 125fps = 125 Physik-Iterationen/s. Genau das hast du jetzt.
PM_StepSlideMove — 4-Bump Collision mit ClipVelocity, Step-Up, Slide — direkt aus bg_pmove.c
PM_Friction / PM_Accelerate — identische Formel: control * friction * dt, accel * dt * wishspeed
PM_WalkMove / PM_AirMove — Trennung Boden/Luft mit unterschiedlichen Accel-Werten (6 vs 1)
PM_CmdScale — Input-Normalisierung wie PM_CmdScale in Q3
Sofortige Jump-Velocity — velocity.y = jumpVelocity (kein Force, kein AddForce)
Manuelle Gravity — velocity.y -= gravity * dt statt Rigidbody
CapsuleCast statt CharacterController — näher an Q3's Trace-System als Unitys eingebaute Physik
Die Werte (pm_maxspeed=28, pm_gravity=80, pm_friction=6, pm_accelerate=6, pm_airaccelerate=1, jumpvel=27) sind SoF2-Defaults. Einziger Unterschied zu purem Q3: du hast zusätzliche Slope-Friction und die SoF2-spezifischen Step-Up Limits (pm_maxstep=1.8, pm_maxbarrier=3.2), was korrekt ist — SoF2 hat das gegenüber Q3 erweitert.
Was fehlt für 100% Authentizität wäre Strafe-Jumping / Air-Control (Q3 pm_airaccelerate erlaubt Speed-Gain durch Richtungswechsel in der Luft). Das funktioniert bei dir automatisch, weil PM_AirMove mit pm_airaccelerate=1 und der Q3-Accelerate-Formel arbeitet — die erlaubt den klassischen Speed-Gain Bug by design.
*/

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Owner-Client-Controller für Player-Character.
    /// SoF2/Quake3-Style manuelle Physik: Velocity-basierte Bewegung mit CapsuleCasts.
    /// Client-Side Prediction: Physik wird lokal angewendet (responsiv),
    /// dann an Server gesendet zur Validierung.
    /// Kein Player-Player Collision client-seitig (wie Q3/SoF2).
    /// </summary>
    [RequireComponent(typeof(NetworkedPlayerCharacter))]
    public class ClientPlayerCharacter : MonoBehaviour
    {
        // ===== Serialized References =====

        [SerializeField]
        private NetworkedPlayerCharacter m_NetworkedPlayerCharacter;

        /// <summary>
        /// SoF2 Collider-System: berechnet Capsule-Groesse aus Bones (Cranium, Fuesse).
        /// Stellt Capsule-Parameter fuer CapsuleCasts bereit.
        /// </summary>
        [SerializeField]
        private ClientColliderSystem m_ColliderSystem;

        /// <summary>
        /// Root-GameObject des Kamera-Setups (CameraManager, Main Camera, etc.).
        /// Wird bei Remote-Clients komplett deaktiviert, damit keine doppelten
        /// Kameras/AudioListeners existieren.
        /// </summary>
        [SerializeField]
        private GameObject m_CameraRoot;

        /// <summary>
        /// AimCameraController auf dem Player-Prefab. Steuert Kamera-Rotation (Yaw/Pitch).
        /// Wird nur fuer den Owner aktiviert.
        /// </summary>
        [SerializeField]
        private AimCameraController m_AimCameraController;

        /// <summary>
        /// CameraSwitcher auf dem Player-Prefab. Umschalter zwischen First-Person und
        /// Third-Person Kamera. Wird nur fuer den Owner aktiviert.
        /// </summary>
        [SerializeField]
        private CameraSwitcher m_CameraSwitcher;

        /// <summary>
        /// SkinHandler-Referenz fuer das OnVisualInstantiated-Event.
        /// Wird benoetigt um Yaw/Pitch/CameraTarget nach Visual-Instanziierung zu finden.
        /// </summary>
        [SerializeField]
        private ClientCharacterSkinHandler m_SkinHandler;

        // ===== SoF2 Physics Constants =====

        [Header("SoF2 Physics Constants")]
        [SerializeField]
        private float m_PmAccelerate = 6.0f;

        [SerializeField]
        private float m_PmAirAccelerate = 1.0f;

        [SerializeField]
        private float m_PmFriction = 6.0f;

        [SerializeField]
        private float m_PmStopSpeed = 10.0f;

        /// <summary>Maximum Wish-Speed / g_speed (SoF2 Standard: 28).</summary>
        [SerializeField]
        private float m_PmMaxSpeed = 28.0f;

        /// <summary>Gravitation in Units/s² (SoF2 Standard: 80).</summary>
        [SerializeField]
        private float m_PmGravity = 80.0f;

        /// <summary>Max horizontale Velocity in der Luft.</summary>
        [SerializeField]
        private float m_PhysMaxVelocity = 32f;

        /// <summary>Max horizontale Velocity am Boden.</summary>
        [SerializeField]
        private float m_PhysMaxWalkVelocity = 32f;

        /// <summary>Sofortige Y-Velocity beim Sprung (SoF2 phys_jumpvel).</summary>
        [SerializeField]
        private float m_JumpVelocity = 27.0f;

        [Header("Movement Limits")]
        [SerializeField]
        private float m_PmMaxSteepness = 0.7f; // Entspricht ca. 45 Grad, wie in SoF2 (pm_maxsteepness = 0.7)

        [SerializeField]
        private float m_PmMaxStep = 1.8f;

        [SerializeField]
        private float m_PmStepSize = 1.8f;

        [SerializeField]
        private float m_PmMaxBarrier = 3.2f;

        [SerializeField]
        private float m_PmDuckScale = 0.25f; // SoF2 duckscale: 0.25 = 25%, sollte die Speed auf 25% reduziert werden.

        [SerializeField]
        private float m_JumpDebounceAfterMs = 0.25f;

        [SerializeField]
        private float m_GroundGracePeriod = 0.15f;

        [SerializeField]
        private LayerMask m_GroundMask = ~0;

        // ===== Physics Constants =====

        /// <summary>Overclip-Konstante fuer PM_ClipVelocity (SoF2-Wert).</summary>
        private const float OVERCLIP = 1.001f;

        /// <summary>Skin-Width: minimaler Abstand zu Oberflaechen.</summary>
        private const float SKIN_WIDTH = 0.01f;

        // ===== Bone / Visual References =====

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

        // ===== Movement State =====

        /// <summary>Aktuelle Velocity (Welt-Raum, XYZ).</summary>
        private Vector3 m_Velocity;

        /// <summary>Auf dem Boden (nach letztem FixedUpdate)?</summary>
        private bool m_IsGrounded;

        /// <summary>Springt gerade (bis zur naechsten Landung)?</summary>
        private bool m_IsJumping;

        /// <summary>Geduckt?</summary>
        private bool m_IsCrouching;

        /// <summary>War im vorherigen FixedUpdate grounded?</summary>
        private bool m_WasGroundedPrev;

        /// <summary>Jump-Debounce aktiv (nach Landung).</summary>
        private bool m_IsDebounceActive;

        /// <summary>Jump-Debounce Timer (countdown in Update).</summary>
        private float m_JumpDebounce;

        /// <summary>Zeitpunkt des letzten Sprungs (Time.time).</summary>
        private float m_LastJumpTime;

        /// <summary>Zeitpunkt der letzten Ground-Detection.</summary>
        private float m_LastGroundedTime;

        /// <summary>Zeitpunkt des letzten Step-Up.</summary>
        private float m_LastStepUpTime;

        /// <summary>Gespeicherter Ground-Hit fuer Slope-Berechnungen.</summary>
        private RaycastHit m_LastGroundHit;

        /// <summary>Landing-Event bereits gefeuert (verhindert Doppel-Trigger).</summary>
        private bool m_LandedThisGround;

        /// <summary>
        /// Gecachtes deltaTime fuer die aktuelle Physik-Iteration.
        /// Wird zu Beginn von RunPhysicsStep() gesetzt, damit alle Physik-Methoden
        /// denselben konsistenten dt-Wert nutzen.
        /// </summary>
        private float m_PhysicsDeltaTime;

        // ===== Input State =====

        /// <summary>Buffered MoveInput (gelesen in Update, genutzt in Physik-Pipeline + LateUpdate).</summary>
        private Vector2 m_MoveInput;

        /// <summary>Walk-Taste gedrueckt (Shift).</summary>
        private bool m_IsWalkingPressed;

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

            // Kamera-Setup deaktiviert bis Owner-Entscheidung
            m_CameraRoot.SetActive(false);
            m_AimCameraController.enabled = false;
            m_CameraSwitcher.enabled = false;

            m_NetworkedPlayerCharacter.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        private void OnDestroy()
        {
            m_NetworkedPlayerCharacter.OnNetworkSpawnHook -= OnNetworkSpawn;

            // Jump-Callback abmelden
            m_PlayerActions.Jump.performed -= OnJumpPerformed;

            // Visual-Event abmelden
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated -= OnVisualInstantiated;
            }

            // TogglePauseMenu Callback entfernen
            m_PlayerActions.TogglePauseMenu.performed -= OnMenuToggle;

            // Input Actions aufraeumen
            m_AvatarActions?.Dispose();
            m_AvatarActions = null;
        }

        /// <summary>
        /// Wird aufgerufen wenn der NetworkObject gespawnt wird.
        /// Owner: aktiviert Input + SoF2-Physik.
        /// Remote: deaktiviert (kein client-seitiges Player-Player Collision wie Q3/SoF2).
        /// </summary>
        private void OnNetworkSpawn()
        {
            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                // Remote-Client: kein Collision noetig (Q3/SoF2-Style)
                enabled = false;
                return;
            }

            // Owner: Player Action Map aktivieren
            m_PlayerActions.Enable();

            // Jump per Callback (zuverlaessiger als WasPressedThisFrame in FixedUpdate)
            m_PlayerActions.Jump.performed += OnJumpPerformed;

            // TogglePauseMenu per Callback statt PlayerInput-SendMessage
            m_PlayerActions.TogglePauseMenu.performed += OnMenuToggle;

            // Kamera-Setup nur fuer Owner aktivieren (verhindert doppelte Camera/AudioListener)
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

            Debug.Log("[ClientPlayerCharacter] Owner: SoF2-Physik + Prediction aktiv");
        }

        /// <summary>
        /// Input-Callback fuer Jump (performed). Setzt isJumping + Velocity sofort.
        /// Exaktes Verhalten wie MyPlayerControllerCustom.TryJump.
        /// </summary>
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            TryJump();
        }

        private void Update()
        {
            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                return;
            }

            // Input lesen (wird in Physik-Pipeline + LateUpdate konsumiert)
            m_MoveInput = m_PlayerActions.Move.ReadValue<Vector2>();
            m_IsWalkingPressed = m_PlayerActions.Walk.IsPressed();

            // Jump-Debounce Timer (laeuft in Update wie im Original)
            if (m_IsDebounceActive && m_JumpDebounce > 0f)
            {
                m_JumpDebounce -= Time.deltaTime;
                if (m_JumpDebounce <= 0f)
                {
                    m_IsDebounceActive = false;
                }
            }

            SyncCharacterRotation();
            HandleActionInput();

            // SoF2-Physik-Pipeline in Update (wie im Original: Framerate-gebunden).
            // Garantiert stutter-freie Bewegung, da transform.position jeden Frame aktualisiert wird.
            RunPhysicsStep();

            UpdateAnimationState();
        }

        /// <summary>
        /// Komplette SoF2-Physik-Pipeline pro Frame.
        /// Reihenfolge exakt wie MyPlayerControllerCustom:
        /// Gravity → Move → GroundCheck → WalkMove/AirMove → Landing → Server-Sync.
        /// Laeuft in Update statt FixedUpdate fuer stutter-freie Bewegung
        /// (SoF2/Q3 lief Physik ebenfalls auf Client-Framerate).
        /// </summary>
        private void RunPhysicsStep()
        {
            // Collider-System muss initialisiert sein
            if (m_ColliderSystem == null || m_ColliderSystem.GetCurrentCapsuleHeight() <= 0f)
            {
                return;
            }

            // deltaTime fuer diesen Frame cachen (alle Physik-Methoden nutzen m_PhysicsDeltaTime)
            m_PhysicsDeltaTime = Time.deltaTime;

            // Vorherigen Ground-State speichern
            bool wasGrounded = m_WasGroundedPrev;

            // 1. Gravity anwenden
            ApplyGravity();

            // 2. Character bewegen (PM_StepSlideMove — Collision + Sliding)
            MoveCharacter();

            // 3. Ground-Check (inklusive Slope + Grace-Period Fallbacks)
            CheckGroundedState(wasGrounded);

            // Visual-Debug: Grounded-State an ColliderSystem uebergeben
            if (m_ColliderSystem != null)
            {
                m_ColliderSystem.SetGroundedState(m_IsGrounded);
            }

            // 4. Ground-State Updates
            if (m_IsGrounded)
            {
                m_LastGroundedTime = Time.time;
            }

            // 5. Edge Detection: gerade gelandet?
            bool justLanded = !wasGrounded && m_IsGrounded;
            m_WasGroundedPrev = m_IsGrounded;

            // 6. Friction + Acceleration (setzt Velocity fuer naechsten Frame)
            if (m_IsGrounded)
            {
                PM_WalkMove();
            }
            else
            {
                PM_AirMove();
            }

            // 7. Landing-Events
            HandleLandingEvents(justLanded);

            // 8. Predicted Position an Server senden
            m_NetworkedPlayerCharacter.SendPredictedMovement(
                transform.position, transform.rotation, Time.deltaTime);
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

            // Collider-System initialisieren: Capsule-Groesse aus Bones berechnen
            if (m_ColliderSystem != null)
            {
                m_ColliderSystem.CalculateAutoCapsuleSize(cranium, pelvis, leftHandBolt, rightHandBolt, leftFoot, rightFoot);


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
        /// </summary>
        private void SyncCharacterRotation()
        {
            if (m_YawTarget == null)
            {
                return;
            }

            float yaw = m_YawTarget.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // YawTarget-Weltrotation wiederherstellen (Parent-Rotation hat sich geaendert)
            m_YawTarget.rotation = Quaternion.Euler(0f, yaw, 0f);
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
        /// Berechnet den aktuellen Animation-State aus Velocity und Ground-State.
        /// Schreibt den State in die NetworkVariable auf NetworkedPlayerCharacter,
        /// damit Remote-Clients die Animation synchron sehen.
        /// </summary>
        private void UpdateAnimationState()
        {
            Vector2 moveInput = m_MoveInput;
            bool isWalking = m_IsWalkingPressed;

            // Input-Werte glaetten fuer fluessiges Animator-Blending
            float animT = Mathf.Clamp01(m_AnimParamSmooth * Time.deltaTime);
            m_AnimHorizontal = Mathf.Lerp(m_AnimHorizontal, moveInput.x, animT);
            m_AnimVertical = Mathf.Lerp(m_AnimVertical, moveInput.y, animT);

            // Horizontale Geschwindigkeit berechnen (ohne Y-Komponente)
            Vector3 horizontalVelocity = new(m_Velocity.x, 0f, m_Velocity.z);
            float speed = horizontalVelocity.magnitude;
            bool isMoving = speed > 0.01f;

            NetworkAnimationState state = new()
            {
                Speed = speed,
                Horizontal = m_AnimHorizontal,
                Vertical = m_AnimVertical,
                IsMoving = isMoving,
                IsGrounded = m_IsGrounded,
                IsWalking = isWalking,
                IsCrouching = m_IsCrouching
            };

            // In NetworkVariable schreiben + lokal auf Animator anwenden
            m_NetworkedPlayerCharacter.WriteAnimationState(state);
        }

        // ===================================================================
        // SoF2 Physics Engine — exakt portiert von MyPlayerControllerCustom
        // ===================================================================

        /// <summary>
        /// Gravity anwenden (SoF2 ApplyGravity).
        /// Nur in der Luft: velocity.y -= pm_gravity * dt.
        /// Am Boden: negative Y-Velocity auf 0 setzen.
        /// </summary>
        private void ApplyGravity()
        {
            if (!m_IsGrounded)
            {
                m_Velocity.y -= m_PmGravity * m_PhysicsDeltaTime;
                m_LandedThisGround = false;
            }
            else
            {
                if (m_Velocity.y < 0f)
                {
                    m_Velocity.y = 0f;
                }
            }
        }

        /// <summary>
        /// Character bewegen (ruft PM_StepSlideMove auf).
        /// </summary>
        private void MoveCharacter()
        {
            PM_StepSlideMove();
        }

        /// <summary>
        /// SoF2 PM_Friction — Bodenreibung (Luft: kein Drop).
        /// Exakter Port aus bg_pmove.c mit zusaetzlicher Slope-Friction.
        /// </summary>
        private void PM_Friction()
        {
            Vector3 vec = m_Velocity;

            if (m_IsGrounded)
            {
                vec.y = 0f;
            }

            float speed = vec.magnitude;
            float drop = 0f;

            if (speed < 1f)
            {
                m_Velocity.x = 0f;
                m_Velocity.z = 0f;
                return;
            }

            if (m_IsGrounded)
            {
                float control = speed < m_PmStopSpeed ? m_PmStopSpeed : speed;
                drop += control * m_PmFriction * m_PhysicsDeltaTime;

                // Zusaetzliche Slope-Friction (Custom, nicht im SoF2-Original)
                if (m_LastGroundHit.collider != null)
                {
                    float slopeDot = Vector3.Dot(m_LastGroundHit.normal, Vector3.up);
                    if (slopeDot < 0.9f)
                    {
                        drop += control * m_PmFriction * 1.0f * m_PhysicsDeltaTime;
                    }

                    if (slopeDot < 0.7f)
                    {
                        drop += control * m_PmFriction * 1.5f * m_PhysicsDeltaTime;
                    }
                }
            }

            float newspeed = speed - drop;
            if (newspeed < 0f)
            {
                newspeed = 0f;
            }

            if (newspeed != speed)
            {
                newspeed /= speed;
                m_Velocity.x *= newspeed;
                m_Velocity.z *= newspeed;
                if (m_IsGrounded)
                {
                    m_Velocity.y *= newspeed;
                }
            }
        }

        /// <summary>
        /// SoF2 PM_Accelerate — Q2/Q3 Stil.
        /// Projiziert aktuelle Velocity auf wishdir, addiert beschleunigte Differenz.
        /// Ermoeglicht Strafe-Jumping (Perpendicular-Velocity bleibt unberuehrt).
        /// </summary>
        private void PM_Accelerate(Vector3 wishdir, float wishspeed, float accel)
        {
            float currentspeed = Vector3.Dot(m_Velocity, wishdir);
            float addspeed = wishspeed - currentspeed;

            if (addspeed <= 0f)
            {
                return;
            }

            float accelspeed = accel * m_PhysicsDeltaTime * wishspeed;
            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            m_Velocity.x += accelspeed * wishdir.x;
            m_Velocity.y += accelspeed * wishdir.y;
            m_Velocity.z += accelspeed * wishdir.z;
        }

        /// <summary>
        /// SoF2 PM_ClipVelocity — entfernt Velocity-Komponente die in eine Oberflaeche zeigt.
        /// OVERCLIP verhindert Float-Precision Creep in Oberflaechen.
        /// </summary>
        private static void PM_ClipVelocity(Vector3 input, Vector3 normal, out Vector3 output, float overbounce)
        {
            float backoff = Vector3.Dot(input, normal);

            if (backoff < 0f)
            {
                backoff *= overbounce;
            }
            else
            {
                backoff /= overbounce;
            }

            output = new Vector3(
                input.x - normal.x * backoff,
                input.y - normal.y * backoff,
                input.z - normal.z * backoff);
        }

        /// <summary>
        /// SoF2 Velocity-Limits: horizontale Speed auf phys_maxwalkvelocity (Boden)
        /// bzw. phys_maxvelocity (Luft) begrenzen. Y bleibt unberuehrt.
        /// </summary>
        private void ApplyVelocityLimits()
        {
            Vector3 horizontalVel = new(m_Velocity.x, 0f, m_Velocity.z);
            float horizontalSpeed = horizontalVel.magnitude;

            float maxVelocity = m_IsGrounded ? m_PhysMaxWalkVelocity : m_PhysMaxVelocity;

            if (horizontalSpeed > maxVelocity)
            {
                float scale = maxVelocity / horizontalSpeed;
                m_Velocity.x *= scale;
                m_Velocity.z *= scale;
            }
        }

        /// <summary>
        /// SoF2 PM_CmdScale — normalisiert diagonale Inputs und skaliert mit g_speed.
        /// Exakter Port: scale = speed * max / (127 * total).
        /// Verhindert sqrt(2)-Speed-Boost bei diagonaler Eingabe
        /// und erlaubt proportionale Geschwindigkeit bei partiellem Stick-Input.
        /// </summary>
        private float PM_CmdScale()
        {
            float fmove = Mathf.Abs(m_MoveInput.y) * 127f;
            float smove = Mathf.Abs(m_MoveInput.x) * 127f;

            float max = Mathf.Max(fmove, smove);
            if (max <= 0f)
            {
                return 0f;
            }

            float total = Mathf.Sqrt(fmove * fmove + smove * smove);
            return m_PmMaxSpeed * max / (127f * total);
        }

        /// <summary>
        /// SoF2 PM_CheckJump — prueft ob gerade gesprungen wird.
        /// </summary>
        private bool PM_CheckJump()
        {
            if (m_IsDebounceActive)
            {
                return false;
            }

            return m_IsJumping;
        }

        /// <summary>
        /// SoF2 PM_WalkMove — Boden-Bewegung.
        /// Prueft Jump, wendet Friction an, berechnet Wish-Direction
        /// projiziert auf Ground-Plane, beschleunigt.
        /// </summary>
        private void PM_WalkMove()
        {
            if (PM_CheckJump())
            {
                PM_AirMove();
                return;
            }

            PM_Friction();

            // Bewegungsrichtung aus YawTarget (Kamera-Blickrichtung)
            Vector3 forward = m_YawTarget != null ? m_YawTarget.forward : transform.forward;
            Vector3 right = m_YawTarget != null ? m_YawTarget.right : transform.right;

            // Auf Ground-Plane projizieren
            Vector3 groundNormal = Vector3.up;
            if (m_LastGroundHit.collider != null)
            {
                float slopeThreshold = m_PmMaxSteepness > 1f
                    ? Mathf.Cos(m_PmMaxSteepness * Mathf.Deg2Rad)
                    : m_PmMaxSteepness;
                if (Vector3.Dot(m_LastGroundHit.normal, Vector3.up) > slopeThreshold)
                {
                    groundNormal = m_LastGroundHit.normal;
                }
            }

            forward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
            right = Vector3.ProjectOnPlane(right, groundNormal).normalized;

            // SoF2-Style input scaling (fmove/smove = ±127)
            float fmove = m_MoveInput.y * 127f;
            float smove = m_MoveInput.x * 127f;
            Vector3 wishvel = forward * fmove + right * smove;

            if (wishvel.sqrMagnitude > 0.01f)
            {
                wishvel = Vector3.ProjectOnPlane(wishvel, groundNormal);
            }

            float scale = PM_CmdScale();

            Vector3 wishdir = wishvel;
            float wishspeed = wishdir.magnitude;
            if (wishspeed > 0.0001f)
            {
                wishdir /= wishspeed;
            }
            else
            {
                wishdir = Vector3.zero;
                wishspeed = 0f;
            }

            wishspeed *= scale;

            if (wishspeed > m_PmMaxSpeed)
            {
                wishspeed = m_PmMaxSpeed;
            }

            PM_Accelerate(wishdir, wishspeed, m_PmAccelerate);

            // SoF2: Velocity auf Ground-Plane clippen + Speed erhalten.
            // Verhindert Speed-Verlust auf Slopes (bg_pmove.c PM_WalkMove).
            if (m_LastGroundHit.collider != null)
            {
                float vel = m_Velocity.magnitude;
                PM_ClipVelocity(m_Velocity, m_LastGroundHit.normal, out Vector3 clipped, OVERCLIP);
                m_Velocity = clipped;
                float clippedMag = m_Velocity.magnitude;
                if (clippedMag > 0.001f)
                {
                    m_Velocity = m_Velocity / clippedMag * vel;
                }
            }

            // SoF2: nicht bewegen wenn stillstehend
            if (Mathf.Abs(m_Velocity.x) < 0.001f && Mathf.Abs(m_Velocity.z) < 0.001f)
            {
                return;
            }

            ApplyVelocityLimits();
        }

        /// <summary>
        /// SoF2 PM_AirMove — Luft-Bewegung.
        /// Kein Ground-Plane-Projektion, niedrigere Acceleration (pm_airaccelerate).
        /// Ermoeglicht Strafe-Jumping durch Q3-Accelerate-Projektion.
        /// </summary>
        private void PM_AirMove()
        {
            PM_Friction();

            Vector3 forward = m_YawTarget != null ? m_YawTarget.forward : transform.forward;
            Vector3 right = m_YawTarget != null ? m_YawTarget.right : transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            float fmove = m_MoveInput.y * 127f;
            float smove = m_MoveInput.x * 127f;

            Vector3 wishvel = forward * fmove + right * smove;
            wishvel.y = 0f;

            float scale = PM_CmdScale();

            Vector3 wishdir = wishvel;
            float wishspeed = wishdir.magnitude;

            if (wishspeed > 0.0001f)
            {
                wishdir /= wishspeed;
            }
            else
            {
                wishdir = Vector3.zero;
                wishspeed = 0f;
            }

            wishspeed *= scale;

            if (wishspeed > m_PmMaxSpeed)
            {
                wishspeed = m_PmMaxSpeed;
            }

            PM_Accelerate(wishdir, wishspeed, m_PmAirAccelerate);
            ApplyVelocityLimits();
        }

        /// <summary>
        /// Jump ausfuehren (SoF2 TryJump).
        /// Setzt velocity.y = jumpVelocity, isJumping = true.
        /// Aufgerufen von Input-Callback (OnJumpPerformed).
        /// </summary>
        private void TryJump()
        {
            if (m_IsDebounceActive)
            {
                return;
            }

            if (!m_IsGrounded)
            {
                return;
            }

            if (m_IsJumping)
            {
                return;
            }

            m_IsJumping = true;
            m_IsDebounceActive = false;
            m_Velocity.y = m_JumpVelocity;
            m_LastJumpTime = Time.time;

            // Jump-Animation ueber Netzwerk triggern
            m_NetworkedPlayerCharacter.RequestJumpTrigger();
        }

        // ===================================================================
        // Collision & Step-Up (PM_StepSlideMove + TryStepUp)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_StepSlideMove — Kern-Collision-Handling.
        /// CapsuleCast in Bewegungsrichtung, bei Hit: Step-Up versuchen oder
        /// Velocity clippen und entlang der Oberflaeche sliden.
        /// Bis zu 4 Bumps pro Frame (Ecken, komplexe Geometrie).
        /// </summary>
        private void PM_StepSlideMove()
        {
            int numbumps = 4;
            Vector3 currentPos = transform.position;
            float timeLeft = 1.0f;

            float halfHeightLocal = Mathf.Max(0f,
                (m_ColliderSystem.GetCurrentCapsuleHeight() * 0.5f) - m_ColliderSystem.GetCurrentCapsuleRadius());

            Vector3 vel = m_Velocity;
            bool stepUpAttempted = false;

            for (int bump = 0; bump < numbumps; bump++)
            {
                Vector3 worldCenter = GetWorldCenterAtPosition(currentPos);
                Vector3 top = worldCenter + transform.up * halfHeightLocal;
                Vector3 bottom = worldCenter - transform.up * halfHeightLocal;

                Vector3 end = currentPos + vel * m_PhysicsDeltaTime * timeLeft;
                Vector3 castDir = end - currentPos;
                float castDist = castDir.magnitude;

                if (castDist < 1e-6f)
                {
                    if (vel.magnitude < 0.1f && m_IsGrounded)
                    {
                        vel *= 0.5f;
                    }

                    break;
                }

                Vector3 castDirNorm = castDir / castDist;

                if (Physics.CapsuleCast(top, bottom, m_ColliderSystem.GetCurrentCapsuleRadius(),
                        castDirNorm, out RaycastHit hit, castDist + SKIN_WIDTH,
                        m_GroundMask, QueryTriggerInteraction.Ignore))
                {
                    // Step-Up versuchen (einmal pro Frame)
                    if (!stepUpAttempted && TryStepUp(currentPos, hit, out Vector3 stepUpPos))
                    {
                        currentPos = stepUpPos;
                        stepUpAttempted = true;

                        Vector3 remainingMovement = vel * m_PhysicsDeltaTime * timeLeft;
                        remainingMovement.y = 0f;

                        if (remainingMovement.magnitude > 0.001f)
                        {
                            Vector3 newTop = GetWorldCenterAtPosition(currentPos) + transform.up * halfHeightLocal;
                            Vector3 newBottom = GetWorldCenterAtPosition(currentPos) - transform.up * halfHeightLocal;

                            if (!Physics.CapsuleCast(newTop, newBottom, m_ColliderSystem.GetCurrentCapsuleRadius(),
                                    remainingMovement.normalized, out RaycastHit _,
                                    remainingMovement.magnitude + SKIN_WIDTH,
                                    m_GroundMask, QueryTriggerInteraction.Ignore))
                            {
                                currentPos += remainingMovement;
                                break;
                            }
                        }

                        break;
                    }

                    // Bis kurz vor den Hit bewegen (Skin-Width Abstand)
                    float moveDist = Mathf.Max(hit.distance - SKIN_WIDTH, 0f);

                    if (moveDist < 0.001f)
                    {
                        if (vel.magnitude < 0.1f)
                        {
                            vel *= 0.3f;
                            break;
                        }
                    }
                    else
                    {
                        currentPos += castDirNorm * moveDist;
                    }

                    // Velocity entlang der Oberflaeche clippen (Slide)
                    PM_ClipVelocity(vel, hit.normal, out Vector3 clipVel, OVERCLIP);
                    vel = clipVel;

                    if (vel.magnitude < 0.01f)
                    {
                        vel *= 0.1f;
                        break;
                    }

                    float fraction = moveDist / castDist;
                    timeLeft -= timeLeft * fraction;

                    if (timeLeft <= 0.001f)
                    {
                        break;
                    }
                }
                else
                {
                    // Kein Treffer → komplette Strecke gehen
                    currentPos = end;
                    break;
                }
            }

            m_Velocity = vel;
            transform.position = currentPos;

            // Finaler Ground-Check nach Bewegung
            bool wasGroundedBeforeMove = m_IsGrounded;
            bool newGroundCheck = CheckGroundedAtPosition(currentPos, out RaycastHit downHit);

            // Slope-Fallback wenn vorher grounded
            if (!newGroundCheck && wasGroundedBeforeMove)
            {
                newGroundCheck = TrySlopeGroundCheck(currentPos, halfHeightLocal, out downHit);
            }

            if (!wasGroundedBeforeMove || newGroundCheck)
            {
                m_IsGrounded = newGroundCheck;
            }

            // Am Boden: Y-Velocity null, Position korrigieren, Slope-Projektion
            if (m_IsGrounded && m_Velocity.y <= 0f)
            {
                m_Velocity.y = 0f;

                if (downHit.collider != null)
                {
                    CorrectGroundPosition(currentPos, downHit, halfHeightLocal);

                    float slopeDot = Vector3.Dot(downHit.normal, Vector3.up);
                    if (slopeDot < 0.95f)
                    {
                        Vector3 groundProjectedVel = Vector3.ProjectOnPlane(m_Velocity, downHit.normal);
                        m_Velocity = groundProjectedVel;
                        m_Velocity.y = 0f;

                        if (slopeDot < 0.7f)
                        {
                            m_Velocity *= 0.8f;
                            m_Velocity.y = 0f;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// SoF2 Step-Up Logik: versucht ueber ein Hindernis zu steigen.
        /// Prueft Hindernis-Hoehe, Headroom, horizontale Blockade, Boden-Verification.
        /// </summary>
        private bool TryStepUp(Vector3 currentPos, RaycastHit hit, out Vector3 stepUpPos)
        {
            stepUpPos = currentPos;

            if (!m_IsGrounded || Mathf.Abs(m_Velocity.y) > 1.0f)
            {
                return false;
            }

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.1f)
            {
                return false;
            }

            float obstacleHeight = hit.point.y - (currentPos.y - m_ColliderSystem.GetCurrentCapsuleRadius());

            if (obstacleHeight > m_PmMaxBarrier)
            {
                return false;
            }

            if (obstacleHeight > m_PmMaxStep || obstacleHeight < 0.5f)
            {
                return false;
            }

            float stepUpAmount = Mathf.Min(obstacleHeight + 0.1f, m_PmStepSize);
            Vector3 stepUpTarget = currentPos + Vector3.up * stepUpAmount;

            float halfHeight = Mathf.Max(0f,
                (m_ColliderSystem.GetCurrentCapsuleHeight() * 0.5f) - m_ColliderSystem.GetCurrentCapsuleRadius());
            Vector3 worldCenter = GetWorldCenterAtPosition(stepUpTarget);
            Vector3 top = worldCenter + transform.up * halfHeight;
            Vector3 bottom = worldCenter - transform.up * halfHeight;

            // Ceiling-Check
            if (Physics.CapsuleCast(bottom, top, m_ColliderSystem.GetCurrentCapsuleRadius(),
                    Vector3.up, out RaycastHit _, m_PmStepSize,
                    m_GroundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Horizontaler Obstacle-Check
            Vector3 horizontalVel = new(m_Velocity.x, 0f, m_Velocity.z);
            if (horizontalVel.magnitude > 0.1f)
            {
                Vector3 horizontalDir = horizontalVel.normalized;
                float checkDistance = Mathf.Min(horizontalVel.magnitude * m_PhysicsDeltaTime, 0.5f);

                Vector3 stepTop = GetWorldCenterAtPosition(stepUpTarget) + transform.up * halfHeight;
                Vector3 stepBottom = GetWorldCenterAtPosition(stepUpTarget) - transform.up * halfHeight;

                if (Physics.CapsuleCast(stepBottom, stepTop, m_ColliderSystem.GetCurrentCapsuleRadius(),
                        horizontalDir, out RaycastHit horizontalHit, checkDistance + SKIN_WIDTH,
                        m_GroundMask, QueryTriggerInteraction.Ignore))
                {
                    if (horizontalHit.point.y <= hit.point.y)
                    {
                        return false;
                    }
                }
            }

            // Ground-Verification: Cast nach unten, begehbarer Boden muss existieren
            float groundCheckDist = stepUpAmount + m_ColliderSystem.GetCurrentGroundCheckDistance();
            if (!Physics.CapsuleCast(top, bottom, m_ColliderSystem.GetCurrentCapsuleRadius() * 0.95f,
                    Vector3.down, out RaycastHit groundHit, groundCheckDist,
                    m_GroundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float slopeThreshold = m_PmMaxSteepness > 1f
                ? Mathf.Cos(m_PmMaxSteepness * Mathf.Deg2Rad)
                : m_PmMaxSteepness;
            if (Vector3.Dot(groundHit.normal, Vector3.up) < slopeThreshold)
            {
                return false;
            }

            stepUpPos = stepUpTarget;
            m_LastStepUpTime = Time.time;
            return true;
        }

        // ===================================================================
        // Ground Detection
        // ===================================================================

        /// <summary>
        /// Einheitlicher Ground-Check per CapsuleCast nach unten.
        /// Beruecksichtigt Jump-Grace-Period und Slope-Threshold.
        /// </summary>
        private bool CheckGroundedAtPosition(Vector3 position, out RaycastHit groundHit)
        {
            groundHit = new RaycastHit();

            // Jump-Grace: kurz nach Sprung keinen Boden erkennen
            if (m_IsJumping && (Time.time - m_LastJumpTime) < 0.01f)
            {
                return false;
            }

            if (m_IsJumping && m_Velocity.y > 5f)
            {
                return false;
            }

            float halfHeight = Mathf.Max(0f,
                (m_ColliderSystem.GetCurrentCapsuleHeight() * 0.5f) - m_ColliderSystem.GetCurrentCapsuleRadius());
            Vector3 center = GetWorldCenterAtPosition(position);
            Vector3 top = center + transform.up * halfHeight;
            Vector3 bottom = center - transform.up * halfHeight;

            float baseDistance = m_ColliderSystem.GetCurrentGroundCheckDistance();
            float verticalComponent = Mathf.Abs(m_Velocity.y) * m_PhysicsDeltaTime;
            float horizontalComponent = new Vector3(m_Velocity.x, 0f, m_Velocity.z).magnitude * m_PhysicsDeltaTime;
            float dynamicCastDistance = baseDistance + 0.01f + verticalComponent + horizontalComponent * 0.5f;

            if (Physics.CapsuleCast(top, bottom, m_ColliderSystem.GetCurrentCapsuleRadius() * 0.95f,
                    Vector3.down, out RaycastHit hit, dynamicCastDistance,
                    m_GroundMask, QueryTriggerInteraction.Ignore))
            {
                float slopeThreshold = m_PmMaxSteepness > 1f
                    ? Mathf.Cos(m_PmMaxSteepness * Mathf.Deg2Rad)
                    : m_PmMaxSteepness;

                if (Vector3.Dot(hit.normal, Vector3.up) > slopeThreshold)
                {
                    if (m_IsJumping && m_Velocity.y > 1.0f)
                    {
                        return false;
                    }

                    groundHit = hit;
                    m_LastGroundHit = hit;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Triple Ground-Check: Standard → Slope-Fallback → Grace-Period.
        /// Exakt wie MyPlayerControllerCustom.FixedUpdate.
        /// </summary>
        private void CheckGroundedState(bool wasGrounded)
        {
            m_IsGrounded = CheckGroundedAtPosition(transform.position, out RaycastHit _);

            // Slope-Fallback mit 4x Distanz
            if (!m_IsGrounded)
            {
                float halfHeight = Mathf.Max(0f,
                    (m_ColliderSystem.GetCurrentCapsuleHeight() * 0.5f) - m_ColliderSystem.GetCurrentCapsuleRadius());
                m_IsGrounded = TrySlopeGroundCheck(transform.position, halfHeight, out RaycastHit _);
            }

            // Grace-Period: wenn vorher grounded und kuerzlich verloren
            if (!m_IsGrounded && wasGrounded && (Time.time - m_LastGroundedTime) < m_GroundGracePeriod)
            {
                float halfHeight = Mathf.Max(0f,
                    (m_ColliderSystem.GetCurrentCapsuleHeight() * 0.5f) - m_ColliderSystem.GetCurrentCapsuleRadius());
                m_IsGrounded = TrySlopeGroundCheck(transform.position, halfHeight, out RaycastHit _);
            }
        }

        /// <summary>
        /// Aggressive Slope Ground-Check mit 4x Distanz.
        /// </summary>
        private bool TrySlopeGroundCheck(Vector3 position, float halfHeight, out RaycastHit slopeHit)
        {
            slopeHit = new RaycastHit();
            Vector3 center = GetWorldCenterAtPosition(position);
            Vector3 top = center + transform.up * halfHeight;
            Vector3 bottom = center - transform.up * halfHeight;

            float slopeCheckDistance = m_ColliderSystem.GetCurrentGroundCheckDistance() * 4f;

            if (Physics.CapsuleCast(top, bottom, m_ColliderSystem.GetCurrentCapsuleRadius() * 0.95f,
                    Vector3.down, out RaycastHit hit, slopeCheckDistance,
                    m_GroundMask, QueryTriggerInteraction.Ignore))
            {
                float slopeThreshold = m_PmMaxSteepness > 1f
                    ? Mathf.Cos(m_PmMaxSteepness * Mathf.Deg2Rad)
                    : m_PmMaxSteepness;

                if (Vector3.Dot(hit.normal, Vector3.up) > slopeThreshold)
                {
                    slopeHit = hit;
                    m_LastGroundHit = hit;
                    m_IsGrounded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Korrigiert die Position ueber dem Boden (verhindert Einsinken).
        /// </summary>
        private void CorrectGroundPosition(Vector3 currentPos, RaycastHit downHit, float halfHeight)
        {
            Vector3 capsuleBottom = GetWorldCenterAtPosition(currentPos) - transform.up * halfHeight;
            float desiredDistance = m_ColliderSystem.GetCurrentCapsuleRadius() * 0.1f;
            float currentDistance = Vector3.Dot(capsuleBottom - downHit.point, downHit.normal);

            if (currentDistance < desiredDistance)
            {
                float correction = desiredDistance - currentDistance;
                transform.position += downHit.normal * correction;
            }
        }

        /// <summary>
        /// Welt-Center der Capsule an gegebener Position berechnen.
        /// </summary>
        private Vector3 GetWorldCenterAtPosition(Vector3 position)
        {
            Vector3 baseCenter = transform.TransformPoint(m_ColliderSystem.GetCurrentCapsuleCenter());
            Vector3 positionDelta = position - transform.position;
            return baseCenter + positionDelta;
        }

        /// <summary>
        /// Landing-Events (Debounce-Aktivierung, Jump-Reset).
        /// </summary>
        private void HandleLandingEvents(bool justLanded)
        {
            if (!justLanded || m_LandedThisGround)
            {
                return;
            }

            m_LandedThisGround = true;

            if (m_IsJumping)
            {
                m_IsJumping = false;
                m_IsDebounceActive = true;
                m_JumpDebounce = m_JumpDebounceAfterMs;
            }
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
