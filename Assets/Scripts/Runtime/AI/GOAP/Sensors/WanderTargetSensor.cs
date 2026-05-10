using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Zielsensor: Generiert eine zufaellige Wanderposition in der Naehe des Bots,
    /// validiert per NavMesh damit der Bot nicht in Waende oder Off-Mesh-Bereiche zielt.
    /// Wird als Fallback-Ziel verwendet wenn keine anderen Ziele verfuegbar sind.
    /// Waehlt einen neuen Punkt nur wenn das bisherige Ziel erreicht oder ungueltig wurde.
    /// </summary>
    public class WanderTargetSensor : LocalTargetSensorBase
    {
        /// <summary>Maximale Wanderdistanz in Metern.</summary>
        private const float k_WanderRadius = 20f;

        /// <summary>Mindestdistanz zum Ziel um ein neues zu generieren (Meter).</summary>
        private const float k_ArrivalThreshold = 3f;

        /// <summary>Maximaler vertikaler NavMesh-Sample-Radius in Metern.</summary>
        private const float k_NavMeshSampleHeight = 5f;

        /// <summary>Anzahl Versuche um eine valide NavMesh-Position zu finden.</summary>
        private const int k_MaxSampleAttempts = 5;

        /// <summary>Sensor-Timer: Reduziert, da Wanderpunkte selten wechseln.</summary>
        public override ISensorTimer Timer => SensorTimer.Interval(1f);

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

            Vector3 agentPos = controller.transform.position;

            // Bestehendes Ziel wiederverwenden wenn noch nicht erreicht
            if (existingTarget is PositionTarget posTarget && posTarget.IsValid())
            {
                float dist = Vector3.Distance(agentPos, posTarget.Position);
                if (dist > k_ArrivalThreshold)
                {
                    return posTarget;
                }
            }

            // Neuen Wanderpunkt generieren — auf NavMesh validieren um Walls/Off-Mesh zu vermeiden
            for (int attempt = 0; attempt < k_MaxSampleAttempts; attempt++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(k_WanderRadius * 0.3f, k_WanderRadius);
                Vector3 candidate = agentPos + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, k_NavMeshSampleHeight, NavMesh.AllAreas))
                {
                    continue;
                }

                if (existingTarget is PositionTarget reuse)
                {
                    reuse.SetPosition(hit.position);
                    return reuse;
                }

                return new PositionTarget(hit.position);
            }

            // Kein gueltiger Punkt gefunden → bestehendes Ziel beibehalten
            return existingTarget;
        }
    }
}
