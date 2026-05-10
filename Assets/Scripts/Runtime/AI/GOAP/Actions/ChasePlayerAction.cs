using CrashKonijn.Agent.Core;
using CrashKonijn.Agent.Runtime;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// GOAP-Aktion: Bewegt den Seeker-Bot auf den sichtbaren Spieler zu.
    /// Vorbedingung: IsPlayerVisible >= 1.
    /// Effekt: Erhoehung von IsPlayerInRange.
    /// Springt periodisch um durch Quake/SoF2-Bewegungsphysik schneller aufzuholen.
    /// Wenn die Sicht verloren geht: laeuft zur letzten bekannten Position
    /// und scannt dort die Umgebung fuer mehrere Sekunden bevor zur Patrouille zurueckgekehrt wird.
    /// </summary>
    public class ChasePlayerAction : GoapActionBase<AIActionData>
    {
        /// <summary>
        /// Minimales Intervall zwischen Chase-Spruengen in Sekunden.
        /// Kompromiss: schnell genug fuer Bhop-Feel, aber nicht so dicht dass
        /// der Bot auf Slopes/Treppen die Boden-Beschleunigung verliert.
        /// </summary>
        private const float k_ChaseJumpIntervalMin = 0.7f;

        /// <summary>
        /// Maximales Intervall zwischen Chase-Spruengen in Sekunden.
        /// Jitter damit der Bot nicht maschinell wirkt.
        /// </summary>
        private const float k_ChaseJumpIntervalMax = 1.4f;

        /// <summary>Ankunftsschwelle zum Last-Known-Point (Meter).</summary>
        private const float k_LastKnownArrivalThreshold = 2.5f;

        /// <summary>Dauer der Sweep-Suche am Last-Known-Point (Sekunden).</summary>
        private const float k_ScanDurationSeconds = 4.0f;

        /// <summary>Sweep-Periode: eine volle Links-Rechts-Bewegung in Sekunden.</summary>
        private const float k_ScanSweepPeriod = 2.0f;

        /// <summary>Maximaler Sweep-Winkel relativ zur Vorwaertsrichtung in Grad.</summary>
        private const float k_ScanSweepAngle = 75f;

        /// <summary>Sweep-Blickdistanz vom Bot in Metern (visuelle Look-Target-Distanz).</summary>
        private const float k_ScanLookDistance = 8f;

        /// <summary>Zeitpunkt des naechsten Chase-Sprungs (Time.time).</summary>
        private float m_NextChaseJumpTime;

        /// <summary>Time.time-Zeitpunkt an dem der Scan begonnen hat (0 = nicht aktiv).</summary>
        private float m_ScanStartTime;

        /// <summary>Mittelpunkt des Scan-Bereichs (Last-Known-Position bei Eintritt).</summary>
        private Vector3 m_ScanCenter;

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Start(IMonoAgent agent, AIActionData data)
        {
            m_NextChaseJumpTime = Time.time + Random.Range(k_ChaseJumpIntervalMin, k_ChaseJumpIntervalMax);
            m_ScanStartTime = 0f;
        }

        /// <inheritdoc />
        public override IActionRunState Perform(IMonoAgent agent, AIActionData data, IActionContext context)
        {
            AIBotController controller = agent.Injector.GetCachedComponent<AIBotController>();
            if (controller == null)
            {
                return ActionRunState.Stop;
            }

            // Spieler nicht mehr sichtbar und kein Damage-Reaction → zur letzten bekannten Position laufen
            if (!controller.PlayerSensorDetected && !controller.WasDamagedRecently)
            {
                return PerformLastKnownSearch(agent, controller);
            }

            // Spieler wieder sichtbar → Scan abbrechen, normaler Chase-Loop
            m_ScanStartTime = 0f;

            // Echtzeit-Sensorposition verwenden (nicht gecachtes GOAP-Target)
            controller.FindNearestPlayerBySensors(out Vector3 direction, out float distance, out bool detected);

            Vector3 playerPos;
            if (detected)
            {
                playerPos = controller.EyePosition + direction * distance;
            }
            else if (controller.WasDamagedRecently && controller.LastAttackerPosition.HasValue)
            {
                playerPos = controller.LastAttackerPosition.Value;
            }
            else if (data.Target != null)
            {
                playerPos = data.Target.Position;
            }
            else
            {
                return ActionRunState.Completed;
            }

            controller.SetMoveTarget(playerPos);
            controller.SetLookTarget(playerPos);

            // Spieler-Aktionen spiegeln (Springen, Schiessen — kein Ducken)
            controller.MirrorNearestPlayerActions();

            // Periodischer Chase-Sprung: Quake/SoF2-Physik gibt Sprung-Bonus auf Geschwindigkeit
            if (Time.time >= m_NextChaseJumpTime)
            {
                controller.SetShouldJump(true);
                m_NextChaseJumpTime = Time.time + Random.Range(k_ChaseJumpIntervalMin, k_ChaseJumpIntervalMax);
            }

            // Horizontale XZ-Distanz fuer Reichweite (3D-Distanz ist wegen Augenhoehe groesser)
            if (controller.PlayerSensorDistanceXZ <= controller.WeaponRangeMeters)
            {
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        /// <summary>
        /// Sucht den verlorenen Spieler an der zuletzt bekannten Position:
        /// 1) Lauf zum Last-Known-Point.
        /// 2) Bei Ankunft: scanne Umgebung mit Links-Rechts-Sweep fuer mehrere Sekunden.
        /// 3) Spieler erneut sichtbar / Damage → sofort wieder Chase.
        /// 4) Scan-Timeout → Completed (Planner schaltet auf Patrol).
        /// </summary>
        private IActionRunState PerformLastKnownSearch(IMonoAgent agent, AIBotController controller)
        {
            // Keine Erinnerung mehr → fertig, Patrol uebernimmt
            if (!controller.LastKnownPlayerPosition.HasValue)
            {
                m_ScanStartTime = 0f;
                return ActionRunState.Completed;
            }

            Vector3 lastKnown = controller.LastKnownPlayerPosition.Value;
            Vector3 botPos = agent.Transform.position;
            float distXZ = Mathf.Sqrt(
                (botPos.x - lastKnown.x) * (botPos.x - lastKnown.x)
                + (botPos.z - lastKnown.z) * (botPos.z - lastKnown.z));

            // Phase 1: hinlaufen
            if (distXZ > k_LastKnownArrivalThreshold)
            {
                controller.SetMoveTarget(lastKnown);
                controller.SetLookTarget(lastKnown);
                m_ScanStartTime = 0f;
                return ActionRunState.Continue;
            }

            // Phase 2: an LKP angekommen → Scan starten/fortsetzen
            if (m_ScanStartTime <= 0f)
            {
                m_ScanStartTime = Time.time;
                m_ScanCenter = lastKnown;
            }

            // Bot soll stehenbleiben am Scan-Punkt
            controller.SetMoveTarget(m_ScanCenter);

            // Sweep-Winkel: Sinus-Schwingung um Vorwaertsrichtung
            float scanElapsed = Time.time - m_ScanStartTime;
            float sweepPhase = (scanElapsed / k_ScanSweepPeriod) * Mathf.PI * 2f;
            float sweepDeg = Mathf.Sin(sweepPhase) * k_ScanSweepAngle;

            Vector3 forward = agent.Transform.forward;
            Vector3 sweepDir = Quaternion.AngleAxis(sweepDeg, Vector3.up) * forward;
            Vector3 lookPos = controller.EyePosition + sweepDir * k_ScanLookDistance;
            controller.SetLookTarget(lookPos);

            // Scan-Timeout → fertig
            if (scanElapsed >= k_ScanDurationSeconds)
            {
                m_ScanStartTime = 0f;
                return ActionRunState.Completed;
            }

            return ActionRunState.Continue;
        }

        /// <inheritdoc />
        public override void End(IMonoAgent agent, AIActionData data) { }
    }
}
