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
            { HitRegion.LeftArm, new GoreAreaMapping("arm_lower", false) },
            { HitRegion.LeftHand, new GoreAreaMapping("hand", false) },

            // --- Right Arm ---
            { HitRegion.RightShoulder, new GoreAreaMapping("arm_upper", true) },
            { HitRegion.RightArm, new GoreAreaMapping("arm_lower", true) },
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
    }
}
