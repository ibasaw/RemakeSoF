using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Zielsensor: Ermittelt die geschaetzte Position des naechsten sichtbaren Spielers.
    /// Basiert auf Sensor3D-Array-Treffern auf dem Player-Layer.
    /// Gibt ein PositionTarget mit der geschaetzten Spielerposition zurueck.
    /// </summary>
    public class NearestPlayerTargetSensor : LocalTargetSensorBase
    {
        /// <summary>Sensor-Timer: Jedes Frame abtasten fuer reaktive AI.</summary>
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

            // Primaer: Sensor-basierte Erkennung
            if (detected)
            {
                Vector3 estimatedPos = controller.EyePosition + direction * distance;

                if (existingTarget is PositionTarget posTarget)
                {
                    posTarget.SetPosition(estimatedPos);
                    return posTarget;
                }

                return new PositionTarget(estimatedPos);
            }

            // Sekundaer: Letzte bekannte Angreifer-Position (Damage-Reaction)
            if (controller.WasDamagedRecently && controller.LastAttackerPosition.HasValue)
            {
                Vector3 attackerPos = controller.LastAttackerPosition.Value;

                if (existingTarget is PositionTarget posTarget)
                {
                    posTarget.SetPosition(attackerPos);
                    return posTarget;
                }

                return new PositionTarget(attackerPos);
            }

            return existingTarget;
        }
    }
}
