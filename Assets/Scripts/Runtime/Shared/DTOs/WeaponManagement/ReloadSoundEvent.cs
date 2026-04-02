using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Ein einzelnes Sound-Event waehrend einer Reload-Animation.
    /// Definiert wann (normalisierte Zeit 0-1) welcher Sound-Key gespielt wird.
    /// SoF2-Referenz: Animation-Events in GLM-Dateien triggerten Sounds bei bestimmten Frames.
    /// </summary>
    [Serializable]
    public class ReloadSoundEvent
    {
        /// <summary>
        /// Normalisierte Zeit innerhalb der Reload-Phase (0.0 = Start, 1.0 = Ende).
        /// Bei Standard-Reload: relativ zur gesamten mp_reload Animation.
        /// Bei Shell-Reload: relativ zur jeweiligen Phase (start/shell/end).
        /// </summary>
        [JsonProperty("time")]
        public float Time;

        /// <summary>
        /// Sound-Key aus dem Sounds-Dictionary der WeaponDefinition (z.B. "clipOut", "clipIn", "boltRelease").
        /// </summary>
        [JsonProperty("sound")]
        public string Sound;
    }
}
