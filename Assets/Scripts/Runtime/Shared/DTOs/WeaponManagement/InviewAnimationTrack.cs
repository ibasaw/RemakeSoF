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
        /// Alternative Animation-Clip-Namen fuer Fire-Varianten.
        /// SoF2: Rifles (AK-74, M3A1, M60) und Knife haben mehrere Fire-Clips,
        /// die zufaellig ausgewaehlt werden fuer visuelles Feedback.
        /// Index korrespondiert mit <see cref="Ends"/> und <see cref="Transitions"/>.
        /// </summary>
        [JsonProperty("variants")]
        public string[] Variants;

        /// <summary>
        /// Zusaetzliche Idle-Animationen (z.B. Finger-Adjust, Finger-Spin).
        /// SoF2: Werden gelegentlich statt der Standard-Idle-Animation abgespielt.
        /// </summary>
        [JsonProperty("extras")]
        public string[] Extras;

        /// <summary>
        /// End-State-Namen pro Variante fuer Knife-Combo-System.
        /// SoF2: Nach Fire/Firetrans wird der End-State anhand des Varianten-Index
        /// nachgeschlagen (z.B. ends[0] = "fireend1" → Knife kehrt zur Ruhepose zurueck).
        /// </summary>
        [JsonProperty("ends")]
        public string[] Ends;

        /// <summary>
        /// Transition-State-Namen pro Variante fuer Knife-Combo-System.
        /// SoF2: Bei erneutem Angriff waehrend Fire wird der Transition-State anhand
        /// des Varianten-Index nachgeschlagen (z.B. transitions[0] = "firetrans1" → naechster Schlag).
        /// </summary>
        [JsonProperty("transitions")]
        public string[] Transitions;

        /// <summary>
        /// Berechnet die Dauer der Animation in Sekunden.
        /// </summary>
        public float DurationInSeconds => Fps > 0 ? (float)Duration / Fps : 0f;
    }
}
