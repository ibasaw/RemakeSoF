using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// SoF2 First-Person View-Offset: positioniert die Kamera relativ zum Standard-Viewpoint.
    /// Werte in Quake-Units (QU), werden zur Laufzeit in Unity-Meter konvertiert (× 0.0254).
    /// </summary>
    [Serializable]
    public class WeaponViewOffsetDefinition
    {
        /// <summary>
        /// Vorwärts-Offset in QU (positiv = weiter nach vorn).
        /// </summary>
        [JsonProperty("forward")]
        public float Forward;

        /// <summary>
        /// Seitwärts-Offset in QU (positiv = nach rechts).
        /// </summary>
        [JsonProperty("right")]
        public float Right;

        /// <summary>
        /// Vertikal-Offset in QU (positiv = nach oben).
        /// </summary>
        [JsonProperty("up")]
        public float Up;
    }
}
