using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Characters.Server;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Projectiles
{
    /// <summary>
    /// Server-seitige Feuer-Schadenszone fuer Incendiary (ANM14) und Phosphorus (M15) Granaten.
    /// Erzeugt eine Trigger-Sphere die periodisch Schaden an Spielern im Bereich verursacht.
    /// SoF2-authentisch: Feuer am Boden verursacht Burn-Damage-over-Time.
    /// </summary>
    public class FireDamageZone : MonoBehaviour
    {
        /// <summary>SoF2-Unit → Unity-Meter.</summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;

        /// <summary>Schaden pro Tick (SoF2: ~5 Damage pro Halbe-Sekunde fuer Feuer).</summary>
        private int m_DamagePerTick = 5;

        /// <summary>Interval zwischen Damage-Ticks in Sekunden.</summary>
        private float m_TickInterval = 0.5f;

        /// <summary>Verbleibende Lebenszeit der Zone in Sekunden.</summary>
        private float m_RemainingLifetime;

        /// <summary>Timer seit letztem Damage-Tick.</summary>
        private float m_TickTimer;

        /// <summary>Radius der Schadenszone in Unity-Metern.</summary>
        private float m_Radius;

        /// <summary>LayerMask fuer Spieler-Erkennung.</summary>
        private int m_PlayerLayerMask;

        /// <summary>Client-ID des Werfers (fuer Kill-Attribution).</summary>
        private ulong m_OwnerClientId;

        /// <summary>
        /// Initialisiert die Feuer-Schadenszone.
        /// Wird vom Server nach Instantiate aufgerufen (z.B. aus ServerProjectile.Detonate).
        /// </summary>
        /// <param name="position">Weltposition der Zone (sollte am Boden sein).</param>
        /// <param name="radiusQU">Schadensradius in SoF2-Units.</param>
        /// <param name="damagePerTick">Schaden pro Tick.</param>
        /// <param name="tickInterval">Sekunden zwischen Ticks.</param>
        /// <param name="duration">Gesamtdauer in Sekunden.</param>
        /// <param name="ownerClientId">Client-ID des Werfers.</param>
        public void Initialize(Vector3 position, int radiusQU, int damagePerTick, float tickInterval, float duration, ulong ownerClientId)
        {
            m_Radius = radiusQU * SOF2_UNIT_SCALE;
            m_DamagePerTick = damagePerTick;
            m_TickInterval = tickInterval;
            m_RemainingLifetime = duration;
            m_OwnerClientId = ownerClientId;
            m_TickTimer = 0f;
            m_PlayerLayerMask = LayerMask.GetMask("Player");

            // Position auf Boden snappen via Raycast
            Vector3 groundPos = SnapToGround(position);
            transform.position = groundPos;
        }

        /// <summary>
        /// Raycast nach unten um die Bodenposition zu finden.
        /// Fallback: Originalposition wenn kein Boden gefunden wird.
        /// </summary>
        private Vector3 SnapToGround(Vector3 origin)
        {
            int worldMask = ~(LayerMask.GetMask("Player") | LayerMask.GetMask("Hitbox"));
            if (Physics.Raycast(origin + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 5f, worldMask))
            {
                return hit.point + Vector3.up * 0.05f;
            }

            return origin;
        }

        /// <summary>
        /// Server-seitiger Update: Damage-Ticks und Lifetime-Management.
        /// </summary>
        private void Update()
        {
            m_RemainingLifetime -= Time.deltaTime;
            if (m_RemainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            m_TickTimer += Time.deltaTime;
            if (m_TickTimer < m_TickInterval)
            {
                return;
            }

            m_TickTimer -= m_TickInterval;

            // OverlapSphere auf Player-Layer (wie ServerProjectile.Detonate)
            Collider[] hits = Physics.OverlapSphere(transform.position, m_Radius, m_PlayerLayerMask);

            foreach (Collider col in hits)
            {
                ServerCharacterController controller = col.GetComponentInParent<ServerCharacterController>();
                if (controller == null || !controller.IsAlive)
                {
                    continue;
                }

                controller.ApplyDamage(m_DamagePerTick, m_OwnerClientId);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                NetworkedCharacterState state = col.GetComponentInParent<NetworkedCharacterState>();
                string name = state != null ? state.CharacterName : "?";
                Debug.Log($"[FireDamageZone] Burn tick: {m_DamagePerTick} damage to {name} | Remaining={m_RemainingLifetime:F1}s");
#endif
            }
        }
    }
}
