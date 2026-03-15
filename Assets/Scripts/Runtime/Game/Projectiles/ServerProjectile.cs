using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Projectiles
{
    using Characters.Networked;
    using Characters.Shared;
    /// <summary>
    /// Server-seitiges Projektil (RPG, Granate, MM1, Knife-Throw).
    /// Simuliert Flugbahn mit Gravitation, Speed und optionalem Bounce.
    /// Detoniert bei Impact (Kollision), Timer (Countdown) oder Sticky (haftet an Oberflaeche).
    /// Impact/Timer: Explosion-Damage via OverlapSphere auf Hitbox-Layer.
    /// Sticky: Direkter Damage bei Hitbox-Treffer, haftet dann an Oberflaeche als Pickup.
    /// Kein NetworkObject — Server berechnet Damage, Clients bekommen Visual-RPC.
    /// </summary>
    public class ServerProjectile : MonoBehaviour
    {
        /// <summary>SoF2-Unit → Unity-Meter (1 QU = 0.0254m).</summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;

        /// <summary>SoF2 Gravitation in Unity-Meter/s² (800 QU/s² × 0.0254 = 20.32).</summary>
        private const float SOF2_GRAVITY = 20.32f;

        /// <summary>Physics Layer Name fuer Hitbox-Collider.</summary>
        private const string HITBOX_LAYER_NAME = "Hitbox";

        /// <summary>Max Lebensdauer in Sekunden (Safety-Cleanup fuer verlorene Projektile).</summary>
        private const float MAX_LIFETIME = 15f;

        /// <summary>Aktuelle Flugrichtung und -geschwindigkeit (Unity-Meter/Sek).</summary>
        private Vector3 m_Velocity;

        /// <summary>Gravitations-Skalierung (0 = keine, 0.5 = halbe, 1.0 = volle).</summary>
        private float m_GravityScale;

        /// <summary>Bounce-Faktor (0 = kein Bounce, 0.45 = F1 Grenade).</summary>
        private float m_Bounce;

        /// <summary>Detonationsart: "impact", "timer" oder "sticky".</summary>
        private string m_Detonation;

        /// <summary>Timer-Countdown fuer Timer-Detonation (Sekunden).</summary>
        private float m_Timer;

        /// <summary>Explosions-Damage.</summary>
        private int m_Damage;

        /// <summary>Explosions-Radius in SoF2-Units.</summary>
        private int m_Radius;

        /// <summary>Seit Spawn vergangene Zeit.</summary>
        private float m_Lifetime;

        /// <summary>Ob das Projektil bereits detoniert ist.</summary>
        private bool m_HasDetonated;

        /// <summary>Owner-ClientId (fuer Self-Damage-Prevention und Sticky-Pickup).</summary>
        private ulong m_OwnerClientId;

        /// <summary>Name der Waffe die dieses Projektil geworfen hat (fuer Sticky-Pickup).</summary>
        private string m_WeaponName;

        /// <summary>Ob das Projektil an einer Oberflaeche haftet (Sticky-Detonation).</summary>
        private bool m_IsStuck;

        /// <summary>Eindeutige ID fuer Visual-Cleanup bei Sticky-Pickup.</summary>
        private uint m_ProjectileId;

        /// <summary>Callback wenn Sticky-Projektil aufgehoben wird (ID fuer Visual-Cleanup).</summary>
        public event System.Action<uint> OnPickedUp;

        /// <summary>Pickup-Radius in Unity-Metern fuer Sticky-Projektile.</summary>
        private const float PICKUP_RADIUS = 0.75f;

        /// <summary>LayerMask fuer Hitbox-Raycasts.</summary>
        private int m_HitboxLayerMask;

        /// <summary>LayerMask fuer Welt-Kollision (alles ausser Hitbox).</summary>
        private int m_WorldLayerMask;

        /// <summary>Callback wenn Projektil detoniert (fuer Visual-RPC vom Spawner).</summary>
        public event System.Action<Vector3> OnDetonated;

        /// <summary>
        /// Initialisiert das Projektil mit Waffen-Daten.
        /// Wird vom Server nach Instantiate aufgerufen.
        /// </summary>
        public void Initialize(
            Vector3 spawnPosition,
            Vector3 direction,
            float speedQU,
            float gravityScale,
            float bounce,
            string detonation,
            float timer,
            int damage,
            int radiusQU,
            ulong ownerClientId,
            string weaponName = "",
            uint projectileId = 0)
        {
            m_ProjectileId = projectileId;
            transform.position = spawnPosition;
            m_Velocity = direction.normalized * (speedQU * SOF2_UNIT_SCALE);
            m_GravityScale = gravityScale;
            m_Bounce = bounce;
            m_Detonation = detonation;
            m_Timer = timer;
            m_Damage = damage;
            m_Radius = radiusQU;
            m_OwnerClientId = ownerClientId;
            m_WeaponName = weaponName;
            m_Lifetime = 0f;
            m_HasDetonated = false;
            m_IsStuck = false;

            m_HitboxLayerMask = LayerMask.GetMask(HITBOX_LAYER_NAME);
            // Welt-Kollision: Default Layer (alles was nicht Hitbox ist)
            m_WorldLayerMask = ~m_HitboxLayerMask;

            // Rotation in Flugrichtung
            if (m_Velocity.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(m_Velocity);
            }
        }

        /// <summary>
        /// Server-seitige Physik-Simulation pro Frame.
        /// </summary>
        private void Update()
        {
            if (m_HasDetonated)
            {
                // Sticky-Projektile ticken weiter fuer Lifetime-Cleanup
                if (m_IsStuck)
                {
                    m_Lifetime += Time.deltaTime;
                    if (m_Lifetime > MAX_LIFETIME)
                    {
                        Destroy(gameObject);
                    }
                }

                return;
            }

            float dt = Time.deltaTime;
            m_Lifetime += dt;

            // Safety-Cleanup
            if (m_Lifetime > MAX_LIFETIME)
            {
                Detonate(transform.position);
                return;
            }

            // Timer-Detonation
            if (m_Detonation == "timer")
            {
                m_Timer -= dt;
                if (m_Timer <= 0f)
                {
                    Detonate(transform.position);
                    return;
                }
            }

            // Gravitation anwenden (SoF2: 800 QU/s² = 20.32 m/s², nicht Unity 9.81)
            if (m_GravityScale > 0f)
            {
                m_Velocity.y -= SOF2_GRAVITY * m_GravityScale * dt;
            }

            // Rotation in Flugrichtung aktualisieren
            if (m_Velocity.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(m_Velocity);
            }

            // Bewegung mit Kollisionserkennung
            Vector3 movement = m_Velocity * dt;
            float distance = movement.magnitude;

            if (distance < 0.001f)
            {
                return;
            }

            Vector3 direction = movement / distance;

            // Raycast fuer Welt-Kollision (Waende, Boden)
            if (Physics.Raycast(transform.position, direction, out RaycastHit worldHit, distance, m_WorldLayerMask))
            {
                transform.position = worldHit.point + worldHit.normal * 0.01f;

                if (m_Detonation == "impact")
                {
                    Detonate(worldHit.point);
                    return;
                }

                if (m_Detonation == "sticky")
                {
                    StickToSurface(worldHit.point, worldHit.normal);
                    return;
                }

                // Bounce (Timer-Granaten)
                if (m_Bounce > 0f)
                {
                    m_Velocity = Vector3.Reflect(m_Velocity, worldHit.normal) * m_Bounce;
                }
                else
                {
                    m_Velocity = Vector3.zero;
                }

                return;
            }

            // Raycast fuer Hitbox-Kollision (Spieler direkt treffen)
            if (Physics.Raycast(transform.position, direction, out RaycastHit hitboxHit, distance, m_HitboxLayerMask))
            {
                if (m_Detonation == "impact")
                {
                    Detonate(hitboxHit.point);
                    return;
                }

                if (m_Detonation == "sticky")
                {
                    ApplyDirectDamage(hitboxHit);
                    StickToSurface(hitboxHit.point, hitboxHit.normal);
                    return;
                }
            }

            // Keine Kollision — normal bewegen
            transform.position += movement;
        }

        /// <summary>
        /// Detoniert das Projektil: Explosions-Damage im Radius anwenden.
        /// </summary>
        private void Detonate(Vector3 explosionPoint)
        {
            if (m_HasDetonated)
            {
                return;
            }

            m_HasDetonated = true;

            float radiusMeters = m_Radius * SOF2_UNIT_SCALE;

            // Alle Hitboxen im Explosionsradius finden
            Collider[] hits = Physics.OverlapSphere(explosionPoint, radiusMeters, m_HitboxLayerMask);

            // Pro Spieler nur einmal Schaden anwenden (hoechster Treffer zaehlt)
            System.Collections.Generic.Dictionary<NetworkedCharacterState, float> damagePerTarget =
                new();

            foreach (Collider col in hits)
            {
                HitboxCollider hitbox = col.GetComponent<HitboxCollider>();
                if (hitbox == null)
                {
                    continue;
                }

                NetworkedCharacterState targetState =
                    col.GetComponentInParent<NetworkedCharacterState>();
                if (targetState == null)
                {
                    continue;
                }

                // Distanz-basierter Damage-Falloff (linear: voller Damage im Zentrum, 0 am Rand)
                float distance = Vector3.Distance(explosionPoint, col.transform.position);
                float falloff = 1f - Mathf.Clamp01(distance / radiusMeters);
                float effectiveDamage = m_Damage * falloff;

                // Hoechsten Damage pro Spieler merken
                if (!damagePerTarget.ContainsKey(targetState) || damagePerTarget[targetState] < effectiveDamage)
                {
                    damagePerTarget[targetState] = effectiveDamage;
                }
            }

            // Damage anwenden
            foreach (System.Collections.Generic.KeyValuePair<NetworkedCharacterState, float> kvp in damagePerTarget)
            {
                int finalDamage = Mathf.RoundToInt(kvp.Value);
                if (finalDamage <= 0)
                {
                    continue;
                }

                int newHealth = Mathf.Max(0, kvp.Key.Health - finalDamage);
                kvp.Key.SetHealth(newHealth);

                Debug.Log($"[ServerProjectile] Explosion hit {kvp.Key.CharacterName} | Damage={finalDamage} | Health={newHealth}");
            }

            // Event fuer Visual-RPC
            OnDetonated?.Invoke(explosionPoint);

            Debug.Log($"[ServerProjectile] Detonated at {explosionPoint} | Radius={radiusMeters:F1}m | Targets={damagePerTarget.Count}");

            // Zerstoeren
            Destroy(gameObject);
        }

        /// <summary>
        /// Sticky-Projektil an Oberflaeche fixieren: Stoppt Bewegung, wird zum Pickup.
        /// Fuegt SphereCollider-Trigger + kinematischen Rigidbody hinzu fuer OnTriggerEnter.
        /// </summary>
        private void StickToSurface(Vector3 point, Vector3 normal)
        {
            m_HasDetonated = true;
            m_IsStuck = true;
            transform.position = point + normal * 0.02f;

            if (normal.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(normal);
            }

            // Trigger-Collider fuer Pickup-Detection
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = PICKUP_RADIUS;

            // Kinematischer Rigidbody fuer Trigger-Events
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            Debug.Log($"[ServerProjectile] Sticky stuck at {point} — awaiting pickup by client {m_OwnerClientId}");
        }

        /// <summary>
        /// Direkter Schaden bei Sticky-Treffer (kein Explosionsradius, kein Falloff).
        /// </summary>
        private void ApplyDirectDamage(RaycastHit hit)
        {
            HitboxCollider hitbox = hit.collider.GetComponent<HitboxCollider>();
            if (hitbox == null)
            {
                return;
            }

            NetworkedCharacterState targetState =
                hit.collider.GetComponentInParent<NetworkedCharacterState>();
            if (targetState == null)
            {
                return;
            }

            int newHealth = Mathf.Max(0, targetState.Health - m_Damage);
            targetState.SetHealth(newHealth);

            Debug.Log($"[ServerProjectile] Sticky direct hit {targetState.CharacterName} | Damage={m_Damage} | Health={newHealth}");
        }

        /// <summary>
        /// Trigger-Event: Spieler laeuft ueber das Sticky-Projektil → Ammo-Pickup.
        /// Nur der Owner (Werfer) kann sein Projektil wieder aufheben.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!m_IsStuck)
            {
                return;
            }

            NetworkedCharacterState targetState =
                other.GetComponentInParent<NetworkedCharacterState>();
            if (targetState == null)
            {
                return;
            }

            // Nur der Owner kann sein geworfenes Projektil aufheben
            if (targetState.OwnerClientId != m_OwnerClientId)
            {
                return;
            }

            targetState.AddReserveAmmoForWeapon(m_WeaponName, 1);

            Debug.Log($"[ServerProjectile] Sticky picked up by client {m_OwnerClientId} — weapon={m_WeaponName}, id={m_ProjectileId}");

            OnPickedUp?.Invoke(m_ProjectileId);
            Destroy(gameObject);
        }
    }
}
