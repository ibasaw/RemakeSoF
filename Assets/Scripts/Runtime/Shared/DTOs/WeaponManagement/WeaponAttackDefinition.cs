using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.WeaponManagement
{
    /// <summary>
    /// Angriffsdefinition einer Waffe (Primär- oder Alternativangriff).
    /// </summary>
    [Serializable]
    public class WeaponAttackDefinition
    {
        /// <summary>
        /// Schaden pro Treffer (MP-Wert).
        /// </summary>
        [JsonProperty("damage")]
        public int Damage;

        /// <summary>
        /// Reichweite in Game-Units (Hitscan-Waffen).
        /// </summary>
        [JsonProperty("range")]
        public int Range;

        /// <summary>
        /// Explosionsradius (Projektilwaffen).
        /// </summary>
        [JsonProperty("radius")]
        public int Radius;

        /// <summary>
        /// Knockback-Staerke fuer Explosionen (SoF2 default: g_knockback = 700).
        /// Konfigurierbar pro Waffe fuer unterschiedliche Rueckstoss-Werte.
        /// 0 oder fehlend = Standard (700).
        /// </summary>
        [JsonProperty("knockback")]
        public int Knockback;

        /// <summary>
        /// Feuermodus ("single", "burst", "auto").
        /// </summary>
        [JsonProperty("fireMode")]
        public string FireMode;

        /// <summary>
        /// Feuer-Verzögerung in ms (z.B. MM1).
        /// </summary>
        [JsonProperty("fireDelay")]
        public int FireDelay;

        /// <summary>
        /// Ob Gore-Effekte ausgelöst werden.
        /// </summary>
        [JsonProperty("gore")]
        public bool Gore;

        /// <summary>
        /// Lautstärke des Schusses (0.0 - 1.0).
        /// </summary>
        [JsonProperty("volume")]
        public float Volume;

        /// <summary>
        /// Rückstoß-Winkel [min, max, left, right].
        /// </summary>
        [JsonProperty("kickAngles")]
        public List<float> KickAngles;

        /// <summary>
        /// Streuung (Anfangswert).
        /// </summary>
        [JsonProperty("inaccuracy")]
        public float Inaccuracy;

        /// <summary>
        /// Maximale Streuung.
        /// </summary>
        [JsonProperty("maxInaccuracy")]
        public float MaxInaccuracy;

        /// <summary>
        /// Anzahl Projektile pro Schuss (Schrotflinten, z.B. M590: 8 Pellets).
        /// </summary>
        [JsonProperty("pellets")]
        public int Pellets;

        /// <summary>
        /// Zusaetzliche Streuung pro Pellet (Schrotflinten-Spread in Grad).
        /// </summary>
        [JsonProperty("spread")]
        public float Spread;

        /// <summary>
        /// Muzzle-Flash-Effekt-Pfad.
        /// </summary>
        [JsonProperty("muzzleFlash")]
        public string MuzzleFlash;

        /// <summary>
        /// Muzzle-Smoke-Effekt-Pfad.
        /// </summary>
        [JsonProperty("muzzleSmoke")]
        public string MuzzleSmoke;

        /// <summary>
        /// Shell-Casing-Effekt-Pfad.
        /// </summary>
        [JsonProperty("shellCasingEject")]
        public string ShellCasingEject;

        /// <summary>
        /// Bone für Shell-Casing-Ejektion.
        /// </summary>
        [JsonProperty("ejectBone")]
        public string EjectBone;

        /// <summary>
        /// Tracer-Effekt-Pfad.
        /// </summary>
        [JsonProperty("tracerEffect")]
        public string TracerEffect;

        /// <summary>
        /// Verfügbare Feuermodi (z.B. ["single", "burst", "auto"]).
        /// </summary>
        [JsonProperty("fireModes")]
        public List<string> FireModes;

        /// <summary>
        /// Melee-Typ für Alternativangriff (z.B. "bayonet").
        /// </summary>
        [JsonProperty("melee")]
        public string Melee;

        /// <summary>
        /// Projektildefinition (für Projektilwaffen).
        /// </summary>
        [JsonProperty("projectile")]
        public WeaponProjectileDefinition Projectile;

        /// <summary>
        /// Separate Munition für den Alternativangriff (z.B. M203 am M4).
        /// </summary>
        [JsonProperty("ammo")]
        public WeaponAmmoDefinition Ammo;
    }
}
