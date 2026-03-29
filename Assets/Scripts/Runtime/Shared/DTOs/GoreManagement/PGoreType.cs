namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// SoF2 PGORE (Projected Gore) Typen — 1:1 Port von goreEnum_t aus G2_gore_shared.h.
    /// Jeder Enum-Wert mappt auf einen Shader/Textur-Eintrag aus gore.g2shader.
    /// Wird von PGoreWeaponDispatch ausgewaehlt und von PGoreDecalApplier dargestellt.
    /// </summary>
    public enum PGoreType
    {
        /// <summary>Kein Gore.</summary>
        None = 0,

        /// <summary>Ruestungstreffer (unused in MP).</summary>
        Armor = 1,

        /// <summary>Grosses Einschussloch.</summary>
        BulletBig = 2,

        /// <summary>Messerschnitt Variante 1 (gil_knife2).</summary>
        KnifeSlash = 3,

        /// <summary>Messerstich (knife_puncture).</summary>
        Puncture = 4,

        /// <summary>Schrotflinten-Wunde (bullet_hole_shotgun).</summary>
        Shotgun = 5,

        /// <summary>Grosse Schrotflinten-Wunde (bullet_hole_shotgun2).</summary>
        ShotgunBig = 6,

        /// <summary>Verbrennung mit Feuer-Sprite (immolation_sensation).</summary>
        Immolate = 7,

        /// <summary>Brandmal (damage_scorch).</summary>
        Burn = 8,

        /// <summary>Blut-Spritzer mit Tropfen (spurter).</summary>
        Spurt = 9,

        /// <summary>Blut-Kleckse auf Oberflaeche (splatter).</summary>
        Splatter = 10,

        /// <summary>Blut auf Glas (bloody_glass).</summary>
        BloodyGlass = 11,

        /// <summary>Blut auf Glas Variante B (bloody_glass_b).</summary>
        BloodyGlassB = 12,

        /// <summary>Blutiger Matsch (bloody_ick).</summary>
        BloodyIck = 13,

        /// <summary>Herunterlaufendes Blut (bloody_droop).</summary>
        BloodyDroop = 14,

        /// <summary>Zerfleischt (bloody_maul).</summary>
        BloodyMaul = 15,

        /// <summary>Bluttropfen (bloody_drops).</summary>
        BloodyDrops = 16,

        /// <summary>Einschussloch Variante E (gil_bullet_holeE_128).</summary>
        BulletE = 17,

        /// <summary>Einschussloch Variante F (gil_bullet_holeF).</summary>
        BulletF = 18,

        /// <summary>Einschussloch Variante G (gil_bullet_holeG).</summary>
        BulletG = 19,

        /// <summary>Einschussloch Variante H (gil_bullet_holeH).</summary>
        BulletH = 20,

        /// <summary>Einschussloch Variante I (alias E).</summary>
        BulletI = 21,

        /// <summary>Einschussloch Variante J (alias E).</summary>
        BulletJ = 22,

        /// <summary>Einschussloch Variante K (alias E).</summary>
        BulletK = 23,

        /// <summary>Blutiger Handabdruck (bloody_hand).</summary>
        BloodyHand = 24,

        /// <summary>Dichter Pulverbrand (powder_burn_dense).</summary>
        PowderBurnDense = 25,

        /// <summary>Klumpiger Pulverbrand (powder_burn_chunky).</summary>
        PowderBurnChunky = 26,

        /// <summary>Messerschnitt Variante 2 (knife_slash2 / gil_knife).</summary>
        KnifeSlash2 = 27,

        /// <summary>Messerschnitt Variante 3 (knife_slash3 / gil_knife2).</summary>
        KnifeSlash3 = 28,

        /// <summary>Chunky Splat (gt_chunky_splat).</summary>
        ChunkySplat = 29,

        /// <summary>Grosser Blutklecks (big_splatter / gil_big_splatter).</summary>
        BigSplatter = 30,

        /// <summary>Blut-Fleck (bloody_splotch / gil_bloody_splotch).</summary>
        BloodySplotch = 31,

        /// <summary>Blutender Effekt (bleeder / gil_drip).</summary>
        Bleeder = 32,

        /// <summary>Schrotflinten-Pellets (pellets / gil_bullet_holeH).</summary>
        Pellets = 33,

        /// <summary>Wachsende Blutlache auf Haut (knife_soak / gil_knife_soak).</summary>
        KnifeSoak = 34,

        /// <summary>Dichtes Bluten (bleeder_dense / gil_drip).</summary>
        BleederDense = 35,

        /// <summary>Blut-Fleck Variante 2 (bloody_splotch2).</summary>
        BloodySplotch2 = 36,

        /// <summary>Bluttropfen (bloody_drips / gil_big_drips).</summary>
        BloodyDrips = 37,

        /// <summary>Herunterlaufende Tropfen (dripping_down).</summary>
        DrippingDown = 38,

        /// <summary>Bauchtreffer (chunky_splat alias).</summary>
        Gutshot = 39,

        /// <summary>Splitterwunde (gore_shrapnel).</summary>
        Shrapnel = 40,

        /// <summary>Gesamtanzahl der Gore-Typen.</summary>
        Count = 41
    }
}
