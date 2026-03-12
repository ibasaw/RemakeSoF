using System;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// SoF2/Quake3-Style Physik-Simulation.
    /// Serialisierbare C# Klasse (kein MonoBehaviour) — wird von Client (Prediction)
    /// und Server (Authority) mit identischem Code ausgeführt.
    /// Exakter Port der SoF2 bg_pmove.c Physik-Pipeline.
    /// Alle linearen Werte sind /10 skaliert (SoF2 → Unity).
    /// </summary>
    [Serializable]
    public class PlayerPhysicsSimulation
    {
        // ===== Physics Parameters (SoF2 Defaults, /10 skaliert) =====

        /// <summary>Boden-Beschleunigung (SoF2 pm_accelerate).</summary>
        public float PmAccelerate = 6.0f;

        /// <summary>Luft-Beschleunigung (SoF2 pm_airaccelerate).</summary>
        public float PmAirAccelerate = 1.0f;

        /// <summary>Boden-Reibung (SoF2 pm_friction).</summary>
        public float PmFriction = 6.0f;

        /// <summary>Stop-Speed Schwelle (SoF2 pm_stopspeed /10).</summary>
        public float PmStopSpeed = 10.0f;

        /// <summary>Maximum Wish-Speed / g_speed (SoF2: 280/10 = 28).</summary>
        public float PmMaxSpeed = 28.0f;

        /// <summary>Gravitation in Units/s² (SoF2: 800/10 = 80).</summary>
        public float PmGravity = 80.0f;

        /// <summary>Max horizontale Velocity in der Luft (SoF2: 320/10 = 32).</summary>
        public float PhysMaxVelocity = 32f;

        /// <summary>Max horizontale Velocity am Boden (SoF2: 320/10 = 32).</summary>
        public float PhysMaxWalkVelocity = 32f;

        /// <summary>Sofortige Y-Velocity beim Sprung (SoF2: 270/10 = 27).</summary>
        public float JumpVelocity = 27.0f;

        /// <summary>Maximale Steigung (Dot mit Up). SoF2 MIN_WALK_NORMAL = 0.7.</summary>
        public float PmMaxSteepness = 0.7f;

        /// <summary>Maximale Step-Hoehe (SoF2: 18/10 = 1.8).</summary>
        public float PmMaxStep = 1.8f;

        /// <summary>Step-Size für Step-Up (SoF2 STEPSIZE: 18/10 = 1.8).</summary>
        public float PmStepSize = 1.8f;

        /// <summary>Maximale Barriere-Hoehe (SoF2: 32/10 = 3.2).</summary>
        public float PmMaxBarrier = 3.2f;

        /// <summary>Duck Speed Scale (SoF2 PM_DUCKSCALE = 0.25).</summary>
        public float PmDuckScale = 0.25f;

        /// <summary>Jump-Debounce nach Landung (Sekunden).</summary>
        public float JumpDebounceAfterMs = 0.25f;

        /// <summary>Höhen-Schwelle ab der ein Sprung als Step-Up gilt und Debounce übersprungen wird.</summary>
        public float StepUpHeightThreshold = 0.7f;

        /// <summary>Ground Grace-Period (Sekunden).</summary>
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

        /// <summary>Ground-Check-Distanz für CapsuleCast nach unten.</summary>
        [NonSerialized] public float GroundCheckDistance = 1f;

        // ===== Constants =====

        /// <summary>Overclip-Konstante für PM_ClipVelocity (SoF2-Wert).</summary>
        private const float OVERCLIP = 1.001f;

        /// <summary>Skin-Width: minimaler Abstand zu Oberflächen.</summary>
        private const float SKIN_WIDTH = 0.01f;

        // ===== Simulation State (Runtime, nicht serialisiert) =====

        /// <summary>Aktuelle Velocity (Welt-Raum, XYZ).</summary>
        [NonSerialized] public Vector3 Velocity;

        /// <summary>Auf dem Boden.</summary>
        [NonSerialized] public bool IsGrounded;

        /// <summary>Springt gerade (bis zur nächsten Landung).</summary>
        [NonSerialized] public bool IsJumping;

        /// <summary>Geduckt.</summary>
        [NonSerialized] public bool IsCrouching;

        /// <summary>Jump-Debounce aktiv (nach Landung).</summary>
        [NonSerialized] public bool IsDebounceActive;

        /// <summary>Jump-Debounce Timer (countdown).</summary>
        [NonSerialized] public float JumpDebounce;

        /// <summary>Gespeicherter Ground-Hit für Slope-Berechnungen.</summary>
        [NonSerialized] public RaycastHit LastGroundHit;

        /// <summary>Landing-Event bereits gefeuert (verhindert Doppel-Trigger).</summary>
        [NonSerialized] public bool LandedThisGround;

        /// <summary>Simulations-Zeitstempel (akkumuliert aus cmd.DeltaTime).</summary>
        [NonSerialized] public float SimulationTime;

        /// <summary>True wenn ein Jump in diesem Step ausgelöst wurde (für Animation-Trigger).</summary>
        public bool JumpTriggered { get; private set; }

        /// <summary>True wenn in diesem Step gelanded wurde.</summary>
        public bool JustLanded { get; private set; }

        private float m_LastJumpTime;
        private float m_LastGroundedTime;
        private float m_LastStepUpTime;
        private bool m_WasGroundedPrev;
        private float m_DeltaTime;
        private float m_JumpStartY;

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

        /// <summary>
        /// Führt einen kompletten Physik-Step aus.
        /// Exakte SoF2 bg_pmove.c Pipeline:
        /// JumpDebounce → Jump → Gravity → Move → GroundCheck →
        /// WalkMove/AirMove → Landing.
        /// </summary>
        /// <param name="position">Aktuelle Position (wird modifiziert).</param>
        /// <param name="cmd">Player-Command mit Input-Daten.</param>
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

            // Jump-Debounce Timer
            if (IsDebounceActive && JumpDebounce > 0f)
            {
                JumpDebounce -= m_DeltaTime;
                if (JumpDebounce <= 0f)
                {
                    IsDebounceActive = false;
                }
            }

            // Jump Command verarbeiten
            if (cmd.HasButton(CommandButtons.Jump))
            {
                ProcessJump();
            }

            // Jump-Startposition merken für Height-Based Debounce
            if (JumpTriggered)
            {
                m_JumpStartY = position.y;
            }

            bool wasGrounded = m_WasGroundedPrev;

            // 1. Gravity anwenden
            ApplyGravity();

            // 2. Character bewegen (PM_StepSlideMove — Collision + Sliding)
            PM_StepSlideMove(ref position);

            // 3. Ground-Check (inklusive Slope + Grace-Period Fallbacks)
            CheckGroundedState(position, wasGrounded);

            // 4. Ground-State Updates
            if (IsGrounded)
            {
                m_LastGroundedTime = SimulationTime;
            }

            // 5. Edge Detection: gerade gelandet?
            JustLanded = !wasGrounded && IsGrounded;
            m_WasGroundedPrev = IsGrounded;

            // 6. Friction + Acceleration (setzt Velocity für nächsten Frame)
            if (IsGrounded)
            {
                PM_WalkMove(cmd);
            }
            else
            {
                PM_AirMove(cmd);
            }

            // 7. Landing-Events
            HandleLandingEvents(JustLanded, position.y);
        }

        // ===================================================================
        // SoF2 Physics Engine — exakt portiert von bg_pmove.c
        // ===================================================================

        /// <summary>
        /// Gravity anwenden (SoF2 ApplyGravity).
        /// Nur in der Luft: velocity.y -= pm_gravity * dt.
        /// Am Boden: Y-Velocity wird NICHT genullt, damit die Slope-Projektion
        /// aus WalkMove's PM_ClipVelocity erhalten bleibt und StepSlideMove
        /// der Schräge folgt statt horizontal zu laufen (Treppen-Effekt).
        /// Y wird nach Bewegung in PM_StepSlideMove's Ground-Handling genullt.
        /// </summary>
        private void ApplyGravity()
        {
            if (!IsGrounded)
            {
                Velocity.y -= PmGravity * m_DeltaTime;
                LandedThisGround = false;
            }
        }

        /// <summary>
        /// Jump ausführen (SoF2 TryJump).
        /// Setzt velocity.y = jumpVelocity, isJumping = true.
        /// </summary>
        private void ProcessJump()
        {
            if (IsDebounceActive)
            {
                return;
            }

            if (!IsGrounded)
            {
                return;
            }

            if (IsJumping)
            {
                return;
            }

            IsJumping = true;
            IsDebounceActive = false;
            Velocity.y = JumpVelocity;
            m_LastJumpTime = SimulationTime;
            JumpTriggered = true;
        }

        /// <summary>
        /// SoF2 PM_Friction — Bodenreibung (Luft: kein Drop).
        /// Exakter Port aus bg_pmove.c mit zusätzlicher Slope-Friction.
        /// </summary>
        private void PM_Friction()
        {
            Vector3 vec = Velocity;

            if (IsGrounded)
            {
                vec.y = 0f;
            }

            float speed = vec.magnitude;
            float drop = 0f;

            if (speed < 1f)
            {
                Velocity.x = 0f;
                Velocity.z = 0f;
                return;
            }

            if (IsGrounded)
            {
                float control = speed < PmStopSpeed ? PmStopSpeed : speed;
                drop += control * PmFriction * m_DeltaTime;

                // Zusätzliche Slope-Friction (Custom, nicht im SoF2-Original)
                if (LastGroundHit.collider != null)
                {
                    float slopeDot = Vector3.Dot(LastGroundHit.normal, Vector3.up);
                    if (slopeDot < 0.9f)
                    {
                        drop += control * PmFriction * 1.0f * m_DeltaTime;
                    }

                    if (slopeDot < 0.7f)
                    {
                        drop += control * PmFriction * 1.5f * m_DeltaTime;
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
                Velocity.x *= newspeed;
                Velocity.z *= newspeed;
                if (IsGrounded)
                {
                    Velocity.y *= newspeed;
                }
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
        /// Unitys DigitalNormalized-Dpad liefert bei Diagonal bereits (0.707, 0.707)
        /// mit Magnitude 1.0, daher reicht Clamp01 fuer konsistente Speed
        /// in allen Bewegungsrichtungen (cardinal + diagonal).
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

        /// <summary>
        /// SoF2 PM_CheckJump — prüft ob gerade gesprungen wird.
        /// </summary>
        private bool PM_CheckJump()
        {
            if (IsDebounceActive)
            {
                return false;
            }

            return IsJumping;
        }

        /// <summary>
        /// SoF2 PM_WalkMove — Boden-Bewegung.
        /// Prüft Jump, wendet Friction an, berechnet Wish-Direction
        /// projiziert auf Ground-Plane, beschleunigt.
        /// Forward/Right werden aus dem Command-YawAngle berechnet (kein Transform nötig).
        /// </summary>
        private void PM_WalkMove(PlayerCommand cmd)
        {
            if (PM_CheckJump())
            {
                PM_AirMove(cmd);
                return;
            }

            PM_Friction();

            // Bewegungsrichtung aus YawAngle (Kamera-Blickrichtung)
            Quaternion yawRotation = Quaternion.Euler(0f, cmd.YawAngle, 0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;

            // Auf Ground-Plane projizieren
            Vector3 groundNormal = Vector3.up;
            if (LastGroundHit.collider != null)
            {
                float slopeThreshold = PmMaxSteepness > 1f
                    ? Mathf.Cos(PmMaxSteepness * Mathf.Deg2Rad)
                    : PmMaxSteepness;
                if (Vector3.Dot(LastGroundHit.normal, Vector3.up) > slopeThreshold)
                {
                    groundNormal = LastGroundHit.normal;
                }
            }

            forward = Vector3.ProjectOnPlane(forward, groundNormal).normalized;
            right = Vector3.ProjectOnPlane(right, groundNormal).normalized;

            // SoF2-Style input scaling (fmove/smove = ±127)
            float fmove = cmd.MoveInput.y * 127f;
            float smove = cmd.MoveInput.x * 127f;
            Vector3 wishvel = forward * fmove + right * smove;

            if (wishvel.sqrMagnitude > 0.01f)
            {
                wishvel = Vector3.ProjectOnPlane(wishvel, groundNormal);
            }

            float scale = PM_CmdScale(cmd.MoveInput);

            Vector3 wishdir = wishvel.sqrMagnitude > 0.0001f
                ? wishvel.normalized
                : Vector3.zero;
            float wishspeed = scale * PmMaxSpeed;

            if (wishspeed > PmMaxSpeed)
            {
                wishspeed = PmMaxSpeed;
            }

            PM_Accelerate(wishdir, wishspeed, PmAccelerate);

            // SoF2: Velocity auf Ground-Plane clippen + Speed erhalten.
            // Verhindert Speed-Verlust auf Slopes (bg_pmove.c PM_WalkMove).
            if (LastGroundHit.collider != null)
            {
                float vel = Velocity.magnitude;
                PM_ClipVelocity(Velocity, LastGroundHit.normal, out Vector3 clipped, OVERCLIP);
                Velocity = clipped;
                float clippedMag = Velocity.magnitude;
                if (clippedMag > 0.001f)
                {
                    Velocity = Velocity / clippedMag * vel;
                }
            }

            // SoF2: nicht bewegen wenn stillstehend
            if (Mathf.Abs(Velocity.x) < 0.001f && Mathf.Abs(Velocity.z) < 0.001f)
            {
                return;
            }

            ApplyVelocityLimits();
        }

        /// <summary>
        /// SoF2 PM_AirMove — Luft-Bewegung.
        /// Kein Ground-Plane-Projektion, niedrigere Acceleration (pm_airaccelerate).
        /// Ermöglicht Strafe-Jumping durch Q3-Accelerate-Projektion.
        /// Forward/Right werden aus dem Command-YawAngle berechnet.
        /// </summary>
        private void PM_AirMove(PlayerCommand cmd)
        {
            PM_Friction();

            Quaternion yawRotation = Quaternion.Euler(0f, cmd.YawAngle, 0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            float fmove = cmd.MoveInput.y * 127f;
            float smove = cmd.MoveInput.x * 127f;
            Vector3 wishvel = forward * fmove + right * smove;
            wishvel.y = 0f;

            float scale = PM_CmdScale(cmd.MoveInput);

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

            if (wishspeed > PmMaxSpeed)
            {
                wishspeed = PmMaxSpeed;
            }

            PM_Accelerate(wishdir, wishspeed, PmAirAccelerate);
            ApplyVelocityLimits();
        }

        // ===================================================================
        // Collision & Step-Up (PM_StepSlideMove + TryStepUp)
        // ===================================================================

        /// <summary>
        /// SoF2 PM_StepSlideMove — Kern-Collision-Handling.
        /// CapsuleCast in Bewegungsrichtung, bei Hit: Step-Up versuchen oder
        /// Velocity clippen und entlang der Oberfläche sliden.
        /// Bis zu 4 Bumps pro Frame (Ecken, komplexe Geometrie).
        /// </summary>
        private void PM_StepSlideMove(ref Vector3 position)
        {
            int numbumps = 4;
            Vector3 currentPos = position;
            float timeLeft = 1.0f;

            float halfHeightLocal = Mathf.Max(0f,
                (CapsuleHeight * 0.5f) - CapsuleRadius);

            Vector3 vel = Velocity;
            bool stepUpAttempted = false;

            for (int bump = 0; bump < numbumps; bump++)
            {
                Vector3 worldCenter = GetWorldCenterAtPosition(currentPos);
                Vector3 top = worldCenter + Vector3.up * halfHeightLocal;
                Vector3 bottom = worldCenter - Vector3.up * halfHeightLocal;

                Vector3 end = currentPos + vel * m_DeltaTime * timeLeft;
                Vector3 castDir = end - currentPos;
                float castDist = castDir.magnitude;

                if (castDist < 1e-6f)
                {
                    if (vel.magnitude < 0.1f && IsGrounded)
                    {
                        vel *= 0.5f;
                    }

                    break;
                }

                Vector3 castDirNorm = castDir / castDist;

                if (Physics.CapsuleCast(top, bottom, CapsuleRadius,
                        castDirNorm, out RaycastHit hit, castDist + SKIN_WIDTH,
                        GroundMask, QueryTriggerInteraction.Ignore))
                {
                    // Step-Up versuchen (einmal pro Frame)
                    if (!stepUpAttempted && TryStepUp(currentPos, hit, out Vector3 stepUpPos))
                    {
                        currentPos = stepUpPos;
                        stepUpAttempted = true;

                        Vector3 remainingMovement = vel * m_DeltaTime * timeLeft;
                        remainingMovement.y = 0f;

                        if (remainingMovement.magnitude > 0.001f)
                        {
                            Vector3 newTop = GetWorldCenterAtPosition(currentPos) + Vector3.up * halfHeightLocal;
                            Vector3 newBottom = GetWorldCenterAtPosition(currentPos) - Vector3.up * halfHeightLocal;

                            if (!Physics.CapsuleCast(newTop, newBottom, CapsuleRadius,
                                    remainingMovement.normalized, out RaycastHit _,
                                    remainingMovement.magnitude + SKIN_WIDTH,
                                    GroundMask, QueryTriggerInteraction.Ignore))
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

                    // Velocity entlang der Oberfläche clippen (Slide)
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

            Velocity = vel;
            position = currentPos;

            // Finaler Ground-Check nach Bewegung
            bool wasGroundedBeforeMove = IsGrounded;
            bool newGroundCheck = CheckGroundedAtPosition(currentPos, out RaycastHit downHit);

            // Slope-Fallback wenn vorher grounded
            if (!newGroundCheck && wasGroundedBeforeMove)
            {
                newGroundCheck = TrySlopeGroundCheck(currentPos, halfHeightLocal, out downHit);
            }

            if (!wasGroundedBeforeMove || newGroundCheck)
            {
                IsGrounded = newGroundCheck;
            }

            // Am Boden: Position korrigieren (Einsinken verhindern).
            // Y-Velocity wird NICHT genullt — WalkMove's PM_ClipVelocity + Speed-Restore
            // setzt jeden Frame die korrekte Slope-Velocity (inkl. Y-Komponente).
            // StepSlideMove braucht dieses Y um der Schräge zu folgen statt
            // horizontal dagegen zu laufen (was zu Kollisionen + Speed-Verlust führt).
            if (IsGrounded && downHit.collider != null)
            {
                CorrectGroundPosition(ref position, downHit, halfHeightLocal);
            }
        }

        /// <summary>
        /// SoF2 Step-Up Logik: versucht über ein Hindernis zu steigen.
        /// Prüft Hindernis-Höhe, Headroom, horizontale Blockade, Boden-Verification.
        /// </summary>
        private bool TryStepUp(Vector3 currentPos, RaycastHit hit, out Vector3 stepUpPos)
        {
            stepUpPos = currentPos;

            if (!IsGrounded || Mathf.Abs(Velocity.y) > 1.0f)
            {
                return false;
            }

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.1f)
            {
                return false;
            }

            float obstacleHeight = hit.point.y - (currentPos.y - CapsuleRadius);

            if (obstacleHeight > PmMaxBarrier)
            {
                return false;
            }

            if (obstacleHeight > PmMaxStep || obstacleHeight < 0.5f)
            {
                return false;
            }

            float stepUpAmount = Mathf.Min(obstacleHeight + 0.1f, PmStepSize);
            Vector3 stepUpTarget = currentPos + Vector3.up * stepUpAmount;

            float halfHeight = Mathf.Max(0f,
                (CapsuleHeight * 0.5f) - CapsuleRadius);
            Vector3 worldCenter = GetWorldCenterAtPosition(stepUpTarget);
            Vector3 top = worldCenter + Vector3.up * halfHeight;
            Vector3 bottom = worldCenter - Vector3.up * halfHeight;

            // Ceiling-Check
            if (Physics.CapsuleCast(bottom, top, CapsuleRadius,
                    Vector3.up, out RaycastHit _, PmStepSize,
                    GroundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            // Horizontaler Obstacle-Check
            Vector3 horizontalVel = new(Velocity.x, 0f, Velocity.z);
            if (horizontalVel.magnitude > 0.1f)
            {
                Vector3 horizontalDir = horizontalVel.normalized;
                float checkDistance = Mathf.Min(horizontalVel.magnitude * m_DeltaTime, 0.5f);

                Vector3 stepTop = GetWorldCenterAtPosition(stepUpTarget) + Vector3.up * halfHeight;
                Vector3 stepBottom = GetWorldCenterAtPosition(stepUpTarget) - Vector3.up * halfHeight;

                if (Physics.CapsuleCast(stepBottom, stepTop, CapsuleRadius,
                        horizontalDir, out RaycastHit horizontalHit, checkDistance + SKIN_WIDTH,
                        GroundMask, QueryTriggerInteraction.Ignore))
                {
                    if (horizontalHit.point.y <= hit.point.y)
                    {
                        return false;
                    }
                }
            }

            // Ground-Verification: Cast nach unten, begehbarer Boden muss existieren
            float groundCheckDist = stepUpAmount + GroundCheckDistance;
            if (!Physics.CapsuleCast(top, bottom, CapsuleRadius * 0.95f,
                    Vector3.down, out RaycastHit groundHit, groundCheckDist,
                    GroundMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            float slopeThreshold = PmMaxSteepness > 1f
                ? Mathf.Cos(PmMaxSteepness * Mathf.Deg2Rad)
                : PmMaxSteepness;
            if (Vector3.Dot(groundHit.normal, Vector3.up) < slopeThreshold)
            {
                return false;
            }

            stepUpPos = stepUpTarget;
            m_LastStepUpTime = SimulationTime;
            return true;
        }

        // ===================================================================
        // Ground Detection
        // ===================================================================

        /// <summary>
        /// Einheitlicher Ground-Check per CapsuleCast nach unten.
        /// Berücksichtigt Jump-Grace-Period und Slope-Threshold.
        /// </summary>
        private bool CheckGroundedAtPosition(Vector3 position, out RaycastHit groundHit)
        {
            groundHit = new RaycastHit();

            // Jump-Grace: kurz nach Sprung keinen Boden erkennen
            if (IsJumping && (SimulationTime - m_LastJumpTime) < 0.01f)
            {
                return false;
            }

            if (IsJumping && Velocity.y > 5f)
            {
                return false;
            }

            float halfHeight = Mathf.Max(0f,
                (CapsuleHeight * 0.5f) - CapsuleRadius);
            Vector3 center = GetWorldCenterAtPosition(position);
            Vector3 top = center + Vector3.up * halfHeight;
            Vector3 bottom = center - Vector3.up * halfHeight;

            float baseDistance = GroundCheckDistance;
            float verticalComponent = Mathf.Abs(Velocity.y) * m_DeltaTime;
            float horizontalComponent = new Vector3(Velocity.x, 0f, Velocity.z).magnitude * m_DeltaTime;
            float dynamicCastDistance = baseDistance + 0.01f + verticalComponent + horizontalComponent * 0.5f;

            if (Physics.CapsuleCast(top, bottom, CapsuleRadius * 0.95f,
                    Vector3.down, out RaycastHit hit, dynamicCastDistance,
                    GroundMask, QueryTriggerInteraction.Ignore))
            {
                float slopeThreshold = PmMaxSteepness > 1f
                    ? Mathf.Cos(PmMaxSteepness * Mathf.Deg2Rad)
                    : PmMaxSteepness;

                if (Vector3.Dot(hit.normal, Vector3.up) > slopeThreshold)
                {
                    if (IsJumping && Velocity.y > 1.0f)
                    {
                        return false;
                    }

                    groundHit = hit;
                    LastGroundHit = hit;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Triple Ground-Check: Standard → Slope-Fallback → Coyote Time.
        /// </summary>
        private void CheckGroundedState(Vector3 position, bool wasGrounded)
        {
            IsGrounded = CheckGroundedAtPosition(position, out RaycastHit _);

            // Slope-Fallback mit 4x Distanz
            if (!IsGrounded)
            {
                float halfHeight = Mathf.Max(0f,
                    (CapsuleHeight * 0.5f) - CapsuleRadius);
                IsGrounded = TrySlopeGroundCheck(position, halfHeight, out RaycastHit _);
            }

            // Coyote Time: kurz nach Bodenverlust weiterhin als grounded behandeln.
            // Verhindert Ground-Flicker auf Schrägen/unebenem Terrain und
            // erlaubt Springen kurz nach Verlassen einer Kante.
            // Nicht aktiv während eines echten Sprungs (IsJumping = true).
            if (!IsGrounded && wasGrounded && !IsJumping &&
                (SimulationTime - m_LastGroundedTime) < GroundGracePeriod)
            {
                IsGrounded = true;
            }
        }

        /// <summary>
        /// Aggressive Slope Ground-Check mit 4x Distanz.
        /// </summary>
        private bool TrySlopeGroundCheck(Vector3 position, float halfHeight, out RaycastHit slopeHit)
        {
            slopeHit = new RaycastHit();
            Vector3 center = GetWorldCenterAtPosition(position);
            Vector3 top = center + Vector3.up * halfHeight;
            Vector3 bottom = center - Vector3.up * halfHeight;

            float slopeCheckDistance = GroundCheckDistance * 4f;

            if (Physics.CapsuleCast(top, bottom, CapsuleRadius * 0.95f,
                    Vector3.down, out RaycastHit hit, slopeCheckDistance,
                    GroundMask, QueryTriggerInteraction.Ignore))
            {
                float slopeThreshold = PmMaxSteepness > 1f
                    ? Mathf.Cos(PmMaxSteepness * Mathf.Deg2Rad)
                    : PmMaxSteepness;

                if (Vector3.Dot(hit.normal, Vector3.up) > slopeThreshold)
                {
                    slopeHit = hit;
                    LastGroundHit = hit;
                    IsGrounded = true;
                    return true;
                }
            }

            return false;
        }

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
        /// Für aufrechte Charaktere (nur Y-Rotation): position + CapsuleCenter.
        /// </summary>
        private Vector3 GetWorldCenterAtPosition(Vector3 position)
        {
            return position + CapsuleCenter;
        }

        /// <summary>
        /// Landing-Events (Debounce-Aktivierung, Jump-Reset).
        /// Wenn der Spieler auf einer höheren Ebene landet (Step-Up),
        /// wird der Debounce übersprungen damit sofort weitergesprungen werden kann.
        /// </summary>
        private void HandleLandingEvents(bool justLanded, float currentY)
        {
            if (!justLanded || LandedThisGround)
            {
                return;
            }

            LandedThisGround = true;

            if (IsJumping)
            {
                float heightDifference = currentY - m_JumpStartY;
                bool landedHigher = heightDifference > StepUpHeightThreshold;

                IsJumping = false;

                if (landedHigher)
                {
                    // Auf höherer Ebene gelandet → Debounce sofort aufheben
                    JumpDebounce = 0f;
                    IsDebounceActive = false;
                }
                else
                {
                    // Normal gelandet → Debounce aktivieren
                    IsDebounceActive = true;
                    JumpDebounce = JumpDebounceAfterMs;
                }
            }
        }
    }
}
