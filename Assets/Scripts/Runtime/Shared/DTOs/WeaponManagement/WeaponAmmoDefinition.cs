using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Munitionsdefinition einer Waffe.
    /// </summary>
    [Serializable]
    public class WeaponAmmoDefinition
    {
        /// <summary>
        /// Munitionstyp (z.B. "5.56mm", "RPG7", "40mm grenade").
        /// </summary>
        [JsonProperty("type")]
        public string Type;

        /// <summary>
        /// Maximale Magazingröße.
        /// </summary>
        [JsonProperty("maxClip")]
        public int MaxClip;

        /// <summary>
        /// Anzahl zusätzlicher Magazine.
        /// </summary>
        [JsonProperty("extraClips")]
        public int ExtraClips;

        /// <summary>
        /// Anfangs-Magazin beim Spawn.
        /// </summary>
        [JsonProperty("startClip")]
        public int StartClip;

        /// <summary>
        /// Anfangs-Reserve beim Spawn.
        /// </summary>
        [JsonProperty("startReserve")]
        public int StartReserve;

        /// <summary>
        /// Ob die Munition unendlich ist (z.B. Knife).
        /// </summary>
        [JsonProperty("infinite")]
        public bool Infinite;
    }
}
