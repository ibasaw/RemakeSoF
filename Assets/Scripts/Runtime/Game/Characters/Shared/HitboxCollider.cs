using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// Identifiziert einen Hitbox-Collider mit seiner SoF2 Hit Region und Damage-Multiplikator.
    /// Wird von Waffen-Raycasts ausgelesen um Region-spezifischen Schaden zu berechnen.
    /// </summary>
    public class HitboxCollider : MonoBehaviour
    {
        [SerializeField]
        private HitRegion m_HitRegion;

        [SerializeField]
        private float m_DamageMultiplier = 1.0f;

        /// <summary>SoF2 Hit Region die dieser Collider abdeckt.</summary>
        public HitRegion HitRegion => m_HitRegion;

        /// <summary>Damage-Multiplikator fuer diese Region (Head=1.75, Arms=0.7, etc.).</summary>
        public float DamageMultiplier => m_DamageMultiplier;

        /// <summary>
        /// Initialisiert den HitboxCollider mit Region und Multiplikator.
        /// </summary>
        public void Initialize(HitRegion hitRegion, float damageMultiplier)
        {
            m_HitRegion = hitRegion;
            m_DamageMultiplier = damageMultiplier;
        }
    }
}
