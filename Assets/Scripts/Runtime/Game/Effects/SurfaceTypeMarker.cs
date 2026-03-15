using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// MonoBehaviour-Marker fuer den Surface-Typ eines Colliders.
    /// Wird auf Welt-Geometrie platziert (manuell oder automatisch vom MapLoader).
    /// EffectFactory liest den Typ per GetComponentInParent aus, um den korrekten Impact-Effekt
    /// aus SoF2_data_per_surface.json zu ermitteln.
    /// </summary>
    public class SurfaceTypeMarker : MonoBehaviour
    {
        /// <summary>
        /// Surface-Typ-Name (z.B. "concrete", "metal", "wood", "dirt").
        /// Muss exakt mit den Keys in SoF2_data_per_surface.json uebereinstimmen.
        /// </summary>
        [SerializeField]
        [Tooltip("Surface type name matching SoF2_data_per_surface.json keys (e.g. concrete, metal, wood, dirt)")]
        private string m_SurfaceType = "default";

        /// <summary>
        /// Gibt den Surface-Typ-Namen zurueck.
        /// </summary>
        public string SurfaceType => m_SurfaceType;

        /// <summary>
        /// Setzt den Surface-Typ programmatisch (z.B. vom MapLoader).
        /// </summary>
        public void SetSurfaceType(string surfaceType)
        {
            m_SurfaceType = surfaceType;
        }
    }
}
