using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// MonoBehaviour fuer SoF2-emitFx: Spawnt periodisch einen Sub-Effekt an der Position
    /// des fliegenden Emitter-Objekts (z.B. Rauchschweif hinter Truemmer, Glutfunken bei Brandstuecken).
    /// Wird von EffectFactory.SpawnEmitterChunks angehaengt wenn das Emitter-Segment ein emitFx definiert.
    /// </summary>
    public class EmitFxBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Effect-ID des Sub-Effekts (aus EffectEmitterDefinition.EmitFx).
        /// </summary>
        private string m_EmitFxId;

        /// <summary>
        /// Intervall zwischen Sub-Effekt-Spawns in Sekunden.
        /// </summary>
        private float m_Interval;

        /// <summary>
        /// Verbleibende Zeit bis Emission stoppt.
        /// </summary>
        private float m_RemainingLifetime;

        /// <summary>
        /// Timer fuer naechsten Spawn.
        /// </summary>
        private float m_Timer;

        /// <summary>
        /// Initialisiert die EmitFx-Parameter.
        /// Spawn-Intervall wird aus Lebensdauer abgeleitet (ca. 5-8 Spawns pro Lebensdauer).
        /// </summary>
        public void Initialize(string emitFxId, float lifetime)
        {
            m_EmitFxId = emitFxId;
            m_RemainingLifetime = lifetime;
            m_Interval = Mathf.Max(lifetime / 6f, 0.05f);
            m_Timer = 0f;
        }

        /// <summary>
        /// Spawnt Sub-Effekte in regelmaessigen Abstaenden via EffectFactory.
        /// </summary>
        private void Update()
        {
            if (string.IsNullOrEmpty(m_EmitFxId))
            {
                return;
            }

            m_RemainingLifetime -= Time.deltaTime;
            if (m_RemainingLifetime <= 0f)
            {
                enabled = false;
                return;
            }

            m_Timer += Time.deltaTime;
            if (m_Timer >= m_Interval)
            {
                m_Timer -= m_Interval;

                EffectFactory effectFactory = ServiceLocator.Get<EffectFactory>();
                if (effectFactory != null)
                {
                    effectFactory.SpawnImpactEffect(transform.position, Vector3.up, m_EmitFxId);
                }
            }
        }
    }
}
