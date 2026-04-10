using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Zielsensor: Ermittelt die Position des naechsten uneroberten Checkpoints.
    /// Checkpoints werden statisch ueber AIBotController.SetCheckpoints() gesetzt.
    /// </summary>
    public class CheckpointTargetSensor : LocalTargetSensorBase
    {
        /// <summary>Sensor-Timer: Jede Sekunde genuegt (Checkpoints bewegen sich nicht).</summary>
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
            Vector3? nearest = controller.GetNearestCheckpoint(agentPos);
            if (!nearest.HasValue)
            {
                return existingTarget;
            }

            if (existingTarget is PositionTarget posTarget)
            {
                posTarget.SetPosition(nearest.Value);
                return posTarget;
            }

            return new PositionTarget(nearest.Value);
        }
    }
}
