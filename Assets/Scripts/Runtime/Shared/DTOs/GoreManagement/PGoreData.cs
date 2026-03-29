using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// SoF2 SSkinGoreData-Aequivalent — beschreibt eine einzelne projizierte Gore-Markierung.
    /// Wird von PGoreWeaponDispatch erzeugt und von PGoreDecalApplier auf den Charakter projiziert.
    /// </summary>
    public struct PGoreData
    {
        /// <summary>Gore-Typ (Textur/Shader-Auswahl).</summary>
        public PGoreType GoreType;

        /// <summary>Breite der Wunde in SoF2-Units (S-Texturrichtung).</summary>
        public float SSize;

        /// <summary>Hoehe der Wunde in SoF2-Units (T-Texturrichtung).</summary>
        public float TSize;

        /// <summary>Rotation der Wunde in Radians (0..2*PI).</summary>
        public float Theta;

        /// <summary>Einschlagpunkt in Weltkoordinaten.</summary>
        public Vector3 HitLocation;

        /// <summary>Einschlagrichtung (normalisiert).</summary>
        public Vector3 RayDirection;

        /// <summary>Wachstumsdauer in Millisekunden. -1 = kein Wachstum (sofort volle Groesse).</summary>
        public int GrowDuration;

        /// <summary>Startgroesse als Bruchteil (0.0..1.0). Nur relevant wenn GrowDuration > 0.</summary>
        public float GoreScaleStartFraction;

        /// <summary>Lebensdauer in Millisekunden. 0 = unbegrenzt.</summary>
        public int LifeTime;

        /// <summary>Auf Vorderseiten projizieren.</summary>
        public bool FrontFaces;

        /// <summary>Auf Rueckseiten projizieren.</summary>
        public bool BackFaces;
    }
}
