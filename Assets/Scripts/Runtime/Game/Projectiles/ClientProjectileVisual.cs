using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.EffectManagement;
using Tolik.RemakeSoF.Runtime.Game.Effects;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.SoundManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using Tolik.RemakeSoF.Runtime.WeaponManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

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

        /// <summary>Gecachter Sprites/Default Shader fuer Fallback-Materialien.</summary>
        private static Shader s_CachedSpritesShader;

        /// <summary>Gecachter SoF2/MapSurface Shader fuer Projektil-Models (Unlit-Basis + Lambert-Licht).</summary>
        private static Shader s_CachedMapSurfaceShader;

        /// <summary>
        /// LightBlend fuer Projektil-Materialien (identisch zu WeaponLoader).
        /// 0.5 = 50% Unlit-Basis + 50% Lambert-Beleuchtung.
        /// </summary>
        private const float PROJECTILE_LIGHT_BLEND = 0.5f;

        /// <summary>SoF2 Gravitation in Unity-Meter/s² (800 QU/s² × 0.0254 = 20.32).</summary>
        private const float SOF2_GRAVITY = 20.32f;

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

        /// <summary>Effekt-ID fuer datengetriebene Visuals (aus JSON).</summary>
        private string m_EffectId;

        /// <summary>Explosions-Effekt-ID fuer Detonation (aus JSON).</summary>
        private string m_ExplosionEffectId;

        /// <summary>Addressable-Key fuer Projektil-Model (z.B. "knife", "f1"). Leer = kein Model.</summary>
        private string m_ModelKey;

        /// <summary>Instanziiertes Projektil-Model (geladen via PrefabManager).</summary>
        private GameObject m_ModelInstance;

        /// <summary>Looping AudioSource fuer Flug-Sound (z.B. RPG Flyby, Granaten-Pfeifen).</summary>
        private AudioSource m_LoopAudioSource;

        /// <summary>Referenz auf dynamisch erstelltes Fallback-Sphere-Material fuer Cleanup.</summary>
        private Material m_FallbackSphereMaterial;

        /// <summary>
        /// SoF2 Bounce-Stop-Threshold: 40 QU/s * 0.0254 = 1.016 m/s (g_missile.c:37/59).
        /// Granate stoppt auf horizontaler Flaeche (normal.y > 0.2) wenn Geschwindigkeit darunter.
        /// </summary>
        private const float BOUNCE_STOP_SPEED = 1.016f;

        /// <summary>Akkumulierter ROLL-Winkel fuer Messer-Wurfrotation (SoF2: lerpAngles[ROLL] += cg.time * 1.75).</summary>
        private float m_KnifeRollAngle;

        /// <summary>
        /// Model-Korrektur fuer SoF2/Ghoul2-Projektile.
        /// Grenade/RPG: Euler(0, 0, -90) — identisch zum WeaponLoader (Hand-Attachment).
        /// Knife: Euler(-90, 0, 0) — Blade (lokale Y-Achse) auf Flugrichtung (Z) ausrichten.
        /// </summary>
        private static readonly Quaternion MODEL_ROTATION_DEFAULT = Quaternion.Euler(0f, 0f, -90f);
        private static readonly Quaternion MODEL_ROTATION_KNIFE = Quaternion.Euler(0f, -90f, 0f);

        /// <summary>Rotationsgeschwindigkeit fuer Messer-Wurfrotation in Grad/Sek (SoF2: 1.75 Grad/ms = 1750 Grad/s).</summary>
        private const float KNIFE_ROTATION_SPEED = 1750f;

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
            uint projectileId = 0,
            string effectId = "",
            string explosionEffectId = "",
            string modelKey = "",
            string loopSoundPath = "")
        {
            m_ProjectileId = projectileId;
            m_EffectId = effectId;
            m_ExplosionEffectId = explosionEffectId;
            m_ModelKey = modelKey;
            transform.position = spawnPosition;
            m_Velocity = direction.normalized * (speedQU * SOF2_UNIT_SCALE);
            m_GravityScale = gravityScale;
            m_Bounce = bounce;
            m_Detonation = detonation;
            m_Timer = timer;
            m_Lifetime = 0f;
            m_HasDetonated = false;

            // Welt-Kollision: alles ausser Hitbox-Layer, BrushCollision und Player-Movement-Collider (visuelle Kollision fuer Bounce/Impact)
            int hitboxLayer = LayerMask.GetMask("Hitbox");
            m_WorldLayerMask = ~(hitboxLayer | LayerMask.GetMask("BrushCollision", "Player"));

            // Rotation in Flugrichtung
            if (m_Velocity.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(m_Velocity);
            }

            // Visuelles Setup
            CreateVisuals();

            // Looping Flug-Sound (RPG Flyby, Granaten-Pfeifen etc.)
            StartLoopSound(loopSoundPath);
        }

        /// <summary>
        /// Erstellt eine loopende AudioSource fuer den Flug-Sound des Projektils.
        /// </summary>
        private void StartLoopSound(string loopSoundPath)
        {
            if (string.IsNullOrEmpty(loopSoundPath))
            {
                return;
            }

            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null)
            {
                return;
            }

            AudioClip clip = soundManager.GetClip(loopSoundPath);
            if (clip == null)
            {
                return;
            }

            m_LoopAudioSource = gameObject.AddComponent<AudioSource>();
            m_LoopAudioSource.clip = clip;
            m_LoopAudioSource.loop = true;
            m_LoopAudioSource.spatialBlend = 1f;
            m_LoopAudioSource.minDistance = 2f;
            m_LoopAudioSource.maxDistance = 50f;
            m_LoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
            m_LoopAudioSource.outputAudioMixerGroup = soundManager.SfxGroup;
            m_LoopAudioSource.Play();
        }

        /// <summary>
        /// Erstellt die visuellen Komponenten: 3D-Model (wenn vorhanden) oder Fallback-Kugel + TrailRenderer.
        /// </summary>
        private void CreateVisuals()
        {
            // Model laden via PrefabManager (z.B. "knife", "f1")
            bool hasModel = !string.IsNullOrEmpty(m_ModelKey) && TryLoadModel();

            if (!hasModel)
            {
                CreateFallbackSphere();
            }

            // Datengetriebene Effekte via EffectFactory
            EffectFactory factory = ServiceLocator.Get<EffectFactory>();
            EffectDefinition definition = factory?.GetDefinition(m_EffectId);

            if (definition != null && definition.Segments != null)
            {
                CreateDataDrivenVisuals(factory, definition);
            }
            else if (!hasModel)
            {
                // Fallback-Trail nur wenn kein 3D-Model vorhanden.
                // SoF2: Knife hat keinen Trail (cg_weaponinit.c: kein tracerEffect fuer WP_KNIFE).
                CreateFallbackTrail();
            }
        }

        /// <summary>
        /// Versucht das Projektil-Model via PrefabManager zu laden und zu instanziieren.
        /// Gibt true zurueck wenn erfolgreich, false als Fallback.
        /// </summary>
        private bool TryLoadModel()
        {
            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
            if (prefabManager == null)
            {
                Debug.LogWarning("[ClientProjectileVisual] PrefabManager nicht verfuegbar — Fallback auf Sphere.");
                return false;
            }

            GameObject prefab = prefabManager.LoadPrefab<GameObject>(m_ModelKey);
            if (prefab == null)
            {
                Debug.LogWarning($"[ClientProjectileVisual] Projektil-Model '{m_ModelKey}' nicht gefunden — Fallback auf Sphere.");
                return false;
            }

            m_ModelInstance = Instantiate(prefab, transform);
            m_ModelInstance.name = $"ProjectileModel_{m_ModelKey}";

            // Ghoul2Meta-Texturen anwenden (mapped_texture_0..N)
            PrefabTextureApplier.ApplyTextures(m_ModelInstance);

            // viewModel-Texturen anwenden (Base + Specular aus WeaponDefinition)
            ApplyViewModelTextures(m_ModelInstance, m_ModelKey);

            // SoF2/Ghoul2 Model-Korrektur: Knife hat Blade entlang Y, andere Models brauchen Z-Rotation
            m_ModelInstance.transform.localRotation = m_ModelKey == "knife"
                ? MODEL_ROTATION_KNIFE
                : MODEL_ROTATION_DEFAULT;

            // Collider entfernen (Projektil-Models sind rein visuell, Kollision ist punkt-basiert)
            foreach (Collider col in m_ModelInstance.GetComponentsInChildren<Collider>())
            {
                Destroy(col);
            }

            // Knife: ROLL-Rotation wird direkt auf dem Transform angewendet (nicht auf Bone),
            // passend zu SoF2 cg_ents.c CG_Missile(): lerpAngles[ROLL] += cg.time * 1.75

            //Debug.Log($"[ClientProjectileVisual] Projektil-Model '{m_ModelKey}' geladen.");
            return true;
        }

        /// <summary>
        /// Wendet viewModel-Texturen (Base + Specular) auf das Projektil-Model an.
        /// Nutzt den viewModel-Pfad aus WeaponDefinition als Textur-Key.
        /// SoF2 Shader-Konvention: viewModel = Base-Textur, viewModel_spec = Specular-Map.
        /// </summary>
        private static void ApplyViewModelTextures(GameObject instance, string modelKey)
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager == null)
            {
                return;
            }

            WeaponDataLoader weaponLoader = ServiceLocator.Get<WeaponDataLoader>();
            WeaponDefinition definition = weaponLoader?.GetById(modelKey);
            if (definition == null || string.IsNullOrEmpty(definition.ViewModel))
            {
                return;
            }

            string viewModelPath = definition.ViewModel;

            // Base-Textur laden (z.B. "models/weapons/knife/knife")
            TextureData baseData = textureManager.GetTextureData(viewModelPath)
                ?? textureManager.GetTextureDataByAlias(viewModelPath);
            Texture2D baseTexture = baseData != null && baseData.HasTexture() ? baseData.Texture : null;

            if (baseTexture == null)
            {
                return;
            }

            // Specular-Textur laden (z.B. "models/weapons/knife/knife_spec")
            string specPath = viewModelPath + "_spec";
            TextureData specData = textureManager.GetTextureData(specPath)
                ?? textureManager.GetTextureDataByAlias(specPath);
            Texture2D specTexture = specData != null && specData.HasTexture() ? specData.Texture : null;

            if (s_CachedMapSurfaceShader == null)
            {
                s_CachedMapSurfaceShader = Shader.Find("SoF2/MapSurface");
            }
            Shader shader = s_CachedMapSurfaceShader;
            if (shader == null)
            {
                return;
            }

            // Material erstellen: SoF2/MapSurface mit anteiliger Lambert-Beleuchtung
            Material material = new(shader) { name = viewModelPath };

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", baseTexture);
            }
            else
            {
                material.mainTexture = baseTexture;
            }
            material.SetColor("_BaseColor", Color.white);

            // LightBlend: Projektile reagieren auf Szenen-Licht
            if (material.HasProperty("_LightBlend"))
            {
                material.SetFloat("_LightBlend", PROJECTILE_LIGHT_BLEND);
            }

            // Backface-Culling OFF (SoF2: cull disable)
            material.SetFloat("_Cull", (float)CullMode.Off);

            // Auf alle Renderer anwenden
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                // Alte Instanz-Materialien freigeben bevor neue zugewiesen werden
                Material[] oldMats = renderer.materials;
                foreach (Material oldMat in oldMats)
                {
                    if (oldMat != null)
                    {
                        Destroy(oldMat);
                    }
                }

                Material[] materials = renderer.sharedMaterials;
                Material[] newMaterials = new Material[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    newMaterials[i] = material;
                }
                renderer.materials = newMaterials;
            }
        }

        /// <summary>
        /// Sucht rekursiv nach einem Child-Transform mit dem angegebenen Namen.
        /// </summary>
        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Erstellt eine Fallback-Kugel wenn kein Projektil-Model vorhanden ist.
        /// </summary>
        private void CreateFallbackSphere()
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
                if (s_CachedSpritesShader == null)
                {
                    s_CachedSpritesShader = Shader.Find("Sprites/Default");
                }
                Material mat = new(s_CachedSpritesShader);
                mat.color = m_Detonation == "timer" ? new Color(0.2f, 0.8f, 0.2f)
                    : m_Detonation == "sticky" ? new Color(0.85f, 0.85f, 0.9f)
                    : new Color(1f, 0.5f, 0f);
                renderer.material = mat;
                m_FallbackSphereMaterial = mat;
            }
        }

        /// <summary>
        /// Erstellt datengetriebene Visuals aus der EffectDefinition (Trail + ParticleSystem pro Segment).
        /// </summary>
        private void CreateDataDrivenVisuals(EffectFactory factory, EffectDefinition definition)
        {
            foreach (EffectSegment segment in definition.Segments)
            {
                if (segment.Type == "tail")
                {
                    m_Trail = gameObject.AddComponent<TrailRenderer>();
                    factory.ConfigureTrailRenderer(m_Trail, segment);
                }
                else if (segment.Type == "particle")
                {
                    GameObject psGo = new($"Effect_{segment.Name}");
                    psGo.transform.SetParent(transform, false);
                    ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    factory.ConfigureParticleSystem(ps, segment);
                    ps.Play();
                }
            }

            // Kein Fallback-Trail wenn datengetriebene Visuals vorhanden (z.B. m203_trail hat nur Partikel).
            // SoF2: Wenn ein Effect nur Partikel definiert, gibt es keinen Trail.
        }

        /// <summary>
        /// Erstellt einen einfachen Fallback-Trail wenn keine EffectDefinition vorhanden.
        /// </summary>
        private void CreateFallbackTrail()
        {
            m_Trail = gameObject.AddComponent<TrailRenderer>();
            m_Trail.time = TRAIL_TIME;
            m_Trail.startWidth = TRAIL_START_WIDTH;
            m_Trail.endWidth = TRAIL_END_WIDTH;
            if (s_CachedSpritesShader == null)
            {
                s_CachedSpritesShader = Shader.Find("Sprites/Default");
            }
            Material fallbackTrailMat = new(s_CachedSpritesShader);
            m_Trail.material = fallbackTrailMat;

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

            // Gravitation anwenden (SoF2: 800 QU/s² = 20.32 m/s², nicht Unity 9.81)
            if (m_GravityScale > 0f)
            {
                m_Velocity.y -= SOF2_GRAVITY * m_GravityScale * dt;
            }

            // Rotation in Flugrichtung aktualisieren
            if (m_Velocity.sqrMagnitude > 0.001f)
            {
                Quaternion flightRotation = Quaternion.LookRotation(m_Velocity);

                // SoF2 Knife ROLL-Rotation: cg_ents.c CG_Missile() → lerpAngles[ROLL] += cg.time * 1.75
                // 1.75 Grad/ms = 1750 Grad/s. Model-Korrektur Euler(0,-90,0) legt die Klinge entlang Z,
                // deshalb Spin um die rechte Achse (X) damit die Klinge sichtbar rotiert.
                if (m_ModelKey == "knife")
                {
                    m_KnifeRollAngle += KNIFE_ROTATION_SPEED * dt;
                    flightRotation *= Quaternion.AngleAxis(m_KnifeRollAngle, Vector3.right);
                }

                transform.rotation = flightRotation;
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

                    // SoF2 g_missile.c:37/59: Granate stoppt auf horizontaler Flaeche bei niedriger Geschwindigkeit
                    if (hit.normal.y > 0.2f && m_Velocity.magnitude < BOUNCE_STOP_SPEED)
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
            StopLoopSound();

            // Datengetriebene Explosion via EffectFactory (falls vorhanden)
            EffectFactory factory = ServiceLocator.Get<EffectFactory>();
            if (factory != null && !string.IsNullOrEmpty(m_ExplosionEffectId))
            {
                factory.SpawnExplosion(transform.position, m_ExplosionEffectId);
            }
            else
            {
                CreateExplosionVisual(transform.position);
            }

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
            StopLoopSound();

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
        /// Stoppt den Flug-Loop-Sound, falls aktiv.
        /// </summary>
        private void StopLoopSound()
        {
            if (m_LoopAudioSource != null)
            {
                m_LoopAudioSource.Stop();
                Destroy(m_LoopAudioSource);
                m_LoopAudioSource = null;
            }
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
            // Temporaeres Licht auf eigenem GameObject (URP UniversalAdditionalLightData)
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
