using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GoreManagement
{
    /// <summary>
    /// Animiert das Wachstum eines PGORE-Decals von der Startgroesse zur Zielgroesse.
    /// SoF2-Aequivalent: SSkinGoreData.growDuration + goreScaleStartFraction.
    /// Wird von PGoreDecalApplier an wachsende Decals (z.B. PGORE_KNIFE_SOAK) angeheftet.
    /// </summary>
    public class PGoreGrowBehaviour : MonoBehaviour
    {
        private Vector3 m_TargetScale;
        private float m_Duration;
        private float m_Elapsed;
        private Vector3 m_StartScale;
        private bool m_Initialized;

        /// <summary>
        /// Initialisiert die Wachstums-Animation.
        /// </summary>
        /// <param name="targetScale">Endgroesse des Decals.</param>
        /// <param name="durationSeconds">Dauer des Wachstums in Sekunden.</param>
        public void Initialize(Vector3 targetScale, float durationSeconds)
        {
            m_TargetScale = targetScale;
            m_Duration = durationSeconds;
            m_StartScale = transform.localScale;
            m_Elapsed = 0f;
            m_Initialized = true;
        }

        private void Update()
        {
            if (!m_Initialized)
            {
                return;
            }

            m_Elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(m_Elapsed / m_Duration);

            transform.localScale = Vector3.Lerp(m_StartScale, m_TargetScale, t);

            if (t >= 1f)
            {
                m_Initialized = false;
                Destroy(this);
            }
        }
    }
}
