using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Hand-Definitionen fuer SoF2-Style First-Person-Waffendarstellung.
    /// Beide Haende (lhand.glm / rhand.glm) werden an Buffer-Bolts befestigt.
    /// Nicht alle Waffen nutzen beide Haende — einige haben nur eine linke Hand.
    /// </summary>
    [Serializable]
    public class WeaponHandsDefinition
    {
        /// <summary>
        /// Linke Hand — fast immer vorhanden (z.B. boltToBone = "lhand_m4").
        /// </summary>
        [JsonProperty("left")]
        public WeaponHandDefinition Left;

        /// <summary>
        /// Rechte Hand — bei manchen Waffen nicht vorhanden (z.B. M60, USAS-12).
        /// Null bedeutet: keine separate rechte Hand fuer diese Waffe.
        /// </summary>
        [JsonProperty("right")]
        public WeaponHandDefinition Right;
    }
}
