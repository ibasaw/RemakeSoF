using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Zielsensor: Generiert eine zufaellige Wanderposition in der Naehe des Bots.
    /// Wird als Fallback-Ziel verwendet wenn keine anderen Ziele verfuegbar sind.
    /// Waehlt einen neuen Punkt nur wenn das bisherige Ziel erreicht oder ungueltig wurde.
    /// </summary>
    public class WanderTargetSensor : LocalTargetSensorBase
    {
        /// <summary>Maximale Wanderdistanz in Metern.</summary>
        private const float k_WanderRadius = 20f;

        /// <summary>Mindestdistanz zum Ziel um ein neues zu generieren (Meter).</summary>
        private const float k_ArrivalThreshold = 3f;

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

            // Neuen Wanderpunkt generieren
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(k_WanderRadius * 0.3f, k_WanderRadius);
            Vector3 wanderPos = agentPos + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;

            if (existingTarget is PositionTarget reuse)
            {
                reuse.SetPosition(wanderPos);
                return reuse;
            }

            return new PositionTarget(wanderPos);
        }
    }
}
