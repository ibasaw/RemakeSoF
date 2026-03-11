namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// SoF2 Hit Regions mit Original-Index-Keys aus SoF2_DATA.json.
    /// Jeder Wert entspricht dem originalen HitRegion-Index fuer Damage/Animation Lookup.
    /// </summary>
    public enum HitRegion
    {
        /// <summary>Kopf — cranium → cervical.</summary>
        Head = 0,

        /// <summary>Rechtes Schienbein — rtibia → rtarsal.</summary>
        RightLeg = 1,

        /// <summary>Hals — cervical → thoracic.</summary>
        Neck = 4,

        /// <summary>Brust — thoracic → upper_lumbar.</summary>
        Chest = 8,

        /// <summary>Rechter Fuss — rtarsal.</summary>
        RightFoot = 12,

        /// <summary>Linke Schulter — lclavical → lhumerus.</summary>
        LeftShoulder = 16,

        /// <summary>Linker Oberarm — lhumerus → lradius.</summary>
        LeftArm = 20,

        /// <summary>Linke Hand — lradius → lhand.</summary>
        LeftHand = 24,

        /// <summary>Rechte Schulter — rclavical → rhumerus.</summary>
        RightShoulder = 28,

        /// <summary>Rechter Oberarm — rhumerus → rradius.</summary>
        RightArm = 32,

        /// <summary>Rechte Hand — rradius → rhand.</summary>
        RightHand = 36,

        /// <summary>Bauch — upper_lumbar → lower_lumbar.</summary>
        Gut = 40,

        /// <summary>Leiste — lower_lumbar → pelvis.</summary>
        Groin = 44,

        /// <summary>Linker Oberschenkel — lfemurYZ → ltibia.</summary>
        LeftThigh = 48,

        /// <summary>Linkes Schienbein — ltibia → ltarsal.</summary>
        LeftLeg = 52,

        /// <summary>Linker Fuss — ltarsal.</summary>
        LeftFoot = 56,

        /// <summary>Rechter Oberschenkel — rfemurYZ → rtibia.</summary>
        RightThigh = 60
    }
}
