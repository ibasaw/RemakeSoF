using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Projectiles
{
    using Characters.Client;
    using Characters.Networked;
    using Characters.Server;
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

        /// <summary>Physics Layer Name fuer Spieler-Collider (Direkt-Treffer + Explosions-Erkennung).</summary>
        private const string PLAYER_LAYER_NAME = "Player";

        /// <summary>Max Lebensdauer in Sekunden (Safety-Cleanup fuer verlorene Projektile).</summary>
        private const float MAX_LIFETIME = 15f;

        /// <summary>
        /// SoF2 Bounce-Stop-Threshold: Granate stoppt wenn auf horizontaler Flaeche (normal.y > 0.2)
        /// und Geschwindigkeit unter 40 QU/s (g_missile.c:37/59). 40 QU/s * 0.0254 = 1.016 m/s.
        /// </summary>
        private const float BOUNCE_STOP_SPEED = 1.016f;

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

        /// <summary>Knockback-Staerke (SoF2 g_knockback, default 700). Konfigurierbar pro Waffe.</summary>
        private int m_Knockback;

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

        /// <summary>Guard-Flag: Verhindert mehrfaches Pickup durch mehrere Collider im selben Frame.</summary>
        private bool m_IsPickedUp;

        /// <summary>Explosion-Effect-ID fuer Feuer/Phosphorus-Erkennung (z.B. "effects/explosions/incendiary_explosion_mp").</summary>
        private string m_ExplosionEffectId;

        /// <summary>Eindeutige ID fuer Visual-Cleanup bei Sticky-Pickup.</summary>
        private uint m_ProjectileId;

        /// <summary>Callback wenn Sticky-Projektil aufgehoben wird (ID fuer Visual-Cleanup).</summary>
        public event System.Action<uint> OnPickedUp;

        /// <summary>Pickup-Radius in Unity-Metern fuer Sticky-Projektile.</summary>
        private const float PICKUP_RADIUS = 0.75f;

        /// <summary>LayerMask fuer Direkt-Treffer Raycasts (Player-Movement-BoxCollider).</summary>
        private int m_PlayerLayerMask;

        /// <summary>LayerMask fuer Bone-Trigger-BoxCollider auf Hitbox-Layer.</summary>
        private int m_HitboxLayerMask;

        /// <summary>LayerMask fuer Explosions-Erkennung (Player-Physics-Collider).</summary>
        private int m_ExplosionLayerMask;

        /// <summary>LayerMask fuer Welt-Kollision (alles ausser Player und BrushCollision).</summary>
        private int m_WorldLayerMask;

        /// <summary>Callback wenn Projektil detoniert (fuer Visual-RPC vom Spawner).</summary>
        public event System.Action<Vector3> OnDetonated;

        /// <summary>Root-Transform des Owners (fuer Raycast-Filterung: eigene Collider ignorieren).</summary>
        private Transform m_OwnerRoot;

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
            int knockback,
            ulong ownerClientId,
            string weaponName = "",
            uint projectileId = 0,
            string explosionEffectId = "")
        {
            m_ProjectileId = projectileId;
            m_ExplosionEffectId = explosionEffectId ?? "";
            transform.position = spawnPosition;
            m_Velocity = direction.normalized * (speedQU * SOF2_UNIT_SCALE);
            m_GravityScale = gravityScale;
            m_Bounce = bounce;
            m_Detonation = detonation;
            m_Timer = timer;
            m_Damage = damage;
            m_Radius = radiusQU;
            m_Knockback = knockback > 0 ? knockback : 700;
            m_OwnerClientId = ownerClientId;
            m_WeaponName = weaponName;
            m_Lifetime = 0f;
            m_HasDetonated = false;
            m_IsStuck = false;

            m_PlayerLayerMask = LayerMask.GetMask(PLAYER_LAYER_NAME);
            m_HitboxLayerMask = LayerMask.GetMask("Hitbox");
            // SoF2 G_RadiusDamage nutzt Entity-Origins, nicht per-Bone Hitboxen.
            // Explosions-Erkennung ueber Player-Physics-Collider (BoxCollider auf Player-Layer).
            m_ExplosionLayerMask = m_PlayerLayerMask;
            // Welt-Kollision: alles was nicht Hitbox, Player oder BrushCollision ist
            m_WorldLayerMask = ~(m_HitboxLayerMask | m_PlayerLayerMask | LayerMask.GetMask("BrushCollision"));

            // Owner-Root finden fuer Raycast-Filterung (eigene Collider ignorieren)
            foreach (Unity.Netcode.NetworkClient client in Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == ownerClientId && client.PlayerObject != null)
                {
                    m_OwnerRoot = client.PlayerObject.transform;
                    break;
                }
            }

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
            // RaycastAll + Filter: eigenen Owner-Collider ignorieren
            RaycastHit[] worldHits = Physics.RaycastAll(transform.position, direction, distance, m_WorldLayerMask);
            RaycastHit worldHit = default;
            bool hasWorldHit = false;
            float closestWorldDist = float.MaxValue;
            foreach (RaycastHit wh in worldHits)
            {
                if (m_OwnerRoot != null && wh.collider.transform.IsChildOf(m_OwnerRoot))
                {
                    continue;
                }
                if (wh.distance < closestWorldDist)
                {
                    closestWorldDist = wh.distance;
                    worldHit = wh;
                    hasWorldHit = true;
                }
            }
            if (hasWorldHit)
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

                    // SoF2 g_missile.c:37/59: Granate stoppt auf horizontaler Flaeche bei niedriger Geschwindigkeit
                    if (worldHit.normal.y > 0.2f && m_Velocity.magnitude < BOUNCE_STOP_SPEED)
                    {
                        m_Velocity = Vector3.zero;
                    }
                }
                else
                {
                    m_Velocity = Vector3.zero;
                }

                return;
            }

            // Raycast fuer Spieler-Kollision (Bone-Trigger-BoxCollider auf Hitbox-Layer)
            // RaycastAll + Filter: eigenen Owner ignorieren
            RaycastHit[] hitboxHits = Physics.RaycastAll(transform.position, direction, distance, m_HitboxLayerMask, QueryTriggerInteraction.Collide);
            RaycastHit hitboxHit = default;
            bool hasHitboxHit = false;
            float closestHitboxDist = float.MaxValue;
            foreach (RaycastHit hh in hitboxHits)
            {
                if (m_OwnerRoot != null && hh.collider.transform.IsChildOf(m_OwnerRoot))
                {
                    continue;
                }
                if (hh.distance < closestHitboxDist)
                {
                    closestHitboxDist = hh.distance;
                    hitboxHit = hh;
                    hasHitboxHit = true;
                }
            }
            if (hasHitboxHit)
            {
                if (m_Detonation == "impact")
                {
                    Detonate(hitboxHit.point);
                    return;
                }

                if (m_Detonation == "sticky")
                {
                    ApplyDirectDamage(hitboxHit, transform.position, direction);
                    StickToSurface(hitboxHit.point, hitboxHit.normal);
                    return;
                }
            }

            // Keine Kollision — normal bewegen
            transform.position += movement;
        }

        /// <summary>
        /// SoF2 maximaler Knockback-Wert (g_combat.c:844: knockback > 200 → 200).
        /// </summary>
        private const float MAX_KNOCKBACK = 200f;

        /// <summary>
        /// Detoniert das Projektil: Explosions-Damage und Knockback im Radius anwenden.
        /// SoF2-authentisch: Self-Damage ×2, Knockback = min(damage, 200),
        /// kvel = dir * 700 * knockback / 200 * 0.8 (g_combat.c:543).
        /// </summary>
        private void Detonate(Vector3 explosionPoint)
        {
            if (m_HasDetonated)
            {
                return;
            }

            m_HasDetonated = true;

            float radiusMeters = m_Radius * SOF2_UNIT_SCALE;

            // SoF2 G_RadiusDamage: OverlapSphere auf Player-Physics-Collider (nicht per-Bone Hitboxen).
            // Hitbox-Trigger-Collider an Animator-Bones werden von OverlapSphere nicht zuverlaessig gefunden.
            Collider[] hits = Physics.OverlapSphere(explosionPoint, radiusMeters, m_ExplosionLayerMask);

            // Pro Spieler: Damage mit Distanz-Falloff sammeln
            // SoF2 G_RadiusDamage → CanDamage(): Trace von Explosion zu Entity-Origin.
            // Wenn Wand dazwischen → kein Schaden (Granate um Ecke = sicher).
            int losMask = ~(m_PlayerLayerMask | m_HitboxLayerMask);
            System.Collections.Generic.Dictionary<NetworkedCharacterState, (float damage, Vector3 hitPos)> damagePerTarget =
                new();

            foreach (Collider col in hits)
            {
                NetworkedCharacterState targetState =
                    col.GetComponentInParent<NetworkedCharacterState>();
                if (targetState == null)
                {
                    continue;
                }

                // SoF2 CanDamage: Linecast von Explosion zum Spieler-Zentrum.
                // Wenn Welt-Geometrie blockiert → kein Damage.
                Vector3 targetCenter = col.bounds.center;
                if (Physics.Linecast(explosionPoint, targetCenter, losMask))
                {
                    continue;
                }

                // SoF2 G_RadiusDamage: Distanz von Explosion zum Entity-Origin (Collider-Center)
                float distance = Vector3.Distance(explosionPoint, col.transform.position);
                float falloff = 1f - Mathf.Clamp01(distance / radiusMeters);
                float effectiveDamage = m_Damage * falloff;

                // Hoechsten Damage pro Spieler merken (mit Position fuer Knockback-Richtung)
                if (!damagePerTarget.ContainsKey(targetState) || damagePerTarget[targetState].damage < effectiveDamage)
                {
                    damagePerTarget[targetState] = (effectiveDamage, col.transform.position);
                }
            }

            // Damage + Knockback anwenden
            foreach (System.Collections.Generic.KeyValuePair<NetworkedCharacterState, (float damage, Vector3 hitPos)> kvp in damagePerTarget)
            {
                float baseDamage = kvp.Value.damage;
                bool isSelf = kvp.Key.OwnerClientId == m_OwnerClientId;

                // SoF2 g_combat.c:916: Self-Damage × 2
                float take = isSelf ? baseDamage * 2f : baseDamage;

                int finalDamage = Mathf.RoundToInt(take);
                if (finalDamage <= 0)
                {
                    continue;
                }

                // Damage anwenden
                int newHealth = Mathf.Max(0, kvp.Key.Health - finalDamage);
                kvp.Key.SetHealth(newHealth);

                // SoF2 Knockback (g_combat.c:844): knockback = min(baseDamage, 200), NICHT take
                float knockback = Mathf.Min(baseDamage, MAX_KNOCKBACK);

                // Knockback-Richtung: Explosion → Spieler
                // Q3-Style: Center-of-Mass 24 QU höher als origin, damit Explosionen
                // unter den Füßen den Spieler nach oben schleudern (Rocket-Jumping).
                // SoF2 hat dir[2]=0 (kein vertikaler KB), aber wir wollen Rocket-Jump.
                Vector3 knockbackDir = kvp.Value.hitPos - explosionPoint;
                knockbackDir.y += 24f * SOF2_UNIT_SCALE;
                if (knockbackDir.sqrMagnitude < 0.001f)
                {
                    knockbackDir = Vector3.up;
                }
                knockbackDir.Normalize();

                // ServerPlayerCharacter fuer Velocity-Aenderung finden
                ServerPlayerCharacter serverPlayer = kvp.Key.GetComponentInParent<ServerPlayerCharacter>();
                if (serverPlayer != null && knockback > 0f)
                {
                    serverPlayer.ApplyKnockback(knockbackDir, knockback, m_Knockback);
                }
            }

            // Incendiary/Phosphorus: Persistente Feuer-Schadenszone am Boden spawnen (SoF2-authentisch)
            SpawnFireDamageZoneIfNeeded(explosionPoint);

            // Event fuer Visual-RPC
            OnDetonated?.Invoke(explosionPoint);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ServerProjectile] Detonated at {explosionPoint} | Radius={radiusMeters:F1}m | Targets={damagePerTarget.Count}");
#endif

            // Zerstoeren
            Destroy(gameObject);
        }

        /// <summary>
        /// Incendiary (ANM14) und Phosphorus (M15): Persistente Feuer-Schadenszone am Boden.
        /// SoF2-authentisch: Feuer verursacht Burn-Damage-over-Time an Spielern im Bereich.
        /// ANM14: 10s Dauer, 5 DMG/0.5s (100 DMG total bei durchgehendem Kontakt).
        /// M15: 6s Dauer, 3 DMG/0.5s (36 DMG total).
        /// </summary>
        private void SpawnFireDamageZoneIfNeeded(Vector3 explosionPoint)
        {
            if (string.IsNullOrEmpty(m_ExplosionEffectId))
            {
                return;
            }

            bool isIncendiary = m_ExplosionEffectId.Contains("incendiary");
            bool isPhosphorus = m_ExplosionEffectId.Contains("phosphorus");

            if (!isIncendiary && !isPhosphorus)
            {
                return;
            }

            string zoneName = isIncendiary ? "IncendiaryFireZone" : "PhosphorusFireZone";
            GameObject zoneObj = new(zoneName);
            FireDamageZone zone = zoneObj.AddComponent<FireDamageZone>();

            if (isIncendiary)
            {
                // ANM14: 10s brennend, 5 DMG/0.5s, Radius == Explosionsradius
                zone.Initialize(explosionPoint, m_Radius, 5, 0.5f, 10f, m_OwnerClientId);
            }
            else
            {
                // M15 Phosphorus: 6s brennend, 3 DMG/0.5s, etwas kleinerer Radius
                zone.Initialize(explosionPoint, Mathf.RoundToInt(m_Radius * 0.8f), 3, 0.5f, 6f, m_OwnerClientId);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ServerProjectile] {zoneName} spawned at {explosionPoint} | Duration={( isIncendiary ? 10 : 6 )}s");
#endif
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ServerProjectile] Sticky stuck at {point} — awaiting pickup by client {m_OwnerClientId}");
#endif
        }

        /// <summary>
        /// Direkter Schaden bei Sticky-Treffer (kein Explosionsradius, kein Falloff).
        /// Nutzt Bone-Point-Aufloesung fuer praezise HitRegion-Bestimmung.
        /// </summary>
        private void ApplyDirectDamage(RaycastHit hit, Vector3 rayOrigin, Vector3 rayDirection)
        {
            HitboxCollider hitboxCollider = hit.collider.GetComponent<HitboxCollider>();
            if (hitboxCollider == null)
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ServerProjectile] Sticky direct hit {targetState.CharacterName} | Region={hitboxCollider.HitRegion} | Damage={m_Damage} | Health={newHealth}");
#endif
        }

        /// <summary>
        /// Trigger-Event: Spieler laeuft ueber das Sticky-Projektil → Ammo-Pickup.
        /// Nur der Owner (Werfer) kann sein Projektil wieder aufheben.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!m_IsStuck || m_IsPickedUp)
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

            m_IsPickedUp = true;
            targetState.AddReserveAmmoForWeapon(m_WeaponName, 1);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[ServerProjectile] Sticky picked up by client {m_OwnerClientId} — weapon={m_WeaponName}, id={m_ProjectileId}");
#endif

            OnPickedUp?.Invoke(m_ProjectileId);
            Destroy(gameObject);
        }
    }
}
