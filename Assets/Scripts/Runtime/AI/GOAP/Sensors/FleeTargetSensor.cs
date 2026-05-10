using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Zielsensor: Berechnet eine Fluchtposition in der entgegengesetzten Richtung
    /// des naechsten sichtbaren Spielers. Validiert per NavMesh damit der Bot nicht
    /// in eine Wand oder Off-Mesh-Bereich fluechtet.
    /// Wird vom Hider genutzt.
    /// </summary>
    public class FleeTargetSensor : LocalTargetSensorBase
    {
        /// <summary>Fluchtdistanz in Metern.</summary>
        private const float k_FleeDistance = 15f;

        /// <summary>Maximaler vertikaler NavMesh-Sample-Radius in Metern.</summary>
        private const float k_NavMeshSampleHeight = 5f;

        /// <summary>Anzahl Winkel-Spreizungen falls direkter Fluchtweg blockiert ist.</summary>
        private const int k_MaxFleeAttempts = 5;

        /// <summary>Maximaler Winkel-Offset (Grad) wenn direkter Fluchtweg blockiert ist.</summary>
        private const float k_MaxFleeAngleOffset = 60f;

        /// <summary>Sensor-Timer: 10 Hz reicht; Fluchtpunkt muss nicht pro Frame neu berechnet werden.</summary>
        public override ISensorTimer Timer => SensorTimer.Interval(0.1f);

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Update() { }

        /// <inheritdoc />
        public override ITarget Sense(IActionReceiver agent, IComponentReference references, ITarget existingTarget)
        {
            AIBotController controller = references.GetCachedComponent<AIBotController>();
            if (controller == null)
            {
                return existingTarget;
            }

            controller.EnsureSensorsTicked();
            controller.FindNearestPlayerBySensors(out Vector3 direction, out float distance, out bool detected);

            Vector3 agentPos = controller.transform.position;

            if (!detected)
            {
                // Kein Spieler sichtbar: zufaellige Richtung waehlen, NavMesh-validiert
                Vector3? fallback = SampleNavMeshInDirection(agentPos, RandomHorizontalDirection());
                return fallback.HasValue ? UpdateOrCreate(existingTarget, fallback.Value) : existingTarget;
            }

            // Entgegengesetzte Richtung zum Spieler — bei Blockade Winkel um bis zu +/-60deg fanchern
            Vector3 fleeDir = -new Vector3(direction.x, 0f, direction.z).normalized;
            if (fleeDir.sqrMagnitude < 0.01f)
            {
                fleeDir = controller.transform.forward;
            }

            for (int attempt = 0; attempt < k_MaxFleeAttempts; attempt++)
            {
                float offsetDeg = attempt == 0
                    ? 0f
                    : Random.Range(-k_MaxFleeAngleOffset, k_MaxFleeAngleOffset);
                Vector3 candidateDir = Quaternion.AngleAxis(offsetDeg, Vector3.up) * fleeDir;
                Vector3? sampled = SampleNavMeshInDirection(agentPos, candidateDir);
                if (sampled.HasValue)
                {
                    return UpdateOrCreate(existingTarget, sampled.Value);
                }
            }

            // Kein gueltiger Fluchtpunkt → bestehendes Ziel beibehalten
            return existingTarget;
        }

        /// <summary>Sampled einen Punkt in <paramref name="direction"/> auf den NavMesh.</summary>
        private static Vector3? SampleNavMeshInDirection(Vector3 origin, Vector3 direction)
        {
            Vector3 candidate = origin + direction * k_FleeDistance;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, k_NavMeshSampleHeight, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return null;
        }

        /// <summary>Erzeugt eine zufaellige horizontale Einheitsrichtung.</summary>
        private static Vector3 RandomHorizontalDirection()
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(randomAngle), 0f, Mathf.Cos(randomAngle));
        }

        /// <summary>Wiederverwendet oder erstellt ein PositionTarget.</summary>
        private static ITarget UpdateOrCreate(ITarget existing, Vector3 position)
        {
            if (existing is PositionTarget posTarget)
            {
                posTarget.SetPosition(position);
                return posTarget;
            }

            return new PositionTarget(position);
        }
    }
}
