using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// SoF2 impactFx-Emulation: Spawnt blood_splat_mp Decals an Kollisionspunkten
    /// wenn Blutpartikel auf Boden oder Waende treffen.
    /// Wird von EffectFactory an ParticleSystems mit impactFx-Flag angehaengt.
    /// In SoF2 erzeugen fallende Bluttropfen beim Aufprall Blutfleck-Decals auf dem Boden.
    /// </summary>
    public class BloodDropCollisionBehaviour : MonoBehaviour
    {
        /// <summary>Max Anzahl Splats pro ParticleSystem (Performance-Limit).</summary>
        private const int MAX_SPLATS = 4;

        /// <summary>Effect-ID fuer den Aufprall-Decal (Standard: blood_splat_mp).</summary>
        private string m_ImpactEffectId = "effects/blood_splat_mp";

        /// <summary>Referenz auf das ParticleSystem zu dem dieses Behaviour gehoert.</summary>
        private ParticleSystem m_ParticleSystem;

        /// <summary>Wiederverwendbare Liste fuer Kollisions-Events (vermeidet Allokation pro Frame).</summary>
        private List<ParticleCollisionEvent> m_CollisionEvents;

        /// <summary>Zaehler fuer bereits erzeugte Splats.</summary>
        private int m_SplatCount;

        /// <summary>
        /// Initialisiert die Impact-Effect-ID. Falls leer, wird der Standard (blood_splat_mp) verwendet.
        /// </summary>
        /// <param name="impactEffectId">Effect-ID fuer den Aufprall-Sub-Effekt.</param>
        public void Initialize(string impactEffectId)
        {
            if (!string.IsNullOrEmpty(impactEffectId))
            {
                m_ImpactEffectId = impactEffectId;
            }
        }

        /// <summary>
        /// Cached ParticleSystem-Referenz und initialisiert die Kollisions-Event-Liste.
        /// </summary>
        private void Awake()
        {
            m_ParticleSystem = GetComponent<ParticleSystem>();
            m_CollisionEvents = new List<ParticleCollisionEvent>(16);
        }

        /// <summary>
        /// Unity-Callback: Wird aufgerufen wenn Partikel mit einem Collider kollidieren.
        /// Spawnt blood_splat_mp Decals an den Kollisionspunkten via EffectFactory.
        /// Limitiert auf MAX_SPLATS pro ParticleSystem fuer Performance.
        /// </summary>
        private void OnParticleCollision(GameObject other)
        {
            if (m_ParticleSystem == null || m_SplatCount >= MAX_SPLATS)
            {
                return;
            }

            int eventCount = m_ParticleSystem.GetCollisionEvents(other, m_CollisionEvents);

            EffectFactory effectFactory = ServiceLocator.Get<EffectFactory>();
            if (effectFactory == null)
            {
                return;
            }

            for (int i = 0; i < eventCount && m_SplatCount < MAX_SPLATS; i++)
            {
                Vector3 hitPos = m_CollisionEvents[i].intersection;
                Vector3 hitNormal = m_CollisionEvents[i].normal;
                effectFactory.SpawnImpactEffect(hitPos, hitNormal, m_ImpactEffectId);
                m_SplatCount++;
            }
        }
    }
}
