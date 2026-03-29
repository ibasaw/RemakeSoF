using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.Game.Characters.Shared;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Maps HitRegion enum values to SoF2 gore area location strings and side information.
    /// This is the bridge between the per-bone BoxCollider hit detection and the gore data system.
    /// </summary>
    public static class GoreHitRegionMapping
    {
        /// <summary>
        /// Result of a HitRegion-to-GoreArea lookup.
        /// </summary>
        public readonly struct GoreAreaMapping
        {
            /// <summary>Gore area location key as defined in SoF2_DATA.json (e.g. "hand", "arm_lower", "head").</summary>
            public readonly string GoreAreaLocation;

            /// <summary>True if the hit is on the right side (primary side in SoF2 template system).</summary>
            public readonly bool IsRightSide;

            /// <summary>
            /// Initializes a new GoreAreaMapping.
            /// </summary>
            public GoreAreaMapping(string goreAreaLocation, bool isRightSide)
            {
                GoreAreaLocation = goreAreaLocation;
                IsRightSide = isRightSide;
            }
        }

        private static readonly Dictionary<HitRegion, GoreAreaMapping> s_Mapping = new()
        {
            // --- Head / Neck ---
            { HitRegion.Head, new GoreAreaMapping("head", true) },
            { HitRegion.Neck, new GoreAreaMapping("head", true) },

            // --- Torso ---
            { HitRegion.Chest, new GoreAreaMapping("torso", true) },
            { HitRegion.Gut, new GoreAreaMapping("torso", true) },

            // --- Groin / Hip ---
            { HitRegion.Groin, new GoreAreaMapping("hip", true) },

            // --- Left Arm ---
            { HitRegion.LeftShoulder, new GoreAreaMapping("arm_upper", false) },
            { HitRegion.LeftArm, new GoreAreaMapping("arm_upper", false) },
            { HitRegion.LeftForearm, new GoreAreaMapping("arm_lower", false) },
            { HitRegion.LeftHand, new GoreAreaMapping("hand", false) },

            // --- Right Arm ---
            { HitRegion.RightShoulder, new GoreAreaMapping("arm_upper", true) },
            { HitRegion.RightArm, new GoreAreaMapping("arm_upper", true) },
            { HitRegion.RightForearm, new GoreAreaMapping("arm_lower", true) },
            { HitRegion.RightHand, new GoreAreaMapping("hand", true) },

            // --- Left Leg ---
            { HitRegion.LeftThigh, new GoreAreaMapping("leg_upper", false) },
            { HitRegion.LeftLeg, new GoreAreaMapping("leg_lower", false) },
            { HitRegion.LeftFoot, new GoreAreaMapping("foot", false) },

            // --- Right Leg ---
            { HitRegion.RightThigh, new GoreAreaMapping("leg_upper", true) },
            { HitRegion.RightLeg, new GoreAreaMapping("leg_lower", true) },
            { HitRegion.RightFoot, new GoreAreaMapping("foot", true) },
        };

        /// <summary>
        /// Returns the gore area mapping for a given HitRegion.
        /// </summary>
        /// <param name="hitRegion">The hit region from the BoxCollider hit.</param>
        /// <param name="mapping">The resolved GoreAreaMapping with location and side.</param>
        /// <returns>True if a mapping was found, false otherwise.</returns>
        public static bool TryGetMapping(HitRegion hitRegion, out GoreAreaMapping mapping)
        {
            return s_Mapping.TryGetValue(hitRegion, out mapping);
        }

        /// <summary>
        /// Zentrale Gore-Area-Locations die nur einen Hitbox-Collider haben (kein Links/Rechts).
        /// Fuer diese Areas wird isRightSide beim Hitbox-Lookup ignoriert, da Head, Neck, Chest, Gut
        /// und Groin nur jeweils EINE HitRegion im Mapping haben (nicht Left/Right varianten).
        /// </summary>
        private static readonly HashSet<string> s_CenterAreas = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "head", "torso", "hip"
        };

        /// <summary>
        /// Gibt alle HitRegions zurueck die zu einer bestimmten GoreArea-Location und Seite gehoeren.
        /// Wird nach Dismemberment verwendet um alle betroffenen Hitboxen zu deaktivieren.
        /// Fuer zentrale Areas (head, torso, hip) wird isRightSide ignoriert, da diese nur
        /// eine HitRegion besitzen (z.B. Head, Neck, Chest, Gut, Groin).
        /// </summary>
        /// <param name="goreAreaLocation">Gore-Area-Location (z.B. "head", "arm_upper").</param>
        /// <param name="isRightSide">True fuer rechte Seite (wird fuer zentrale Areas ignoriert).</param>
        /// <returns>Liste der betroffenen HitRegions.</returns>
        public static List<HitRegion> GetHitRegionsForArea(string goreAreaLocation, bool isRightSide)
        {
            bool isCenterArea = s_CenterAreas.Contains(goreAreaLocation);
            List<HitRegion> result = new();

            foreach (KeyValuePair<HitRegion, GoreAreaMapping> entry in s_Mapping)
            {
                if (string.Equals(entry.Value.GoreAreaLocation, goreAreaLocation, System.StringComparison.OrdinalIgnoreCase)
                    && (isCenterArea || entry.Value.IsRightSide == isRightSide))
                {
                    result.Add(entry.Key);
                }
            }

            return result;
        }
    }
}
