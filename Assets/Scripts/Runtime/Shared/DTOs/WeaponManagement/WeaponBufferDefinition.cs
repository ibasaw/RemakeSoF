using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Buffer-Skeleton-Definition fuer SoF2-Style First-Person-Waffendarstellung.
    /// Der Buffer ist ein unsichtbares Skeleton, das zwischen Waffe und Haenden liegt
    /// und waffen-spezifische Attachment-Bolts bereitstellt (Muzzle, Hand-Bolts).
    /// Buffer-Typen: rifle, pistol, mg, gren, at, acc, golda — je nach Waffenkategorie.
    /// </summary>
    [Serializable]
    public class WeaponBufferDefinition
    {
        /// <summary>
        /// Pfad zum Buffer-Model (z.B. "models/weapons/buffer/rifle/buffer").
        /// Wird als Addressable-Key fuer den PrefabManager verwendet.
        /// </summary>
        [JsonProperty("model")]
        public string Model;

        /// <summary>
        /// Bolt-Name auf dem Buffer, an dem die Waffe angebunden wird (z.B. "gun", "handle").
        /// </summary>
        [JsonProperty("boltToBone")]
        public string BoltToBone;

        /// <summary>
        /// Bolt-Name auf dem Buffer fuer den Muzzle-Flash-Effekt (z.B. "flash_m4", "flash_knife").
        /// </summary>
        [JsonProperty("muzzleBolt")]
        public string MuzzleBolt;
    }
}
