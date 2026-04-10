using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Weltsensor: Prueft ob ein Spieler per Sensor3D-Array sichtbar ist.
    /// Gibt 1 zurueck wenn mindestens ein Sensor einen Treffer auf dem Player-Layer hat, sonst 0.
    /// </summary>
    public class PlayerVisibilitySensor : LocalWorldSensorBase
    {
        /// <summary>Sensor-Timer: Jedes Frame abtasten fuer reaktive AI.</summary>
        public override ISensorTimer Timer => SensorTimer.Always;

        /// <inheritdoc />
        public override void Created() { }

        /// <inheritdoc />
        public override void Update() { }

        /// <inheritdoc />
        public override SenseValue Sense(IActionReceiver agent, IComponentReference references)
        {
            AIBotController controller = references.GetCachedComponent<AIBotController>();
            if (controller == null)
            {
                return false;
            }

            controller.EnsureSensorsTicked();

            // Spieler sichtbar per Sensor ODER kuerzlich Schaden erlitten (Angreifer-Richtung bekannt)
            return controller.PlayerSensorDetected || controller.WasDamagedRecently;
        }
    }
}
