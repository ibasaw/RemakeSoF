#if EANN_ENABLED
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI
{
    /// <summary>
    /// Berechnet die Fitness eines AI-Bots basierend auf Nearest-Checkpoint-Ansatz,
    /// Exploration (Distanz vom Spawn), Ueberlebenszeit (Hider) und Kills (Seeker).
    /// Der Bot wird immer zum naechstgelegenen uneroberten Checkpoint geleitet (nicht sequenziell).
    /// Evaluation wird jeden Tick aktualisiert und direkt in den Genotype geschrieben.
    /// Wird pro Bot-Instanz vom AIEvolutionManager gehalten.
    /// </summary>
    public class AIFitnessEvaluator
    {
        /// <summary>Gewichtung: Checkpoint-Captures (normalisiert 0..1 pro Capture).</summary>
        private const float k_CheckpointWeight = 15f;

        /// <summary>Gewichtung: Naehe zum naechsten uneroberten Checkpoint (0..1).</summary>
        private const float k_ProximityWeight = 8f;

        /// <summary>Gewichtung: Exploration — Distanz vom Spawnpunkt (normalisiert 0..1).</summary>
        private const float k_ExplorationWeight = 5f;

        /// <summary>Gewichtung: Ueberlebenszeit in Sekunden (Hider-Hauptbelohnung).</summary>
        private const float k_SurvivalTimeWeight = 1f;

        /// <summary>Gewichtung: Kills (Seeker-Hauptbelohnung).</summary>
        private const float k_KillWeight = 20f;

        /// <summary>Gewichtung: Strafe fuer exzessives Ducken-Spam (Anteil der Ticks mit Crouch).</summary>
        private const float k_CrouchPenaltyWeight = 10f;

        /// <summary>Gewichtung: Belohnung fuer hohe Vorwaerts-Geschwindigkeit (0..1 normalisiert).</summary>
        private const float k_SpeedRewardWeight = 8f;

        /// <summary>Gewichtung: Strafe fuer Rueckwaerts-Bewegung (Anteil der Ticks mit negativer Vorwaerts-Geschwindigkeit).</summary>
        private const float k_BackwardPenaltyWeight = 12f;

        /// <summary>Gewichtung: Belohnung fuer Bunny-Hop-Geschwindigkeit ueber PmMaxSpeed (normalizedSpeed > 1.0).</summary>
        private const float k_BunnyHopRewardWeight = 10f;

        /// <summary>Gewichtung: Strafe fuer Nahwand-Kontakt (Anteil der Sensoren die nahe Waende treffen).</summary>
        private const float k_WallPenaltyWeight = 10f;

        /// <summary>Gewichtung: Strafe fuer blindes Feuern ohne Spieler in Sicht (Anteil der Ticks mit Angriff ohne Spieler).</summary>
        private const float k_BlindFirePenaltyWeight = 8f;

        /// <summary>Gewichtung: Belohnung fuer Spieler-Erkennung per Sensor (Anteil der Ticks mit Sichtkontakt).</summary>
        private const float k_PlayerDetectionRewardWeight = 8f;

        /// <summary>Gewichtung: Belohnung fuer Naeherung an erkannten Spieler (Seeker, naeher = mehr Reward).</summary>
        private const float k_PlayerProximityWeight = 6f;

        /// <summary>Maximale Distanz fuer Player-Proximity-Normalisierung (Meter).</summary>
        private const float k_PlayerProximityMaxDistance = 25f;

        /// <summary>Max-Distanz fuer Proximity-Normalisierung (Meter).</summary>
        private const float k_ProximityMaxDistance = 50f;

        /// <summary>Max-Distanz fuer Exploration-Normalisierung (Meter).</summary>
        private const float k_ExplorationMaxDistance = 30f;

        /// <summary>Checkpoint-Positionen (aus Map geladen).</summary>
        private Vector3[] m_Checkpoints;

        /// <summary>Flags welche Checkpoints bereits erobert wurden.</summary>
        private bool[] m_CapturedFlags;

        /// <summary>Anzahl eroberter Checkpoints.</summary>
        private int m_CapturedCount;

        /// <summary>Index des naechsten uneroberten Checkpoints (-1 wenn alle erobert).</summary>
        private int m_NearestUncapturedIndex;

        /// <summary>Einfangradius fuer Checkpoint-Erkennung.</summary>
        private float m_CaptureRadius;

        /// <summary>Belohnung pro Checkpoint (= 1.0 / Gesamtanzahl).</summary>
        private float m_RewardPerCheckpoint;

        /// <summary>Startposition des Bots.</summary>
        private Vector3 m_StartPosition;

        /// <summary>Gesamte Ueberlebenszeit in Sekunden.</summary>
        private float m_SurvivalTime;

        /// <summary>Anzahl Kills (nur Seeker).</summary>
        private int m_Kills;

        /// <summary>Ob dieser Evaluator fuer einen Seeker arbeitet.</summary>
        private bool m_IsSeeker;

        /// <summary>Ob der Evaluator initialisiert wurde.</summary>
        private bool m_Initialized;

        /// <summary>Anzahl Ticks in denen der Bot geduckt war.</summary>
        private int m_CrouchFrameCount;

        /// <summary>Aufsummierte Super-Speed ueber PmMaxSpeed (normalizedSpeed - 1.0 pro Tick, nur wenn > 1.0).</summary>
        private float m_AccumulatedSuperSpeed;

        /// <summary>Gesamtanzahl der Ticks seit Reset.</summary>
        private int m_TotalFrameCount;

        /// <summary>Aufsummierte normalisierte Geschwindigkeit (0..1 pro Tick, fuer Durchschnitt).</summary>
        private float m_AccumulatedSpeed;

        /// <summary>Anzahl Ticks mit negativer Vorwaerts-Geschwindigkeit (Rueckwaerts-Laufen).</summary>
        private int m_BackwardFrameCount;

        /// <summary>Aufsummierter normalisierter Nahwand-Anteil pro Tick (wallHits / totalSensors).</summary>
        private float m_AccumulatedWallRatio;

        /// <summary>Anzahl Ticks in denen ein Spieler per Sensor erkannt wurde.</summary>
        private int m_PlayerDetectedFrameCount;

        /// <summary>Gesamtanzahl Sensoren (fuer Normalisierung).</summary>
        private int m_TotalSensorCount;

        /// <summary>Aufsummierte normalisierte Spieler-Naeherungs-Belohnung (1 - dist/maxDist, nur wenn erkannt).</summary>
        private float m_AccumulatedPlayerProximity;

        /// <summary>Anzahl Ticks mit Spieler-Proximity-Daten (fuer Durchschnittsberechnung).</summary>
        private int m_PlayerProximityFrameCount;

        /// <summary>Anzahl Ticks in denen der Bot ohne Spieler in Sicht angegriffen hat (Blind Fire).</summary>
        private int m_BlindFireFrameCount;

        /// <summary>Anzahl eroberter Checkpoints (oeffentlich fuer Logging).</summary>
        public int CapturedCheckpointCount => m_CapturedCount;

        /// <summary>Captured-Count vom vorherigen Tick (fuer Erkennung neuer Captures).</summary>
        private int m_PreviousCapturedCount;

        /// <summary>True wenn im letzten Update() ein neuer Checkpoint erreicht wurde.</summary>
        public bool CheckpointJustReached { get; private set; }

        /// <summary>Gecachter normalisierter Richtungsvektor zum naechsten Checkpoint (XZ).</summary>
        private Vector3 m_CheckpointDirection;

        /// <summary>Gecachte normalisierte Distanz zum naechsten Checkpoint (0..1).</summary>
        private float m_CheckpointDistanceNormalized;

        /// <summary>
        /// Setzt den Evaluator zurueck fuer eine neue Runde/Generation.
        /// </summary>
        /// <param name="startPosition">Startposition des Bots.</param>
        /// <param name="isSeeker">True wenn Seeker, False wenn Hider.</param>
        /// <param name="checkpoints">Checkpoint-Positionen aus der geladenen Map. Kann null sein.</param>
        /// <param name="captureRadius">Einfangradius fuer Checkpoint-Erkennung.</param>
        public void Reset(Vector3 startPosition, bool isSeeker, Vector3[] checkpoints, float captureRadius)
        {
            m_StartPosition = startPosition;
            m_IsSeeker = isSeeker;
            m_Checkpoints = checkpoints;
            m_CaptureRadius = captureRadius;
            m_CapturedCount = 0;
            m_PreviousCapturedCount = 0;
            CheckpointJustReached = false;
            m_NearestUncapturedIndex = -1;

            if (checkpoints != null && checkpoints.Length > 0)
            {
                m_CapturedFlags = new bool[checkpoints.Length];
                m_RewardPerCheckpoint = 1f / checkpoints.Length;
            }
            else
            {
                m_CapturedFlags = null;
                m_RewardPerCheckpoint = 0f;
            }

            m_SurvivalTime = 0f;
            m_Kills = 0;
            m_CrouchFrameCount = 0;
            m_BackwardFrameCount = 0;
            m_TotalFrameCount = 0;
            m_AccumulatedSpeed = 0f;
            m_AccumulatedSuperSpeed = 0f;
            m_AccumulatedWallRatio = 0f;
            m_PlayerDetectedFrameCount = 0;
            m_AccumulatedPlayerProximity = 0f;
            m_PlayerProximityFrameCount = 0;
            m_BlindFireFrameCount = 0;
            m_Initialized = true;
        }

        /// <summary>
        /// Aktualisiert die Evaluation kontinuierlich.
        /// Prueft Checkpoint-Einfang (nearest-checkpoint), berechnet Proximity + Exploration,
        /// und gibt den aktuellen Evaluation-Wert zurueck (wird in Genotype.Evaluation geschrieben).
        /// Wird pro Server-Tick vom AIEvolutionManager aufgerufen.
        /// </summary>
        /// <param name="currentPosition">Aktuelle Position des Bots.</param>
        /// <param name="deltaTime">Vergangene Zeit seit letztem Tick.</param>
        /// <param name="isJumping">Ob der Bot in diesem Tick springt.</param>
        /// <param name="isCrouching">Ob der Bot in diesem Tick geduckt ist.</param>
        /// <param name="normalizedSpeed">Horizontale Geschwindigkeit normalisiert auf 0..1 (speed / maxSpeed).</param>
        /// <param name="normalizedForwardSpeed">Vorwaerts-Geschwindigkeit normalisiert auf -1..1 (positiv=vorwaerts).</param>
        /// <param name="closeWallHitCount">Anzahl Sensoren die nahe Waende treffen.</param>
        /// <param name="totalSensorCount">Gesamtanzahl Sensoren (fuer Normalisierung).</param>
        /// <param name="playerDetected">Ob mindestens ein Sensor einen Spieler erkennt.</param>
        /// <param name="playerDistance">Distanz zum naechsten erkannten Spieler (Meter, float.MaxValue wenn keiner erkannt).</param>
        /// <param name="isAttacking">Ob der Bot in diesem Tick angreift.</param>
        /// <returns>Aktueller Evaluation-Wert (wird direkt in Genotype geschrieben).</returns>
        public float Update(Vector3 currentPosition, float deltaTime, bool isJumping, bool isCrouching,
            float normalizedSpeed, float normalizedForwardSpeed, int closeWallHitCount, int totalSensorCount,
            bool playerDetected, float playerDistance, bool isAttacking)
        {
            if (!m_Initialized)
            {
                return 0f;
            }

            m_SurvivalTime += deltaTime;
            m_TotalFrameCount++;
            if (isCrouching) m_CrouchFrameCount++;
            if (isAttacking && !playerDetected) m_BlindFireFrameCount++;
            m_AccumulatedSpeed += Mathf.Max(0f, normalizedForwardSpeed);
            if (normalizedForwardSpeed < -0.1f) m_BackwardFrameCount++;
            if (normalizedSpeed > 1f) m_AccumulatedSuperSpeed += normalizedSpeed - 1f;
            m_TotalSensorCount = totalSensorCount > 0 ? totalSensorCount : 1;
            if (totalSensorCount > 0)
            {
                m_AccumulatedWallRatio += (float)closeWallHitCount / totalSensorCount;
            }
            if (playerDetected) m_PlayerDetectedFrameCount++;

            // Player-Proximity akkumulieren: Je naeher desto hoeher der Wert (nur wenn Spieler erkannt)
            if (playerDetected && playerDistance < k_PlayerProximityMaxDistance)
            {
                float proximityNormalized = 1f - Mathf.Clamp01(playerDistance / k_PlayerProximityMaxDistance);
                m_AccumulatedPlayerProximity += proximityNormalized;
                m_PlayerProximityFrameCount++;
            }

            // Checkpoint-Captures pruefen (alle uneroberten innerhalb Radius)
            if (m_Checkpoints != null && m_CapturedFlags != null)
            {
                for (int i = 0; i < m_Checkpoints.Length; i++)
                {
                    if (m_CapturedFlags[i])
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(currentPosition, m_Checkpoints[i]);
                    if (distance <= m_CaptureRadius)
                    {
                        m_CapturedFlags[i] = true;
                        m_CapturedCount++;
                    }
                }
            }

            // Erkennung ob ein neuer Checkpoint erreicht wurde
            CheckpointJustReached = m_CapturedCount > m_PreviousCapturedCount;
            m_PreviousCapturedCount = m_CapturedCount;

            // Nearest uncaptured Checkpoint finden + Richtung cachen
            FindNearestUncaptured(currentPosition);
            UpdateCheckpointDirection(currentPosition);

            return CalculateEvaluation(currentPosition);
        }

        /// <summary>
        /// Gibt die normalisierte Richtung (Welt-XZ) zum naechsten uneroberten Checkpoint zurueck.
        /// Der AIBotController transformiert das in lokale Bot-Orientierung.
        /// </summary>
        public Vector3 CheckpointDirection => m_CheckpointDirection;

        /// <summary>
        /// Gibt die normalisierte Distanz zum naechsten Checkpoint zurueck (0 = angekommen, 1 = >= 50m).
        /// </summary>
        public float CheckpointDistanceNormalized => m_CheckpointDistanceNormalized;

        /// <summary>
        /// Findet den naechsten uneroberten Checkpoint per Distanz.
        /// </summary>
        private void FindNearestUncaptured(Vector3 currentPosition)
        {
            m_NearestUncapturedIndex = -1;

            if (m_Checkpoints == null || m_CapturedFlags == null)
            {
                return;
            }

            float minDistance = float.MaxValue;
            for (int i = 0; i < m_Checkpoints.Length; i++)
            {
                if (m_CapturedFlags[i])
                {
                    continue;
                }

                float distance = Vector3.Distance(currentPosition, m_Checkpoints[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    m_NearestUncapturedIndex = i;
                }
            }
        }

        /// <summary>
        /// Aktualisiert Richtung und Distanz zum naechsten uneroberten Checkpoint.
        /// </summary>
        private void UpdateCheckpointDirection(Vector3 currentPosition)
        {
            if (m_NearestUncapturedIndex < 0)
            {
                m_CheckpointDirection = Vector3.zero;
                m_CheckpointDistanceNormalized = 0f;
                return;
            }

            Vector3 target = m_Checkpoints[m_NearestUncapturedIndex];
            Vector3 toTarget = target - currentPosition;
            toTarget.y = 0f; // Nur XZ-Ebene

            float distance = toTarget.magnitude;
            m_CheckpointDirection = distance > 0.01f ? toTarget / distance : Vector3.zero;
            m_CheckpointDistanceNormalized = Mathf.Clamp01(distance / k_ProximityMaxDistance);
        }

        /// <summary>
        /// Registriert einen Kill (nur relevant fuer Seeker).
        /// </summary>
        public void RegisterKill()
        {
            m_Kills++;
        }

        /// <summary>
        /// Berechnet den aktuellen Evaluation-Wert.
        /// Komponenten: Checkpoint-Captures + Proximity zum Naechsten + Exploration + rollenspezifisch.
        /// </summary>
        private float CalculateEvaluation(Vector3 currentPosition)
        {
            float evaluation = 0f;

            // 1) Checkpoint-Captures (normalisiert: capturedCount / total)
            if (m_Checkpoints != null && m_Checkpoints.Length > 0)
            {
                evaluation += m_CapturedCount * m_RewardPerCheckpoint * k_CheckpointWeight;
            }

            // 2) Proximity zum naechsten uneroberten Checkpoint (naeher = mehr Belohnung)
            if (m_NearestUncapturedIndex >= 0)
            {
                float distance = Vector3.Distance(currentPosition, m_Checkpoints[m_NearestUncapturedIndex]);
                float proximity = 1f - Mathf.Clamp01(distance / k_ProximityMaxDistance);
                evaluation += proximity * k_ProximityWeight;
            }

            // 3) Exploration: Distanz vom Spawnpunkt (erzeugt Selection Pressure in Gen 1)
            float displacement = Vector3.Distance(currentPosition, m_StartPosition);
            evaluation += Mathf.Clamp01(displacement / k_ExplorationMaxDistance) * k_ExplorationWeight;

            // 4) Ueberlebenszeit (Hider-Bonus)
            if (!m_IsSeeker)
            {
                evaluation += m_SurvivalTime * k_SurvivalTimeWeight;
            }

            // 5) Kills (Seeker-Bonus)
            if (m_IsSeeker)
            {
                evaluation += m_Kills * k_KillWeight;
            }

            // 6) Crouch-Penalty: Nur exzessives Ducken bestrafen (Springen ist frei fuer EANN-Lernen)
            if (m_TotalFrameCount > 0)
            {
                float crouchRatio = (float)m_CrouchFrameCount / m_TotalFrameCount;
                evaluation -= crouchRatio * k_CrouchPenaltyWeight;
            }

            // 7) Speed-Reward: Durchschnittliche Vorwaerts-Geschwindigkeit belohnen (nicht gekappt)
            if (m_TotalFrameCount > 0)
            {
                float avgSpeed = m_AccumulatedSpeed / m_TotalFrameCount;
                evaluation += avgSpeed * k_SpeedRewardWeight;
            }

            // 7c) Bunny-Hop-Reward: Geschwindigkeit ueber PmMaxSpeed belohnen (Strafe-Jump-Bonus)
            if (m_TotalFrameCount > 0)
            {
                float avgSuperSpeed = m_AccumulatedSuperSpeed / m_TotalFrameCount;
                evaluation += avgSuperSpeed * k_BunnyHopRewardWeight;
            }

            // 7b) Backward-Penalty: Anteil der Ticks mit Rueckwaerts-Bewegung bestrafen
            if (m_TotalFrameCount > 0)
            {
                float backwardRatio = (float)m_BackwardFrameCount / m_TotalFrameCount;
                evaluation -= backwardRatio * k_BackwardPenaltyWeight;
            }

            // 8) Wall-Penalty: Durchschnittlicher Anteil der Sensoren die nahe Waende treffen
            if (m_TotalFrameCount > 0)
            {
                float avgWallRatio = m_AccumulatedWallRatio / m_TotalFrameCount;
                evaluation -= avgWallRatio * k_WallPenaltyWeight;
            }

            // 9) Player-Detection-Reward: Anteil der Ticks in denen ein Spieler per Sensor gesehen wurde
            if (m_IsSeeker && m_TotalFrameCount > 0)
            {
                float playerSeenRatio = (float)m_PlayerDetectedFrameCount / m_TotalFrameCount;
                evaluation += playerSeenRatio * k_PlayerDetectionRewardWeight;
            }

            // 10) Player-Proximity-Reward: Durchschnittliche Naeherung an erkannten Spieler (Seeker)
            if (m_IsSeeker && m_PlayerProximityFrameCount > 0)
            {
                float avgProximity = m_AccumulatedPlayerProximity / m_PlayerProximityFrameCount;
                evaluation += avgProximity * k_PlayerProximityWeight;
            }

            // 11) Blind-Fire-Penalty: Anteil der Ticks in denen ohne Spieler in Sicht geschossen wurde (Seeker)
            if (m_IsSeeker && m_TotalFrameCount > 0)
            {
                float blindFireRatio = (float)m_BlindFireFrameCount / m_TotalFrameCount;
                evaluation -= blindFireRatio * k_BlindFirePenaltyWeight;
            }

            return Mathf.Max(0f, evaluation);
        }

        /// <summary>
        /// Gibt eine kompakte Aufschluesselung der Fitness-Komponenten als String zurueck.
        /// Fuer Debug-Logging beim Genotype-Swap.
        /// </summary>
        public string GetBreakdown(Vector3 currentPosition)
        {
            if (!m_Initialized || m_TotalFrameCount == 0)
            {
                return "n/a";
            }

            float cpScore = 0f;
            if (m_Checkpoints != null && m_Checkpoints.Length > 0)
            {
                cpScore = m_CapturedCount * m_RewardPerCheckpoint * k_CheckpointWeight;
            }

            float proxScore = 0f;
            if (m_NearestUncapturedIndex >= 0)
            {
                float distance = Vector3.Distance(currentPosition, m_Checkpoints[m_NearestUncapturedIndex]);
                float proximity = 1f - Mathf.Clamp01(distance / k_ProximityMaxDistance);
                proxScore = proximity * k_ProximityWeight;
            }

            float displacement = Vector3.Distance(currentPosition, m_StartPosition);
            float explScore = Mathf.Clamp01(displacement / k_ExplorationMaxDistance) * k_ExplorationWeight;

            float speedScore = m_AccumulatedSpeed / m_TotalFrameCount * k_SpeedRewardWeight;
            float bunnyScore = m_AccumulatedSuperSpeed / m_TotalFrameCount * k_BunnyHopRewardWeight;
            float wallPenalty = m_AccumulatedWallRatio / m_TotalFrameCount * k_WallPenaltyWeight;
            float crouchPenalty = (float)m_CrouchFrameCount / m_TotalFrameCount * k_CrouchPenaltyWeight;
            float backPenalty = (float)m_BackwardFrameCount / m_TotalFrameCount * k_BackwardPenaltyWeight;

            float killScore = m_IsSeeker ? m_Kills * k_KillWeight : 0f;
            float detectScore = m_IsSeeker ? (float)m_PlayerDetectedFrameCount / m_TotalFrameCount * k_PlayerDetectionRewardWeight : 0f;
            float playerProxScore = m_IsSeeker && m_PlayerProximityFrameCount > 0
                ? m_AccumulatedPlayerProximity / m_PlayerProximityFrameCount * k_PlayerProximityWeight
                : 0f;
            float survivalScore = !m_IsSeeker ? m_SurvivalTime * k_SurvivalTimeWeight : 0f;
            float blindFirePenalty = m_IsSeeker ? (float)m_BlindFireFrameCount / m_TotalFrameCount * k_BlindFirePenaltyWeight : 0f;

            return $"CP={cpScore:F1} Prox={proxScore:F1} Expl={explScore:F1} Spd={speedScore:F1} Bunny={bunnyScore:F1} " +
                   $"Kill={killScore:F1} Detect={detectScore:F1} PlrProx={playerProxScore:F1} " +
                   $"Surv={survivalScore:F1} | -Wall={wallPenalty:F1} -Crouch={crouchPenalty:F1} -Back={backPenalty:F1} -BlindFire={blindFirePenalty:F1}";
        }
    }
}
#endif
