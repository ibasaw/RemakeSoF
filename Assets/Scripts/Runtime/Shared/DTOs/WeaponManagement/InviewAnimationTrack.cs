using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Einzelner Animations-Track fuer ein Composite-Modell-Slot (Waffe, lhand, rhand).
    /// Mappt auf SoF2 SOF2.inview "info"-Eintraege pro Animation-State.
    /// Clip-Name referenziert einen Animation-Clip im Prefab (z.B. "lm4idle", "rknifeready").
    /// </summary>
    [Serializable]
    public class InviewAnimationTrack
    {
        /// <summary>
        /// Name des Animation-Clips im Prefab (z.B. "lm4idle", "rm67fire").
        /// Wird ueber Animation["clipName"] oder Animator-State abgespielt.
        /// </summary>
        [JsonProperty("clip")]
        public string Clip;

        /// <summary>
        /// Abspielgeschwindigkeit. SoF2 MP nutzt oft andere Speeds als SP.
        /// Typisch: 0.3-0.5 fuer Idle (langsamer), 1.0-3.5 fuer Aktionen.
        /// </summary>
        [JsonProperty("speed")]
        public float Speed = 1f;

        /// <summary>
        /// Frames pro Sekunde aus der .frames-Datei.
        /// Wird als Metadaten gespeichert (FBX bakt FPS bereits in den Clip).
        /// </summary>
        [JsonProperty("fps")]
        public int Fps;

        /// <summary>
        /// Dauer der Animation in Frames aus der .frames-Datei.
        /// </summary>
        [JsonProperty("duration")]
        public int Duration;

        /// <summary>
        /// Ob die Animation loopen soll. Idle-Animationen loopen,
        /// Action-Animationen (fire, reload, ready, done) spielen einmal.
        /// </summary>
        [JsonProperty("loop")]
        public bool Loop;

        /// <summary>
        /// Berechnet die Dauer der Animation in Sekunden.
        /// </summary>
        public float DurationInSeconds => Fps > 0 ? (float)Duration / Fps : 0f;
    }
}
