using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Tolik.RemakeSoF.Runtime.Game.Camera;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using Tolik.RemakeSoF.Runtime.Game.WeaponManagement;
/**
    * Owner-Client-Controller fÃ¼r Player-Character.
    * SoF2/Quake3-Style manuelle Physik: Velocity-basierte Bewegung mit CapsuleCasts.
    * Client-Side Prediction: Physik wird lokal angewendet (responsiv),
    * dann an Server gesendet zur Validierung.
    * Auf Remote-Clients: nur CapsuleCollider fuer Kollision.
    Ja, das ist jetzt sehr nah am Original:

Physik auf Framerate â€” Q3/SoF2 lieÃŸ PM_Move pro Client-Frame laufen (nicht auf fixem Tick). 125fps = 125 Physik-Iterationen/s. Genau das hast du jetzt.
PM_StepSlideMove â€” 4-Bump Collision mit ClipVelocity, Step-Up, Slide â€” direkt aus bg_pmove.c
PM_Friction / PM_Accelerate â€” identische Formel: control * friction * dt, accel * dt * wishspeed
PM_WalkMove / PM_AirMove â€” Trennung Boden/Luft mit unterschiedlichen Accel-Werten (6 vs 1)
PM_CmdScale â€” Input-Normalisierung wie PM_CmdScale in Q3
Sofortige Jump-Velocity â€” velocity.y = jumpVelocity (kein Force, kein AddForce)
Manuelle Gravity â€” velocity.y -= gravity * dt statt Rigidbody
CapsuleCast statt CharacterController â€” nÃ¤her an Q3's Trace-System als Unitys eingebaute Physik
Die Werte (pm_maxspeed=28, pm_gravity=80, pm_friction=6, pm_accelerate=6, pm_airaccelerate=1, jumpvel=27) sind SoF2-Defaults. Einziger Unterschied zu purem Q3: du hast zusÃ¤tzliche Slope-Friction und die SoF2-spezifischen Step-Up Limits (pm_maxstep=1.8, pm_maxbarrier=3.2), was korrekt ist â€” SoF2 hat das gegenÃ¼ber Q3 erweitert.
Was fehlt fÃ¼r 100% AuthentizitÃ¤t wÃ¤re Strafe-Jumping / Air-Control (Q3 pm_airaccelerate erlaubt Speed-Gain durch Richtungswechsel in der Luft). Das funktioniert bei dir automatisch, weil PM_AirMove mit pm_airaccelerate=1 und der Q3-Accelerate-Formel arbeitet â€” die erlaubt den klassischen Speed-Gain Bug by design.
*/

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Owner-Client-Controller fÃ¼r Player-Character.
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
        /// SoF2 Hitbox-System: erstellt 17 per-bone BoxCollider fuer Hit Region Detection.
        /// </summary>
        [SerializeField]
        private ClientHitboxSystem m_HitboxSystem;

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

        /// <summary>
        /// NetworkedCharacterState-Referenz fuer Waffen-Sync (OnWeaponChanged).
        /// </summary>
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        // ===== Constants =====

        /// <summary>Reconciliation Threshold: ab dieser Abweichung wird korrigiert (Meter).</summary>
        private const float k_ReconciliationThreshold = 0.05f;

        /// <summary>GrÃ¶sse des Prediction-Ringbuffers (Anzahl Commands).</summary>
        private const int k_PredictionBufferSize = 128;

        // ===== Debug HUD Properties =====

        /// <summary>Ob der Spieler am Boden ist.</summary>
        internal bool IsGrounded => m_Simulation.IsGrounded;

        /// <summary>Ob der Spieler gerade angreift.</summary>
        internal bool IsAttacking => m_IsAttacking;

        /// <summary>Ob der Spieler geduckt ist.</summary>
        internal bool IsCrouching => m_Simulation.IsCrouching;

        /// <summary>Aktuelle Velocity (XYZ) aus der Simulation.</summary>
        internal Vector3 Velocity => m_Simulation.Velocity;

        /// <summary>Horizontale Geschwindigkeit (XZ-Ebene).</summary>
        internal float HorizontalSpeed
        {
            get
            {
                Vector3 v = m_Simulation.Velocity;
                return new Vector3(v.x, 0f, v.z).magnitude;
            }
        }

        /// <summary>Vertikale Geschwindigkeit (Y-Achse).</summary>
        internal float VerticalSpeed => m_Simulation.Velocity.y;

        /// <summary>Aktuelle Airtime in Sekunden (0 am Boden).</summary>
        internal float CurrentAirtime => m_Simulation.CurrentAirtime;

        /// <summary>Aktuelle Sprunghoehe in Metern (0 am Boden).</summary>
        internal float CurrentJumpHeight => m_Simulation.CurrentJumpHeight;

        /// <summary>Aktuelle Fallhoehe in Metern (0 am Boden).</summary>
        internal float CurrentFallHeight => m_Simulation.CurrentFallHeight;

        /// <summary>Aktuelle horizontale Distanz in der Luft (Meter).</summary>
        internal float CurrentAirDistanceHoriz => m_Simulation.CurrentAirDistanceHoriz;

        /// <summary>Aktuelle vertikale Distanz in der Luft (Meter).</summary>
        internal float CurrentAirDistanceVert => m_Simulation.CurrentAirDistanceVert;

        /// <summary>Gesamte Airtime der letzten Luftphase (Sekunden).</summary>
        internal float FullAirtime => m_Simulation.FullAirtime;

        /// <summary>Max Sprunghoehe der letzten Luftphase (Meter).</summary>
        internal float FullJumpHeight => m_Simulation.FullJumpHeight;

        /// <summary>Max Fallhoehe der letzten Luftphase (Meter).</summary>
        internal float FullFallHeight => m_Simulation.FullFallHeight;

        /// <summary>Horizontale Distanz der letzten Luftphase (Meter).</summary>
        internal float FullAirDistanceHoriz => m_Simulation.FullAirDistanceHoriz;

        /// <summary>Gesamte vertikale Weglaenge der letzten Luftphase (Meter).</summary>
        internal float FullAirDistanceVert => m_Simulation.FullAirDistanceVert;

        /// <summary>Airtime der Aufstiegsphase der letzten Luftphase (Sekunden).</summary>
        internal float FullJumpPhaseAirtime => m_Simulation.FullJumpPhaseAirtime;

        /// <summary>Airtime der Fallphase der letzten Luftphase (Sekunden).</summary>
        internal float FullFallPhaseAirtime => m_Simulation.FullFallPhaseAirtime;

        /// <summary>Aktuelle Airtime in der Aufstiegsphase (Sekunden).</summary>
        internal float CurrentJumpPhaseAirtime => m_Simulation.CurrentJumpPhaseAirtime;

        /// <summary>Aktuelle Airtime in der Fallphase (Sekunden).</summary>
        internal float CurrentFallPhaseAirtime => m_Simulation.CurrentFallPhaseAirtime;

        // ===== Simulation + Prediction =====

        /// <summary>
        /// Shared SoF2-Physik-Simulation. Wird von Client (Prediction) und Server (Authority)
        /// mit identischem Code ausgeführt. Direkt serialisiert — alle Physik-Parameter im Inspector.
        /// </summary>
        [SerializeField]
        private PlayerPhysicsSimulation m_Simulation = new();

        /// <summary>Ringbuffer: gesendete Commands fÃ¼r Reconciliation-Replay.</summary>
        private readonly PlayerCommand[] m_PredictionCommands = new PlayerCommand[k_PredictionBufferSize];

        /// <summary>Ringbuffer: vorhergesagte Positionen (fÃ¼r Server-Vergleich).</summary>
        private readonly Vector3[] m_PredictedPositions = new Vector3[k_PredictionBufferSize];

        /// <summary>NÃ¤chste Sequenznummer fÃ¼r Commands.</summary>
        private uint m_NextSequenceNumber = 1;

        /// <summary>Letzte vom Server bestÃ¤tigte Sequenznummer.</summary>
        private uint m_LastAcknowledgedSequence;

        /// <summary>Jump-Request aus Input-Callback (wird im nÃ¤chsten RunPhysicsStep konsumiert).</summary>
        private bool m_JumpRequested;

        /// <summary>Ob der Character gerade angreift (fuer Animation + Netzwerk-Sync).</summary>
        private bool m_IsAttacking;

        /// <summary>Verbleibende Attack-Frames (Server-Frame-Counting, nicht zeit-basiert).</summary>
        private int m_AttackFramesRemaining;

        /// <summary>Frame-Akkumulator fuer frame-diskretes Attack-Timing.</summary>
        private float m_AttackFrameAccumulator;

        /// <summary>Anzahl Animation-Frames fuer einen Knife-Slash (aus average_sleeves_mp.frames).</summary>
        private const int k_AttackFrames = 6;

        /// <summary>FPS der Attack-Animation (aus average_sleeves_mp.frames: knifeslash01_mp = 20fps).</summary>
        private const int k_AttackFps = 20;

        /// <summary>
        /// Remote-Modus: Component laeuft auf Remote-Clients nur fuer Bone-Rotation,
        /// Input/Physik sind deaktiviert. Daten kommen aus NetworkVariable.
        /// </summary>
        private bool m_IsRemoteMode;

        // ===== Weapon =====

        /// <summary>
        /// Interner WeaponLoader: laedt Waffen-Prefabs und attached sie an den Hand-Bone.
        /// Kein MonoBehaviour — wird hier orchestriert.
        /// </summary>
        private readonly WeaponLoader m_WeaponLoader = new();

        /// <summary>
        /// Pending Weapon-Name: gesetzt wenn OnWeaponChanged vor OnVisualInstantiated kommt.
        /// Wird beim naechsten OnVisualInstantiated konsumiert.
        /// </summary>
        private string m_PendingWeaponName;

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

            // Ack-Event abmelden
            m_NetworkedPlayerCharacter.OnMovementAcknowledged -= OnServerAcknowledgement;

            // Jump-Callback abmelden
            m_PlayerActions.Jump.performed -= OnJumpPerformed;

            // Visual-Event abmelden
            if (m_SkinHandler != null)
            {
                m_SkinHandler.OnVisualInstantiated -= OnVisualInstantiated;
            }

            // Waffen-Event abmelden
            if (m_CharacterState != null)
            {
                m_CharacterState.OnWeaponChanged -= OnWeaponChanged;
            }

            // Waffe aufraeumen
            m_WeaponLoader.ClearCurrentWeapon();

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
            // Waffen-Event fuer Owner UND Remote abonnieren (beide muessen Waffen laden/anzeigen)
            if (m_CharacterState != null)
            {
                m_CharacterState.OnWeaponChanged += OnWeaponChanged;
            }

            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                // Remote-Client: nur Bone-Rotation, kein Input/Physik
                m_IsRemoteMode = true;

                // Auf Visual-Instanziierung lauschen (Bone-Referenzen fuer Remote-Rotation)
                if (m_SkinHandler != null)
                {
                    m_SkinHandler.OnVisualInstantiated += OnVisualInstantiated;
                }

                return;
            }

            // Owner: Player Action Map aktivieren
            m_PlayerActions.Enable();

            // Jump per Callback (zuverlaessiger als WasPressedThisFrame in FixedUpdate)
            m_PlayerActions.Jump.performed += OnJumpPerformed;

            // Server-Acknowledgement fuer Reconciliation abonnieren
            m_NetworkedPlayerCharacter.OnMovementAcknowledged += OnServerAcknowledgement;

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
        /// Input-Callback fuer Jump (performed). Setzt Jump-Request Flag,
        /// das im naechsten RunPhysicsStep vom Simulation-Command konsumiert wird.
        /// </summary>
        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            m_JumpRequested = true;
        }

        private void Update()
        {
            if (m_IsRemoteMode)
            {
                UpdateRemoteState();
                return;
            }

            if (!m_NetworkedPlayerCharacter.IsOwner)
            {
                return;
            }

            // Input lesen (wird in Physik-Pipeline + LateUpdate konsumiert)
            m_MoveInput = m_PlayerActions.Move.ReadValue<Vector2>();
            m_IsWalkingPressed = m_PlayerActions.Walk.IsPressed();

            // Hold-to-Jump: solange Jump gehalten wird, jeden Frame Jump-Request setzen
            //if (m_PlayerActions.Jump.IsPressed())
            //{
            //    m_JumpRequested = true;
            //}

            SyncCharacterRotation();
            HandleActionInput();

            // SoF2-Physik-Pipeline via Shared Simulation (Client-Side Prediction).
            RunPhysicsStep();

            UpdateAnimationState();
        }

        /// <summary>
        /// Remote-Modus: Liest MoveInput und Pitch aus NetworkVariable
        /// und treibt YawTarget/PitchTarget fuer Bone-Rotation.
        /// </summary>
        private void UpdateRemoteState()
        {
            NetworkAnimationState animState = m_NetworkedPlayerCharacter.CurrentAnimationState;
            m_MoveInput = new Vector2(animState.MoveInputX, animState.MoveInputY);

            // YawTarget aus interpolierter Character-Rotation treiben
            if (m_YawTarget != null)
            {
                m_YawTarget.rotation = transform.rotation;
            }

            // PitchTarget aus synchronisiertem PitchAngle treiben
            if (m_PitchTarget != null)
            {
                float yaw = transform.eulerAngles.y;
                m_PitchTarget.rotation = Quaternion.Euler(animState.PitchAngle, yaw, 0f);
            }
        }

        /// <summary>
        /// Komplette SoF2-Physik-Pipeline pro Frame (Client-Side Prediction).
        /// Baut einen PlayerCommand aus aktuellem Input, fuehrt die Shared-Simulation aus,
        /// speichert das Ergebnis im Prediction-Buffer und sendet den Command an den Server.
        /// Server fuehrt identische Simulation aus (Authority).
        /// </summary>
        private void RunPhysicsStep()
        {
            // Simulation muss initialisiert sein (Capsule-Dimensionen gesetzt)
            if (m_Simulation.CapsuleHeight <= 0f)
            {
                return;
            }

            // PlayerCommand aus aktuellem Input bauen (SoF2 usercmd_t)
            int buttons = 0;
            if (m_JumpRequested) buttons |= CommandButtons.Jump;
            if (m_IsWalkingPressed) buttons |= CommandButtons.Walk;
            if (m_IsAttacking) buttons |= CommandButtons.Attack;

            PlayerCommand cmd = new()
            {
                MoveInput = m_MoveInput,
                YawAngle = m_YawTarget != null ? m_YawTarget.eulerAngles.y : transform.eulerAngles.y,
                Buttons = buttons,
                DeltaTime = Time.deltaTime,
                SequenceNumber = m_NextSequenceNumber++,
            };
            m_JumpRequested = false;

            // Eigenen Collider deaktivieren damit CapsuleCast sich nicht selbst trifft
            CapsuleCollider ownCollider = m_ColliderSystem != null ? m_ColliderSystem.PhysicsCollider : null;
            if (ownCollider != null)
            {
                ownCollider.enabled = false;
            }

            // Client-Side Prediction: lokale Physik ausfuehren (sofortige Reaktion)
            Vector3 position = transform.position;
            m_Simulation.Simulate(ref position, cmd);
            transform.position = position;

            // Eigenen Collider wieder aktivieren
            if (ownCollider != null)
            {
                ownCollider.enabled = true;
            }

            // Jump-Animation ueber Netzwerk triggern
            if (m_Simulation.JumpTriggered)
            {
                m_NetworkedPlayerCharacter.RequestJumpTrigger();
            }

            // Visual-Debug: Grounded-State an ColliderSystem uebergeben
            if (m_ColliderSystem != null)
            {
                m_ColliderSystem.SetGroundedState(m_Simulation.IsGrounded);
            }

            // Prediction-Buffer: Command + Position fuer Reconciliation speichern
            int bufferIndex = (int)(cmd.SequenceNumber % k_PredictionBufferSize);
            m_PredictionCommands[bufferIndex] = cmd;
            m_PredictedPositions[bufferIndex] = position;

            // Command an Server senden (Server fuehrt identische Physik aus)
            m_NetworkedPlayerCharacter.SendPlayerCommand(cmd);
        }

        /// <summary>
        /// Server-Acknowledgement empfangen: Reconciliation durchfuehren.
        /// Vergleicht Server-Position mit vorhergesagter Position.
        /// Bei Abweichung: Server-State uebernehmen und unbestaetigte Commands replaying.
        /// </summary>
        public void OnServerAcknowledgement(ServerMovementAck ack)
        {
            // Stale Ack ignorieren (aeltere Sequenz als bereits bestaetigt)
            if (ack.LastProcessedSequence <= m_LastAcknowledgedSequence)
            {
                return;
            }

            m_LastAcknowledgedSequence = ack.LastProcessedSequence;

            // Vorhergesagte Position zum Zeitpunkt der Server-Bestaetigung abrufen
            int ackIndex = (int)(ack.LastProcessedSequence % k_PredictionBufferSize);
            Vector3 predictedPosition = m_PredictedPositions[ackIndex];

            // Abweichung pruefen
            float error = Vector3.Distance(predictedPosition, ack.Position);
            if (error <= k_ReconciliationThreshold)
            {
                return;
            }

            // Reconciliation: Server-State uebernehmen
            m_Simulation.SetState(ack.Velocity, ack.IsGrounded, ack.IsJumping);
            Vector3 replayPosition = ack.Position;

            // Eigenen Collider deaktivieren damit ResolvePenetration's OverlapCapsule
            // sich nicht selbst trifft (gleiche Pattern wie RunPhysicsStep)
            CapsuleCollider ownCollider = m_ColliderSystem != null ? m_ColliderSystem.PhysicsCollider : null;
            if (ownCollider != null)
            {
                ownCollider.enabled = false;
            }

            // Unbestaetigte Commands replaying (Server hat diese noch nicht verarbeitet)
            for (uint seq = ack.LastProcessedSequence + 1; seq < m_NextSequenceNumber; seq++)
            {
                int idx = (int)(seq % k_PredictionBufferSize);
                m_Simulation.Simulate(ref replayPosition, m_PredictionCommands[idx]);
                m_PredictedPositions[idx] = replayPosition;
            }

            if (ownCollider != null)
            {
                ownCollider.enabled = true;
            }

            transform.position = replayPosition;
        }

        /// <summary>
        /// LateUpdate: Pelvis-Rotation nach Animator-Evaluation anwenden.
        /// Muss in LateUpdate passieren, da der Animator in Update/LateUpdate die Bone-Rotationen setzt
        /// und unsere manuelle Korrektur sonst ueberschrieben wird.
        /// </summary>
        private void LateUpdate()
        {
            if (!m_NetworkedPlayerCharacter.IsOwner && !m_IsRemoteMode)
            {
                return;
            }

            UpdatePelvisRotation();
            UpdateLumbarRotation();
        }

        /// <summary>
        /// Callback wenn das Visual-Prefab instanziiert wurde.
        /// Sucht Bone-Transforms fuer Rotation. Im Owner-Modus zusaetzlich Kamera/Collider Setup.
        /// </summary>
        private void OnVisualInstantiated(GameObject visualInstance)
        {
            Transform yaw = FindDeepChild(visualInstance.transform, "Yaw");
            Transform pitch = FindDeepChild(visualInstance.transform, "Pitch");
            Transform pelvis = FindDeepChild(visualInstance.transform, "pelvis");
            Transform lowerLumbar = FindDeepChild(visualInstance.transform, "lower_lumbar");
            Transform upperLumbar = FindDeepChild(visualInstance.transform, "upper_lumbar");

            if (yaw == null || pitch == null)
            {
                Debug.LogWarning("[ClientPlayerCharacter] Yaw oder Pitch nicht im Visual gefunden!");
                return;
            }

            // Bone-Referenzen fuer Pelvis/Lumbar-Rotation (Owner + Remote)
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

            if (m_PelvisTarget != null)
            {
                Debug.Log("[ClientPlayerCharacter] Pelvis-Target gefunden fuer Bone-Rotation");
            }

            // Remote-Modus: Bone-Referenzen + Collider, kein Kamera/Simulation Setup
            if (m_IsRemoteMode)
            {
                // Collider auch fuer Remote initialisieren (Physics + Visual Debug)
                if (m_ColliderSystem != null)
                {
                    Transform highestPointR = FindDeepChild(visualInstance.transform, "*head_t_0");
                    Transform craniumR = FindDeepChild(visualInstance.transform, "cranium");
                    Transform rightHandBoltR = FindDeepChild(visualInstance.transform, "rhang_tag_bone");
                    Transform leftHandBoltR = FindDeepChild(visualInstance.transform, "lhand_tag_bone");
                    Transform rightFootR = FindDeepChild(visualInstance.transform, "rtarsal");
                    Transform leftFootR = FindDeepChild(visualInstance.transform, "ltarsal");

                    m_ColliderSystem.CalculateAutoCapsuleSize(highestPointR != null ? highestPointR : craniumR, pelvis, leftHandBoltR, rightHandBoltR, leftFootR, rightFootR);
                }

                // Hitboxen fuer Remote erstellen (Schaden wird auf allen Clients erkannt)
                if (m_HitboxSystem != null)
                {
                    m_HitboxSystem.BuildHitboxes(visualInstance.transform);
                }

                // Waffen-Attachment-Bone fuer Remote setzen
                Transform remoteHandBolt = FindDeepChild(visualInstance.transform, "rhang_tag_bone");
                if (remoteHandBolt != null)
                {
                    m_WeaponLoader.SetAttachmentBone(remoteHandBolt);
                    TryLoadPendingWeapon();
                }

                return;
            }

            // === Ab hier nur Owner ===

            Transform cameraTarget = FindDeepChild(visualInstance.transform, "CameraTarget");
            Transform highestPoint = FindDeepChild(visualInstance.transform, "*head_t_0");
            Transform cranium = FindDeepChild(visualInstance.transform, "cranium");
            Transform rightHandBolt = FindDeepChild(visualInstance.transform, "rhang_tag_bone");
            Transform leftHandBolt = FindDeepChild(visualInstance.transform, "lhand_tag_bone");
            Transform rightFoot = FindDeepChild(visualInstance.transform, "rtarsal");
            Transform leftFoot = FindDeepChild(visualInstance.transform, "ltarsal");

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

            // Collider-System initialisieren: Capsule-Groesse aus Bones berechnen
            if (m_ColliderSystem != null)
            {
                m_ColliderSystem.CalculateAutoCapsuleSize(highestPoint != null ? highestPoint : cranium, pelvis, leftHandBolt, rightHandBolt, leftFoot, rightFoot);

                // Simulation Capsule-Dimensionen setzen (fÃ¼r Client-Side Prediction)
                m_Simulation.SetCapsuleDimensions(
                    m_ColliderSystem.GetCurrentCapsuleHeight(),
                    m_ColliderSystem.GetCurrentCapsuleRadius(),
                    m_ColliderSystem.GetCurrentCapsuleCenter(),
                    m_ColliderSystem.GetCurrentGroundCheckDistance()
                );

                // Capsule-Dimensionen an Server senden (fÃ¼r Server-Side Simulation)
                m_NetworkedPlayerCharacter.SendCapsuleDimensions(
                    m_ColliderSystem.GetCurrentCapsuleHeight(),
                    m_ColliderSystem.GetCurrentCapsuleRadius(),
                    m_ColliderSystem.GetCurrentCapsuleCenter(),
                    m_ColliderSystem.GetCurrentGroundCheckDistance()
                );
            }

            // Hitboxen fuer Owner erstellen
            if (m_HitboxSystem != null)
            {
                m_HitboxSystem.BuildHitboxes(visualInstance.transform);
            }

            Debug.Log("[ClientPlayerCharacter] Kamera-Targets verdrahtet (Yaw/Pitch/CameraTarget)");

            // Waffen-Attachment-Bone fuer Owner setzen
            if (rightHandBolt != null)
            {
                m_WeaponLoader.SetAttachmentBone(rightHandBolt);
                TryLoadPendingWeapon();
            }
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
            // Attack-Frames herunterzaehlen (frame-diskret, wie SoF2 Animation-Frames)
            if (m_IsAttacking)
            {
                m_AttackFrameAccumulator += Time.deltaTime;
                float frameInterval = 1f / k_AttackFps;
                while (m_AttackFrameAccumulator >= frameInterval && m_AttackFramesRemaining > 0)
                {
                    m_AttackFrameAccumulator -= frameInterval;
                    m_AttackFramesRemaining--;
                }

                if (m_AttackFramesRemaining <= 0)
                {
                    m_IsAttacking = false;
                    m_AttackFrameAccumulator = 0f;
                }
            }

            // SoF2: Maus gehalten = automatisch wiederholen nach Cooldown (wie BUTTON_ATTACK in usercmd_t)
            if (m_PlayerActions.Attack.IsPressed() && !m_IsAttacking)
            {
                m_IsAttacking = true;
                m_AttackFramesRemaining = k_AttackFrames;
                m_AttackFrameAccumulator = 0f;
            }
        }

        // ===== Weapon Loading =====

        /// <summary>
        /// Callback wenn sich die Waffe im NetworkedCharacterState aendert.
        /// Laedt die neue Waffe sofort, falls das Visual bereits instanziiert ist
        /// (AttachmentBone gesetzt). Andernfalls wird der Name als Pending gespeichert
        /// und beim naechsten OnVisualInstantiated geladen.
        /// </summary>
        private void OnWeaponChanged(string weaponName)
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                m_WeaponLoader.ClearCurrentWeapon();
                m_PendingWeaponName = null;
                return;
            }

            // Pending merken fuer den Fall dass Visual noch nicht instanziiert ist
            m_PendingWeaponName = weaponName;
            TryLoadPendingWeapon();
        }

        /// <summary>
        /// Versucht die Pending-Waffe zu laden, falls AttachmentBone bereits gesetzt ist.
        /// Wird sowohl von OnWeaponChanged als auch von OnVisualInstantiated aufgerufen.
        /// </summary>
        private void TryLoadPendingWeapon()
        {
            if (string.IsNullOrEmpty(m_PendingWeaponName))
            {
                return;
            }

            if (m_WeaponLoader.LoadAndAttachWeapon(m_PendingWeaponName))
            {
                m_PendingWeaponName = null;
            }
        }

        /// <summary>
        /// Berechnet den aktuellen Animation-State aus Simulation-Velocity und Ground-State.
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

            // Horizontale Geschwindigkeit aus Simulation (ohne Y-Komponente)
            Vector3 simVelocity = m_Simulation.Velocity;
            Vector3 horizontalVelocity = new(simVelocity.x, 0f, simVelocity.z);
            float speed = horizontalVelocity.magnitude;
            bool isMoving = speed > 0.01f;

            NetworkAnimationState state = new()
            {
                Speed = speed,
                Horizontal = m_AnimHorizontal,
                Vertical = m_AnimVertical,
                IsMoving = isMoving,
                IsGrounded = m_Simulation.IsGrounded,
                IsWalking = isWalking,
                IsAttacking = m_IsAttacking,
                IsCrouching = m_Simulation.IsCrouching,
                MoveInputX = moveInput.x,
                MoveInputY = moveInput.y,
                PitchAngle = m_PitchTarget != null ? m_PitchTarget.eulerAngles.x : 0f,
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

            bool hasInput = m_MoveInput.sqrMagnitude > 0.0001f;

            if (hasInput)
            {
                // Legs-Richtung aus Input berechnen (SoF2-Stil: bei Rueckwaertsbewegung spiegeln)
                float forwardComp = Mathf.Abs(m_MoveInput.y);
                float effectiveX = (m_MoveInput.y < 0f) ? -m_MoveInput.x : m_MoveInput.x;
                Vector3 inputDir = fwd * forwardComp + rgt * effectiveX;

                if (inputDir.sqrMagnitude > 0.0001f)
                {
                    Vector3 desiredFlat = inputDir;
                    desiredFlat.y = 0f;
                    float t = Mathf.Clamp01(m_BaseLegsRotationSmooth * Time.deltaTime);
                    m_SmoothedLegsForward = Vector3.Slerp(m_SmoothedLegsForward, desiredFlat.normalized, t);
                }

                // Direction-Index aus Raw-Input (SoF2 PM_SetMovementDir)
                m_LastMoveDirIndex = ComputeMovementDir(m_MoveInput);
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

            bool hasInput = m_MoveInput.sqrMagnitude > 0.0001f;

            // Lean-Winkel berechnen (Roll bei SeitwÃ¤rtsbewegung, Pitch bei VorwÃ¤rts/RÃ¼ckwÃ¤rts)
            Vector2 targetLeanAngles = Vector2.zero;
            if (hasInput)
            {
                targetLeanAngles.x = -m_MoveInput.x * m_RollLeanDegrees;
                targetLeanAngles.y = m_MoveInput.y * m_PitchLeanDegrees;
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
                float strafeYaw = m_MoveInput.x * m_StrafeYawDegrees;
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
                float strafeYawUpper = m_MoveInput.x * (m_StrafeYawDegrees * 0.6f);
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
        /// Toggle MenÃ¼-Sichtbarkeit (AvatarActions TogglePauseMenu Callback).
        /// </summary>
        private void OnMenuToggle(InputAction.CallbackContext context)
        {
            GameApplication.Instance.Broadcast(new MenuToggleEvent());
        }

        /// <summary>
        /// Aktiviere oder deaktiviere Gameplay-Inputs und Kamera-Controller.
        /// Wird vom MenÃ¼-System aufgerufen (Pause/Resume).
        /// TogglePauseMenu bleibt immer aktiv, damit ESC auch im MenÃ¼ funktioniert.
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

            // Kamera-Controller ein-/ausschalten (verhindert Mausbewegung im MenÃ¼)
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
