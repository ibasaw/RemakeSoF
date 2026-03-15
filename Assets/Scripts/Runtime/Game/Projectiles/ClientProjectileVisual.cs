using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Projectiles
{
    /// <summary>
    /// Client-seitiges visuelles Projektil.
    /// Simuliert die gleiche Physik wie ServerProjectile (Geschwindigkeit, Gravitation, Bounce)
    /// fuer eine vorhersagbare visuelle Darstellung auf allen Clients.
    /// Zeigt Trail + kleines Objekt, keine Gameplay-Logik (kein Damage).
    /// Sticky-Projektile bleiben an der Auftreffstelle sichtbar.
    /// </summary>
    public class ClientProjectileVisual : MonoBehaviour
    {
        /// <summary>SoF2-Unit → Unity-Meter (1 QU = 0.0254m).</summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;

        /// <summary>Max Lebensdauer in Sekunden (Safety-Cleanup).</summary>
        private const float MAX_LIFETIME = 15f;

        /// <summary>Trail-Breite am Start.</summary>
        private const float TRAIL_START_WIDTH = 0.08f;

        /// <summary>Trail-Breite am Ende.</summary>
        private const float TRAIL_END_WIDTH = 0.01f;

        /// <summary>Trail-Dauer in Sekunden.</summary>
        private const float TRAIL_TIME = 0.5f;

        /// <summary>Groesse der visuellen Projektil-Kugel.</summary>
        private const float SPHERE_SCALE = 0.1f;

        /// <summary>Aktuelle Flugrichtung und -geschwindigkeit (Unity-Meter/Sek).</summary>
        private Vector3 m_Velocity;

        /// <summary>Gravitations-Skalierung.</summary>
        private float m_GravityScale;

        /// <summary>Bounce-Faktor.</summary>
        private float m_Bounce;

        /// <summary>Detonationsart: "impact", "timer" oder "sticky".</summary>
        private string m_Detonation;

        /// <summary>Timer-Countdown fuer Timer-Detonation (Sekunden).</summary>
        private float m_Timer;

        /// <summary>Seit Spawn vergangene Zeit.</summary>
        private float m_Lifetime;

        /// <summary>Ob das Projektil bereits detoniert ist.</summary>
        private bool m_HasDetonated;

        /// <summary>Eindeutige ID fuer servergesteuerte Cleanup-Logik.</summary>
        private uint m_ProjectileId;

        /// <summary>Statisches Lookup fuer aktive Sticky-Visuals nach ID.</summary>
        private static readonly System.Collections.Generic.Dictionary<uint, ClientProjectileVisual> s_ActiveVisuals = new();

        /// <summary>LayerMask fuer Welt-Kollision (visuelle Kollisionserkennung).</summary>
        private int m_WorldLayerMask;

        /// <summary>Trail-Renderer Referenz.</summary>
        private TrailRenderer m_Trail;

        /// <summary>
        /// Initialisiert das visuelle Projektil mit den gleichen Parametern wie ServerProjectile.
        /// </summary>
        public void Initialize(
            Vector3 spawnPosition,
            Vector3 direction,
            float speedQU,
            float gravityScale,
            float bounce,
            string detonation,
            float timer,
            uint projectileId = 0)
        {
            m_ProjectileId = projectileId;
            transform.position = spawnPosition;
            m_Velocity = direction.normalized * (speedQU * SOF2_UNIT_SCALE);
            m_GravityScale = gravityScale;
            m_Bounce = bounce;
            m_Detonation = detonation;
            m_Timer = timer;
            m_Lifetime = 0f;
            m_HasDetonated = false;

            // Welt-Kollision: alles ausser Hitbox-Layer (visuelle Kollision fuer Bounce/Impact)
            int hitboxLayer = LayerMask.GetMask("Hitbox");
            m_WorldLayerMask = ~hitboxLayer;

            // Rotation in Flugrichtung
            if (m_Velocity.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(m_Velocity);
            }

            // Visuelles Setup
            CreateVisuals();
        }

        /// <summary>
        /// Erstellt die visuellen Komponenten: kleine Kugel + TrailRenderer.
        /// </summary>
        private void CreateVisuals()
        {
            // Kleine Kugel als Projektil-Objekt
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * SPHERE_SCALE;

            // Collider entfernen (nur visuell)
            Collider col = sphere.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }

            // Material: Farbe je nach Detonationsart
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new(Shader.Find("Sprites/Default"));
                mat.color = m_Detonation == "timer" ? new Color(0.2f, 0.8f, 0.2f)
                    : m_Detonation == "sticky" ? new Color(0.85f, 0.85f, 0.9f)
                    : new Color(1f, 0.5f, 0f);
                renderer.material = mat;
            }

            // TrailRenderer fuer Flugspur
            m_Trail = gameObject.AddComponent<TrailRenderer>();
            m_Trail.time = TRAIL_TIME;
            m_Trail.startWidth = TRAIL_START_WIDTH;
            m_Trail.endWidth = TRAIL_END_WIDTH;
            m_Trail.material = new Material(Shader.Find("Sprites/Default"));

            // Trail-Farbe: Farbverlauf je nach Detonationsart
            Gradient gradient = new();
            Color trailStart = m_Detonation == "timer" ? Color.green
                : m_Detonation == "sticky" ? Color.white
                : Color.yellow;
            Color trailEnd = m_Detonation == "timer" ? new Color(0f, 0.5f, 0f)
                : m_Detonation == "sticky" ? new Color(0.7f, 0.7f, 0.7f)
                : Color.red;
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new(trailStart, 0f),
                    new(trailEnd, 1f)
                },
                new GradientAlphaKey[]
                {
                    new(1f, 0f),
                    new(0f, 1f)
                }
            );
            m_Trail.colorGradient = gradient;
            m_Trail.minVertexDistance = 0.1f;
        }

        /// <summary>
        /// Client-seitige Physik-Simulation (identisch zu ServerProjectile, aber ohne Damage).
        /// </summary>
        private void Update()
        {
            if (m_HasDetonated)
            {
                return;
            }

            float dt = Time.deltaTime;
            m_Lifetime += dt;

            // Safety-Cleanup
            if (m_Lifetime > MAX_LIFETIME)
            {
                Detonate();
                return;
            }

            // Timer-Detonation
            if (m_Detonation == "timer")
            {
                m_Timer -= dt;
                if (m_Timer <= 0f)
                {
                    Detonate();
                    return;
                }
            }

            // Gravitation anwenden
            if (m_GravityScale > 0f)
            {
                m_Velocity += Physics.gravity * (m_GravityScale * dt);
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

            // Raycast fuer Welt-Kollision
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, m_WorldLayerMask))
            {
                transform.position = hit.point + hit.normal * 0.01f;

                if (m_Detonation == "impact")
                {
                    Detonate();
                    return;
                }

                if (m_Detonation == "sticky")
                {
                    StickToSurface();
                    return;
                }

                // Bounce (Timer-Granaten)
                if (m_Bounce > 0f)
                {
                    m_Velocity = Vector3.Reflect(m_Velocity, hit.normal) * m_Bounce;
                }
                else
                {
                    m_Velocity = Vector3.zero;
                }

                return;
            }

            // Keine Kollision — normal bewegen
            transform.position += movement;
        }

        /// <summary>
        /// Visuell detonieren: kurze Explosion anzeigen, dann zerstoeren.
        /// </summary>
        private void Detonate()
        {
            if (m_HasDetonated)
            {
                return;
            }

            m_HasDetonated = true;

            // Einfache visuelle Explosion: kurz aufblitzendes Licht + Sphere-Scale-Up
            CreateExplosionVisual(transform.position);

            // Trail kurz sichtbar lassen, dann zerstoeren
            Destroy(gameObject, TRAIL_TIME);
        }

        /// <summary>
        /// Sticky-Projektil an Oberflaeche fixieren: Kein Explosions-Effekt, bleibt sichtbar.
        /// Wird nach MAX_LIFETIME automatisch aufgeraeumt.
        /// </summary>
        private void StickToSurface()
        {
            m_HasDetonated = true;

            // In Lookup registrieren fuer servergesteuerte Cleanup-Logik
            if (m_ProjectileId != 0)
            {
                s_ActiveVisuals[m_ProjectileId] = this;
            }

            // Trail abschalten (Projektil ruht)
            if (m_Trail != null)
            {
                m_Trail.emitting = false;
            }

            // Verbleibende Lifetime als Cleanup-Timer
            float remainingLife = MAX_LIFETIME - m_Lifetime;
            if (remainingLife < 1f)
            {
                remainingLife = 1f;
            }

            Destroy(gameObject, remainingLife);
        }

        /// <summary>
        /// Zerstoert das Visual mit der angegebenen ID (aufgerufen via Server-RPC bei Sticky-Pickup).
        /// </summary>
        public static void DestroyById(uint projectileId)
        {
            if (projectileId == 0)
            {
                return;
            }

            if (s_ActiveVisuals.TryGetValue(projectileId, out ClientProjectileVisual visual) && visual != null)
            {
                s_ActiveVisuals.Remove(projectileId);
                Destroy(visual.gameObject);
            }
        }

        /// <summary>
        /// Cleanup: aus statischem Lookup entfernen.
        /// </summary>
        private void OnDestroy()
        {
            if (m_ProjectileId != 0)
            {
                s_ActiveVisuals.Remove(m_ProjectileId);
            }
        }

        /// <summary>
        /// Erstellt eine einfache visuelle Explosion (temporaerer Flash).
        /// </summary>
        private static void CreateExplosionVisual(Vector3 position)
        {
            // Temporaeres Licht fuer Explosion
            GameObject flashObj = new("ProjectileExplosion");
            flashObj.transform.position = position;

            Light flash = flashObj.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.6f, 0.1f);
            flash.intensity = 8f;
            flash.range = 10f;

            Destroy(flashObj, 0.3f);
        }
    }
}
