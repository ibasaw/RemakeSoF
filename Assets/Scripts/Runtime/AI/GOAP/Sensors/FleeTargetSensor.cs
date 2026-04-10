using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Zielsensor: Berechnet eine Fluchtposition in der entgegengesetzten Richtung
    /// des naechsten sichtbaren Spielers. Wird vom Hider genutzt.
    /// </summary>
    public class FleeTargetSensor : LocalTargetSensorBase
    {
        /// <summary>Fluchtdistanz in Metern.</summary>
        private const float k_FleeDistance = 15f;

        /// <summary>Sensor-Timer: Jedes Frame fuer schnelle Reaktion.</summary>
        public override ISensorTimer Timer => SensorTimer.Always;

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
                // Kein Spieler sichtbar: Fluchtposition zufaellig waehlen
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 randomDir = new(Mathf.Sin(randomAngle), 0f, Mathf.Cos(randomAngle));
                Vector3 fallbackPos = agentPos + randomDir * k_FleeDistance;
                return UpdateOrCreate(existingTarget, fallbackPos);
            }

            // Entgegengesetzte Richtung zum Spieler
            Vector3 fleeDir = -new Vector3(direction.x, 0f, direction.z).normalized;
            if (fleeDir.sqrMagnitude < 0.01f)
            {
                fleeDir = controller.transform.forward;
            }

            Vector3 fleePos = agentPos + fleeDir * k_FleeDistance;
            return UpdateOrCreate(existingTarget, fleePos);
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
