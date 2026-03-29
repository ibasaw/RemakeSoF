using Unity.Cinemachine;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Camera
{
    /// <summary>
    /// Cinemachine-Extension fuer SoF2-style First-Person Kamera-Effekte.
    /// Implementiert CG_OffsetFirstPersonView aus cg_view.c:
    /// Bob (Pitch/Roll/Height), Landing-Deflection, Duck-Smoothing
    /// und Velocity-basierte Winkel-Offsets.
    /// Wird auf die First-Person CinemachineCamera gelegt.
    /// </summary>
    [AddComponentMenu("")]
    public class FirstPersonCameraEffects : CinemachineExtension
    {
        // ===== SoF2 Bob Constants (×0.0254 konvertiert fuer Unity m/s) =====

        /// <summary>
        /// SoF2: cg_runpitch = 0.002 (QU-basiert).
        /// Unity: 0.002 / 0.0254 = 0.07874 (m/s-basiert).
        /// Velocity forward → Pitch-Offset.
        /// </summary>
        private const float RUN_PITCH_FACTOR = 0.07874f;

        /// <summary>
        /// SoF2: cg_runroll = 0.005 (QU-basiert).
        /// Unity: 0.005 / 0.0254 = 0.1969 (m/s-basiert).
        /// Velocity seitwaerts → Roll-Offset.
        /// </summary>
        private const float RUN_ROLL_FACTOR = 0.1969f;

        /// <summary>
        /// SoF2: cg_bobpitch = 0.001 (QU-basiert).
        /// Unity: 0.001 / 0.0254 = 0.03937 (m/s-basiert).
        /// Bob-Zyklus → Pitch-Offset.
        /// </summary>
        private const float BOB_PITCH_FACTOR = 0.03937f;

        /// <summary>
        /// SoF2: cg_bobroll = 0.001 (QU-basiert).
        /// Unity: 0.001 / 0.0254 = 0.03937 (m/s-basiert).
        /// Bob-Zyklus → Roll-Offset.
        /// </summary>
        private const float BOB_ROLL_FACTOR = 0.03937f;

        /// <summary>
        /// SoF2: cg_bobup = 0.005 → direkt in Meter (speed_m × 0.005).
        /// Bob-Zyklus → vertikaler Positions-Offset.
        /// </summary>
        private const float BOB_UP_FACTOR = 0.005f;

        /// <summary>
        /// SoF2: bob max = 6 QU = 0.1524m.
        /// Maximaler vertikaler Bob-Offset.
        /// </summary>
        private const float BOB_UP_MAX = 0.1524f;

        /// <summary>
        /// SoF2: Minimum Speed fuer Bob-Amplitude = 200 QU/s = 5.08 m/s.
        /// Unter dieser Geschwindigkeit wird trotzdem mit diesem Minimum gerechnet.
        /// </summary>
        private const float BOB_MIN_SPEED_MPS = 5.08f;

        /// <summary>
        /// SoF2: Crouch-Bob-Multiplikator = 3.
        /// Im Ducken werden Bob-Pitch und Bob-Roll verdreifacht.
        /// </summary>
        private const float CROUCH_BOB_MULTIPLIER = 3f;

        // ===== SoF2 Timing Constants (Sekunden) =====

        /// <summary>SoF2: LAND_DEFLECT_TIME = 150ms. Phase in der die Kamera nach unten gedrueckt wird.</summary>
        private const float LAND_DEFLECT_TIME = 0.15f;

        /// <summary>SoF2: LAND_RETURN_TIME = 300ms. Phase in der die Kamera zurueckfedert.</summary>
        private const float LAND_RETURN_TIME = 0.3f;

        /// <summary>SoF2: DUCK_TIME = 100ms. Smoothing-Dauer beim Ducken/Aufstehen.</summary>
        private const float DUCK_TIME = 0.1f;

        // ===== SoF2 Bob Cycle Constants =====

        /// <summary>SoF2: bobmove fuer Walking (0.4 per ms).</summary>
        private const float BOB_MOVE_WALK = 0.4f;

        /// <summary>SoF2: bobmove fuer Crouching (0.5 per ms).</summary>
        private const float BOB_MOVE_CROUCH = 0.5f;

        /// <summary>Minimum Horizontal-Speed ab der Bob-Phase fortschreitet (m/s).</summary>
        private const float BOB_SPEED_THRESHOLD = 0.5f;

        /// <summary>Fade-Out-Rate fuer BobFracSin wenn nicht am Boden/nicht bewegt.</summary>
        private const float BOB_FADE_RATE = 5f;

        /// <summary>SoF2: Standing Eye-Height (2.2606m × 72/89 = 1.829m).</summary>
        private const float STANDING_EYE_HEIGHT = 2.2606f * (72f / 89f);

        /// <summary>SoF2: Crouching Eye-Height (1.6256m × 72/89 = 1.316m).</summary>
        private const float CROUCHING_EYE_HEIGHT = 1.6256f * (72f / 89f);

        /// <summary>Landing-Change Skalierung: FallHeight → Kamera-Offset.</summary>
        private const float LAND_CHANGE_SCALE = 0.05f;

        /// <summary>Maximaler Landing-Offset in Metern.</summary>
        private const float LAND_CHANGE_MAX = 0.1f;

        // ===== Bob Cycle State =====

        /// <summary>Laufender Bob-Phasen-Zaehler (float-Akku, SoF2: 8-bit bobCycle).</summary>
        private float m_BobPhaseRaw;

        /// <summary>Bob-Zyklus-Toggle (SoF2 Bit 7: alterniert bei Halbzyklus fuer Roll-Richtung).</summary>
        private int m_BobCycle;

        /// <summary>Aktueller Bob-Sinus-Wert (|sin(phase)|, SoF2: bobfracsin).</summary>
        private float m_BobFracSin;

        // ===== Landing State =====

        /// <summary>Zeitpunkt der letzten Landung (Time.time).</summary>
        private float m_LandTime = -10f;

        /// <summary>Hoehen-Offset bei Landung (negativ = nach unten druecken).</summary>
        private float m_LandChange;

        // ===== Duck State =====

        /// <summary>Zeitpunkt der letzten Ducken/Aufstehen-Aenderung (Time.time).</summary>
        private float m_DuckTime = -10f;

        /// <summary>Hoehenaenderung beim Ducken/Aufstehen (positiv = von oben nach unten).</summary>
        private float m_DuckChange;

        /// <summary>Vorheriger Crouch-Zustand (fuer Erkennung von Wechseln).</summary>
        private bool m_WasCrouching;

        // ===== External Data (gesetzt von ClientPlayerCharacter) =====

        /// <summary>Aktuelle Velocity des Spielers (Welt-Raum, m/s).</summary>
        private Vector3 m_Velocity;

        /// <summary>Ob der Spieler auf dem Boden steht.</summary>
        private bool m_IsGrounded;

        /// <summary>Ob der Spieler geduckt ist.</summary>
        private bool m_IsCrouching;

        /// <summary>
        /// Y-Offset zwischen Pitch-Bone-Position und Server-Eye-Height.
        /// Da die FP-Kamera dem Pitch-Bone folgt (fuer korrekte Rotation),
        /// muss die Position vertikal korrigiert werden um exakt auf Augenhoehe zu sitzen.
        /// Wird jeden Frame von ClientPlayerCharacter aktualisiert, da der Pitch-Bone
        /// je nach Waffen-Animation auf unterschiedlicher Hoehe liegt.
        /// </summary>
        private float m_EyeHeightOffset;

        /// <summary>
        /// SoF2 Weapon View-Offset in Unity-Metern (bereits konvertiert aus QU).
        /// X = Right, Y = Up, Z = Forward.
        /// Wird bei jedem Waffenwechsel von ClientPlayerCharacter aktualisiert.
        /// </summary>
        private Vector3 m_WeaponViewOffset;

        /// <summary>Quake-Units zu Unity-Meter Konvertierung.</summary>
        private const float QU_TO_METERS = 0.0254f;

        /// <summary>
        /// Setzt den vertikalen Offset zwischen Pitch-Bone und Server-Augenhoehe.
        /// Wird jeden Frame aufgerufen, da der Pitch-Bone je nach Waffen-Animation variiert.
        /// </summary>
        /// <param name="offset">Differenz in Metern (positiv = Kamera muss hoeher).</param>
        public void SetEyeHeightOffset(float offset)
        {
            m_EyeHeightOffset = offset;
        }

        /// <summary>
        /// Setzt den waffenspezifischen View-Offset (SoF2 viewoffset).
        /// Konvertiert Quake-Units in Unity-Meter.
        /// Wird bei jedem Waffenwechsel aufgerufen.
        /// </summary>
        /// <param name="forwardQU">Vorwaerts-Offset in Quake-Units.</param>
        /// <param name="rightQU">Seitwaerts-Offset in Quake-Units (positiv = rechts).</param>
        /// <param name="upQU">Vertikal-Offset in Quake-Units (positiv = oben).</param>
        public void SetWeaponViewOffset(float forwardQU, float rightQU, float upQU)
        {
            m_WeaponViewOffset = new Vector3(
                rightQU * QU_TO_METERS,
                upQU * QU_TO_METERS,
                forwardQU * QU_TO_METERS);
        }

        /// <summary>
        /// Aktualisiert die Physik-Daten fuer die Kamera-Effekte.
        /// Wird jeden Frame von ClientPlayerCharacter.Update() aufgerufen,
        /// bevor Cinemachine in LateUpdate die Pipeline ausfuehrt.
        /// </summary>
        /// <param name="velocity">Aktuelle Spieler-Velocity (Welt-Raum, m/s).</param>
        /// <param name="isGrounded">Ob der Spieler auf dem Boden steht.</param>
        /// <param name="isCrouching">Ob der Spieler geduckt ist.</param>
        /// <param name="justLanded">Ob der Spieler in diesem Frame gelandet ist.</param>
        /// <param name="fallHeight">Fallhoehe in Metern (nur relevant wenn justLanded=true).</param>
        public void UpdatePhysicsData(
            Vector3 velocity,
            bool isGrounded,
            bool isCrouching,
            bool justLanded,
            float fallHeight)
        {
            m_Velocity = velocity;
            m_IsGrounded = isGrounded;

            // Landing Detection: Kamera nach unten druecken proportional zur Fallhoehe
            if (justLanded && fallHeight > 0.01f)
            {
                m_LandTime = Time.time;
                m_LandChange = -Mathf.Min(fallHeight * LAND_CHANGE_SCALE, LAND_CHANGE_MAX);
            }

            // Duck Detection: Smooth-Transition bei Crouch-Wechsel
            if (isCrouching != m_WasCrouching)
            {
                m_DuckTime = Time.time;
                float heightDelta = STANDING_EYE_HEIGHT - CROUCHING_EYE_HEIGHT;
                m_DuckChange = isCrouching ? heightDelta : -heightDelta;
                m_WasCrouching = isCrouching;
            }

            m_IsCrouching = isCrouching;
        }

        /// <summary>
        /// Cinemachine-Pipeline Callback. Wendet SoF2 First-Person Offsets an:
        /// Bob (Position + Rotation), Landing-Deflection, Duck-Smoothing,
        /// Velocity-basierte Pitch/Roll.
        /// Laeuft nur im Finalize-Stage, nachdem Body und Aim gesetzt sind.
        /// </summary>
        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
            {
                return;
            }

            float dt = deltaTime > 0f ? deltaTime : Time.deltaTime;

            // ===== Horizontale Geschwindigkeit =====
            float velX = m_Velocity.x;
            float velZ = m_Velocity.z;
            float xySpeed = Mathf.Sqrt(velX * velX + velZ * velZ);

            // ===== Bob Cycle (SoF2 PM_Footsteps: bobCycle += bobmove * msec) =====
            if (m_IsGrounded && xySpeed > BOB_SPEED_THRESHOLD)
            {
                float bobMove = m_IsCrouching ? BOB_MOVE_CROUCH : BOB_MOVE_WALK;
                float dtMs = dt * 1000f;
                m_BobPhaseRaw += bobMove * dtMs;

                // Wrap bei 256 (8-bit wie SoF2 bobCycle)
                while (m_BobPhaseRaw >= 256f)
                {
                    m_BobPhaseRaw -= 256f;
                }

                // SoF2: bobcycle = (bobCycle & 128) >> 7 (Toggle fuer Roll-Richtung)
                m_BobCycle = ((int)m_BobPhaseRaw & 128) >> 7;

                // SoF2: bobfracsin = fabs(sin((bobCycle & 127) / 127.0 * PI))
                m_BobFracSin = Mathf.Abs(Mathf.Sin(((int)m_BobPhaseRaw & 127) / 127f * Mathf.PI));
            }
            else
            {
                // Nicht am Boden oder stehend: BobFracSin smooth ausfaden
                m_BobFracSin = Mathf.MoveTowards(m_BobFracSin, 0f, dt * BOB_FADE_RATE);
            }

            // SoF2: speed = max(xyspeed, 200 QU/s) fuer Bob-Amplitude
            float bobAmplitudeSpeed = xySpeed > BOB_MIN_SPEED_MPS ? xySpeed : BOB_MIN_SPEED_MPS;

            // ===== Position Offsets =====
            Vector3 posOffset = Vector3.zero;

            // Eye-Height Korrektur: Pitch-Bone → Server-Augenhoehe
            posOffset.y += m_EyeHeightOffset;

            // Weapon View-Offset (SoF2: per-weapon camera offset in Forward/Right/Up)
            posOffset += m_WeaponViewOffset;

            // Bob Height (SoF2: origin[2] += bobfracsin * xyspeed * cg_bobup, max 6 QU)
            float bobUp = m_BobFracSin * xySpeed * BOB_UP_FACTOR;
            if (bobUp > BOB_UP_MAX)
            {
                bobUp = BOB_UP_MAX;
            }
            posOffset.y += bobUp;

            // Landing Deflection (SoF2: LAND_DEFLECT_TIME + LAND_RETURN_TIME)
            float landDelta = Time.time - m_LandTime;
            if (landDelta < LAND_DEFLECT_TIME)
            {
                float f = landDelta / LAND_DEFLECT_TIME;
                posOffset.y += m_LandChange * f;
            }
            else if (landDelta < LAND_DEFLECT_TIME + LAND_RETURN_TIME)
            {
                float f = 1f - (landDelta - LAND_DEFLECT_TIME) / LAND_RETURN_TIME;
                posOffset.y += m_LandChange * f;
            }

            // Duck Smoothing (SoF2: vieworg[2] -= duckChange * (DUCK_TIME - timeDelta) / DUCK_TIME)
            float duckDelta = Time.time - m_DuckTime;
            if (duckDelta < DUCK_TIME)
            {
                posOffset.y -= m_DuckChange * (DUCK_TIME - duckDelta) / DUCK_TIME;
            }

            // Position-Offset in Camera-Space anwenden
            state.PositionCorrection += state.RawOrientation * posOffset;

            // ===== Rotation Offsets =====

            // View-Axes fuer Velocity-Dot-Products (SoF2 cg.refdef.viewaxis)
            // Flache Forward/Right Richtung aus der Kamera-Orientation ableiten
            Quaternion flatRot = Quaternion.Euler(0f, state.RawOrientation.eulerAngles.y, 0f);
            Vector3 flatForward = flatRot * Vector3.forward;
            Vector3 flatRight = flatRot * Vector3.right;

            float pitchOffset = 0f;
            float rollOffset = 0f;

            // Velocity-based pitch (SoF2: DotProduct(velocity, viewaxis[0]) * cg_runpitch)
            float forwardVel = Vector3.Dot(m_Velocity, flatForward);
            pitchOffset += forwardVel * RUN_PITCH_FACTOR;

            // Velocity-based roll (SoF2: DotProduct(velocity, viewaxis[1]) * cg_runroll)
            float sideVel = Vector3.Dot(m_Velocity, flatRight);
            rollOffset -= sideVel * RUN_ROLL_FACTOR;

            // Bob pitch (SoF2: bobfracsin * cg_bobpitch * speed)
            float bobPitch = m_BobFracSin * BOB_PITCH_FACTOR * bobAmplitudeSpeed;
            if (m_IsCrouching && m_IsGrounded)
            {
                bobPitch *= CROUCH_BOB_MULTIPLIER;
            }
            pitchOffset += bobPitch;

            // Bob roll (SoF2: bobfracsin * cg_bobroll * speed, Richtung alterniert)
            float bobRoll = m_BobFracSin * BOB_ROLL_FACTOR * bobAmplitudeSpeed;
            if (m_IsCrouching && m_IsGrounded)
            {
                bobRoll *= CROUCH_BOB_MULTIPLIER;
            }
            if (m_BobCycle == 1)
            {
                bobRoll = -bobRoll;
            }
            rollOffset += bobRoll;

            // SoF2: angles[PITCH] = Com_Clampf(-89, 89, angles[PITCH])
            pitchOffset = Mathf.Clamp(pitchOffset, -89f, 89f);

            // Rotation-Offset anwenden
            state.OrientationCorrection *= Quaternion.Euler(pitchOffset, 0f, rollOffset);
        }
    }
}
