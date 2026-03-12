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

        /// <summary>Max horizontale Velocity in der Luft (SoF2: 320 × 0.0254 = 8.128 m/s).</summary>
        public float PhysMaxVelocity = 8.128f;

        /// <summary>Max horizontale Velocity am Boden (SoF2: 320 × 0.0254 = 8.128 m/s).</summary>
        public float PhysMaxWalkVelocity = 8.128f;

        /// <summary>Sofortige Y-Velocity beim Sprung (SoF2: 270 × 0.0254 = 6.858 m/s).</summary>
        public float JumpVelocity = 6.858f;

        /// <summary>Maximale Steigung (Dot mit Up). SoF2 MIN_WALK_NORMAL = 0.7. Dimensionslos.</summary>
        public float PmMaxSteepness = 0.7f;

        /// <summary>Maximale Step-Hoehe (SoF2: 18 × 0.0254 = 0.4572m).</summary>
        public float PmMaxStep = 0.4572f;

        /// <summary>Step-Size für Step-Up (SoF2 STEPSIZE: 18 × 0.0254 = 0.4572m).</summary>
        public float PmStepSize = 0.4572f;

        /// <summary>Maximale Barriere-Hoehe (SoF2: 32 × 0.0254 = 0.8128m).</summary>
        public float PmMaxBarrier = 0.8128f;

        /// <summary>Duck Speed Scale (SoF2 PM_DUCKSCALE = 0.25).</summary>
        public float PmDuckScale = 0.25f;

        /// <summary>Jump-Debounce nach harter Landung (SoF2: 250ms).</summary>
        public float JumpDebounceAfterMs = 0.25f;

        /// <summary>Höhen-Schwelle ab der ein Sprung als Step-Up gilt (Legacy, nicht mehr aktiv).</summary>
        public float StepUpHeightThreshold = 0.178f;

        /// <summary>Ground Grace-Period (Legacy, nicht mehr aktiv — SoF2 hat keine Coyote Time).</summary>
        public float GroundGracePeriod = 0.15f;

        /// <summary>LayerMask für Ground-Detection.</summary>
        public LayerMask GroundMask = ~0;

        // ===== Capsule Dimensions (Runtime, nicht serialisiert) =====

        /// <summary>Capsule-Hoehe (wird vom Client nach Bone-Berechnung gesetzt).</summary>
        [NonSerialized] public float CapsuleHeight;

        /// <summary>Capsule-Radius.</summary>
        [NonSerialized] public float CapsuleRadius;

        /// <summary>Capsule-Center (lokaler Offset relativ zur Character-Position).</summary>
        [NonSerialized] public Vector3 CapsuleCenter;

        /// <summary>Ground-Check-Distanz (Legacy-Feld, nicht mehr intern genutzt).</summary>
        [NonSerialized] public float GroundCheckDistance = 0.254f;

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
        /// Erhöht auf 0.08m für CapsuleCast-Präzision bei Unity-Meshes.
        /// Zu kleine Werte führen dazu, dass der Spieler beim Laufen durch den Boden fällt.</summary>
        private const float GROUND_TRACE_DIST = 0.08f;

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

        private float m_LastJumpTime;
        private float m_LastGroundedTime;
        private float m_LastStepUpTime;
        private bool m_WasGroundedPrev;
        private float m_DeltaTime;
        private float m_JumpStartY;

        /// <summary>SoF2 pml.walking — auf begehbarem Boden (frame-local).</summary>
        private bool m_Walking;

        /// <summary>SoF2 pml.groundPlane — Boden-Ebene vorhanden, ggf. zu steil (frame-local).</summary>
        private bool m_GroundPlane;

        /// <summary>SoF2 pml.groundTrace — Ground-Trace-Ergebnis (frame-local).</summary>
        private RaycastHit m_GroundTrace;

        /// <summary>SoF2 pml.previous_velocity — Velocity vor diesem Frame (für CrashLand).</summary>
        private Vector3 m_PreviousVelocity;

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

        // ===================================================================
        // Public API
        // ===================================================================

        /// <summary>
        /// Setzt Capsule-Dimensionen (wird vom Client nach Bone-Berechnung
        /// und vom Server nach Empfang der Client-Daten aufgerufen).
        /// </summary>
        public void SetCapsuleDimensions(float height, float radius, Vector3 center, float groundCheckDist)
        {
            CapsuleHeight = height;
            CapsuleRadius = radius;
            CapsuleCenter = center;
            GroundCheckDistance = groundCheckDist;
        }

        /// <summary>
        /// Setzt den Simulations-State extern (für Reconciliation).
        /// Client ruft dies auf wenn der Server eine Korrektur sendet.
        /// </summary>
        public void SetState(Vector3 velocity, bool isGrounded, bool isJumping)
        {
            Velocity = velocity;
            IsGrounded = isGrounded;
            IsJumping = isJumping;
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

            // SoF2: save previous velocity for crash-landing detection
            m_PreviousVelocity = Velocity;

            // SoF2: release jump debounce when button released (PMD_JUMP clear)
            if (!cmd.HasButton(CommandButtons.Jump))
            {
                IsDebounceActive = false;
            }

            // SoF2: pm_time countdown (landing lockout)
            if (JumpDebounce > 0f)
            {
                JumpDebounce -= m_DeltaTime;
            }

            bool wasGrounded = IsGrounded;

            // 1. Ground trace before movement (SoF2: PM_GroundTrace)
            PM_GroundTrace(ref position);

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

            // 4. Landing detection
            JustLanded = !wasGrounded && IsGrounded;
            m_WasGroundedPrev = IsGrounded;

            if (IsGrounded)
            {
                m_LastGroundedTime = SimulationTime;
            }

            // 5. Airtime tracking: leaving ground
            if (wasGrounded && !IsGrounded)
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
            if (JustLanded)
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

            // 8. Landing event
            HandleLandingEvents(JustLanded, position.y);
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
            float halfHeight = Mathf.Max(0f, (CapsuleHeight * 0.5f) - CapsuleRadius);
            Vector3 center = GetWorldCenterAtPosition(position);
            Vector3 top = center + Vector3.up * halfHeight;
            Vector3 bottom = center - Vector3.up * halfHeight;

            if (!Physics.CapsuleCast(top, bottom, CapsuleRadius * 0.95f,
                    Vector3.down, out RaycastHit hit, GROUND_TRACE_DIST,
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

            // Erste Landung: CrashLand-Lockout (SoF2: PMF_TIME_LAND)
            if (!IsGrounded)
            {
                IsGrounded = true;
                IsJumping = false;

                // SoF2: previous_velocity[2] < -200 -> pm_time = 250
                if (m_PreviousVelocity.y < -5.08f)
                {
                    JumpDebounce = JumpDebounceAfterMs;
                }
            }

            // Position auf Boden korrigieren (Einsinken verhindern)
            CorrectGroundPosition(ref position, hit, halfHeight);
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
            m_LastJumpTime = SimulationTime;
            m_JumpStartY = currentY;
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
            if (m_Walking)
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
        /// SoF2 Velocity-Limits: horizontale Speed auf phys_maxwalkvelocity (Boden)
        /// bzw. phys_maxvelocity (Luft) begrenzen. Y bleibt unberührt.
        /// </summary>
        private void ApplyVelocityLimits()
        {
            Vector3 horizontalVel = new(Velocity.x, 0f, Velocity.z);
            float horizontalSpeed = horizontalVel.magnitude;

            float maxVelocity = IsGrounded ? PhysMaxWalkVelocity : PhysMaxVelocity;

            if (horizontalSpeed > maxVelocity)
            {
                float scale = maxVelocity / horizontalSpeed;
                Velocity.x *= scale;
                Velocity.z *= scale;
            }
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

            PM_Accelerate(wishdir, wishspeed, PmAccelerate);

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

            ApplyVelocityLimits();

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

            ApplyVelocityLimits();

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

            float halfHeight = Mathf.Max(0f, (CapsuleHeight * 0.5f) - CapsuleRadius);

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
                Vector3 top = center + Vector3.up * halfHeight;
                Vector3 bottom = center - Vector3.up * halfHeight;
                Vector3 castDirNorm = castDir / castDist;

                if (!Physics.CapsuleCast(top, bottom, CapsuleRadius,
                        castDirNorm, out RaycastHit trace, castDist + SKIN_WIDTH,
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

            return bumpcount != 0;
        }

        // ===================================================================
        // SoF2 PM_StepSlideMove (bg_slidemove.c)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_StepSlideMove — exakter Port.
        /// Versucht zuerst PM_SlideMove. Bei Kollision: Step-Up + SlideMove + Step-Down.
        /// </summary>
        private void PM_StepSlideMove(ref Vector3 position, bool gravity)
        {
            Vector3 startO = position;
            Vector3 startV = Velocity;

            // First try: regular slide
            if (!PM_SlideMove(ref position, gravity))
            {
                // No collision — got where we wanted
                ResolvePenetration(ref position);
                return;
            }

            float halfHeight = Mathf.Max(0f, (CapsuleHeight * 0.5f) - CapsuleRadius);

            // SoF2: trace down from start position to check for ground
            Vector3 center = GetWorldCenterAtPosition(startO);
            Vector3 top = center + Vector3.up * halfHeight;
            Vector3 bottom = center - Vector3.up * halfHeight;

            bool hasGroundBelow = Physics.CapsuleCast(top, bottom, CapsuleRadius * 0.95f,
                Vector3.down, out RaycastHit downFromStart, PmStepSize,
                GroundMask, QueryTriggerInteraction.Ignore);

            // SoF2: never step up when going up and (no ground or too steep)
            if (Velocity.y > 0f && (!hasGroundBelow || downFromStart.normal.y < PmMaxSteepness))
            {
                ResolvePenetration(ref position);
                return;
            }

            // Save first SlideMove result
            Vector3 downO = position;
            Vector3 downV = Velocity;

            // Reset to start
            position = startO;
            Velocity = startV;

            // Step up
            center = GetWorldCenterAtPosition(position);
            top = center + Vector3.up * halfHeight;
            bottom = center - Vector3.up * halfHeight;

            float stepSize = PmStepSize;
            if (Physics.CapsuleCast(bottom, top, CapsuleRadius,
                    Vector3.up, out RaycastHit upTrace, PmStepSize,
                    GroundMask, QueryTriggerInteraction.Ignore))
            {
                if (upTrace.distance < SKIN_WIDTH)
                {
                    // Can't step up at all — use first slide result
                    position = downO;
                    Velocity = downV;
                    ResolvePenetration(ref position);
                    return;
                }
                stepSize = Mathf.Max(upTrace.distance - SKIN_WIDTH, 0f);
            }

            position.y += stepSize;

            // Try SlideMove from stepped-up position (with original velocity)
            PM_SlideMove(ref position, gravity);

            // Push down the final amount
            center = GetWorldCenterAtPosition(position);
            top = center + Vector3.up * halfHeight;
            bottom = center - Vector3.up * halfHeight;

            if (Physics.CapsuleCast(top, bottom, CapsuleRadius * 0.95f,
                    Vector3.down, out RaycastHit stepDownTrace, stepSize + 0.01f,
                    GroundMask, QueryTriggerInteraction.Ignore))
            {
                float dropDist = Mathf.Max(stepDownTrace.distance - SKIN_WIDTH, 0f);
                position += Vector3.down * dropDist;

                if (stepDownTrace.normal.y >= PmMaxSteepness)
                {
                    PM_ClipVelocity(Velocity, stepDownTrace.normal, out Vector3 cv, OVERCLIP);
                    Velocity = cv;
                }
            }
            else
            {
                // Nothing below — drop full step (SoF2: trace.fraction == 1)
                position.y -= stepSize;
            }

            ResolvePenetration(ref position);
        }

        // ===================================================================
        // Utility Methods
        // ===================================================================

        /// <summary>
        /// Korrigiert die Position über dem Boden (verhindert Einsinken).
        /// </summary>
        private void CorrectGroundPosition(ref Vector3 position, RaycastHit downHit, float halfHeight)
        {
            Vector3 capsuleBottom = GetWorldCenterAtPosition(position) - Vector3.up * halfHeight;
            float desiredDistance = CapsuleRadius * 0.1f;
            float currentDistance = Vector3.Dot(capsuleBottom - downHit.point, downHit.normal);

            if (currentDistance < desiredDistance)
            {
                float correction = desiredDistance - currentDistance;
                position += downHit.normal * correction;
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
        /// Prüft per OverlapCapsule ob die Capsule in Geometrie steckt und
        /// schiebt sie iterativ heraus. Clippt Velocity gegen die Trennungs-Normalen.
        /// </summary>
        private void ResolvePenetration(ref Vector3 position)
        {
            float halfHeight = Mathf.Max(0f, (CapsuleHeight * 0.5f) - CapsuleRadius);

            for (int iteration = 0; iteration < MAX_DEPENETRATION_ITERATIONS; iteration++)
            {
                Vector3 worldCenter = GetWorldCenterAtPosition(position);
                Vector3 top = worldCenter + Vector3.up * halfHeight;
                Vector3 bottom = worldCenter - Vector3.up * halfHeight;

                Collider[] overlaps = Physics.OverlapCapsule(
                    top, bottom, CapsuleRadius,
                    GroundMask, QueryTriggerInteraction.Ignore);

                if (overlaps.Length == 0)
                {
                    return;
                }

                bool resolved = false;

                for (int i = 0; i < overlaps.Length; i++)
                {
                    Collider col = overlaps[i];

                    // ClosestPoint unterstützt nur Box, Sphere, Capsule und convex Mesh.
                    // Nicht-konvexe MeshCollider und TerrainCollider überspringen.
                    if (col is MeshCollider mc && !mc.convex)
                    {
                        continue;
                    }
                    if (col is TerrainCollider)
                    {
                        continue;
                    }

                    Vector3 closestPoint = col.ClosestPoint(worldCenter);
                    Vector3 diff = worldCenter - closestPoint;

                    if (diff.sqrMagnitude < 1e-8f)
                    {
                        diff = Vector3.up;
                    }

                    Vector3 normal = diff.normalized;
                    float penetrationDepth = CapsuleRadius - diff.magnitude;

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

        /// <summary>
        /// Landing-Events (Jump-Reset, Tracking).
        /// SoF2: Landing-Logik ist in PM_GroundTrace (IsJumping=false, JumpDebounce).
        /// </summary>
        private void HandleLandingEvents(bool justLanded, float currentY)
        {
            if (!justLanded)
            {
                return;
            }

            // IsJumping is already cleared in PM_GroundTrace.
            // JumpDebounce for hard landing is also set there.
            // Nothing additional needed.
        }
    }
}
