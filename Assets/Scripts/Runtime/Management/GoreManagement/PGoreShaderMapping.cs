using System.Collections.Generic;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Mappt PGoreType-Enum-Werte auf Texturpfade aus gore.g2shader.
    /// 1:1 Port der SoF2 goreEnumShader_t Tabelle.
    /// Texturpfade referenzieren die clampmap-Eintraege aus den Shader-Definitionen.
    /// </summary>
    public static class PGoreShaderMapping
    {
        /// <summary>Texturpfad-Lookup fuer jeden PGoreType.</summary>
        private static readonly Dictionary<PGoreType, string> s_TextureMap = new()
        {
            { PGoreType.Armor,             "models/characters/gore/damage_large" },
            { PGoreType.BulletBig,         "models/characters/gore/gil_bullet_holeF" },
            { PGoreType.KnifeSlash,        "models/characters/gore/gil_knife2" },
            { PGoreType.Puncture,          "models/characters/gore/knife_puncture" },
            { PGoreType.Shotgun,           "models/characters/gore/bullet_hole_shotgun" },
            { PGoreType.ShotgunBig,        "models/characters/gore/bullet_hole_shotgun2" },
            { PGoreType.Immolate,          "models/characters/gore/damage_scorch" },
            { PGoreType.Burn,              "models/characters/gore/damage_scorch" },
            { PGoreType.Spurt,             "models/characters/gore/gil_blood_drop" },
            { PGoreType.Splatter,          "models/characters/gore/gil_blood_1_dropD" },
            { PGoreType.BloodyGlass,       "models/characters/gore/gil_bloody_glass3" },
            { PGoreType.BloodyGlassB,      "models/characters/gore/gil_bloody_glass3" },
            { PGoreType.BloodyIck,         "models/characters/gore/gil_bloody_ick_256" },
            { PGoreType.BloodyDroop,       "models/characters/gore/gil_bloody_droop" },
            { PGoreType.BloodyMaul,        "models/characters/gore/gil_bloody_maul" },
            { PGoreType.BloodyDrops,       "models/characters/gore/gil_bloody_drops" },
            { PGoreType.BulletE,           "models/characters/gore/gil_bullet_holeE_128" },
            { PGoreType.BulletF,           "models/characters/gore/gil_bullet_holeF" },
            { PGoreType.BulletG,           "models/characters/gore/gil_bullet_holeG" },
            { PGoreType.BulletH,           "models/characters/gore/gil_bullet_holeH" },
            { PGoreType.BulletI,           "models/characters/gore/gil_bullet_holeE_128" },
            { PGoreType.BulletJ,           "models/characters/gore/gil_bullet_holeE_128" },
            { PGoreType.BulletK,           "models/characters/gore/gil_bullet_holeE_128" },
            { PGoreType.BloodyHand,        "models/characters/gore/gil_bloody_hand" },
            { PGoreType.PowderBurnDense,   "models/characters/gore/gil_powder_burn_dense" },
            { PGoreType.PowderBurnChunky,  "models/characters/gore/gil_powder_burn_chunky" },
            { PGoreType.KnifeSlash2,       "models/characters/gore/gil_knife" },
            { PGoreType.KnifeSlash3,       "models/characters/gore/gil_knife2" },
            { PGoreType.ChunkySplat,       "models/characters/gore/gt_chunky_splat" },
            { PGoreType.BigSplatter,       "models/characters/gore/gil_big_splatter" },
            { PGoreType.BloodySplotch,     "models/characters/gore/gil_bloody_splotch" },
            { PGoreType.Bleeder,           "models/characters/gore/gil_drip" },
            { PGoreType.Pellets,           "models/characters/gore/gil_bullet_holeH" },
            { PGoreType.KnifeSoak,         "models/characters/gore/gil_knife_soak" },
            { PGoreType.BleederDense,      "models/characters/gore/gil_drip" },
            { PGoreType.BloodySplotch2,    "models/characters/gore/gil_bloody_splotch2" },
            { PGoreType.BloodyDrips,       "models/characters/gore/gil_big_drips" },
            { PGoreType.DrippingDown,      "models/characters/gore/gil_dripping_down" },
            { PGoreType.Gutshot,           "models/characters/gore/gt_chunky_splat" },
            { PGoreType.Shrapnel,          "models/characters/gore/gore_shrapnel" },
        };

        /// <summary>
        /// Gibt den Texturpfad fuer den angegebenen PGoreType zurueck.
        /// Fallback auf bullet_holeF falls der Typ nicht gemappt ist.
        /// </summary>
        public static string GetTexturePath(PGoreType goreType)
        {
            if (s_TextureMap.TryGetValue(goreType, out string path))
            {
                return path;
            }

            return "models/characters/gore/gil_bullet_holeF";
        }
    }
}
