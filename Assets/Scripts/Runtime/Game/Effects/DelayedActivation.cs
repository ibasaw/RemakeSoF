using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// Aktiviert den Renderer des eigenen GameObjects nach einer konfigurierbaren Verzoegerung.
    /// Wird fuer verzoegte Decal-Einblendung verwendet (SoF2 Decal delay).
    /// Der Renderer wird initial deaktiviert und nach Ablauf der Wartezeit wieder aktiviert.
    /// </summary>
    public class DelayedActivation : MonoBehaviour
    {
        private float m_Delay;
        private Renderer m_Renderer;

        /// <summary>
        /// Setzt die Verzoegerung in Sekunden.
        /// Der Renderer muss vorher bereits deaktiviert worden sein.
        /// </summary>
        public void Initialize(float delay)
        {
            m_Delay = delay;
            m_Renderer = GetComponent<Renderer>();
        }

        private void Update()
        {
            m_Delay -= Time.deltaTime;
            if (m_Delay <= 0f)
            {
                if (m_Renderer != null)
                {
                    m_Renderer.enabled = true;
                }

                Destroy(this);
            }
        }
    }
}
