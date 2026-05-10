using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.WeaponManagement;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Lokaler Weltsensor: Prueft ob der Bot Munition hat.
    /// Gibt 1 zurueck wenn Primary-Clip > 0, Alt-Clip > 0 oder Waffe infinite ist.
    /// </summary>
    public class AmmoSensor : LocalWorldSensorBase
    {
        /// <summary>Sensor-Timer: Munition aendert sich nicht 60x/s; 4 Hz reicht voellig.</summary>
        public override ISensorTimer Timer => SensorTimer.Interval(0.25f);

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

            // Primary Clip oder Alt Clip vorhanden → hat Ammo
            if (characterState.CurrentClipAmmo > 0 || characterState.AltClipAmmo > 0)
            {
                return true;
            }

            // Waffe ist infinite (z.B. Knife primary) → immer Ammo
            WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition weapon = loader?.GetById(characterState.CurrentWeaponName);
            if (weapon?.Ammo != null && weapon.Ammo.Infinite)
            {
                return true;
            }

            return false;
        }
    }
}
