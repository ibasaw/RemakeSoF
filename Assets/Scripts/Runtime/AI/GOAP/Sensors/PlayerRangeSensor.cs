using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Weltsensor: Prueft ob ein erkannter Spieler innerhalb der Waffenreichweite ist.
    /// Gibt 1 zurueck wenn die naechste Spieler-Sensor-Distanz kleiner als die Waffenreichweite ist,
    /// oder wenn der Bot kuerzlich Schaden erhalten hat und der Angreifer in Reichweite ist.
    /// </summary>
    public class PlayerRangeSensor : LocalWorldSensorBase
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
            float weaponRange = controller.WeaponRangeMeters;

            // Primaer: Horizontale XZ-Distanz fuer Reichweitenvergleich (3D-Distanz
            // ist wegen Augenhoehen-Differenz groesser als die tatsaechliche Entfernung)
            float playerDist = controller.PlayerSensorDistanceXZ;
            if (playerDist <= weaponRange)
            {
                return true;
            }

            // Sekundaer: Angreifer-Distanz (Damage-Reaction)
            if (controller.WasDamagedRecently && controller.LastAttackerPosition.HasValue)
            {
                float attackerDist = Vector3.Distance(controller.EyePosition, controller.LastAttackerPosition.Value);
                return attackerDist <= weaponRange;
            }

            return false;
        }
    }
}
