using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Weltsensor: Prueft ob der Bot Munition hat.
    /// Gibt 1 zurueck wenn CurrentClipAmmo > 0, sonst 0.
    /// </summary>
    public class AmmoSensor : LocalWorldSensorBase
    {
        /// <summary>Sensor-Timer: Jedes Frame abtasten.</summary>
        public override ISensorTimer Timer => SensorTimer.Always;

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Update() { }

        /// <inheritdoc />
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            NetworkedCharacterState characterState = references.GetCachedComponent<NetworkedCharacterState>();
            if (characterState == null)
            {
                return false;
            }

            return characterState.CurrentClipAmmo > 0;
        }
    }
}
