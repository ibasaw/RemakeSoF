using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Reload-Sound-Konfiguration fuer eine Waffe.
    /// Enthaelt Sound-Events fuer Standard-Reload oder phasenbasierte Shell-Reloads.
    /// SoF2-Referenz: Sounds wie clipOut, clipIn, boltRelease wurden per Animation-Event getriggert.
    /// </summary>
    [Serializable]
    public class ReloadSoundDefinition
    {
        /// <summary>
        /// Sound-Events fuer Standard-Reload (einzelne Animation, z.B. M4, AK74).
        /// Normalisierte Zeiten relativ zu mp_reload Duration.
        /// </summary>
        [JsonProperty("events")]
        public ReloadSoundEvent[] Events;

        /// <summary>
        /// Sound-Events fuer die Start-Phase beim Shell-Reload (z.B. M590 pump open, MM1 drum open).
        /// Normalisierte Zeiten relativ zu mp_reloadStart Duration.
        /// </summary>
        [JsonProperty("startEvents")]
        public ReloadSoundEvent[] StartEvents;

        /// <summary>
        /// Sound-Events fuer jede einzelne Shell-Lade-Phase (z.B. M590 shell insert).
        /// Wird pro Shell wiederholt. Normalisierte Zeiten relativ zu mp_reloadShell Duration.
        /// </summary>
        [JsonProperty("shellEvents")]
        public ReloadSoundEvent[] ShellEvents;

        /// <summary>
        /// Sound-Events fuer die End-Phase beim Shell-Reload (z.B. M590 pump close, MM1 drum close).
        /// Normalisierte Zeiten relativ zu mp_reloadEnd Duration.
        /// </summary>
        [JsonProperty("endEvents")]
        public ReloadSoundEvent[] EndEvents;
    }
}
