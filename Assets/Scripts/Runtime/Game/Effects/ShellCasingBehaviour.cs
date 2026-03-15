using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// MonoBehaviour fuer SoF2-Patronenhuelsen mit Impact-Effekt.
    /// Spawnt beim ersten Aufprall den referenzierten impactFx-Effekt
    /// (z.B. shells/shell_bouce_brass → niedrigere LOD-Huelse die weiterhuepft).
    /// SoF2 impactKills-Flag: Originalhuelse wird beim Impact zerstoert.
    /// </summary>
    public class ShellCasingBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Effect-ID des Impact-Effekts (aus EffectEmitterDefinition.ImpactFx).
        /// </summary>
        private string m_ImpactFxId;

        /// <summary>
        /// Ob die Huelse beim Impact zerstoert wird (SoF2 impactKills-Flag).
        /// </summary>
        private bool m_ImpactKills;

        /// <summary>
        /// Ob bereits ein Impact verarbeitet wurde (verhindert Mehrfach-Spawns).
        /// </summary>
        private bool m_HasImpacted;

        /// <summary>
        /// Initialisiert die Impact-Parameter.
        /// </summary>
        public void Initialize(string impactFxId, bool impactKills)
        {
            m_ImpactFxId = impactFxId;
            m_ImpactKills = impactKills;
        }

        /// <summary>
        /// Bei Kollision wird der Impact-Effekt gespawnt.
        /// SoF2 shell_brass.efx: impactKills zerstoert hires-Modell,
        /// spawnt shell_bouce_brass (lowres-Modell das weiterhuepft).
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (m_HasImpacted || string.IsNullOrEmpty(m_ImpactFxId))
            {
                return;
            }

            m_HasImpacted = true;

            EffectFactory effectFactory = ServiceLocator.Get<EffectFactory>();
            if (effectFactory != null)
            {
                ContactPoint contact = collision.GetContact(0);
                effectFactory.SpawnShellCasing(contact.point, transform.rotation, m_ImpactFxId);
            }

            if (m_ImpactKills)
            {
                Destroy(gameObject);
            }
        }
    }
}
