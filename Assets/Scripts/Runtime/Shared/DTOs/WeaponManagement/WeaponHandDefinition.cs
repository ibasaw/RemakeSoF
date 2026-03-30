using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Einzelne Hand-Definition (links oder rechts) fuer SoF2-Style First-Person-Darstellung.
    /// Definiert, an welchem Buffer-Bolt die Hand angebunden wird.
    /// </summary>
    [Serializable]
    public class WeaponHandDefinition
    {
        /// <summary>
        /// Bolt-Name auf dem Buffer, an dem diese Hand befestigt wird
        /// (z.B. "lhand_m4", "rhand_m4", "handle_m4").
        /// </summary>
        [JsonProperty("boltToBone")]
        public string BoltToBone;
    }
}
