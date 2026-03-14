using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Einzelne Animations-Definition einer Waffe (z.B. mp_idle, mp_attack).
    /// </summary>
    [Serializable]
    public class WeaponAnimationEntry
    {
        /// <summary>
        /// Animations-Enum-Name (z.B. "TORSO_IDLE_KNIFE", "TORSO_ATTACK_M4").
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary>
        /// Quell-Dateiname der Animation (xsi-Dateiname ohne Pfad/Extension).
        /// </summary>
        [JsonProperty("source")]
        public string Source;

        /// <summary>
        /// Start-Frame in der Skeleton-Animation.
        /// </summary>
        [JsonProperty("startFrame")]
        public int StartFrame;

        /// <summary>
        /// Dauer der Animation in Frames.
        /// </summary>
        [JsonProperty("duration")]
        public int Duration;

        /// <summary>
        /// Frames pro Sekunde.
        /// </summary>
        [JsonProperty("fps")]
        public int Fps;

        /// <summary>
        /// Ob die Animation loopen soll.
        /// </summary>
        [JsonProperty("loop")]
        public bool Loop;

        /// <summary>
        /// Berechnet die Dauer der Animation in Sekunden.
        /// </summary>
        public float DurationInSeconds => Fps > 0 ? (float)Duration / Fps : 0f;
    }
}
