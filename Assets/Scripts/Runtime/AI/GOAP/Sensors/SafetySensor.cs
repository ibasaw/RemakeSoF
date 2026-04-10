using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Weltsensor: Gibt immer 0 zurueck.
    /// IsSafe wird absichtlich nie als erfuellt gemeldet,
    /// damit der Hider-Planner immer eine Flucht- oder Wander-Aktion plant.
    /// </summary>
    public class SafetySensor : LocalWorldSensorBase
    {
        /// <summary>Sensor-Timer: Einmal genuegt (Wert aendert sich nie).</summary>
        public override ISensorTimer Timer => SensorTimer.Once;

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Update() { }

        /// <inheritdoc />
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            return false;
        }
    }
}
