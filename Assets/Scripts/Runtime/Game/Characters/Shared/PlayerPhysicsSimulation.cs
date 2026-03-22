using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// SoF2/Quake3-Style Physik-Simulation.
    /// Serialisierbare C# Klasse (kein MonoBehaviour) — wird von Client (Prediction)
    /// und Server (Authority) mit identischem Code ausgeführt.
    /// Exakter Port der SoF2 bg_pmove.c / bg_slidemove.c Physik-Pipeline.
    /// Alle linearen Werte sind ×0.0254 skaliert (1 SoF2-Unit = 1 Inch = 0.0254m).
    /// </summary>
    [Serializable]
    public class PlayerPhysicsSimulation
    {
        // ===== Physics Parameters (SoF2 Defaults, ×0.0254 Inches→Meter) =====

        /// <summary>Boden-Beschleunigung (SoF2 pm_accelerate). Dimensionslos.</summary>
        public float PmAccelerate = 6.0f;

        /// <summary>Luft-Beschleunigung (SoF2 pm_airaccelerate). Dimensionslos.</summary>
        public float PmAirAccelerate = 1.0f;

        /// <summary>Boden-Reibung (SoF2 pm_friction). Dimensionslos.</summary>
        public float PmFriction = 6.0f;

        /// <summary>Stop-Speed Schwelle (SoF2: 100 × 0.0254 = 2.54 m/s).</summary>
        public float PmStopSpeed = 2.54f;

        /// <summary>Maximum Wish-Speed / g_speed (SoF2: 280 × 0.0254 = 7.112 m/s).</summary>
        public float PmMaxSpeed = 7.112f;

        /// <summary>Gravitation in m/s² (SoF2: 800 × 0.0254 = 20.32).</summary>
        public float PmGravity = 20.32f;

        /// <summary>Sofortige Y-Velocity beim Sprung (SoF2: 270 × 0.0254 = 6.858 m/s).</summary>
        public float JumpVelocity = 6.858f;

        /// <summary>Maximale Steigung (Dot mit Up). SoF2 MIN_WALK_NORMAL = 0.7. Dimensionslos.</summary>
        public float PmMaxSteepness = 0.7f;

        /// <summary>Step-Size für Step-Up (SoF2 STEPSIZE: 18 × 0.0254 = 0.4572m).</summary>
        public float PmStepSize = 0.4572f;

        /// <summary>Maximale Barriere-Hoehe (SoF2: 32 × 0.0254 = 0.8128m).</summary>
        public float PmMaxBarrier = 0.8128f;

        /// <summary>Duck Speed Scale (SoF2 PM_DUCKSCALE = 0.25).</summary>
        public float PmDuckScale = 0.25f;

        /// <summary>Walk Speed Scale (SoF2 BUTTON_WALKING halves speed in PM_CmdScale).</summary>
        public float PmWalkScale = 0.5f;

        /// <summary>SoF2 Standing-Hoehe: 89 Units (-46 bis 43) × 0.0254 m/unit.</summary>
        public float StandingHeight = 2.2606f;

        /// <summary>SoF2 Crouching-Hoehe: 64 Units (-46 bis 18) × 0.0254 m/unit.</summary>
        public float CrouchingHeight = 1.6256f;

        /// <summary>Jump-Debounce nach harter Landung (SoF2: 250ms).</summary>
        public float JumpDebounceAfterMs = 0.25f;

        /// <summary>LayerMask für Ground-Detection.</summary>
        public LayerMask GroundMask = ~0;

        // ===== Capsule Dimensions (Runtime, nicht serialisiert) =====

        /// <summary>Capsule-Hoehe (wird vom Client nach Bone-Berechnung gesetzt).</summary>
        [NonSerialized] public float CapsuleHeight;

        /// <summary>Capsule-Radius.</summary>
        [NonSerialized] public float CapsuleRadius;

        /// <summary>Capsule-Center (lokaler Offset relativ zur Character-Position).</summary>
        [NonSerialized] public Vector3 CapsuleCenter;

        /// <summary>Box half-extents fuer AABB-Casts (SoF2 playerMins/playerMaxs). X=Radius, Y=Height/2, Z=Radius.</summary>
        private Vector3 BoxHalfExtents => new(CapsuleRadius, CapsuleHeight * 0.5f, CapsuleRadius);

        // ===== Constants =====

        /// <summary>Overclip-Konstante für PM_ClipVelocity (SoF2-Wert).</summary>
        private const float OVERCLIP = 1.001f;

        /// <summary>Skin-Width: minimaler Abstand zu Oberflächen.</summary>
        private const float SKIN_WIDTH = 0.02f;

        /// <summary>Maximale Depenetration-Iterationen pro Frame.</summary>
        private const int MAX_DEPENETRATION_ITERATIONS = 3;

        /// <summary>Max Clip-Planes für PM_SlideMove (SoF2: MAX_CLIP_PLANES = 5).</summary>
        private const int MAX_CLIP_PLANES = 5;

        /// <summary>SoF2 Ground-Trace Distanz: 0.25 Quake-Units × 0.0254 = 0.00635m.
        /// Erhöht auf 0.04m weil Unity BoxCast auf Slopes bei kleineren Werten
        /// den Bodenkontakt verliert. CorrectGroundPosition gleicht das Schweben aus.</summary>
        private const float GROUND_TRACE_DIST = 0.08f; //original: 0.00635f leider nur am sliden.

        // ===== Simulation State (Runtime, nicht serialisiert) =====

        /// <summary>Aktuelle Velocity (Welt-Raum, XYZ).</summary>
        [NonSerialized] public Vector3 Velocity;

        /// <summary>Auf dem Boden.</summary>
        [NonSerialized] public bool IsGrounded;

        /// <summary>Springt gerade (bis zur nächsten Landung).</summary>
        [NonSerialized] public bool IsJumping;

        /// <summary>Geduckt.</summary>
        [NonSerialized] public bool IsCrouching;

        /// <summary>SoF2 PMD_JUMP: Jump-Button muss losgelassen werden bevor erneut gesprungen werden kann.</summary>
        [NonSerialized] public bool IsDebounceActive;

        /// <summary>SoF2 pm_time: Landing-Lockout-Timer nach harter Landung (Sekunden, countdown).</summary>
        [NonSerialized] public float JumpDebounce;

        /// <summary>
        /// SoF2 PMF_TIME_KNOCKBACK Timer (Sekunden, countdown).
        /// Wenn > 0: Friction deaktiviert, Air-Accelerate am Boden, Gravity am Boden.
        /// Verhindert dass der Spieler Knockback-Momentum sofort canceln kann.
        /// </summary>
        [NonSerialized] public float KnockbackTime;

        /// <summary>Gespeicherter Ground-Hit für externe Abfragen.</summary>
        [NonSerialized] public RaycastHit LastGroundHit;

        /// <summary>Landing-Event bereits gefeuert (verhindert Doppel-Trigger).</summary>
        [NonSerialized] public bool LandedThisGround;

        /// <summary>Simulations-Zeitstempel (akkumuliert aus cmd.DeltaTime).</summary>
        [NonSerialized] public float SimulationTime;

        /// <summary>True wenn ein Jump in diesem Step ausgelöst wurde (für Animation-Trigger).</summary>
        public bool JumpTriggered { get; private set; }

        /// <summary>True wenn in diesem Step gelanded wurde.</summary>
        public bool JustLanded { get; private set; }

        // ===== Private State =====

        private float m_DeltaTime;

        /// <summary>SoF2 pml.walking — auf begehbarem Boden (frame-local).</summary>
        private bool m_Walking;

        /// <summary>SoF2 pml.groundPlane — Boden-Ebene vorhanden, ggf. zu steil (frame-local).</summary>
        private bool m_GroundPlane;

        /// <summary>SoF2 pml.groundTrace — Ground-Trace-Ergebnis (frame-local).</summary>
        private RaycastHit m_GroundTrace;

        /// <summary>SoF2 pml.previous_velocity — Velocity vor diesem Frame (für CrashLand).</summary>
        private Vector3 m_PreviousVelocity;

        /// <summary>SoF2 pml.previous_origin — Position vor diesem Frame (für CrashLand Delta-Berechnung).</summary>
        private Vector3 m_PreviousPosition;

        /// <summary>SoF2 PMF_CROUCH_JUMP — In der Luft geduckt (Crouch-High-Jump).</summary>
        private bool m_CrouchJumping;

        /// <summary>Scratch-Array für PM_SlideMove Clip-Planes (vermeidet Heap-Allokation).</summary>
        private readonly Vector3[] m_ClipPlanes = new Vector3[MAX_CLIP_PLANES];

        // ===== Airtime / Distance / Height Tracking (fuer Debug-HUD) =====

        /// <summary>Position (XZ + Y) beim Verlassen des Bodens.</summary>
        private Vector3 m_AirStartPosition;

        /// <summary>Hoechster Y-Wert waehrend des aktuellen Luftaufenthalts.</summary>
        private float m_HighestYInAir;

        /// <summary>Zeitpunkt (SimulationTime) beim Verlassen des Bodens.</summary>
        private float m_AirStartTime;

        /// <summary>Ob der Peak bereits ueberschritten wurde (Velocity.y wechselt zu <= 0 nach Aufstieg).</summary>
        private bool m_InFallPhase;

        /// <summary>SimulationTime beim Erreichen des Peaks.</summary>
        private float m_PeakTime;

        // --- Live-Werte (jeden Frame in der Luft aktualisiert, 0 am Boden) ---

        /// <summary>Aktuelle Airtime seit Verlassen des Bodens (Sekunden). 0 am Boden.</summary>
        [NonSerialized] public float CurrentAirtime;

        /// <summary>Aktuelle Sprunghoehe ueber Startposition (Meter). 0 am Boden oder bei Fall.</summary>
        [NonSerialized] public float CurrentJumpHeight;

        /// <summary>Aktuelle Fallhoehe unter Peak/Startposition (Meter). 0 am Boden.</summary>
        [NonSerialized] public float CurrentFallHeight;

        /// <summary>Aktuelle horizontale Distanz seit Absprung (XZ, Meter). 0 am Boden.</summary>
        [NonSerialized] public float CurrentAirDistanceHoriz;

        /// <summary>Gesamte vertikale Weglaenge seit Absprung (JumpHeight + FallHeight, Meter). 0 am Boden.</summary>
        [NonSerialized] public float CurrentAirDistanceVert;

        /// <summary>Airtime in der Aufstiegsphase (Absprung bis Peak, Sekunden). 0 am Boden oder nach Peak.</summary>
        [NonSerialized] public float CurrentJumpPhaseAirtime;

        /// <summary>Airtime in der Fallphase (Peak bis jetzt, Sekunden). 0 am Boden oder im Aufstieg.</summary>
        [NonSerialized] public float CurrentFallPhaseAirtime;

        // --- Full-Werte (nach Landung gespeichert, persistieren bis zur naechsten Landung) ---

        /// <summary>Gesamte Airtime der letzten Luftphase (Sekunden).</summary>
        [NonSerialized] public float FullAirtime;

        /// <summary>Maximale Sprunghoehe der letzten Luftphase (Meter).</summary>
        [NonSerialized] public float FullJumpHeight;

        /// <summary>Maximale Fallhoehe der letzten Luftphase (Meter).</summary>
        [NonSerialized] public float FullFallHeight;

        /// <summary>Horizontale Distanz der letzten Luftphase (XZ, Meter).</summary>
        [NonSerialized] public float FullAirDistanceHoriz;

        /// <summary>Gesamte vertikale Weglaenge der letzten Luftphase (FullJumpHeight + FullFallHeight, Meter).</summary>
        [NonSerialized] public float FullAirDistanceVert;

        /// <summary>Airtime der Aufstiegsphase beim letzten Sprung (Sekunden).</summary>
        [NonSerialized] public float FullJumpPhaseAirtime;

        /// <summary>Airtime der Fallphase beim letzten Sprung (Sekunden).</summary>
        [NonSerialized] public float FullFallPhaseAirtime;

        // ===== Bhop Chain Tracking =====

        /// <summary>Max erlaubte Boden-Zeit um die Chain nicht zu brechen (Sekunden).</summary>
        private const float BHOP_CHAIN_GROUND_TIMEOUT = 0.3f;

        /// <summary>Minimale horizontale Speed damit ein Jump als Bhop zählt (m/s).
        /// 2.0 m/s ≈ 79 QU/s — filtert langsame Bewegung und Restgeschwindigkeiten.</summary>
        private const float BHOP_MIN_SPEED = 2.0f;

        /// <summary>Startposition (XZ) der aktuellen Bhop-Chain.</summary>
        private Vector3 m_BhopChainStartPosition;

        /// <summary>Wie lange der Spieler seit letzter Landung am Boden ist (Sekunden).</summary>
        private float m_GroundTime;

        /// <summary>Anzahl Spruenge in der aktuellen Bhop-Chain.</summary>
        [NonSerialized] public int BhopChainCount;

        /// <summary>Hoechste horizontale Speed waehrend der aktuellen Bhop-Chain (m/s).</summary>
        [NonSerialized] public float BhopChainPeakSpeed;

        /// <summary>Gesamte horizontale Distanz seit Start der Bhop-Chain (Meter).</summary>
        [NonSerialized] public float BhopChainDistance;

        /// <summary>Letzte abgeschlossene Bhop-Chain: Anzahl Spruenge.</summary>
        [NonSerialized] public int LastBhopChainCount;

        /// <summary>Letzte abgeschlossene Bhop-Chain: Peak Speed (m/s).</summary>
        [NonSerialized] public float LastBhopChainPeakSpeed;

        /// <summary>Letzte abgeschlossene Bhop-Chain: Distanz (Meter).</summary>
        [NonSerialized] public float LastBhopChainDistance;

        // ===================================================================
        // Public API
        // ===================================================================

        /// <summary>
        /// Setzt Capsule-Dimensionen (wird vom Client nach Bone-Berechnung
        /// und vom Server nach Empfang der Client-Daten aufgerufen).
        /// </summary>
        public void SetCapsuleDimensions(float height, float radius, Vector3 center)
        {
            CapsuleHeight = height;
            CapsuleRadius = radius;
            CapsuleCenter = center;
        }

        /// <summary>
        /// Setzt den Simulations-State extern (für Reconciliation).
        /// Client ruft dies auf wenn der Server eine Korrektur sendet.
        /// Position bleibt bei den Fuessen — CapsuleCenter-Offset wird
        /// automatisch durch PM_CheckDuck gesetzt (kein Teleport noetig).
        /// </summary>
        public void SetState(Vector3 velocity, bool isGrounded, bool isJumping, bool isCrouching,
                             float knockbackTime = 0f)
        {
            Velocity = velocity;
            IsGrounded = isGrounded;
            IsJumping = isJumping;
            IsCrouching = isCrouching;
            KnockbackTime = knockbackTime;

            // Crouch-Jump State ableiten: aktiv wenn in der Luft + springend + geduckt.
            // PM_CheckDuck liest m_CrouchJumping und setzt CapsuleCenter entsprechend.
            if (isGrounded || !isJumping)
            {
                m_CrouchJumping = false;
            }
            else
            {
                m_CrouchJumping = isCrouching;
            }
        }

        // ===================================================================
        // Simulate — SoF2 PmoveSingle Pipeline (bg_pmove.c)
        // ===================================================================

        /// <summary>
        /// Führt einen kompletten Physik-Step aus.
        /// Exakte SoF2 bg_pmove.c PmoveSingle Pipeline:
        /// PM_GroundTrace → PM_WalkMove/PM_AirMove → PM_GroundTrace.
        /// Gravity wird INNERHALB PM_SlideMove per Half-Step integriert (bg_slidemove.c).
        /// </summary>
        public void Simulate(ref Vector3 position, PlayerCommand cmd)
        {
            if (CapsuleHeight <= 0f)
            {
                return;
            }

            m_DeltaTime = cmd.DeltaTime;
            SimulationTime += m_DeltaTime;
            JumpTriggered = false;
            JustLanded = false;

            // SoF2: save previous velocity and origin for crash-landing detection
            m_PreviousVelocity = Velocity;
            m_PreviousPosition = position;

            // SoF2: release jump debounce when button released (PMD_JUMP clear)
            if (!cmd.HasButton(CommandButtons.Jump))
            {
                IsDebounceActive = false;
            }

            bool wasGrounded = IsGrounded;

            // SoF2: PM_CheckDuck before PM_GroundTrace (sets mins/maxs)
            PM_CheckDuck(ref position, cmd);

            // 1. Ground trace before movement (SoF2: PM_GroundTrace)
            PM_GroundTrace(ref position);

            // Landing detection BEFORE PM_CheckJump can clear IsGrounded again.
            // Beim Sprung-Spam landet und springt der Spieler im selben Frame —
            // ohne fruehe Erkennung wuerde JustLanded nie true werden und die
            // Animation bekaeme nie IsGrounded=true (→ "Bouncing"-Effekt).
            JustLanded = !wasGrounded && IsGrounded;

            // SoF2: PM_CheckCrouchJump after PM_GroundTrace
            PM_CheckCrouchJump(cmd);

            // SoF2: PM_DropTimers — pm_time countdown (landing lockout)
            // Runs AFTER ground checks but BEFORE movement, matching SoF2 pipeline.
            if (JumpDebounce > 0f)
            {
                JumpDebounce -= m_DeltaTime;
            }

            // SoF2: PMF_TIME_KNOCKBACK countdown (g_combat.c:580)
            if (KnockbackTime > 0f)
            {
                KnockbackTime -= m_DeltaTime;
                if (KnockbackTime < 0f)
                {
                    KnockbackTime = 0f;
                }
            }

            // 2. Movement (includes friction, acceleration, collision, gravity in air)
            if (m_Walking)
            {
                PM_WalkMove(ref position, cmd);
            }
            else
            {
                PM_AirMove(ref position, cmd);
            }

            // 3. Ground trace after movement (SoF2: second PM_GroundTrace)
            PM_GroundTrace(ref position);

            // Landing detection nach 2. GroundTrace: Normale Landungen werden hier
            // erkannt, weil der Spieler oft erst NACH AirMove (Gravity) den Boden
            // erreicht — die 1. GroundTrace sah ihn noch in der Luft.
            // Die 1. GroundTrace erkennt nur Same-Frame-Landungen (Jump-Spam).
            if (!JustLanded && !wasGrounded && IsGrounded)
            {
                JustLanded = true;
            }

            // 5. Airtime tracking: leaving ground
            // JustLanded && !IsGrounded = Same-Frame Land+Jump: behandle als
            // "erst gelandet, dann abgehoben" — Full-Werte sichern, dann neuen
            // Luftstart tracken.
            bool leftGround = (wasGrounded && !IsGrounded)
                           || (JustLanded && !IsGrounded);

            // Same-Frame Land+Jump: Full-Werte sichern bevor neuer Start
            if (JustLanded && !IsGrounded)
            {
                FullAirtime = CurrentAirtime;
                FullJumpHeight = Mathf.Max(0f, m_HighestYInAir - m_AirStartPosition.y);
                FullFallHeight = Mathf.Max(0f, m_HighestYInAir - position.y);
                FullJumpPhaseAirtime = m_InFallPhase ? m_PeakTime - m_AirStartTime : FullAirtime;
                FullFallPhaseAirtime = m_InFallPhase ? FullAirtime - FullJumpPhaseAirtime : 0f;
                Vector3 hDeltaSF = new(position.x - m_AirStartPosition.x, 0f, position.z - m_AirStartPosition.z);
                FullAirDistanceHoriz = hDeltaSF.magnitude;
                FullAirDistanceVert = FullJumpHeight + FullFallHeight;
                LandedThisGround = true;
            }

            if (leftGround)
            {
                m_AirStartPosition = position;
                m_HighestYInAir = position.y;
                m_AirStartTime = SimulationTime;
                m_InFallPhase = false;
            }

            // 6. Airtime tracking: in air
            if (!IsGrounded)
            {
                CurrentAirtime = SimulationTime - m_AirStartTime;
                if (position.y > m_HighestYInAir)
                {
                    m_HighestYInAir = position.y;
                }
                CurrentJumpHeight = Mathf.Max(0f, position.y - m_AirStartPosition.y);
                CurrentFallHeight = Mathf.Max(0f, m_HighestYInAir - position.y);
                Vector3 hDelta = new(position.x - m_AirStartPosition.x, 0f, position.z - m_AirStartPosition.z);
                CurrentAirDistanceHoriz = hDelta.magnitude;
                CurrentAirDistanceVert = CurrentJumpHeight + CurrentFallHeight;

                if (!m_InFallPhase && Velocity.y <= 0f)
                {
                    m_InFallPhase = true;
                    m_PeakTime = SimulationTime;
                }

                if (m_InFallPhase)
                {
                    CurrentJumpPhaseAirtime = m_PeakTime - m_AirStartTime;
                    CurrentFallPhaseAirtime = SimulationTime - m_PeakTime;
                }
                else
                {
                    CurrentJumpPhaseAirtime = CurrentAirtime;
                    CurrentFallPhaseAirtime = 0f;
                }
            }

            // 7. Landing: save full values, reset live values
            // Same-Frame Land+Jump bereits oben in Punkt 5 behandelt — nur
            // ausfuehren wenn wir tatsaechlich am Boden gelandet SIND (nicht
            // schon wieder in der Luft).
            if (JustLanded && IsGrounded)
            {
                FullAirtime = CurrentAirtime;
                FullJumpHeight = Mathf.Max(0f, m_HighestYInAir - m_AirStartPosition.y);
                FullFallHeight = Mathf.Max(0f, m_HighestYInAir - position.y);
                FullJumpPhaseAirtime = m_InFallPhase ? m_PeakTime - m_AirStartTime : FullAirtime;
                FullFallPhaseAirtime = m_InFallPhase ? FullAirtime - FullJumpPhaseAirtime : 0f;
                Vector3 hDeltaFull = new(position.x - m_AirStartPosition.x, 0f, position.z - m_AirStartPosition.z);
                FullAirDistanceHoriz = hDeltaFull.magnitude;
                FullAirDistanceVert = FullJumpHeight + FullFallHeight;
                CurrentAirtime = 0f;
                CurrentJumpHeight = 0f;
                CurrentFallHeight = 0f;
                CurrentAirDistanceHoriz = 0f;
                CurrentAirDistanceVert = 0f;
                CurrentJumpPhaseAirtime = 0f;
                CurrentFallPhaseAirtime = 0f;
                LandedThisGround = true;
            }

            // 8. Bhop chain tracking
            UpdateBhopChainTracking(position);

        }

        /// <summary>
        /// Trackt Bhop-Chains: aufeinanderfolgende Spruenge ohne lange Bodenkontaktzeit.
        /// Chain bricht ab wenn der Spieler laenger als BHOP_CHAIN_GROUND_TIMEOUT am Boden bleibt.
        /// </summary>
        private void UpdateBhopChainTracking(Vector3 position)
        {
            if (IsGrounded)
            {
                m_GroundTime += m_DeltaTime;

                // Chain brechen wenn zu lange am Boden ohne zu springen
                if (m_GroundTime > BHOP_CHAIN_GROUND_TIMEOUT && BhopChainCount > 0)
                {
                    // Speichere letzte Chain bevor sie resettet wird
                    LastBhopChainCount = BhopChainCount;
                    LastBhopChainPeakSpeed = BhopChainPeakSpeed;
                    LastBhopChainDistance = BhopChainDistance;
                    BhopChainCount = 0;
                    BhopChainPeakSpeed = 0f;
                    BhopChainDistance = 0f;
                }
            }
            else
            {
                m_GroundTime = 0f;
            }

            // Neuer Jump: Chain erweitern oder starten (nur mit Mindest-Speed)
            if (JumpTriggered)
            {
                float horizSpeed = new Vector3(Velocity.x, 0f, Velocity.z).magnitude;
                if (horizSpeed >= BHOP_MIN_SPEED)
                {
                    if (BhopChainCount == 0)
                    {
                        m_BhopChainStartPosition = position;
                    }

                    BhopChainCount++;
                }
            }

            // Peak Speed und Distanz live updaten waehrend Chain aktiv
            if (BhopChainCount > 0)
            {
                float horizSpeed = new Vector3(Velocity.x, 0f, Velocity.z).magnitude;
                if (horizSpeed > BhopChainPeakSpeed)
                {
                    BhopChainPeakSpeed = horizSpeed;
                }

                Vector3 hDelta = new(position.x - m_BhopChainStartPosition.x, 0f, position.z - m_BhopChainStartPosition.z);
                BhopChainDistance = hDelta.magnitude;
            }
        }

        // ===================================================================
        // SoF2 PM_CheckDuck / PM_CheckCrouchJump (bg_pmove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_CheckDuck — exakter Port.
        /// Setzt IsCrouching basierend auf Crouch-Button und Headroom-Trace.
        /// Aktualisiert CapsuleHeight und CapsuleCenter fuer Standing/Crouching.
        /// Wird VOR PM_GroundTrace aufgerufen (SoF2: PM_CheckDuck sets mins/maxs).
        ///
        /// WICHTIG: In SoF2 aendert PM_CheckDuck mins[2] NICHT basierend auf PMF_CROUCH_JUMP.
        /// Es setzt IMMER mins[2] = MINS_Z = -46. Die Box-Unterseite bleibt konstant.
        /// Der Crouch-Jump-Effekt (hoehere Stufen ueberspringen) wirkt NUR ueber den
        /// Step-Bonus in PM_StepSlideMove, NICHT ueber eine dauerhaft angehobene Box.
        /// </summary>
        private void PM_CheckDuck(ref Vector3 position, PlayerCommand cmd)
        {
            if (cmd.HasButton(CommandButtons.Crouch))
            {
                // SoF2: pm->ps->pm_flags |= PMF_DUCKED
                IsCrouching = true;
            }
            else
            {
                // Aufstehen: pruefen ob genug Platz (SoF2: trace mit standing maxs)
                // SoF2: maxs[2] = DEFAULT_PLAYER_Z_MAX, trace, check !allsolid
                // Keine Sonderbehandlung fuer PMF_CROUCH_JUMP — immer volle Stehbox testen.
                if (IsCrouching)
                {
                    Vector3 standingHalf = new(CapsuleRadius, StandingHeight * 0.5f, CapsuleRadius);
                    Vector3 standingCenter = position + new Vector3(0f, StandingHeight * 0.5f, 0f);

                    if (!Physics.CheckBox(standingCenter, standingHalf, Quaternion.identity, GroundMask, QueryTriggerInteraction.Ignore))
                    {
                        // SoF2: pm->ps->pm_flags &= ~PMF_DUCKED
                        IsCrouching = false;
                    }
                    // Else: bleib geduckt, kein Platz zum Aufstehen
                }
            }

            // SoF2: mins[2] = MINS_Z (immer -46), maxs[2] je nach PMF_DUCKED.
            // In Unity: position = Fuesse, Box-Bottom immer bei position.
            // CapsuleCenter = (0, height/2, 0) — keine Anhebung, auch nicht bei CJ.
            float targetHeight = IsCrouching ? CrouchingHeight : StandingHeight;

            CapsuleHeight = targetHeight;
            CapsuleCenter = new Vector3(0f, targetHeight * 0.5f, 0f);
        }

        /// <summary>
        /// SoF2 PM_CheckCrouchJump — exakter Port.
        /// Erlaubt Crouching in der Luft waehrend eines Sprungs.
        /// Zieht die Beine hoch (Box wird von oben gekuerzt) — ermoeglicht
        /// das Springen durch niedrige Oeffnungen (Crouch-High-Jump).
        /// </summary>
        private void PM_CheckCrouchJump(PlayerCommand cmd)
        {
            // Bereits im Crouch-Jump: pruefen ob vorbei
            if (m_CrouchJumping)
            {
                if (m_GroundPlane)
                {
                    // SoF2: pm->ps->pm_flags &= ~PMF_CROUCH_JUMP
                    m_CrouchJumping = false;
                }
            }
            else
            {
                // Nicht am Boden + springend + Crouch gedrueckt → Crouch-Jump
                if (!m_GroundPlane && IsJumping && cmd.HasButton(CommandButtons.Crouch))
                {
                    // SoF2: pm->ps->pm_flags |= PMF_CROUCH_JUMP
                    m_CrouchJumping = true;
                }
            }
        }

        // ===================================================================
        // SoF2 PM_GroundTrace — Ground Detection (bg_pmove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_GroundTrace — exakter Port.
        /// Tracet GROUND_TRACE_DIST nach unten. Setzt m_Walking, m_GroundPlane, IsGrounded.
        /// Kickoff-Check: velocity.y > 0 und Dot(velocity, normal) > 10 (×0.0254).
        /// Slope-Check: normal.y kleiner PmMaxSteepness (0.7) = zu steil.
        /// Velocity wird NICHT genullt (SoF2: "don't reset the z velocity for slopes").
        /// </summary>
        private void PM_GroundTrace(ref Vector3 position)
        {
            Vector3 center = GetWorldCenterAtPosition(position);
            Vector3 halfExtents = BoxHalfExtents;
            // SoF2 benutzt die volle Bounding Box fuer Ground-Traces (kein Shrink).
            // Der Slope-Check (normal.y < PmMaxSteepness) filtert Wand-Hits bereits.

            if (!Physics.BoxCast(center, halfExtents,
                    Vector3.down, out RaycastHit hit, Quaternion.identity, GROUND_TRACE_DIST,
                    GroundMask, QueryTriggerInteraction.Ignore))
            {
                if (IsGrounded)
                {
                    LandedThisGround = false;
                }
                IsGrounded = false;
                m_GroundPlane = false;
                m_Walking = false;
                return;
            }

            m_GroundTrace = hit;
            LastGroundHit = hit;

            // SoF2 Kickoff-Check: velocity[2] > 0 && DotProduct(velocity, normal) > 10
            if (Velocity.y > 0f && Vector3.Dot(Velocity, hit.normal) > 0.254f)
            {
                IsGrounded = false;
                m_GroundPlane = false;
                m_Walking = false;
                return;
            }

            // SoF2: Slope zu steil? (normal[2] < MIN_WALK_NORMAL)
            if (hit.normal.y < PmMaxSteepness)
            {
                IsGrounded = false;
                m_GroundPlane = true;
                m_Walking = false;
                return;
            }

            // Begehbarer Boden
            m_GroundPlane = true;
            m_Walking = true;

            // Erste Landung: CrashLand + Lockout (SoF2: PM_CrashLand → PMF_TIME_LAND)
            if (!IsGrounded)
            {
                IsGrounded = true;

                // SoF2: PM_CrashLand VOR IsJumping=false (CrashLand liest PMF_JUMPING).
                // Berechnet Landing-Delta, schneidet horizontale Velocity bei Sprung-Landung
                // (Anti-Strafe-Jump), setzt pm_time=750, nullt vertikale Velocity.
                PM_CrashLand(position, hit.normal);

                // SoF2: previous_velocity[2] < -200 → pm_time = 250 (PMF_TIME_LAND)
                // Ueberschreibt CrashLand's 750ms Timer bei harter Landung.
                if (m_PreviousVelocity.y < -5.08f)
                {
                    JumpDebounce = JumpDebounceAfterMs;
                }
            }

            // Position auf Boden korrigieren (Einsinken verhindern)
            CorrectGroundPosition(ref position, hit);
        }

        // ===================================================================
        // SoF2 PM_CheckJump (bg_pmove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_CheckJump — exakter Port.
        /// Prüft pm_time Lockout, Button-State, PMD_JUMP Debounce.
        /// Wird nur aus PM_WalkMove aufgerufen.
        /// </summary>
        private bool PM_CheckJump(PlayerCommand cmd, float currentY)
        {
            // SoF2: pm_time lockout (hard landing)
            if (JumpDebounce > 0f)
            {
                return false;
            }

            // SoF2: Can't jump when ducked (PMF_DUCKED check)
            if (IsCrouching)
            {
                return false;
            }

            // Not pressing jump
            if (!cmd.HasButton(CommandButtons.Jump))
            {
                return false;
            }

            // SoF2 PMD_JUMP: must release button first
            if (IsDebounceActive)
            {
                return false;
            }

            // Jump!
            m_GroundPlane = false;
            m_Walking = false;
            IsGrounded = false;
            IsJumping = true;
            IsDebounceActive = true;

            Velocity.y = JumpVelocity;
            JumpTriggered = true;

            return true;
        }

        // ===================================================================
        // SoF2 Physics Engine — Friction, Acceleration, ClipVelocity
        // ===================================================================

        /// <summary>
        /// SoF2 PM_Friction — exakter Port.
        /// vec[2]=0 wenn walking (nur fuer Speed-Berechnung).
        /// Friction-Drop nur am Boden. speed kleiner 1 QU (0.0254 m/s) -> Stop.
        /// Scaling wird auf alle 3 Komponenten angewandt.
        /// </summary>
        private void PM_Friction()
        {
            Vector3 vec = Velocity;

            if (m_Walking)
            {
                vec.y = 0f;
            }

            float speed = vec.magnitude;
            float drop = 0f;

            // SoF2: speed < 1 (in Quake-Units) = 0.0254 m/s
            if (speed < 0.0254f)
            {
                Velocity.x = 0f;
                Velocity.z = 0f;
                return;
            }

            // Friction nur am Boden (SoF2: pml.walking && !SURF_SLICK)
            // SoF2: if (!(pm->ps->pm_flags & PMF_TIME_KNOCKBACK)) — skip friction during knockback
            if (m_Walking && KnockbackTime <= 0f)
            {
                float control = speed < PmStopSpeed ? PmStopSpeed : speed;
                drop += control * PmFriction * m_DeltaTime;
            }

            float newspeed = speed - drop;
            if (newspeed < 0f)
            {
                newspeed = 0f;
            }

            if (newspeed != speed)
            {
                newspeed /= speed;
                Velocity.x *= newspeed;
                Velocity.y *= newspeed;
                Velocity.z *= newspeed;
            }
        }

        /// <summary>
        /// SoF2 PM_Accelerate — Q2/Q3 Stil.
        /// Projiziert aktuelle Velocity auf wishdir, addiert beschleunigte Differenz.
        /// Ermöglicht Strafe-Jumping (Perpendicular-Velocity bleibt unberührt).
        /// </summary>
        private void PM_Accelerate(Vector3 wishdir, float wishspeed, float accel)
        {
            float currentspeed = Vector3.Dot(Velocity, wishdir);
            float addspeed = wishspeed - currentspeed;

            if (addspeed <= 0f)
            {
                return;
            }

            float accelspeed = accel * m_DeltaTime * wishspeed;
            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            Velocity.x += accelspeed * wishdir.x;
            Velocity.y += accelspeed * wishdir.y;
            Velocity.z += accelspeed * wishdir.z;
        }

        /// <summary>
        /// SoF2 PM_ClipVelocity — entfernt Velocity-Komponente die in eine Oberfläche zeigt.
        /// OVERCLIP verhindert Float-Precision Creep in Oberflächen.
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
        /// SoF2 PM_CmdScale — skaliert Input-Magnitude auf 0–1.
        /// Unity DigitalNormalized liefert bei Diagonal (0.707, 0.707) mit Magnitude 1.0.
        /// </summary>
        private float PM_CmdScale(Vector2 moveInput)
        {
            float inputMagnitude = moveInput.magnitude;
            if (inputMagnitude <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(inputMagnitude);
        }

        // ===================================================================
        // SoF2 PM_WalkMove / PM_AirMove (bg_pmove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_WalkMove — exakter Port.
        /// CheckJump → Friction → Forward/Right auf Ground projizieren →
        /// Accelerate → ClipVelocity+SpeedRestore → StepSlideMove(false).
        /// </summary>
        private void PM_WalkMove(ref Vector3 position, PlayerCommand cmd)
        {
            // SoF2: check jump inside WalkMove
            if (PM_CheckJump(cmd, position.y))
            {
                PM_AirMove(ref position, cmd);
                return;
            }

            PM_Friction();

            // Forward/Right aus YawAngle
            Quaternion yawRotation = Quaternion.Euler(0f, cmd.YawAngle, 0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;

            // SoF2: flatten then project onto ground via ClipVelocity
            forward.y = 0f;
            right.y = 0f;
            PM_ClipVelocity(forward, m_GroundTrace.normal, out forward, OVERCLIP);
            PM_ClipVelocity(right, m_GroundTrace.normal, out right, OVERCLIP);
            forward.Normalize();
            right.Normalize();

            // SoF2: wishvel includes Y component from slope projection
            float fmove = cmd.MoveInput.y;
            float smove = cmd.MoveInput.x;
            Vector3 wishvel = new(
                forward.x * fmove + right.x * smove,
                forward.y * fmove + right.y * smove,
                forward.z * fmove + right.z * smove);

            Vector3 wishdir = wishvel;
            float wishspeed = wishdir.magnitude;
            if (wishspeed > 0.0001f)
            {
                wishdir /= wishspeed;
            }
            else
            {
                wishdir = Vector3.zero;
            }

            float scale = PM_CmdScale(cmd.MoveInput);
            wishspeed = scale * PmMaxSpeed;

            // SoF2: Walk Speed Clamp (BUTTON_WALKING → halbe Speed)
            // bg_pmove.c PM_CmdScale: if (BUTTON_WALKING) scale *= 0.5
            if (cmd.HasButton(CommandButtons.Walk))
            {
                float walkMax = PmMaxSpeed * PmWalkScale;
                if (wishspeed > walkMax)
                {
                    wishspeed = walkMax;
                }
            }

            // SoF2: Duck Speed Clamp — nur in WalkMove, NICHT in AirMove
            // bg_pmove.c: if (pm->ps->pm_flags & PMF_DUCKED) wishspeed *= pm_duckScale
            if (IsCrouching)
            {
                float duckMax = PmMaxSpeed * PmDuckScale;
                if (wishspeed > duckMax)
                {
                    wishspeed = duckMax;
                }
            }

            // SoF2: accelerate faster when ducked (bg_pmove.c: accelerate *= 2)
            // SoF2: PMF_TIME_KNOCKBACK → use air-accelerate on ground (bg_pmove.c:780)
            float accelerate;
            if (KnockbackTime > 0f)
            {
                accelerate = PmAirAccelerate;
            }
            else
            {
                accelerate = PmAccelerate;
                if (IsCrouching)
                {
                    accelerate *= 2f;
                }
            }

            PM_Accelerate(wishdir, wishspeed, accelerate);

            // SoF2: PMF_TIME_KNOCKBACK → apply gravity while on ground (bg_pmove.c:797)
            if (KnockbackTime > 0f)
            {
                Velocity.y -= PmGravity * m_DeltaTime;
            }

            // SoF2: clip velocity to ground plane + speed restore
            // "don't decrease velocity when going up or down a slope"
            float vel = Velocity.magnitude;
            PM_ClipVelocity(Velocity, m_GroundTrace.normal, out Vector3 clipped, OVERCLIP);
            Velocity = clipped;
            float clippedMag = Velocity.magnitude;
            if (clippedMag > 0.001f)
            {
                Velocity = Velocity / clippedMag * vel;
            }

            // SoF2: nicht bewegen wenn stillstehend
            if (Mathf.Abs(Velocity.x) < 0.001f && Mathf.Abs(Velocity.z) < 0.001f)
            {
                return;
            }

            // SoF2: PM_StepSlideMove(qfalse) — kein Gravity für Boden
            PM_StepSlideMove(ref position, false);
        }

        /// <summary>
        /// SoF2 PM_AirMove — exakter Port.
        /// Friction → Flat Forward/Right → Accelerate →
        /// Clip gegen steile GroundPlane → StepSlideMove(true).
        /// Gravity wird innerhalb PM_SlideMove per Half-Step integriert.
        /// </summary>
        private void PM_AirMove(ref Vector3 position, PlayerCommand cmd)
        {
            PM_Friction();

            Quaternion yawRotation = Quaternion.Euler(0f, cmd.YawAngle, 0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            float fmove = cmd.MoveInput.y;
            float smove = cmd.MoveInput.x;
            Vector3 wishvel = forward * fmove + right * smove;
            wishvel.y = 0f;

            Vector3 wishdir = wishvel;
            float wishspeed = wishdir.magnitude;
            if (wishspeed > 0.0001f)
            {
                wishdir /= wishspeed;
            }
            else
            {
                wishdir = Vector3.zero;
            }

            float scale = PM_CmdScale(cmd.MoveInput);
            wishspeed = scale * PmMaxSpeed;

            PM_Accelerate(wishdir, wishspeed, PmAirAccelerate);

            // SoF2: clip against steep ground plane if present
            if (m_GroundPlane)
            {
                PM_ClipVelocity(Velocity, m_GroundTrace.normal, out Vector3 cv, OVERCLIP);
                Velocity = cv;
            }

            // SoF2: PM_StepSlideMove(qtrue) — Gravity wird in PM_SlideMove integriert
            PM_StepSlideMove(ref position, true);
        }

        // ===================================================================
        // SoF2 PM_SlideMove — Multi-Plane Velocity Clipping (bg_slidemove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_SlideMove — exakter Port aus bg_slidemove.c.
        /// 4-Bump Loop mit Multi-Plane Velocity-Clipping (bis zu 5 Planes).
        /// Optionale Gravity-Integration per Half-Step (smooth parabolic arcs).
        /// Cross-Product Crease-Sliding bei 2-Plane Intersections.
        /// Returns true wenn Velocity geclippt wurde (Kollision aufgetreten).
        /// </summary>
        private bool PM_SlideMove(ref Vector3 position, bool gravity)
        {
            int numbumps = 4;
            int numPlanes;
            Vector3 primalVelocity = Velocity;
            Vector3 endVelocity = Velocity;

            // SoF2: gravity half-step integration
            if (gravity)
            {
                endVelocity.y = Velocity.y - PmGravity * m_DeltaTime;
                Velocity.y = (Velocity.y + endVelocity.y) * 0.5f;
                primalVelocity.y = endVelocity.y;

                if (m_GroundPlane)
                {
                    PM_ClipVelocity(Velocity, m_GroundTrace.normal, out Vector3 cv, OVERCLIP);
                    Velocity = cv;
                }
            }

            float timeLeft = m_DeltaTime;

            // Initialize clip planes
            numPlanes = 0;
            if (m_GroundPlane)
            {
                m_ClipPlanes[numPlanes++] = m_GroundTrace.normal;
            }
            // Never turn against original velocity direction
            Vector3 velNorm = Velocity.normalized;
            if (velNorm.sqrMagnitude > 0.001f)
            {
                m_ClipPlanes[numPlanes++] = velNorm;
            }

            int bumpcount;
            for (bumpcount = 0; bumpcount < numbumps; bumpcount++)
            {
                Vector3 end = position + Velocity * timeLeft;
                Vector3 castDir = end - position;
                float castDist = castDir.magnitude;

                if (castDist < 1e-6f)
                {
                    break;
                }

                Vector3 center = GetWorldCenterAtPosition(position);
                Vector3 castDirNorm = castDir / castDist;

                if (!Physics.BoxCast(center, BoxHalfExtents,
                        castDirNorm, out RaycastHit trace, Quaternion.identity, castDist + SKIN_WIDTH,
                        GroundMask, QueryTriggerInteraction.Ignore))
                {
                    // No hit — moved entire distance
                    position = end;
                    break;
                }

                // SoF2: trace.fraction equivalent
                float fraction = Mathf.Clamp01(trace.distance / castDist);

                // Move to contact point (SoF2: if trace.fraction > 0)
                if (fraction > 0f)
                {
                    float moveDist = Mathf.Max(trace.distance - SKIN_WIDTH, 0f);
                    position += castDirNorm * moveDist;
                }

                // SoF2: if trace.fraction == 1 → full distance
                if (fraction >= 1f)
                {
                    break;
                }

                timeLeft -= timeLeft * fraction;

                if (numPlanes >= MAX_CLIP_PLANES)
                {
                    Velocity = Vector3.zero;
                    return true;
                }

                // SoF2: check for duplicate plane (epsilon with non-axial planes)
                int dupIdx;
                for (dupIdx = 0; dupIdx < numPlanes; dupIdx++)
                {
                    if (Vector3.Dot(trace.normal, m_ClipPlanes[dupIdx]) > 0.99f)
                    {
                        // Nudge velocity out along duplicate plane
                        Velocity += trace.normal;
                        break;
                    }
                }
                if (dupIdx < numPlanes)
                {
                    continue;
                }

                m_ClipPlanes[numPlanes] = trace.normal;
                numPlanes++;

                // SoF2: multi-plane velocity clipping
                // Find a plane that the velocity enters
                int i;
                for (i = 0; i < numPlanes; i++)
                {
                    float into = Vector3.Dot(Velocity, m_ClipPlanes[i]);
                    if (into >= 0.1f)
                    {
                        continue;
                    }

                    // Clip velocity to this plane
                    PM_ClipVelocity(Velocity, m_ClipPlanes[i], out Vector3 clipVelocity, OVERCLIP);
                    PM_ClipVelocity(endVelocity, m_ClipPlanes[i], out Vector3 endClipVelocity, OVERCLIP);

                    // Check against all other planes
                    int j;
                    for (j = 0; j < numPlanes; j++)
                    {
                        if (j == i)
                        {
                            continue;
                        }
                        if (Vector3.Dot(clipVelocity, m_ClipPlanes[j]) >= 0.1f)
                        {
                            continue;
                        }

                        // Clip to second plane
                        PM_ClipVelocity(clipVelocity, m_ClipPlanes[j], out clipVelocity, OVERCLIP);
                        PM_ClipVelocity(endClipVelocity, m_ClipPlanes[j], out endClipVelocity, OVERCLIP);

                        // Does it go back into first plane?
                        if (Vector3.Dot(clipVelocity, m_ClipPlanes[i]) >= 0f)
                        {
                            continue;
                        }

                        // SoF2: slide along crease (cross product of two planes)
                        Vector3 dir = Vector3.Cross(m_ClipPlanes[i], m_ClipPlanes[j]).normalized;
                        float d = Vector3.Dot(dir, Velocity);
                        clipVelocity = dir * d;

                        d = Vector3.Dot(dir, endVelocity);
                        endClipVelocity = dir * d;

                        // Check for third blocking plane
                        int k;
                        for (k = 0; k < numPlanes; k++)
                        {
                            if (k == i || k == j)
                            {
                                continue;
                            }
                            if (Vector3.Dot(clipVelocity, m_ClipPlanes[k]) >= 0.1f)
                            {
                                continue;
                            }

                            // Triple plane intersection — stop dead
                            Velocity = Vector3.zero;
                            return true;
                        }
                    }

                    // Fixed all interactions for this plane
                    Velocity = clipVelocity;
                    endVelocity = endClipVelocity;
                    break;
                }
            }

            if (gravity)
            {
                Velocity = endVelocity;
            }

            // SoF2: don't change velocity if in a timer (pm_time > 0)
            // Verhindert dass Kollisionen die Velocity waehrend des Landing-Lockouts
            // veraendern. Friction/Accelerate (vor SlideMove) bleiben wirksam.
            if (JumpDebounce > 0f)
            {
                Velocity = primalVelocity;
            }

            return bumpcount != 0;
        }

        // ===================================================================
        // SoF2 PM_CrashLand — Landing Impact (bg_pmove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_CrashLand — exakter Port.
        /// Berechnet Landing-Delta (Aufprall-Staerke) per quadratischer Formel
        /// aus Fallhoehe, vorheriger Velocity und Gravitation.
        /// Bei jumped und delta >= 17: horizontale Velocity ×0.25 + pm_time=750ms
        /// (Anti-Strafe-Jump Mechanismus). Nullt vertikale Velocity.
        /// Cleart PMF_JUMPING (IsJumping).
        /// </summary>
        private void PM_CrashLand(Vector3 position, Vector3 impactNormal)
        {
            // SoF2: jumped = (pm_flags & PMF_JUMPING) ? true : false
            bool jumped = IsJumping;

            // SoF2: pm_flags &= ~PMF_JUMPING
            IsJumping = false;

            // SoF2: calculate the exact velocity on landing via quadratic formula
            // dist = origin[2] - previous_origin[2] (vertikale Positionsaenderung)
            float dist = position.y - m_PreviousPosition.y;
            float vel = m_PreviousVelocity.y;
            float acc = -PmGravity;

            float a = acc * 0.5f;
            float b = vel;
            float c = -dist;

            float den = b * b - 4f * a * c;
            if (den < 0f)
            {
                return;
            }

            float t = (-b - Mathf.Sqrt(den)) / (2f * a);
            float impactVelocity = vel + t * acc;

            // SoF2: delta = impactVelocity^2 * 0.000275 (velocity in QU/s)
            float impactVelocityQU = impactVelocity / 0.0254f;
            float delta = impactVelocityQU * impactVelocityQU * 0.000275f;

            // SoF2: Scale delta based on impact normal (steep surfaces reduce delta)
            float f = impactNormal.y;
            if (f < 0.25f)
            {
                delta *= f;
            }

            // SoF2: velocity[2] = 0 — prevent bouncing
            Velocity.y = 0f;

            if (delta < 1f)
            {
                return;
            }

            // SoF2: Anti-Strafe-Jump — cut forward velocity when landing from a jump
            // bg_pmove.c: minDeltaForSlowDown = 17
            // Ein normaler Standsprung ergibt delta ≈ 20 (> 17).
            // Horizontale Velocity wird auf 25% reduziert, pm_time=750ms.
            // pm_time wird von PMF_TIME_LAND (250ms) in PM_GroundTrace ueberschrieben
            // bei harten Landungen (previous_velocity[2] < -200).
            if (jumped && delta >= 17f)
            {
                Velocity.x *= 0.25f;
                Velocity.z *= 0.25f;
                JumpDebounce = 0.75f;
            }
        }

        // ===================================================================
        // SoF2 PM_StepSlideMove (bg_slidemove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 Crouch-Jump Bonus-Stepsize: (DEFAULT_VIEWHEIGHT - CROUCH_VIEWHEIGHT) = 29 QU × 0.0254.
        /// bg_slidemove.c: stepsize += (DEFAULT_VIEWHEIGHT - CROUCH_VIEWHEIGHT);
        /// </summary>
        private const float CROUCH_JUMP_STEP_BONUS = 0.7366f;

        /// <summary>
        /// SoF2 PM_StepSlideMove — exakter Port aus bg_slidemove.c.
        /// 1. SlideMove → bei keiner Kollision fertig.
        /// 2. stepsize = STEPSIZE (+ Crouch-Jump Bonus).
        /// 3. Save erste SlideMove-Position (save_o, save_v).
        /// 4. Box von oben um stepsize kuerzen (maxs[2] -= stepsize).
        /// 5. Origin um stepsize hochsetzen, SlideMove mit verkuerzter Box.
        /// 6. Trace nach unten um stepsize (noch mit verkuerzter Box).
        /// 7. Box wiederherstellen (maxs[2] += stepsize).
        /// 8. Upward-Velocity-Check → revert wenn fliegend.
        /// 9. allsolid/startsolid-Check → revert wenn stuck.
        /// 10. ClipVelocity gegen Step-Down-Normale, "double check not stuck" → revert wenn stuck.
        /// 11. Wenn !result → revert zu save_o/save_v.
        /// </summary>
        private void PM_StepSlideMove(ref Vector3 position, bool gravity)
        {
            Vector3 startO = position;
            Vector3 startV = Velocity;

            // SoF2: first try regular slide
            if (!PM_SlideMove(ref position, gravity))
            {
                // No collision — got where we wanted
                ResolvePenetration(ref position);
                return;
            }

            // SoF2: stepsize = STEPSIZE
            float stepSize = PmStepSize;

            // SoF2: Add to the step size if we are crouched when jumping
            // bg_slidemove.c: if (PMF_CROUCH_JUMP) stepsize += (DEFAULT_VIEWHEIGHT - CROUCH_VIEWHEIGHT)
            if (m_CrouchJumping)
            {
                stepSize += CROUCH_JUMP_STEP_BONUS;
            }

            // SoF2: save_o, save_v — save first SlideMove result in case step-up fails
            Vector3 saveO = position;
            Vector3 saveV = Velocity;

            // SoF2: pm->maxs[2] -= stepsize — shrink box from top
            // In Unity: reduce CapsuleHeight temporarily, keep bottom at same position.
            // UNITY-FIX: In SoF2 funktioniert selbst eine degenerierte Box (maxs < mins)
            // weil BSP-Traces Brushes korrekt erkennen. Unity PhysX braucht eine Box mit
            // sinnvoller Hoehe fuer zuverlaessige BoxCast-Kollisionserkennung.
            // Minimum = 2×Radius stellt sicher, dass die Box nie duenner als breit ist.
            float originalHeight = CapsuleHeight;
            Vector3 originalCenter = CapsuleCenter;
            float minStepBoxHeight = CapsuleRadius * 2f;
            float shortenedHeight = originalHeight - stepSize;
            if (shortenedHeight < minStepBoxHeight)
            {
                shortenedHeight = minStepBoxHeight;
            }
            CapsuleHeight = shortenedHeight;
            // Bottom stays at same position: center.y = bottomOffset + shortenedHeight * 0.5f
            // originalCenter.y = bottomOffset + originalHeight * 0.5f
            // → bottomOffset = originalCenter.y - originalHeight * 0.5f
            float bottomOffset = originalCenter.y - originalHeight * 0.5f;
            CapsuleCenter = new Vector3(0f, bottomOffset + shortenedHeight * 0.5f, 0f);

            // SoF2: reset to start position/velocity, move origin up by stepsize
            position = startO;
            Velocity = startV;
            position.y += stepSize;

            // SoF2: try the move with the altered (shortened) hit box
            PM_SlideMove(ref position, gravity);

            // SoF2: trace down from current position by stepsize (still with shortened box)
            Vector3 downTarget = position;
            downTarget.y -= stepSize;

            Vector3 center = GetWorldCenterAtPosition(position);
            Vector3 halfExtents = BoxHalfExtents;
            Vector3 castDir = downTarget - position;
            float castDist = Mathf.Abs(castDir.y);

            bool stepDownHit = Physics.BoxCast(center, halfExtents,
                Vector3.down, out RaycastHit stepDownTrace, Quaternion.identity, castDist + SKIN_WIDTH,
                GroundMask, QueryTriggerInteraction.Ignore);

            // SoF2: pm->maxs[2] += stepsize — restore box to normal
            CapsuleHeight = originalHeight;
            CapsuleCenter = originalCenter;

            // SoF2: No stepping up if you have upward velocity and (no ground or too steep)
            // bg_slidemove.c: if (velocity[2] > 0 && (trace.fraction == 1.0 || DotProduct(normal, up) < 0.7))
            if (Velocity.y > 0f && (!stepDownHit || stepDownTrace.normal.y < PmMaxSteepness))
            {
                position = saveO;
                Velocity = saveV;
                ResolvePenetration(ref position);
                return;
            }

            bool result = true;

            if (stepDownHit)
            {
                // SoF2: Check allsolid/startsolid (step-down landed in solid)
                // Unity equivalent: CheckBox at the step-down position with FULL box
                float fraction = Mathf.Clamp01(stepDownTrace.distance / castDist);
                if (fraction <= 0f)
                {
                    // Started in solid — equivalent to trace.allsolid/startsolid
                    result = false;
                }
                else
                {
                    // SoF2: ClipVelocity against step-down ground normal
                    // bg_slidemove.c: if (trace.fraction < 1.0) PM_ClipVelocity(...)
                    PM_ClipVelocity(Velocity, stepDownTrace.normal, out Vector3 clippedV, OVERCLIP);
                    Velocity = clippedV;

                    // SoF2: VectorCopy(trace.endpos, origin) — move to step-down contact
                    float dropDist = Mathf.Max(stepDownTrace.distance - SKIN_WIDTH, 0f);
                    position += Vector3.down * dropDist;

                    // SoF2: "Now double check not stuck" — self-trace at final position with FULL box
                    // bg_slidemove.c: pm->trace(&trace, origin, mins, maxs, origin, ...) → if allsolid/startsolid → fail
                    Vector3 checkCenter = GetWorldCenterAtPosition(position);
                    Vector3 fullHalfExtents = BoxHalfExtents;
                    if (Physics.CheckBox(checkCenter, fullHalfExtents, Quaternion.identity,
                            GroundMask, QueryTriggerInteraction.Ignore))
                    {
                        // Stuck in solid — revert
                        result = false;
                    }
                }
            }
            else
            {
                // SoF2: trace.fraction == 1.0 — nothing below, drop full step
                position.y -= stepSize;

                // Double-check not stuck after dropping
                Vector3 checkCenter = GetWorldCenterAtPosition(position);
                Vector3 fullHalfExtents = BoxHalfExtents;
                if (Physics.CheckBox(checkCenter, fullHalfExtents, Quaternion.identity,
                        GroundMask, QueryTriggerInteraction.Ignore))
                {
                    result = false;
                }
            }

            // SoF2: if (!result) revert to save_o/save_v
            if (!result)
            {
                position = saveO;
                Velocity = saveV;
            }

            ResolvePenetration(ref position);
        }

        // ===================================================================
        // Utility Methods
        // ===================================================================

        /// <summary>
        /// Korrigiert die Position auf den Boden (verhindert Einsinken UND Schweben).
        /// Box-Bottom = Center - halfExtents.y.
        /// Noetig weil GROUND_TRACE_DIST (0.08m) groesser ist als SoF2 (0.00635m),
        /// wodurch der Boden erkannt wird bevor die Box ihn tatsaechlich beruehrt.
        /// Korrektur erfolgt NUR vertikal, um seitliches Driften auf Slopes zu vermeiden.
        /// </summary>
        private void CorrectGroundPosition(ref Vector3 position, RaycastHit downHit)
        {
            Vector3 boxBottom = GetWorldCenterAtPosition(position) - Vector3.up * BoxHalfExtents.y;
            float desiredDistance = SKIN_WIDTH;
            // Vertikaler Abstand zum Hitpoint (nicht entlang der Normale)
            float verticalGap = boxBottom.y - downHit.point.y;

            if (Mathf.Abs(verticalGap - desiredDistance) > 0.001f)
            {
                float correction = desiredDistance - verticalGap;
                position.y += correction;
            }
        }

        /// <summary>
        /// Welt-Center der Capsule an gegebener Position berechnen.
        /// </summary>
        private Vector3 GetWorldCenterAtPosition(Vector3 position)
        {
            return position + CapsuleCenter;
        }

        /// <summary>
        /// Depenetration nach Bewegung.
        /// Prüft per OverlapBox ob die Box in Geometrie steckt und
        /// schiebt sie iterativ heraus. Clippt Velocity gegen die Trennungs-Normalen.
        /// </summary>
        private void ResolvePenetration(ref Vector3 position)
        {
            Vector3 halfExtents = BoxHalfExtents;

            for (int iteration = 0; iteration < MAX_DEPENETRATION_ITERATIONS; iteration++)
            {
                Vector3 worldCenter = GetWorldCenterAtPosition(position);

                Collider[] overlaps = Physics.OverlapBox(
                    worldCenter, halfExtents, Quaternion.identity,
                    GroundMask, QueryTriggerInteraction.Ignore);

                if (overlaps.Length == 0)
                {
                    return;
                }

                bool resolved = false;

                for (int i = 0; i < overlaps.Length; i++)
                {
                    Collider col = overlaps[i];

                    // ClosestPoint unterstuetzt nur Box, Sphere, Capsule und convex Mesh.
                    // Fuer nicht-konvexe MeshCollider und TerrainCollider: Raycast-Fallback.
                    if ((col is MeshCollider mc && !mc.convex) || col is TerrainCollider)
                    {
                        // Raycast von oben nach unten um Boden-Oberflaeche zu finden.
                        // Haeufigstes Szenario: Box steckt im Boden-Mesh.
                        Vector3 rayOrigin = new(
                            worldCenter.x,
                            worldCenter.y + CapsuleHeight,
                            worldCenter.z);

                        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit floorHit,
                                CapsuleHeight * 2f, GroundMask, QueryTriggerInteraction.Ignore))
                        {
                            float boxBottomY = position.y + CapsuleCenter.y - CapsuleHeight * 0.5f;
                            if (boxBottomY < floorHit.point.y)
                            {
                                position.y += floorHit.point.y - boxBottomY + SKIN_WIDTH;
                                resolved = true;

                                if (Velocity.y < 0f)
                                {
                                    Velocity.y = 0f;
                                }
                            }
                        }

                        continue;
                    }

                    Vector3 closestPoint = col.ClosestPoint(worldCenter);
                    Vector3 diff = worldCenter - closestPoint;

                    if (diff.sqrMagnitude < 1e-8f)
                    {
                        diff = Vector3.up;
                    }

                    Vector3 normal = diff.normalized;

                    // Box-Penetrationstiefe: Projektion der half-extents auf die Trennungsnormale
                    // ergibt den Box-Rand-Abstand vom Center in Normalenrichtung.
                    float boxExtentAlongNormal = Mathf.Abs(halfExtents.x * normal.x)
                                               + Mathf.Abs(halfExtents.y * normal.y)
                                               + Mathf.Abs(halfExtents.z * normal.z);
                    float penetrationDepth = boxExtentAlongNormal - diff.magnitude;

                    if (penetrationDepth <= 0f)
                    {
                        continue;
                    }

                    position += normal * (penetrationDepth + SKIN_WIDTH);
                    resolved = true;

                    float velocityIntoSurface = Vector3.Dot(Velocity, -normal);
                    if (velocityIntoSurface > 0f)
                    {
                        Velocity += normal * velocityIntoSurface;
                    }
                }

                if (!resolved)
                {
                    return;
                }
            }
        }

    }
}
