using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Camera;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;
using Tolik.RemakeSoF.Runtime.Game.WeaponManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
/**
    * Owner-Client-Controller fuer Player-Character.
    * SoF2/Quake3-Style manuelle Physik: Velocity-basierte Bewegung mit BoxCasts (AABB).
    * Client-Side Prediction: Physik wird lokal angewendet (responsiv),
    * dann an Server gesendet zur Validierung.
    * Auf Remote-Clients: nur BoxCollider fuer Kollision.
    *
    * Physik auf Framerate: Q3/SoF2 liess PM_Move pro Client-Frame laufen (nicht auf fixem Tick).
    * PM_StepSlideMove: 4-Bump Collision mit ClipVelocity, Step-Up, Slide (aus bg_pmove.c).
    * PM_Friction / PM_Accelerate: identische Formel (control * friction * dt, accel * dt * wishspeed).
    * PM_WalkMove / PM_AirMove: Trennung Boden/Luft mit unterschiedlichen Accel-Werten (6 vs 1).
    * PM_CmdScale: Input-Normalisierung wie PM_CmdScale in Q3.
    * Sofortige Jump-Velocity: velocity.y = jumpVelocity (kein Force, kein AddForce).
    * Manuelle Gravity: velocity.y -= gravity * dt statt Rigidbody.
    * BoxCast (AABB) statt CharacterController: naeher an Q3 Trace-System als Unitys eingebaute Physik.
    * Werte in Quake-Units (pm_maxspeed=28, pm_gravity=80, pm_friction=6, pm_accelerate=6,
    *   pm_airaccelerate=1, jumpvel=27) sind SoF2-Defaults, im Code auf Meter konvertiert (* 0.0254).
    * SoF2-spezifische Erweiterungen gegenueber Q3: Slope-Friction, Step-Up Limits
    *   (pm_maxstep=1.8, pm_maxbarrier=3.2).
    * Strafe-Jumping / Air-Control funktioniert automatisch durch PM_AirMove mit
    *   pm_airaccelerate=1 und der Q3-Accelerate-Formel (Speed-Gain by design).
    */

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Client
{
    /// <summary>
    /// Owner-Client-Controller fuer Player-Character.
    /// SoF2/Quake3-Style manuelle Physik: Velocity-basierte Bewegung mit BoxCasts (AABB).
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
        /// SoF2 Collider-System: berechnet Box-Groesse aus Bones (Cranium, Fuesse).
        /// Stellt Box-Parameter fuer BoxCasts (AABB) bereit.
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
        /// Footstep/Landing-Sound-Handler. Spielt surface-abhaengige Sounds ab.
        /// </summary>
        private ClientFootstepHandler m_FootstepHandler;

        /// <summary>
        /// Gecachte WeaponDataLoader-Referenz (ServiceLocator-Lookup vermeiden in Hot-Paths).
        /// </summary>
        private WeaponDataLoader m_WeaponDataLoader;

        /// <summary>
        /// NetworkedCharacterState-Referenz fuer Waffen-Sync (OnWeaponChanged).
        /// </summary>
        [SerializeField]
        private NetworkedCharacterState m_CharacterState;

        /// <summary>
        /// Oeffentlicher Zugriff auf den NetworkedCharacterState (fuer HUD/Controller).
        /// </summary>
        public NetworkedCharacterState CharacterState => m_CharacterState;

        // ===== Constants =====

        /// <summary>Reconciliation Threshold: ab dieser Abweichung wird korrigiert (Meter).</summary>
        private const float k_ReconciliationThreshold = 0.05f;

        /// <summary>GrÃ¶sse des Prediction-Ringbuffers (Anzahl Commands).</summary>
        private const int k_PredictionBufferSize = 128;

        /// <summary>
        /// Minimale FullFallHeight fuer Landing-Sound (SoF2: PM_CrashLand delta kleiner 1 → kein Event).
        /// SoF2 Berechnung: delta = vel² × 0.0001, delta kleiner 1 → |vel| kleiner 100 QU/s.
        /// Fallhoehe: h = v²/(2g) = (100×0.0254)² / (2×20.32) = 6.25 QU = 0.159m.
        /// </summary>
        private const float k_MinFallHeightForLandingSound = 0.16f;

        // ===== Debug HUD Properties =====

        /// <summary>Ob der Spieler am Boden ist.</summary>
        internal bool IsGrounded => m_Simulation.IsGrounded;

        /// <summary>Ob der Spieler gerade angreift.</summary>
        internal bool IsAttacking => m_IsAttacking;

        /// <summary>Ob der Spieler gerade einen Alternativangriff ausfuehrt.</summary>
        internal bool IsAltAttacking => m_IsAltAttacking;

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

        /// <summary>Anzahl Spruenge in der aktuellen Bhop-Chain.</summary>
        internal int BhopChainCount => m_Simulation.BhopChainCount;

        /// <summary>Peak Speed der aktuellen Bhop-Chain (m/s).</summary>
        internal float BhopChainPeakSpeed => m_Simulation.BhopChainPeakSpeed;

        /// <summary>Distanz der aktuellen Bhop-Chain (Meter).</summary>
        internal float BhopChainDistance => m_Simulation.BhopChainDistance;

        /// <summary>Letzte Chain: Anzahl Spruenge.</summary>
        internal int LastBhopChainCount => m_Simulation.LastBhopChainCount;

        /// <summary>Letzte Chain: Peak Speed (m/s).</summary>
        internal float LastBhopChainPeakSpeed => m_Simulation.LastBhopChainPeakSpeed;

        /// <summary>Letzte Chain: Distanz (Meter).</summary>
        internal float LastBhopChainDistance => m_Simulation.LastBhopChainDistance;

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

        ///Initial Knife Attack-FPS (wird von OnWeaponChanged aktualisiert).
        /// <summary>Aktuelle Attack-Frame-Anzahl basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_AttackFrames = 6;

        /// Initial Knife Attack-FPS (wird von OnWeaponChanged aktualisiert).
        /// <summary>Aktuelle Attack-FPS basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_AttackFps = 20;

        /// <summary>Ob die aktuelle Waffe unendliche Munition hat (z.B. Knife).</summary>
        private bool m_CurrentWeaponInfiniteAmmo = true;

        /// <summary>Ob der Character gerade nachladet (fuer Animation + Netzwerk-Sync).</summary>
        private bool m_IsReloading;

        /// <summary>Verbleibende Reload-Frames (frame-basiert, wie Attack).</summary>
        private int m_ReloadFramesRemaining;

        /// <summary>Frame-Akkumulator fuer frame-diskretes Reload-Timing.</summary>
        private float m_ReloadFrameAccumulator;

        /// <summary>Aktuelle Reload-Frame-Anzahl basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ReloadFrames;

        /// <summary>Aktuelle Reload-FPS basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_ReloadFps = 20;

        /// <summary>Ob die aktuelle Waffe Shell-by-Shell nachladet (M590, MM1).</summary>
        private bool m_IsShellReload;

        /// <summary>Frame-Anzahl fuer ReloadStart-Animation (Shell-Reload).</summary>
        private int m_ReloadStartFrames;

        /// <summary>Frame-Anzahl fuer einzelne Shell-Lade-Animation (Shell-Reload).</summary>
        private int m_ReloadShellFrames;

        /// <summary>Frame-Anzahl fuer ReloadEnd-Animation (Shell-Reload).</summary>
        private int m_ReloadEndFrames;

        /// <summary>Aktuelle Phase beim Shell-Reload (Start, Shell, End).</summary>
        private ShellReloadPhase m_ShellReloadPhase;

        /// <summary>Verbleibende Shells die noch geladen werden muessen (Shell-Reload).</summary>
        private int m_ShellsRemaining;

        /// <summary>Verbleibende Frames in der aktuellen Shell-Reload-Phase.</summary>
        private int m_ShellPhaseFramesRemaining;

        /// <summary>Ob der Character gerade einen Alternativangriff ausfuehrt (fuer Animation + Netzwerk-Sync).</summary>
        private bool m_IsAltAttacking;

        /// <summary>Verbleibende AltAttack-Frames (frame-basiert, wie Attack).</summary>
        private int m_AltAttackFramesRemaining;

        /// <summary>Frame-Akkumulator fuer frame-diskretes AltAttack-Timing.</summary>
        private float m_AltAttackFrameAccumulator;

        /// <summary>Aktuelle AltAttack-Frame-Anzahl basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_AltAttackFrames;

        /// <summary>Aktuelle AltAttack-FPS basierend auf aktueller Waffe (aus WeaponDataLoader).</summary>
        private int m_AltAttackFps = 20;

        /// <summary>Ob die aktuelle Waffe einen Alternativangriff hat.</summary>
        private bool m_HasAltAttack;

        /// <summary>Ob der Alt-Attack unendliche Munition verbraucht (z.B. Bayonet).</summary>
        private bool m_AltAttackInfiniteAmmo;

        /// <summary>Ob der primaere Angriff ein Timer-Granaten-Cook ist (Button halten = kochen).</summary>
        private bool m_IsAttackGrenadeCook;

        /// <summary>Ob der Alt-Angriff ein Timer-Granaten-Cook ist (Button halten = kochen).</summary>
        private bool m_IsAltAttackGrenadeCook;

        // ===== Weapon Swap State =====

        /// <summary>Ob der Character gerade die Waffe wechselt (Drop/Raise).</summary>
        private bool m_IsSwapping;

        /// <summary>Aktuelle Phase beim Waffenwechsel.</summary>
        private WeaponSwapPhase m_SwapPhase;

        /// <summary>Verbleibende Frames in der aktuellen Swap-Phase.</summary>
        private int m_SwapFramesRemaining;

        /// <summary>Frame-Akkumulator fuer frame-diskretes Swap-Timing.</summary>
        private float m_SwapFrameAccumulator;

        /// <summary>FPS der aktuellen Swap-Phase.</summary>
        private int m_SwapFps = 10;

        /// <summary>Raise-Frame-Anzahl der Zielwaffe (gesetzt wenn OnWeaponChanged feuert).</summary>
        private int m_SwapRaiseFrames;

        /// <summary>Raise-FPS der Zielwaffe.</summary>
        private int m_SwapRaiseFps = 10;

        /// <summary>Animations-Name der Raise-Animation (z.B. "TORSO_RAISE") fuer Animator.Play.</summary>
        private string m_SwapRaiseAnimName;

        /// <summary>
        /// Client-seitiges Swap-Target: Wird bei jedem Scroll lokal berechnet.
        /// Spiegelt die Server-seitige CycleWeapon-Logik fuer sofortige HUD-Prediction.
        /// </summary>
        private string m_ClientSwapTarget;

        /// <summary>Ob bei leerem Magazin automatisch nachgeladen werden soll.</summary>
        [SerializeField]
        private bool m_AutoReload = true;

        /// <summary>
        /// Remote-Modus: Component laeuft auf Remote-Clients nur fuer Bone-Rotation,
        /// Input/Physik sind deaktiviert. Daten kommen aus NetworkVariable.
        /// </summary>
        private bool m_IsRemoteMode;

        // ===== Fire Mode =====

        /// <summary>
        /// Aktueller Feuermodus der Waffe ("single", "burst", "auto").
        /// Wird bei Waffenwechsel aus der WeaponDefinition initialisiert
        /// und per SwitchFireMode-Input zyklisch gewechselt.
        /// </summary>
        private string m_CurrentFireMode = "auto";

        /// <summary>
        /// Verfuegbare Feuermodi der aktuellen Waffe (z.B. ["single", "burst", "auto"]).
        /// Null oder leer wenn die Waffe keine wechselbaren Feuermodi hat.
        /// </summary>
        private System.Collections.Generic.List<string> m_AvailableFireModes;

        /// <summary>
        /// Index in m_AvailableFireModes fuer den aktuellen Modus.
        /// </summary>
        private int m_CurrentFireModeIndex;

        /// <summary>
        /// Laufende Attack-Sequenznummer fuer Animator Re-Trigger.
        /// Wird bei jedem neuen Angriff inkrementiert.
        /// </summary>
        private byte m_AttackSequence;

        /// <summary>
        /// Verbleibende Burst-Schuesse (nur im "burst"-Modus).
        /// </summary>
        private int m_BurstShotsRemaining;

        /// <summary>
        /// Ob der Attack-Button in diesem Frame erstmals gedrueckt wurde (fuer "single"-Modus).
        /// </summary>
        private bool m_AttackButtonWasPressed;

        /// <summary>
        /// Feuert wenn der Feuermodus gewechselt wird. Parameter: neuer Feuermodus-String.
        /// </summary>
        internal event Action<string> OnFireModeChanged;

        /// <summary>
        /// Aktueller Feuermodus (fuer HUD-Anzeige).
        /// </summary>
        internal string CurrentFireMode => m_CurrentFireMode;

        // ===== Weapon =====

        /// <summary>
        /// Feuert wenn der Client lokal von Drop- auf Raise-Phase wechselt.
        /// Wird fuer HUD-Prediction genutzt (SoF2: Waffenwechsel genau an Drop/Raise-Grenze).
        /// Parameter: Name der neuen Waffe.
        /// </summary>
        internal event Action<string> OnWeaponSwapRaiseStarted;

        /// <summary>
        /// Feuert bei jedem Scroll waehrend der Drop-Phase mit dem neuen Zielwaffen-Namen.
        /// HUD zeigt sofort die naechste Waffe an (SoF2-authentisch: Waffenname wechselt on click).
        /// </summary>
        internal event Action<string> OnWeaponSwapTargetChanged;

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
        /// Skeleton-Root-Bone: oberster Bone im SoF2-Skeleton.
        /// LocalPosition wird in LateUpdate auf Vector3.zero gesetzt,
        /// damit Animations-Keyframes das Mesh nicht vom Physik-Collider wegbewegen.
        /// </summary>
        private Transform m_SkeletonRoot;

        /// <summary>
        /// Model-Root-Bone: direktes Kind von skeleton_root.
        /// Einige Animationen haben Root-Motion-Daten auf diesem Bone.
        /// XZ-Position wird in LateUpdate auf Bind-Pose-Wert zurueckgesetzt.
        /// </summary>
        private Transform m_ModelRoot;

        /// <summary>
        /// Bind-Pose localPosition des pelvis-Bones bei Instantiierung.
        /// Wird als Referenz verwendet: in LateUpdate werden XZ auf diesen Wert
        /// zurueckgesetzt, damit Animations-Drift entfernt wird ohne den
        /// strukturellen Offset zu zerstoeren.
        /// </summary>
        private Vector3 m_PelvisBindLocalPos;

        /// <summary>
        /// Ob die Bind-Pose-Referenzwerte bereits erfasst wurden
        /// (nach erster Animator-Evaluation).
        /// </summary>
        private bool m_HasBindPoseReference;

        /// <summary>
        /// Linker Fuss-Bone fuer dynamische Y-Offset-Berechnung.
        /// </summary>
        private Transform m_LeftFoot;

        /// <summary>
        /// Rechter Fuss-Bone fuer dynamische Y-Offset-Berechnung.
        /// </summary>
        private Transform m_RightFoot;

        /// <summary>
        /// Transform des Visual-Prefab-Instanz (Root des Skins).
        /// Wird in LateUpdate vertikal korrigiert.
        /// </summary>
        private Transform m_VisualInstance;

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

        /// <summary>Crouch-Taste gedrueckt (C, Hold-to-Crouch).</summary>
        private bool m_IsCrouchPressed;

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

            // Weapon-Callbacks abmelden
            m_PlayerActions.NextWeapon.performed -= OnNextWeaponPerformed;
            m_PlayerActions.PreviousWeapon.performed -= OnPreviousWeaponPerformed;
            m_PlayerActions.ReloadWeapon.performed -= OnReloadPerformed;
            m_PlayerActions.SwitchFireMode.performed -= OnSwitchFireModePerformed;

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
            m_WeaponDataLoader = ServiceLocator.Get<WeaponDataLoader>();

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

            // Weapon-Cycling per Callback (SoF2: ScrollUp/Down zum Waffenwechsel)
            m_PlayerActions.NextWeapon.performed += OnNextWeaponPerformed;
            m_PlayerActions.PreviousWeapon.performed += OnPreviousWeaponPerformed;

            // Reload per Callback (R-Taste)
            m_PlayerActions.ReloadWeapon.performed += OnReloadPerformed;

            // Fire-Mode-Wechsel per Callback
            m_PlayerActions.SwitchFireMode.performed += OnSwitchFireModePerformed;

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

        /// <summary>
        /// Input-Callback fuer NextWeapon (performed).
        /// Startet den Waffenwechsel-Prozess (Drop aktuelle Waffe, dann Raise neue Waffe).
        /// SoF2: Kein Re-Switch waehrend laufendem Swap (weaponTime blockiert).
        /// </summary>
        private void OnNextWeaponPerformed(InputAction.CallbackContext context)
        {
            if (m_IsAttacking || m_IsAltAttacking || m_IsReloading || m_IsSwapping)
            {
                return;
            }

            StartClientWeaponSwap();
            CycleClientSwapTarget(1);
            m_CharacterState.RequestNextWeaponServerRpc();
        }

        /// <summary>
        /// Input-Callback fuer PreviousWeapon (performed).
        /// Startet den Waffenwechsel-Prozess (Drop aktuelle Waffe, dann Raise neue Waffe).
        /// SoF2: Kein Re-Switch waehrend laufendem Swap (weaponTime blockiert).
        /// </summary>
        private void OnPreviousWeaponPerformed(InputAction.CallbackContext context)
        {
            if (m_IsAttacking || m_IsAltAttacking || m_IsReloading || m_IsSwapping)
            {
                return;
            }

            StartClientWeaponSwap();
            CycleClientSwapTarget(-1);
            m_CharacterState.RequestPreviousWeaponServerRpc();
        }

        /// <summary>
        /// Input-Callback fuer ReloadWeapon (performed, R-Taste).
        /// Startet client-seitig den Reload wenn moeglich.
        /// </summary>
        private void OnReloadPerformed(InputAction.CallbackContext context)
        {
            TryStartReload();
        }

        /// <summary>
        /// Callback fuer SwitchFireMode-Input. Wechselt zyklisch durch die
        /// verfuegbaren Feuermodi der aktuellen Waffe (falls vorhanden).
        /// </summary>
        private void OnSwitchFireModePerformed(InputAction.CallbackContext context)
        {
            if (m_AvailableFireModes == null || m_AvailableFireModes.Count <= 1)
            {
                return;
            }

            m_CurrentFireModeIndex = (m_CurrentFireModeIndex + 1) % m_AvailableFireModes.Count;
            m_CurrentFireMode = m_AvailableFireModes[m_CurrentFireModeIndex];
            m_BurstShotsRemaining = 0;
            OnFireModeChanged?.Invoke(m_CurrentFireMode);
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
            m_IsCrouchPressed = m_PlayerActions.Crouch.IsPressed();

            // Hold-to-Jump: solange Jump gehalten wird, jeden Frame Jump-Request setzen
            //if (m_PlayerActions.Jump.IsPressed())
            //{
            //    m_JumpRequested = true;
            //}

            SyncCharacterRotation();
            HandleActionInput();

            // SoF2-Physik-Pipeline via Shared Simulation (Client-Side Prediction).
            RunPhysicsStep();

            // FootstepHandler-State aktualisieren (fuer timer-basierte Footstep-Ausloesung)
            if (m_FootstepHandler != null)
            {
                m_FootstepHandler.IsWalking = m_IsWalkingPressed;
                m_FootstepHandler.FallHeight = m_Simulation.FullFallHeight;
                m_FootstepHandler.IsGrounded = m_Simulation.IsGrounded;
                Vector3 vel = m_Simulation.Velocity;
                m_FootstepHandler.HorizontalSpeed = new Vector2(vel.x, vel.z).magnitude;
            }

            // Landing-Sound abspielen wenn Spieler gerade gelandet ist
            CheckLandingSound();

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
            if (m_IsCrouchPressed) buttons |= CommandButtons.Crouch;
            if (m_IsReloading) buttons |= CommandButtons.Reload;
            if (m_IsAltAttacking) buttons |= CommandButtons.AltAttack;

            PlayerCommand cmd = new()
            {
                MoveInput = m_MoveInput,
                YawAngle = m_YawTarget != null ? m_YawTarget.eulerAngles.y : transform.eulerAngles.y,
                PitchAngle = m_PitchTarget != null ? m_PitchTarget.eulerAngles.x : 0f,
                Buttons = buttons,
                DeltaTime = Time.deltaTime,
                SequenceNumber = m_NextSequenceNumber++,
            };
            m_JumpRequested = false;

            // Eigenen Collider deaktivieren damit BoxCast sich nicht selbst trifft
            BoxCollider ownCollider = m_ColliderSystem != null ? m_ColliderSystem.PhysicsCollider : null;
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

            // Crouch-Collider-Update: Simulation hat IsCrouching gesetzt (in PM_CheckDuck),
            // ClientColliderSystem aktualisieren damit der visuelle + physische Collider passt
            if (m_ColliderSystem != null)
            {
                m_ColliderSystem.UpdateCapsuleSizeForState(m_Simulation.IsCrouching);
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
            m_Simulation.SetState(ack.Velocity, ack.IsGrounded, ack.IsJumping, ack.IsCrouching,
                                  ack.KnockbackTime);
            Vector3 replayPosition = ack.Position;

            // Eigenen Collider deaktivieren damit ResolvePenetration's OverlapBox
            // sich nicht selbst trifft (gleiche Pattern wie RunPhysicsStep)
            BoxCollider ownCollider = m_ColliderSystem != null ? m_ColliderSystem.PhysicsCollider : null;
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

            ResetRootBonePositions();
            AdjustVisualYOffset();
            UpdatePelvisRotation();
            UpdateLumbarRotation();
        }

        /// <summary>
        /// Korrigiert die Y-Position des Visual-Prefabs sodass die Fuesse am Boden (Y=0) stehen.
        /// Berechnet den niedrigsten Fuss-Bone in lokalem Raum des Player-Transforms
        /// und verschiebt das Visual entsprechend nach oben.
        /// Funktioniert dynamisch fuer Standing und Crouching.
        /// </summary>
        private void AdjustVisualYOffset()
        {
            if (m_VisualInstance == null || (m_LeftFoot == null && m_RightFoot == null))
            {
                return;
            }

            float lowestFootY = float.MaxValue;
            if (m_LeftFoot != null)
            {
                float y = transform.InverseTransformPoint(m_LeftFoot.position).y;
                lowestFootY = Mathf.Min(lowestFootY, y);
            }
            if (m_RightFoot != null)
            {
                float y = transform.InverseTransformPoint(m_RightFoot.position).y;
                lowestFootY = Mathf.Min(lowestFootY, y);
            }

            // Verschiebe Visual nach oben sodass Fuesse bei Y=0 (Boden) landen
            Vector3 visPos = m_VisualInstance.localPosition;
            m_VisualInstance.localPosition = new Vector3(visPos.x, visPos.y - lowestFootY, visPos.z);
        }

        /// <summary>
        /// Setzt die lokale XZ-Position der Root-Bones und des Pelvis auf Null.
        /// SoF2-Animationen enthalten Positions-Keyframes auf dem pelvis-Bone
        /// die das Mesh vom Physik-Collider wegbewegen. Da Bewegung komplett
        /// ueber PlayerPhysicsSimulation laeuft (applyRootMotion = false),
        /// muss die XZ-Position nach Animator-Evaluation korrigiert werden.
        /// Y bleibt erhalten (Crouch-Hoehe etc.).
        /// </summary>
        private void ResetRootBonePositions()
        {
            if (m_SkeletonRoot != null)
            {
                Vector3 skPos = m_SkeletonRoot.localPosition;
                m_SkeletonRoot.localPosition = new Vector3(0f, skPos.y, 0f);
            }

            if (m_ModelRoot != null)
            {
                Vector3 mrPos = m_ModelRoot.localPosition;
                m_ModelRoot.localPosition = new Vector3(0f, mrPos.y, 0f);
            }

            if (m_PelvisTarget == null)
            {
                return;
            }

            // Erste Animator-Evaluation: Bind-Pose-Referenz erfassen.
            // Erst jetzt sind die Bone-Positionen korrekt (nicht T-Pose).
            if (!m_HasBindPoseReference)
            {
                m_PelvisBindLocalPos = m_PelvisTarget.localPosition;
                m_HasBindPoseReference = true;
                return;
            }

            // Pelvis XZ auf Bind-Pose-Wert zuruecksetzen, Y vom Animator behalten.
            // Der strukturelle Offset (z.B. X=0.45) bleibt erhalten,
            // nur Animations-Drift (Walk/Crouch-Verschiebung) wird entfernt.
            Vector3 pelPos = m_PelvisTarget.localPosition;
            m_PelvisTarget.localPosition = new Vector3(
                m_PelvisBindLocalPos.x,
                pelPos.y,
                m_PelvisBindLocalPos.z
            );
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
            m_SkeletonRoot = FindDeepChild(visualInstance.transform, "skeleton_root");
            m_ModelRoot = FindDeepChild(visualInstance.transform, "model_root");
            m_LeftFoot = FindDeepChild(visualInstance.transform, "ltarsal");
            m_RightFoot = FindDeepChild(visualInstance.transform, "rtarsal");
            m_VisualInstance = visualInstance.transform;
            m_HasBindPoseReference = false;

            // FootstepHandler auf dem Animator-GO initialisieren (AnimationEvents feuern dort)
            Animator visualAnimator = visualInstance.GetComponentInChildren<Animator>();
            if (visualAnimator != null)
            {
                m_FootstepHandler = visualAnimator.gameObject.GetComponent<ClientFootstepHandler>();
                if (m_FootstepHandler == null)
                {
                    m_FootstepHandler = visualAnimator.gameObject.AddComponent<ClientFootstepHandler>();
                }
                m_FootstepHandler.Initialize(transform);
            }

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
                    m_ColliderSystem.GetCurrentCapsuleCenter()
                );

                // Capsule-Dimensionen an Server senden (fÃ¼r Server-Side Simulation)
                m_NetworkedPlayerCharacter.SendCapsuleDimensions(
                    m_ColliderSystem.GetCurrentCapsuleHeight(),
                    m_ColliderSystem.GetCurrentCapsuleRadius(),
                    m_ColliderSystem.GetCurrentCapsuleCenter()
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
        /// Aktionen (Attack, AltAttack, Reload) client-seitig verarbeiten.
        /// Fire-Mode-abhaengig: "auto" = Dauerfeuer bei gehaltenem Button,
        /// "single" = ein Schuss pro Tastendruck, "burst" = 3 Schuesse pro Tastendruck.
        /// </summary>
        private void HandleActionInput()
        {
            // Waffenwechsel-Frames herunterzaehlen (Drop → Raise)
            if (m_IsSwapping)
            {
                TickClientWeaponSwap();
                return;
            }

            // Attack-Frames herunterzaehlen (frame-diskret, wie SoF2 Animation-Frames)
            if (m_IsAttacking)
            {
                m_AttackFrameAccumulator += Time.deltaTime;
                float frameInterval = 1f / m_AttackFps;
                while (m_AttackFrameAccumulator >= frameInterval && m_AttackFramesRemaining > 0)
                {
                    m_AttackFrameAccumulator -= frameInterval;
                    m_AttackFramesRemaining--;
                }

                if (m_AttackFramesRemaining <= 0)
                {
                    // Fire-Mode-abhaengiges Re-Trigger bei gehaltener Maustaste
                    bool hasAmmo = m_CurrentWeaponInfiniteAmmo || m_CharacterState.CurrentClipAmmo > 0;
                    bool shouldRetrigger = false;

                    if (m_CurrentFireMode == "auto" && m_PlayerActions.Attack.IsPressed() && hasAmmo)
                    {
                        shouldRetrigger = true;
                    }
                    else if (m_CurrentFireMode == "burst" && m_BurstShotsRemaining > 0 && hasAmmo)
                    {
                        m_BurstShotsRemaining--;
                        shouldRetrigger = true;
                    }

                    if (shouldRetrigger)
                    {
                        // Sofort neuen Angriff starten (kein Frame mit IsAttacking=false)
                        m_AttackFramesRemaining = m_AttackFrames;
                        m_AttackFrameAccumulator = 0f;
                        m_AttackSequence++;
                    }
                    else if (m_IsAttackGrenadeCook && m_PlayerActions.Attack.IsPressed())
                    {
                        // Granaten-Cook: Button weiterhin als gehalten melden solange physisch gedrueckt
                        // Server-seitiger TickServerGrenadeCook prueft HasButton(Attack) fuer Wurf-/Explosions-Timing
                    }
                    else
                    {
                        m_IsAttacking = false;
                        m_AttackFrameAccumulator = 0f;

                        // Auto-Reload nach letztem Schuss wenn Magazin leer
                        if (m_AutoReload && !hasAmmo)
                        {
                            TryStartReload();
                        }
                    }
                }
            }

            // AltAttack-Frames herunterzaehlen (frame-diskret, wie Attack)
            if (m_IsAltAttacking)
            {
                m_AltAttackFrameAccumulator += Time.deltaTime;
                float altFrameInterval = 1f / m_AltAttackFps;
                while (m_AltAttackFrameAccumulator >= altFrameInterval && m_AltAttackFramesRemaining > 0)
                {
                    m_AltAttackFrameAccumulator -= altFrameInterval;
                    m_AltAttackFramesRemaining--;
                }

                if (m_AltAttackFramesRemaining <= 0)
                {
                    if (m_IsAltAttackGrenadeCook && m_PlayerActions.SecondAttack.IsPressed())
                    {
                        // Granaten-Cook (Alt): Button weiterhin als gehalten melden
                    }
                    else
                    {
                        m_IsAltAttacking = false;
                        m_AltAttackFrameAccumulator = 0f;
                    }
                }
            }

            // Reload-Frames herunterzaehlen (frame-diskret, wie Attack)
            if (m_IsReloading)
            {
                if (m_IsShellReload && m_ShellReloadPhase != ShellReloadPhase.None)
                {
                    TickClientShellReload();
                }
                else
                {
                    m_ReloadFrameAccumulator += Time.deltaTime;
                    float reloadFrameInterval = 1f / m_ReloadFps;
                    while (m_ReloadFrameAccumulator >= reloadFrameInterval && m_ReloadFramesRemaining > 0)
                    {
                        m_ReloadFrameAccumulator -= reloadFrameInterval;
                        m_ReloadFramesRemaining--;
                    }

                    if (m_ReloadFramesRemaining <= 0)
                    {
                        m_IsReloading = false;
                        m_ReloadFrameAccumulator = 0f;
                    }
                }

                // Waehrend Reload kein Attack/AltAttack moeglich
                return;
            }

            // Keine neue Aktion starten wenn eine laeuft
            bool noActionRunning = !m_IsAttacking && !m_IsAltAttacking && !m_IsReloading;

            // Fire-Mode-abhaengiger Attack-Start
            bool hasStartAmmo = m_CurrentWeaponInfiniteAmmo || m_CharacterState.CurrentClipAmmo > 0;
            bool attackPressed = m_PlayerActions.Attack.IsPressed();
            bool canStartAttack = false;

            if (m_CurrentFireMode == "auto")
            {
                // Auto: Dauerfeuer solange gehalten
                canStartAttack = attackPressed;
            }
            else if (m_CurrentFireMode == "single")
            {
                // Single: nur bei frischer Tastenbetaetigung (nicht gehalten)
                canStartAttack = attackPressed && !m_AttackButtonWasPressed;
            }
            else if (m_CurrentFireMode == "burst")
            {
                // Burst: bei frischer Tastenbetaetigung starten, dann 2 weitere automatisch
                if (attackPressed && !m_AttackButtonWasPressed)
                {
                    canStartAttack = true;
                    m_BurstShotsRemaining = 2;
                }
            }

            // Rising-Edge-Tracking fuer "single" und "burst"
            m_AttackButtonWasPressed = attackPressed;

            if (canStartAttack && noActionRunning && hasStartAmmo)
            {
                m_IsAttacking = true;
                m_AttackFramesRemaining = m_AttackFrames;
                m_AttackFrameAccumulator = 0f;
                m_AttackSequence++;
            }
            else if (canStartAttack && noActionRunning && !hasStartAmmo)
            {
                // Leer: Attack-Button fuer einen Frame senden → Server spielt Empty-Sound
                m_IsAttacking = true;
                m_AttackFramesRemaining = 1;
                m_AttackFrameAccumulator = 0f;

                // Auto-Reload direkt anstossen (startet nach dem 1-Frame Empty-Click)
                if (m_AutoReload)
                {
                    TryStartReload();
                }
            }

            // AltAttack (Rechtsklick): SecondAttack Input
            bool hasAltAmmo = HasAltAmmo();
            if (m_HasAltAttack && m_PlayerActions.SecondAttack.IsPressed() && noActionRunning && hasAltAmmo)
            {
                m_IsAltAttacking = true;
                m_AltAttackFramesRemaining = m_AltAttackFrames;
                m_AltAttackFrameAccumulator = 0f;
            }
            else if (m_HasAltAttack && m_PlayerActions.SecondAttack.IsPressed() && noActionRunning && !hasAltAmmo)
            {
                // Leer: AltAttack-Button fuer einen Frame senden → Server spielt Empty-Sound
                m_IsAltAttacking = true;
                m_AltAttackFramesRemaining = 1;
                m_AltAttackFrameAccumulator = 0f;
            }
        }

        /// <summary>
        /// Prueft client-seitig ob Alt-Attack Munition vorhanden ist.
        /// Melee-AltAttack (Bayonet): immer true. Separate Alt-Ammo: AltClip > 0.
        /// Projektil ohne eigene Ammo (Knife-Throw): Reserve > 0.
        /// </summary>
        private bool HasAltAmmo()
        {
            if (m_AltAttackInfiniteAmmo)
            {
                return true;
            }

            WeaponDefinition weapon = m_WeaponDataLoader?.GetById(m_CharacterState.CurrentWeaponName);

            if (weapon?.AltAttack == null)
            {
                return false;
            }

            // Separate Alt-Ammo (M4 M203): AltClip pruefen
            if (weapon.AltAttack.Ammo != null)
            {
                return m_CharacterState.AltClipAmmo > 0;
            }

            // Projektil ohne eigene Ammo (Knife-Throw): Weapon-Reserve pruefen
            if (weapon.AltAttack.Projectile != null)
            {
                return m_CharacterState.ReserveAmmo > 0;
            }

            return true;
        }

        /// <summary>
        /// Versucht client-seitig einen Reload zu starten.
        /// Prueft: kein Attack/Reload aktiv, nicht infinite, Clip nicht voll, Reserve > 0.
        /// </summary>
        private void TryStartReload()
        {
            if (m_IsAttacking || m_IsReloading || m_IsAltAttacking)
            {
                return;
            }

            if (m_CurrentWeaponInfiniteAmmo)
            {
                return;
            }

            // Pruefe ob Clip voll oder Reserve leer (client-seitige Prediction)
            WeaponDefinition weapon = m_WeaponDataLoader?.GetById(m_CharacterState.CurrentWeaponName);
            if (weapon?.Ammo == null)
            {
                return;
            }

            if (m_CharacterState.CurrentClipAmmo >= weapon.Ammo.MaxClip)
            {
                return;
            }

            if (m_CharacterState.ReserveAmmo <= 0)
            {
                return;
            }

            if (m_ReloadFrames <= 0)
            {
                return;
            }

            m_IsReloading = true;
            m_ReloadFrameAccumulator = 0f;

            if (m_IsShellReload)
            {
                int shellsNeeded = weapon.Ammo.MaxClip - m_CharacterState.CurrentClipAmmo;
                m_ShellsRemaining = Mathf.Min(shellsNeeded, m_CharacterState.ReserveAmmo);
                m_ShellReloadPhase = ShellReloadPhase.Start;
                m_ShellPhaseFramesRemaining = m_ReloadStartFrames;
                m_ReloadFramesRemaining = 1;
            }
            else
            {
                m_ShellReloadPhase = ShellReloadPhase.None;
                m_ReloadFramesRemaining = m_ReloadFrames;
            }
        }

        /// <summary>
        /// Client: Tickt den Shell-by-Shell Reload phasenweise (Prediction).
        /// Start → Shell (wiederholt) → End → fertig. Ammo-Transfer kommt vom Server via NetworkVariable.
        /// </summary>
        private void TickClientShellReload()
        {
            m_ReloadFrameAccumulator += Time.deltaTime;
            float frameInterval = 1f / m_ReloadFps;

            while (m_ReloadFrameAccumulator >= frameInterval && m_ShellPhaseFramesRemaining > 0)
            {
                m_ReloadFrameAccumulator -= frameInterval;
                m_ShellPhaseFramesRemaining--;
            }

            if (m_ShellPhaseFramesRemaining > 0)
            {
                return;
            }

            switch (m_ShellReloadPhase)
            {
                case ShellReloadPhase.Start:
                    m_ShellReloadPhase = ShellReloadPhase.Shell;
                    m_ShellPhaseFramesRemaining = m_ReloadShellFrames;
                    break;

                case ShellReloadPhase.Shell:
                    m_ShellsRemaining--;

                    if (m_ShellsRemaining > 0)
                    {
                        m_ShellPhaseFramesRemaining = m_ReloadShellFrames;
                    }
                    else
                    {
                        m_ShellReloadPhase = ShellReloadPhase.End;
                        m_ShellPhaseFramesRemaining = m_ReloadEndFrames;
                    }
                    break;

                case ShellReloadPhase.End:
                    m_ShellReloadPhase = ShellReloadPhase.None;
                    m_ReloadFramesRemaining = 0;
                    m_ReloadFrameAccumulator = 0f;
                    m_IsReloading = false;
                    break;
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
                m_IsSwapping = false;
                m_SwapPhase = WeaponSwapPhase.None;
                m_ClientSwapTarget = null;
                return;
            }

            UpdateClientAttackParameters(weaponName);

            if (m_IsSwapping)
            {
                // Waffe merken aber noch nicht laden (Drop laeuft oder gerade fertig)
                m_PendingWeaponName = weaponName;

                // Raise-Daten der neuen Waffe lesen
                if (m_WeaponDataLoader != null)
                {
                    WeaponDefinition weapon = m_WeaponDataLoader.GetById(weaponName);
                    if (weapon?.Animations != null &&
                        weapon.Animations.TryGetValue("mp_raise", out WeaponAnimationEntry raiseAnim))
                    {
                        m_SwapRaiseFrames = raiseAnim.Duration;
                        m_SwapRaiseFps = raiseAnim.Fps;
                        m_SwapRaiseAnimName = raiseAnim.Name;
                    }
                }

                // Falls Drop bereits abgeschlossen: sofort zur Raise-Phase wechseln
                if (m_SwapPhase == WeaponSwapPhase.Drop && m_SwapFramesRemaining <= 0)
                {
                    TransitionToRaisePhase();
                }

                return;
            }

            // Normaler Waffenwechsel (z.B. beim Spawn oder Server-initiiert ohne Player-Input)
            m_PendingWeaponName = weaponName;
            TryLoadPendingWeapon();
        }

        /// <summary>
        /// Aktualisiert m_AttackFrames und m_AttackFps anhand der mp_attack Animation
        /// der aktuellen Waffe aus dem WeaponDataLoader.
        /// </summary>
        private void UpdateClientAttackParameters(string weaponName)
        {
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            if (loader == null)
            {
                return;
            }

            WeaponDefinition weapon = loader.GetById(weaponName);
            if (weapon == null)
            {
                return;
            }

            if (weapon.Animations != null)
            {
                if (weapon.Animations.TryGetValue("mp_attack", out WeaponAnimationEntry attackAnim))
                {
                    m_AttackFrames = attackAnim.Duration;
                    m_AttackFps = attackAnim.Fps;
                }

                if (weapon.Animations.TryGetValue("mp_reload", out WeaponAnimationEntry reloadAnim))
                {
                    m_ReloadFrames = reloadAnim.Duration;
                    m_ReloadFps = reloadAnim.Fps;
                    m_IsShellReload = false;
                }
                else if (weapon.Animations.TryGetValue("mp_reloadStart", out WeaponAnimationEntry startAnim) &&
                         weapon.Animations.TryGetValue("mp_reloadShell", out WeaponAnimationEntry shellAnim) &&
                         weapon.Animations.TryGetValue("mp_reloadEnd", out WeaponAnimationEntry endAnim))
                {
                    m_ReloadStartFrames = startAnim.Duration;
                    m_ReloadShellFrames = shellAnim.Duration;
                    m_ReloadEndFrames = endAnim.Duration;
                    m_ReloadFps = startAnim.Fps;
                    m_IsShellReload = true;
                    m_ReloadFrames = 1;
                }
                else
                {
                    m_ReloadFrames = 0;
                    m_ReloadFps = 20;
                    m_IsShellReload = false;
                }

                if (weapon.Animations.TryGetValue("mp_altAttack", out WeaponAnimationEntry altAttackAnim))
                {
                    m_AltAttackFrames = altAttackAnim.Duration;
                    m_AltAttackFps = altAttackAnim.Fps;
                }
                else
                {
                    m_AltAttackFrames = 0;
                    m_AltAttackFps = 20;
                }
            }

            m_CurrentWeaponInfiniteAmmo = weapon.Ammo != null && weapon.Ammo.Infinite;

            // AltAttack-Verfuegbarkeit pruefen
            m_HasAltAttack = weapon.AltAttack != null;
            m_AltAttackInfiniteAmmo = weapon.AltAttack != null && !string.IsNullOrEmpty(weapon.AltAttack.Melee);

            // Timer-Granaten: Client muss Attack-Button-Flag halten solange physischer Button gedrueckt
            m_IsAttackGrenadeCook = weapon.Attack?.Projectile?.Detonation == "timer";
            m_IsAltAttackGrenadeCook = weapon.AltAttack?.Projectile?.Detonation == "timer";

            // Laufende Aktionen abbrechen bei Waffenwechsel
            m_IsReloading = false;
            m_ReloadFramesRemaining = 0;
            m_ReloadFrameAccumulator = 0f;
            m_ShellReloadPhase = ShellReloadPhase.None;
            m_ShellsRemaining = 0;
            m_ShellPhaseFramesRemaining = 0;
            m_IsAltAttacking = false;
            m_AltAttackFramesRemaining = 0;
            m_AltAttackFrameAccumulator = 0f;

            // Fire-Mode aus Waffen-Definition initialisieren
            m_AvailableFireModes = weapon.Attack?.FireModes;
            string defaultFireMode = weapon.Attack?.FireMode ?? "auto";

            // Wenn der aktuelle Modus in den verfuegbaren Modi enthalten ist, beibehalten
            // (damit beim Waffenwechsel zurueck der letzte Modus erhalten bleibt).
            // Sonst auf den Default-Modus der Waffe setzen.
            if (m_AvailableFireModes != null && m_AvailableFireModes.Count > 0)
            {
                int existingIndex = m_AvailableFireModes.IndexOf(m_CurrentFireMode);
                if (existingIndex >= 0)
                {
                    m_CurrentFireModeIndex = existingIndex;
                }
                else
                {
                    m_CurrentFireModeIndex = m_AvailableFireModes.IndexOf(defaultFireMode);
                    if (m_CurrentFireModeIndex < 0) m_CurrentFireModeIndex = 0;
                    m_CurrentFireMode = m_AvailableFireModes[m_CurrentFireModeIndex];
                }
            }
            else
            {
                m_CurrentFireMode = defaultFireMode;
                m_CurrentFireModeIndex = 0;
            }

            m_BurstShotsRemaining = 0;
            m_AttackButtonWasPressed = false;
            OnFireModeChanged?.Invoke(m_CurrentFireMode);
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

        // ===== Weapon Swap (Drop/Raise) =====

        /// <summary>
        /// Client: Berechnet das naechste Swap-Target lokal (spiegelt Server CycleWeapon).
        /// Feuert OnWeaponSwapTargetChanged fuer sofortiges HUD-Update.
        /// </summary>
        private void CycleClientSwapTarget(int direction)
        {
            int count = m_CharacterState.WeaponCount;
            if (count <= 1)
            {
                return;
            }

            // Von letztem Client-Target weiter cyclen (wie Server m_PendingSwapTarget)
            string baseWeapon = !string.IsNullOrEmpty(m_ClientSwapTarget)
                ? m_ClientSwapTarget
                : m_CharacterState.CurrentWeaponName;

            int currentIndex = -1;
            for (int i = 0; i < count; i++)
            {
                if (string.Equals(m_CharacterState.GetWeaponAt(i), baseWeapon, StringComparison.Ordinal))
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = (currentIndex + direction + count) % count;
            m_ClientSwapTarget = m_CharacterState.GetWeaponAt(nextIndex);

            OnWeaponSwapTargetChanged?.Invoke(m_ClientSwapTarget);
        }

        /// <summary>
        /// Startet die client-seitige Drop-Phase des Waffenwechsels.
        /// Liest die mp_drop Animationsdaten der aktuellen Waffe.
        /// SoF2 pm_shared.c: weaponTime blockiert ALLE Aktionen waehrend Drop+Raise.
        /// Kein Re-Switch moeglich bis Raise abgeschlossen ist.
        /// </summary>
        private void StartClientWeaponSwap()
        {
            // SoF2: Kein neuer Waffenwechsel waehrend laufendem Swap (Drop oder Raise).
            // pm_shared.c: weaponTime > 0 blockiert PM_BeginWeaponChange komplett.
            if (m_IsSwapping)
            {
                return;
            }

            int dropFrames = 6;
            int dropFps = 10;
            string dropAnimName = "TORSO_DROP";

            if (m_WeaponDataLoader != null)
            {
                WeaponDefinition weapon = m_WeaponDataLoader.GetById(m_CharacterState.CurrentWeaponName);
                if (weapon?.Animations != null &&
                    weapon.Animations.TryGetValue("mp_drop", out WeaponAnimationEntry dropAnim))
                {
                    dropFrames = dropAnim.Duration;
                    dropFps = dropAnim.Fps;
                    dropAnimName = dropAnim.Name;
                }
            }

            m_IsSwapping = true;
            m_SwapPhase = WeaponSwapPhase.Drop;
            m_SwapFramesRemaining = dropFrames;
            m_SwapFrameAccumulator = 0f;
            m_SwapFps = dropFps;
            m_SwapRaiseFrames = 0;
            m_SwapRaiseFps = 10;
            m_SwapRaiseAnimName = null;
            m_PendingWeaponName = null;

            // Animation auf Torso-Layer ab Frame 0 erzwingen
            int dropStateHash = NetworkedPlayerCharacter.GetDropStateHash(dropAnimName);
            m_NetworkedPlayerCharacter.ForcePlaySwapState(dropStateHash, dropFrames, dropFps);
        }

        /// <summary>
        /// Client: Zaehlt Swap-Frames herunter und wechselt die Phase (Drop → Raise → Idle).
        /// SoF2: Kein Re-Switch waehrend Swap — Drop und Raise laufen komplett durch.
        /// </summary>
        private void TickClientWeaponSwap()
        {
            m_SwapFrameAccumulator += Time.deltaTime;
            float frameInterval = 1f / m_SwapFps;
            while (m_SwapFrameAccumulator >= frameInterval && m_SwapFramesRemaining > 0)
            {
                m_SwapFrameAccumulator -= frameInterval;
                m_SwapFramesRemaining--;
            }

            if (m_SwapFramesRemaining <= 0)
            {
                if (m_SwapPhase == WeaponSwapPhase.Drop)
                {
                    TransitionToRaisePhase();
                }
                else if (m_SwapPhase == WeaponSwapPhase.Raise)
                {
                    m_IsSwapping = false;
                    m_SwapPhase = WeaponSwapPhase.None;
                    m_ClientSwapTarget = null;
                }
            }
        }

        /// <summary>
        /// Client: Wechselt von Drop- zur Raise-Phase. Laedt das neue Waffen-Visual.
        /// Falls die neue Waffe vom Server noch nicht empfangen wurde, wird gewartet.
        /// </summary>
        private void TransitionToRaisePhase()
        {
            // Waffe laden (falls OnWeaponChanged bereits empfangen)
            if (!string.IsNullOrEmpty(m_PendingWeaponName))
            {
                TryLoadPendingWeapon();
            }

            // Raise-Daten muessen gesetzt sein (durch OnWeaponChanged)
            if (m_SwapRaiseFrames <= 0)
            {
                return;
            }

            m_SwapPhase = WeaponSwapPhase.Raise;
            m_SwapFramesRemaining = m_SwapRaiseFrames;
            m_SwapFrameAccumulator = 0f;
            m_SwapFps = m_SwapRaiseFps;

            // Raise-Animation auf Torso-Layer ab Frame 0 erzwingen
            if (!string.IsNullOrEmpty(m_SwapRaiseAnimName))
            {
                int raiseStateHash = NetworkedPlayerCharacter.GetRaiseStateHash(m_SwapRaiseAnimName);
                m_NetworkedPlayerCharacter.ForcePlaySwapState(raiseStateHash, m_SwapRaiseFrames, m_SwapRaiseFps);
            }

            // HUD sofort aktualisieren (Client Prediction wie SoF2 PM_FinishWeaponChange)
            if (!string.IsNullOrEmpty(m_PendingWeaponName))
            {
                OnWeaponSwapRaiseStarted?.Invoke(m_PendingWeaponName);
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

            // Beim Sprung-Spam landet und springt der Spieler im selben Frame.
            // IsGrounded ist am Frame-Ende false, aber JustLanded sagt der Animation
            // "wir haben den Boden beruehrt" → Jump→Idle Transition feuert korrekt.
            bool animGrounded = m_Simulation.IsGrounded || m_Simulation.JustLanded;

            NetworkAnimationState state = new()
            {
                Speed = speed,
                Horizontal = m_AnimHorizontal,
                Vertical = m_AnimVertical,
                IsMoving = isMoving,
                IsGrounded = animGrounded,
                IsWalking = isWalking,
                IsAttacking = m_IsAttacking,
                IsCrouching = m_Simulation.IsCrouching,
                IsReloading = m_IsReloading,
                IsAltAttacking = m_IsAltAttacking,
                IsSwapping = m_IsSwapping,
                MoveInputX = moveInput.x,
                MoveInputY = moveInput.y,
                PitchAngle = m_PitchTarget != null ? m_PitchTarget.eulerAngles.x : 0f,
                CurrentWeapon = (byte)NetworkedCharacterState.GetWeaponAnimatorIndex(m_CharacterState.CurrentWeaponName),
                Ammo = (short)m_CharacterState.CurrentClipAmmo,
                AttackSequence = m_AttackSequence,
            };

            // In NetworkVariable schreiben + lokal auf Animator anwenden
            m_NetworkedPlayerCharacter.WriteAnimationState(state);
        }

        // ===== Footstep / Landing Sounds =====

        /// <summary>
        /// AnimationEvent-Stub: Footstep — verhindert "no receiver" Warnungen
        /// falls Events zum Root-GO propagieren. Tatsaechliche Logik in ClientFootstepHandler.
        /// </summary>
        private void OnFootstep(AnimationEvent animationEvent) { }

        /// <summary>
        /// AnimationEvent-Stub: Land — verhindert "no receiver" Warnungen
        /// falls Events zum Root-GO propagieren. Tatsaechliche Logik in CheckLandingSound.
        /// </summary>
        private void OnLand(AnimationEvent animationEvent) { }

        /// <summary>
        /// Prueft ob der Spieler gerade gelandet ist und spielt den passenden Landing-Sound.
        /// SoF2 unterscheidet: "land" (leicht), "land_pain" (mittel, Fallhoehe > 3m), "land_death" (toedlich, > 10m).
        /// Nutzt FullFallHeight (wird vor Reset gespeichert, CurrentFallHeight ist bereits 0).
        /// </summary>
        private void CheckLandingSound()
        {
            if (!m_Simulation.JustLanded || m_FootstepHandler == null)
            {
                return;
            }

            float fallHeight = m_Simulation.FullFallHeight;

            // SoF2 PM_CrashLand: delta = vel² × 0.0001; delta < 1 → kein Event.
            // Entspricht Fallhoehe < ~0.16m (Stufen, kleine Unebenheiten → kein Sound).
            if (fallHeight < k_MinFallHeightForLandingSound)
            {
                return;
            }

            if (fallHeight > 10f)
            {
                m_FootstepHandler.PlayLanding("land_death");
            }
            else if (fallHeight > 3f)
            {
                m_FootstepHandler.PlayLanding("land_pain");
            }
            else
            {
                m_FootstepHandler.PlayLanding("land");
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

        /// <summary>
        /// Setzt die Client-Simulation komplett zurück (Velocity, GroundState, etc.).
        /// Wird vom Server via CorrectionClientRpc aufgerufen, um nach Respawn/Teleport
        /// alte Fall-Velocity zu verwerfen.
        /// </summary>
        public void ResetSimulationForRespawn()
        {
            m_Simulation.SetState(Vector3.zero, true, false, false, 0f);
        }

        /// <summary>
        /// Wendet SoF2 kickAngles als View-Punch auf die Kamera an.
        /// Wird vom Server via ClientRpc aufgerufen (nur Owner).
        /// </summary>
        public void ApplyKickAngles(float pitchKick, float yawKick)
        {
            m_AimCameraController?.AddViewPunch(pitchKick, yawKick);
        }
    }
}
