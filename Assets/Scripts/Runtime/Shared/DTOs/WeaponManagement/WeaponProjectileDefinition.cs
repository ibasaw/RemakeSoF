using System;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Projektildefinition für Projektilwaffen (RPG, M203, MM1, Grenades).
    /// </summary>
    [Serializable]
    public class WeaponProjectileDefinition
    {
        /// <summary>
        /// Gravitätsfaktor (0 = keine Gravitation, 0.5 = halbe).
        /// </summary>
        [JsonProperty("gravity")]
        public float Gravity;

        /// <summary>
        /// Projektilgeschwindigkeit in Game-Units/Sekunde.
        /// </summary>
        [JsonProperty("speed")]
        public float Speed;

        /// <summary>
        /// Detonationsart ("impact", "sticky", "timed").
        /// </summary>
        [JsonProperty("detonation")]
        public string Detonation;

        /// <summary>
        /// Trail-Effekt-Pfad.
        /// </summary>
        [JsonProperty("effect")]
        public string Effect;

        /// <summary>
        /// Explosions-Effekt-Pfad.
        /// </summary>
        [JsonProperty("explosionEffect")]
        public string ExplosionEffect;

        /// <summary>
        /// Unterwasser-Explosions-Effekt.
        /// </summary>
        [JsonProperty("underwaterEffect")]
        public string UnderwaterEffect;

        /// <summary>
        /// Wasser-Oberflächen-Explosions-Effekt.
        /// </summary>
        [JsonProperty("waterExplosionEffect")]
        public string WaterExplosionEffect;

        /// <summary>
        /// Projektil-Loop-Sound (Fluggeräusch).
        /// </summary>
        [JsonProperty("loopSound")]
        public string LoopSound;

        /// <summary>
        /// Projektil-Model-Pfad (z.B. für geworfenes Messer).
        /// </summary>
        [JsonProperty("model")]
        public string Model;

        /// <summary>
        /// Objekttyp (z.B. "knife").
        /// </summary>
        [JsonProperty("objectType")]
        public string ObjectType;
    }
}
