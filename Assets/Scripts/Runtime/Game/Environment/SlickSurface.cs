using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Environment
{
    /// <summary>
    /// SoF2 SURF_SLICK Marker — Oberflächen mit diesem Component haben keine Friction.
    /// Spieler gleiten darüber wie auf Eis (PM_Friction wird uebersprungen).
    /// Wird z.B. auf FenceBarrier-Collider gesetzt.
    /// </summary>
    public class SlickSurface : MonoBehaviour
    {
    }
}
