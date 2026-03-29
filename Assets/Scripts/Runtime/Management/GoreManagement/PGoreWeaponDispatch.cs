using System.Collections.Generic;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// 1:1 Port von SoF2 CG_DoGoreFromWeapon (cg_gore.c).
    /// Erzeugt PGoreData-Eintraege basierend auf Waffen-ID und Angriffsart.
    /// Jede Waffe hat spezifische Wundtypen, Groessen und optionale wachsende Blutlachen.
    ///
    /// SoF2-Referenz: Groessen sind in SoF2-Units (ca. 1 Unit = 1 Inch).
    /// Fuer Unity-Darstellung werden diese durch SOF2_UNIT_SCALE (0.0254) skaliert.
    ///
    /// Gore-Detail-Level (wie SoF2 cg_goreDetail):
    /// 0 = Nur Hauptwunde
    /// 1 = + wachsende Blutlache (PGORE_KNIFE_SOAK)
    /// 2 = + Pellet-Markierungen (nur Schrotflinten)
    /// </summary>
    public static class PGoreWeaponDispatch
    {
        /// <summary>Konvertierung SoF2 Units → Unity Meter (1 QU = 0.0254m).</summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;

        /// <summary>Standard-Wachstumsdauer fuer Blutlachen in Millisekunden (SoF2: 15000).</summary>
        private const int DEFAULT_GROW_DURATION = 15000;

        /// <summary>Standard-Startgroesse fuer wachsende Blutlachen (SoF2: 0.1 = 10%).</summary>
        private const float DEFAULT_GROW_START_FRACTION = 0.1f;

        /// <summary>Aktuelles Gore-Detail-Level (wie SoF2 cg_goreDetail cvar).</summary>
        public static int GoreDetailLevel { get; set; } = 1;

        /// <summary>
        /// Erzeugt alle Gore-Markierungen fuer einen Waffeneinschlag.
        /// 1:1 Port von CG_DoGoreFromWeapon aus cg_gore.c.
        /// </summary>
        /// <param name="weaponId">Waffen-ID aus SoF2_Weapons_New.json (z.B. "knife", "m4", "m590").</param>
        /// <param name="isAltAttack">Ob es sich um den Alternativangriff handelt.</param>
        /// <param name="hitLocation">Einschlagpunkt in Weltkoordinaten.</param>
        /// <param name="hitDirection">Einschlagrichtung (normalisiert).</param>
        /// <returns>Liste von PGoreData-Eintraegen fuer diesen Treffer.</returns>
        public static List<PGoreData> CreateGoreEntries(
            string weaponId,
            bool isAltAttack,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            List<PGoreData> entries = new();

            if (string.IsNullOrEmpty(weaponId))
            {
                return entries;
            }

            switch (weaponId.ToLowerInvariant())
            {
                // ===== KNIFE (WP_KNIFE) =====
                case "knife":
                    if (isAltAttack)
                    {
                        // Thrust (Stich)
                        AddGore(entries, PGoreType.Puncture, Random.Range(3.5f, 4.0f),
                            hitLocation, hitDirection);
                        if (GoreDetailLevel > 0)
                        {
                            AddGrowGore(entries, PGoreType.KnifeSoak, 4.0f * 1.4f,
                                DEFAULT_GROW_DURATION, DEFAULT_GROW_START_FRACTION,
                                hitLocation, hitDirection);
                        }
                    }
                    else
                    {
                        // Slash (Schnitt) — 3 Varianten
                        float angle = (Mathf.PI / 2f * (1f + 2f * Random.Range(0, 2)))
                            + Random.Range(-0.7f, 0.7f);
                        int slashVariant = Random.Range(1, 4);

                        switch (slashVariant)
                        {
                            case 1:
                            {
                                float size = Random.Range(2.8f, 3.2f);
                                float size2 = Random.Range(1.8f, 2.2f);
                                AddSlashGore(entries, PGoreType.KnifeSlash, angle, size, size2,
                                    hitLocation, hitDirection);
                                if (GoreDetailLevel > 0)
                                {
                                    AddSlashGrowGore(entries, PGoreType.KnifeSoak, angle,
                                        size * 1.2f, size2 * 2.0f,
                                        DEFAULT_GROW_DURATION, DEFAULT_GROW_START_FRACTION,
                                        hitLocation, hitDirection);
                                }
                                break;
                            }
                            case 2:
                            {
                                float size = 8.0f * Random.Range(0.8f, 1.2f);
                                float size2 = 1.75f * Random.Range(0.8f, 1.2f);
                                AddSlashGore(entries, PGoreType.KnifeSlash2, angle, size, size2,
                                    hitLocation, hitDirection);
                                if (GoreDetailLevel > 0)
                                {
                                    AddSlashGrowGore(entries, PGoreType.KnifeSoak, angle,
                                        size * 1.2f, size2 * 2.0f,
                                        DEFAULT_GROW_DURATION, DEFAULT_GROW_START_FRACTION,
                                        hitLocation, hitDirection);
                                }
                                break;
                            }
                            default:
                            {
                                float size = Random.Range(3.0f, 4.0f);
                                float size2 = Random.Range(0.5f, 1.0f);
                                AddSlashGore(entries, PGoreType.KnifeSlash3, angle, size, size2,
                                    hitLocation, hitDirection);
                                if (GoreDetailLevel > 0)
                                {
                                    AddSlashGrowGore(entries, PGoreType.KnifeSoak, angle,
                                        size * 1.2f, size2 * 2.0f,
                                        DEFAULT_GROW_DURATION, DEFAULT_GROW_START_FRACTION,
                                        hitLocation, hitDirection);
                                }
                                break;
                            }
                        }
                    }
                    break;

                // ===== PISTOLS (WP_M1911A1, WP_SILVER_TALON, WP_USSOCOM) =====
                case "m1911a1":
                case "silver_talon":
                case "ussocom":
                    if (isAltAttack)
                    {
                        // Pistol Whip (Schlag)
                        AddGore(entries, PGoreType.BloodySplotch2, Random.Range(5.25f, 7.5f),
                            hitLocation, hitDirection);
                    }
                    else
                    {
                        AddBulletGore(entries, Random.Range(3.75f, 4.5f), 4.5f * 1.35f,
                            hitLocation, hitDirection);
                    }
                    break;

                // ===== SUBMACHINE GUNS (WP_MICRO_UZI) =====
                case "microuzi":
                    AddBulletGore(entries, Random.Range(3.75f, 4.5f), 4.5f * 1.35f,
                        hitLocation, hitDirection);
                    break;

                // ===== MEDIUM RIFLES (WP_M3A1, WP_MP5, WP_SIG551/OICW) =====
                case "m3a1":
                case "mp5":
                case "oicw":
                    AddBulletGore(entries, Random.Range(5.25f, 7.5f), 7.5f * 1.3f,
                        hitLocation, hitDirection);
                    break;

                // ===== ASSAULT RIFLES (WP_M4) =====
                case "m4":
                    if (isAltAttack)
                    {
                        // M203 Grenade Launcher
                        AddGore(entries, PGoreType.Shrapnel, Random.Range(14.0f, 17.0f),
                            hitLocation, hitDirection);
                        if (GoreDetailLevel > 1)
                        {
                            AddGore(entries, PGoreType.Pellets, 10.0f,
                                hitLocation, hitDirection);
                        }
                    }
                    else
                    {
                        AddBulletGore(entries, Random.Range(5.25f, 7.5f), 7.5f * 1.3f,
                            hitLocation, hitDirection);
                    }
                    break;

                // ===== ASSAULT RIFLES (WP_AK74) =====
                case "ak74":
                    if (isAltAttack)
                    {
                        // Bayonet
                        AddGore(entries, PGoreType.Puncture, Random.Range(3.5f, 4.0f),
                            hitLocation, hitDirection);
                        if (GoreDetailLevel > 0)
                        {
                            AddGrowGore(entries, PGoreType.KnifeSoak, 4.0f * 1.4f,
                                DEFAULT_GROW_DURATION, DEFAULT_GROW_START_FRACTION,
                                hitLocation, hitDirection);
                        }
                    }
                    else
                    {
                        AddBulletGore(entries, Random.Range(5.25f, 7.5f), 7.5f * 1.3f,
                            hitLocation, hitDirection);
                    }
                    break;

                // ===== SHOTGUNS (WP_M590) =====
                case "m590":
                    if (isAltAttack)
                    {
                        // Melee (Kolbenschlag)
                        AddGore(entries, PGoreType.BloodySplotch2, Random.Range(7.75f, 11.25f),
                            hitLocation, hitDirection);
                    }
                    else
                    {
                        AddShotgunGore(entries, Random.Range(8.25f, 11.25f),
                            hitLocation, hitDirection);
                    }
                    break;

                // ===== SHOTGUNS (WP_USAS12) =====
                case "usas12":
                    AddShotgunGore(entries, Random.Range(8.25f, 11.25f),
                        hitLocation, hitDirection);
                    break;

                // ===== LARGE CALIBER (WP_MSG90A1, WP_M60) =====
                case "msg90a1":
                case "m60":
                    AddBulletGore(entries, Random.Range(6.0f, 9.0f), 9.0f * 1.25f,
                        hitLocation, hitDirection);
                    break;

                // ===== EXPLOSIVES (WP_MM1, WP_RPG7, WP_SMOHG92, WP_F1) =====
                case "mm1":
                case "rpg7":
                case "smohg92":
                case "f1":
                case "m67":
                case "l2a2":
                    AddGore(entries, PGoreType.Shrapnel, Random.Range(14.0f, 17.0f),
                        hitLocation, hitDirection);
                    if (GoreDetailLevel > 1)
                    {
                        AddGore(entries, PGoreType.Pellets, 10.0f,
                            hitLocation, hitDirection);
                    }
                    break;

                // ===== STUN GRENADES (WP_M84, WP_M15) =====
                case "m84":
                case "m15":
                    AddGore(entries, PGoreType.Burn, Random.Range(14.0f, 18.0f),
                        hitLocation, hitDirection);
                    break;

                // ===== INCENDIARY (WP_ANM14) =====
                case "anm14":
                    AddTimedGore(entries, PGoreType.Immolate, Random.Range(18.0f, 22.0f), 4000,
                        hitLocation, hitDirection);
                    if (GoreDetailLevel > 0)
                    {
                        AddGore(entries, PGoreType.Burn, Random.Range(18.0f, 22.0f),
                            hitLocation, hitDirection);
                    }
                    break;

                // ===== MDN11 (similar to SMG) =====
                case "mdn11":
                    AddBulletGore(entries, Random.Range(5.25f, 7.5f), 7.5f * 1.3f,
                        hitLocation, hitDirection);
                    break;

                // ===== DEFAULT: Medium Bullet =====
                default:
                    AddBulletGore(entries, Random.Range(5.25f, 7.5f), 7.5f * 1.3f,
                        hitLocation, hitDirection);
                    break;
            }

            return entries;
        }

        /// <summary>
        /// Standard-Bullet-Gore: zufaelliger Bullet-Typ E/F/G + optionale Blutlache.
        /// SoF2: irand(PGORE_BULLET_E, PGORE_BULLET_G) fuer 3 Varianten.
        /// </summary>
        private static void AddBulletGore(
            List<PGoreData> entries,
            float size,
            float soakSize,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            PGoreType bulletType = (PGoreType)Random.Range(
                (int)PGoreType.BulletE,
                (int)PGoreType.BulletG + 1);

            AddGore(entries, bulletType, size, hitLocation, hitDirection);

            if (GoreDetailLevel > 0)
            {
                AddGrowGore(entries, PGoreType.KnifeSoak, soakSize,
                    DEFAULT_GROW_DURATION, DEFAULT_GROW_START_FRACTION,
                    hitLocation, hitDirection);
            }
        }

        /// <summary>
        /// Schrotflinten-Gore: Shotgun/ShotgunBig + Blutlache + optionale Pellets.
        /// SoF2: irand(PGORE_SHOTGUN, PGORE_SHOTGUNBIG).
        /// </summary>
        private static void AddShotgunGore(
            List<PGoreData> entries,
            float size,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            PGoreType shotgunType = Random.value > 0.5f ? PGoreType.Shotgun : PGoreType.ShotgunBig;

            AddGore(entries, shotgunType, size, hitLocation, hitDirection);

            if (GoreDetailLevel > 0)
            {
                AddGrowGore(entries, PGoreType.KnifeSoak, 11.25f * 1.25f,
                    DEFAULT_GROW_DURATION, DEFAULT_GROW_START_FRACTION,
                    hitLocation, hitDirection);

                if (GoreDetailLevel > 1)
                {
                    AddGore(entries, PGoreType.Pellets, 8.25f,
                        hitLocation, hitDirection);
                }
            }
        }

        /// <summary>
        /// CG_AddGore — statische, sofort sichtbare Gore-Markierung.
        /// Quadratisch (SSize == TSize), zufaellige Rotation.
        /// </summary>
        private static void AddGore(
            List<PGoreData> entries,
            PGoreType goreType,
            float size,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            entries.Add(new PGoreData
            {
                GoreType = goreType,
                SSize = size * SOF2_UNIT_SCALE,
                TSize = size * SOF2_UNIT_SCALE,
                Theta = Random.Range(0f, Mathf.PI * 2f),
                HitLocation = hitLocation,
                RayDirection = hitDirection,
                GrowDuration = -1,
                GoreScaleStartFraction = 1.0f,
                LifeTime = 0,
                FrontFaces = true,
                BackFaces = true,
            });
        }

        /// <summary>
        /// CG_AddGrowGore — wachsende Gore-Markierung (z.B. Blutlache um Wunde).
        /// </summary>
        private static void AddGrowGore(
            List<PGoreData> entries,
            PGoreType goreType,
            float size,
            int growDuration,
            float startFraction,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            entries.Add(new PGoreData
            {
                GoreType = goreType,
                SSize = size * SOF2_UNIT_SCALE,
                TSize = size * SOF2_UNIT_SCALE,
                Theta = Random.Range(0f, Mathf.PI * 2f),
                HitLocation = hitLocation,
                RayDirection = hitDirection,
                GrowDuration = growDuration,
                GoreScaleStartFraction = startFraction,
                LifeTime = 0,
                FrontFaces = true,
                BackFaces = true,
            });
        }

        /// <summary>
        /// CG_AddSlashGore — laengliche Wunde (SSize != TSize) mit spezifischem Winkel.
        /// </summary>
        private static void AddSlashGore(
            List<PGoreData> entries,
            PGoreType goreType,
            float angle,
            float sSize,
            float tSize,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            entries.Add(new PGoreData
            {
                GoreType = goreType,
                SSize = sSize * SOF2_UNIT_SCALE,
                TSize = tSize * SOF2_UNIT_SCALE,
                Theta = angle,
                HitLocation = hitLocation,
                RayDirection = hitDirection,
                GrowDuration = -1,
                GoreScaleStartFraction = 1.0f,
                LifeTime = 0,
                FrontFaces = true,
                BackFaces = true,
            });
        }

        /// <summary>
        /// CG_AddSlashGrowGore — wachsende laengliche Wunde.
        /// </summary>
        private static void AddSlashGrowGore(
            List<PGoreData> entries,
            PGoreType goreType,
            float angle,
            float sSize,
            float tSize,
            int growDuration,
            float startFraction,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            entries.Add(new PGoreData
            {
                GoreType = goreType,
                SSize = sSize * SOF2_UNIT_SCALE,
                TSize = tSize * SOF2_UNIT_SCALE,
                Theta = angle,
                HitLocation = hitLocation,
                RayDirection = hitDirection,
                GrowDuration = growDuration,
                GoreScaleStartFraction = startFraction,
                LifeTime = 0,
                FrontFaces = true,
                BackFaces = true,
            });
        }

        /// <summary>
        /// CG_AddTimedGore — Gore-Markierung mit begrenzter Lebensdauer.
        /// </summary>
        private static void AddTimedGore(
            List<PGoreData> entries,
            PGoreType goreType,
            float size,
            int lifeTime,
            Vector3 hitLocation,
            Vector3 hitDirection)
        {
            entries.Add(new PGoreData
            {
                GoreType = goreType,
                SSize = size * SOF2_UNIT_SCALE,
                TSize = size * SOF2_UNIT_SCALE,
                Theta = Random.Range(0f, Mathf.PI * 2f),
                HitLocation = hitLocation,
                RayDirection = hitDirection,
                GrowDuration = -1,
                GoreScaleStartFraction = 1.0f,
                LifeTime = lifeTime,
                FrontFaces = true,
                BackFaces = true,
            });
        }
    }
}
