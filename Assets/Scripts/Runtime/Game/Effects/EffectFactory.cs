using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.EffectManagement;
using Tolik.RemakeSoF.Runtime.Game.Camera;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Effects
{
    /// <summary>
    /// Factory fuer datengetriebene Effekt-Materialien und ParticleSystem-Konfigurationen.
    /// Laedt EffectDefinitions via EffectDataLoader und baut Materialien via TextureManager.
    /// Cached Materialien pro Textur-Pfad (einmal bauen, mehrfach verwenden).
    /// Zugreifbar ueber ServiceLocator.
    /// </summary>
    public class EffectFactory
    {
        private const string URP_PARTICLE_SHADER = "Universal Render Pipeline/Particles/Unlit";
        private const string BUILTIN_PARTICLE_SHADER = "Particles/Standard Unlit";

        private readonly Dictionary<string, Material> m_MaterialCache = new(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gibt eine gecachte Material-Instanz fuer den angegebenen Textur-Pfad zurueck.
        /// Baut das Material beim ersten Aufruf ueber TextureManager (Lazy Loading).
        /// Additive Blending als Standard (Tracer, Flash). useAlphaBlend fuer Rauch/Staub.
        /// </summary>
        public Material GetMaterial(string texturePath, bool useAlphaBlend = false)
        {
            if (string.IsNullOrEmpty(texturePath))
            {
                return GetFallbackMaterial();
            }

            string cacheKey = useAlphaBlend ? texturePath + "_alpha" : texturePath;

            if (m_MaterialCache.TryGetValue(cacheKey, out Material cached))
            {
                return cached;
            }

            Material material = BuildMaterialFromTexture(texturePath, useAlphaBlend);
            m_MaterialCache[cacheKey] = material;
            return material;
        }

        /// <summary>
        /// Laedt die EffectDefinition per ID aus dem EffectDataLoader.
        /// </summary>
        public EffectDefinition GetDefinition(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                return null;
            }

            EffectDataLoader loader = ServiceLocator.Get<EffectDataLoader>();
            return loader?.GetById(effectId);
        }

        /// <summary>
        /// Konfiguriert einen TrailRenderer anhand eines Tail-Segments aus der EffectDefinition.
        /// </summary>
        public void ConfigureTrailRenderer(TrailRenderer trail, EffectSegment segment)
        {
            if (trail == null || segment == null)
            {
                return;
            }

            trail.material = GetMaterial(segment.Texture);

            EffectTrailDefinition def = segment.Trail;
            if (def != null)
            {
                trail.time = def.Lifetime;
                trail.startWidth = def.StartWidth;
                trail.endWidth = def.EndWidth;
            }

            // Farbverlauf aus Segment-Daten
            Gradient gradient = BuildGradient(segment);
            trail.colorGradient = gradient;
            trail.minVertexDistance = 0.05f;
            trail.numCornerVertices = 2;
            trail.numCapVertices = 2;
            trail.textureMode = LineTextureMode.Stretch;
        }

        /// <summary>
        /// Konfiguriert ein ParticleSystem anhand eines Particle-Segments aus der EffectDefinition.
        /// Setzt Main, Emission, Shape, Color/Size over Lifetime, Renderer.
        /// </summary>
        public void ConfigureParticleSystem(ParticleSystem ps, EffectSegment segment)
        {
            if (ps == null || segment == null)
            {
                return;
            }

            // Blending: useAlpha-Flag aus Segment bestimmt Alpha- vs Additive-Blending
            bool useAlpha = segment.Flags != null && segment.Flags.Contains("useAlpha");

            EffectParticleDefinition def = segment.Particle;
            if (def == null && segment.Trail != null)
            {
                // Tail-Segments: ParticleSystem als gestreckte Funken/Rauchfaeden konfigurieren
                ConfigureTailAsParticleSystem(ps, segment, useAlpha);
                return;
            }
            if (def == null)
            {
                return;
            }

            // === Main Module ===
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(def.LifetimeMin, def.LifetimeMax);
            main.startSpeed = 0f; // Velocity ueber Velocity over Lifetime
            main.maxParticles = def.CountMax * 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = def.GravityModifier;

            // Rotation
            if (def.RotationMin != 0f || def.RotationMax != 0f)
            {
                main.startRotation = new ParticleSystem.MinMaxCurve(
                    def.RotationMin * Mathf.Deg2Rad,
                    def.RotationMax * Mathf.Deg2Rad);
            }

            // Rotation Speed
            if (def.RotationSpeedMin != 0f || def.RotationSpeedMax != 0f)
            {
                ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(
                    def.RotationSpeedMin * Mathf.Deg2Rad,
                    def.RotationSpeedMax * Mathf.Deg2Rad);
            }

            // Start Size
            EffectSizeDefinition size = segment.Size;
            if (size != null)
            {
                main.startSize = new ParticleSystem.MinMaxCurve(size.StartMin, size.StartMax);

                // Size over Lifetime
                ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
                sol.enabled = true;

                float avgStart = (size.StartMin + size.StartMax) * 0.5f;
                float avgEnd = (size.EndMin + size.EndMax) * 0.5f;
                float endRatio = avgStart > 0f ? avgEnd / avgStart : 1f;

                AnimationCurve sizeCurve = AnimationCurve.Linear(0f, 1f, 1f, endRatio);
                sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            }

            // === Emission Module ===
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;

            if (def.Burst)
            {
                // Explosions: Alle Partikel sofort als Burst spawnen
                emission.rateOverTime = 0f;
                emission.rateOverDistance = 0f;
                short burstCount = (short)Mathf.Max(1, (def.CountMin + def.CountMax) / 2);
                emission.SetBursts(new ParticleSystem.Burst[] { new(0f, burstCount) });

                // Start Delay fuer gestaffeltes Spawnen
                if (def.DelayMin > 0f || def.DelayMax > 0f)
                {
                    main.startDelay = new ParticleSystem.MinMaxCurve(def.DelayMin, def.DelayMax);
                }
            }
            else
            {
                // Trails: Kontinuierliche Emission ueber Distanz
                emission.rateOverTime = 0f;
                emission.rateOverDistance = (def.CountMin + def.CountMax) * 0.5f / 0.5f;
            }

            // === Shape Module (Spawn-Offset) ===
            ParticleSystem.ShapeModule shape = ps.shape;
            if (def.OriginMin != null && def.OriginMax != null && def.OriginMin.Length == 3)
            {
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                Vector3 originMin = new(def.OriginMin[0], def.OriginMin[1], def.OriginMin[2]);
                Vector3 originMax = new(def.OriginMax[0], def.OriginMax[1], def.OriginMax[2]);
                shape.scale = originMax - originMin;
                shape.position = (originMin + originMax) * 0.5f;
            }
            else
            {
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.01f;
            }

            // === Velocity over Lifetime ===
            if (def.VelocityMin != null && def.VelocityMax != null && def.VelocityMin.Length == 3)
            {
                ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.Local;
                vel.x = new ParticleSystem.MinMaxCurve(def.VelocityMin[0], def.VelocityMax[0]);
                vel.y = new ParticleSystem.MinMaxCurve(def.VelocityMin[1], def.VelocityMax[1]);
                vel.z = new ParticleSystem.MinMaxCurve(def.VelocityMin[2], def.VelocityMax[2]);
            }

            // === Color over Lifetime (Alpha-Fade) ===
            EffectAlphaDefinition alpha = segment.Alpha;
            if (alpha != null)
            {
                ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
                col.enabled = true;

                float fadeStart = alpha.Parm > 0 ? alpha.Parm / 100f : 0f;

                Gradient gradient = BuildGradient(segment);
                col.color = new ParticleSystem.MinMaxGradient(gradient);
            }

            // === Renderer ===
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = GetMaterial(segment.Texture, useAlpha);
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }
        }

        /// <summary>
        /// Konfiguriert ein ParticleSystem als gestreckte Partikel fuer Tail-Segmente
        /// (Einschlag-Funken, Rauchfaeden). Nutzt Trail-Daten fuer Lifetime, Count, Size, Velocity.
        /// </summary>
        private void ConfigureTailAsParticleSystem(ParticleSystem ps, EffectSegment segment, bool useAlpha)
        {
            EffectTrailDefinition trail = segment.Trail;

            // === Main Module ===
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = trail.Lifetime;
            main.startSpeed = 0f;
            main.maxParticles = Mathf.Max(trail.CountMax, 1) * 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Size: StartWidth als Partikelgroesse
            main.startSize = new ParticleSystem.MinMaxCurve(trail.StartWidth, trail.StartWidth * 1.5f);

            // === Emission (Burst) ===
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            short burstCount = (short)Mathf.Max(1, (trail.CountMin + trail.CountMax) / 2);
            emission.SetBursts(new ParticleSystem.Burst[] { new(0f, burstCount) });

            // === Shape ===
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;

            if (trail.Radius > 0f)
            {
                // Zylindrischer Spawn (Grass-Tails etc.)
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = trail.Radius;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.01f;
            }

            // === Velocity: Funken fliegen in Z-Richtung (entlang Surface-Normal) ===
            float speed = trail.Speed > 0f ? trail.Speed : 0.5f;
            float length = (trail.LengthMin + trail.LengthMax) * 0.5f;
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-speed * 0.5f, speed * 0.5f);
            vel.y = new ParticleSystem.MinMaxCurve(-speed * 0.5f, speed * 0.5f);
            vel.z = new ParticleSystem.MinMaxCurve(speed, speed + length);

            // === Size over Lifetime: von StartWidth zu EndWidth schrumpfen ===
            float endRatio = trail.StartWidth > 0f ? trail.EndWidth / trail.StartWidth : 0.2f;
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, endRatio));

            // === Alpha Fade ===
            EffectAlphaDefinition alpha = segment.Alpha;
            if (alpha != null)
            {
                ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient gradient = BuildGradient(segment);
                col.color = new ParticleSystem.MinMaxGradient(gradient);
            }

            // === Renderer: Stretched Billboard fuer Funkenlinien ===
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = GetMaterial(segment.Texture, useAlpha);
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = length > 0f ? length / Mathf.Max(trail.StartWidth, 0.001f) : 4f;
                renderer.velocityScale = 0.1f;
            }
        }

        /// <summary>
        /// Baut einen Unity-Farbverlauf aus den Segment-Daten (Color + Alpha).
        /// </summary>
        public Gradient BuildGradient(EffectSegment segment)
        {
            Gradient gradient = new();

            // Farbe
            Color startColor = Color.white;
            Color endColor = Color.white;

            EffectColorDefinition colorDef = segment.Color;
            if (colorDef != null)
            {
                if (colorDef.StartMin != null && colorDef.StartMin.Length >= 3)
                {
                    float[] c = colorDef.StartMin;
                    startColor = new Color(c[0], c[1], c[2]);
                }

                if (colorDef.EndMin != null && colorDef.EndMin.Length >= 3)
                {
                    float[] c = colorDef.EndMin;
                    endColor = new Color(c[0], c[1], c[2]);
                }
                else
                {
                    endColor = startColor;
                }
            }

            // Alpha
            float startAlpha = 1f;
            float endAlpha = 0f;
            float fadeStart = 0f;

            EffectAlphaDefinition alphaDef = segment.Alpha;
            if (alphaDef != null)
            {
                startAlpha = alphaDef.StartMin;
                endAlpha = alphaDef.EndMin;
                fadeStart = alphaDef.Parm > 0 ? alphaDef.Parm / 100f : 0f;
            }

            // Gradient Keys
            GradientColorKey[] colorKeys;
            GradientAlphaKey[] alphaKeys;

            if (colorDef?.EndMin != null)
            {
                colorKeys = new GradientColorKey[]
                {
                    new(startColor, 0f),
                    new(endColor, 1f)
                };
            }
            else
            {
                colorKeys = new GradientColorKey[]
                {
                    new(startColor, 0f),
                    new(startColor, 1f)
                };
            }

            if (fadeStart > 0f)
            {
                alphaKeys = new GradientAlphaKey[]
                {
                    new(startAlpha, 0f),
                    new(startAlpha, fadeStart),
                    new(endAlpha, 1f)
                };
            }
            else
            {
                alphaKeys = new GradientAlphaKey[]
                {
                    new(startAlpha, 0f),
                    new(endAlpha, 1f)
                };
            }

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Loesche den Material-Cache (z.B. bei Scene-Wechsel).
        /// </summary>
        public void ClearCache()
        {
            foreach (Material mat in m_MaterialCache.Values)
            {
                if (mat != null)
                {
                    Object.Destroy(mat);
                }
            }

            m_MaterialCache.Clear();
        }

        /// <summary>
        /// Baut ein Material aus einer SoF2-Textur via TextureManager.
        /// Nutzt URP Particles/Unlit Shader mit korrektem Blending (Alpha oder Additive).
        /// </summary>
        private Material BuildMaterialFromTexture(string texturePath, bool useAlphaBlend = false)
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();

            // URP Particle Shader bevorzugen, Built-in als Fallback
            Shader shader = Shader.Find(URP_PARTICLE_SHADER);
            if (shader == null)
            {
                shader = Shader.Find(BUILTIN_PARTICLE_SHADER);
            }
            if (shader == null)
            {
                Debug.LogWarning("[EffectFactory] No particle shader found, using fallback");
                return GetFallbackMaterial();
            }

            Material material = new(shader) { name = $"Effect_{texturePath}" };

            // Versuche Textur ueber TextureManager zu laden
            if (textureManager != null)
            {
                TextureData textureData = textureManager.GetTextureData(texturePath);
                if (textureData != null && textureData.HasTexture())
                {
                    material.mainTexture = textureData.Texture;
                }
                else
                {
                    Debug.LogWarning($"[EffectFactory] Texture not found: {texturePath}, using untextured material");
                }
            }

            // URP: Transparente Oberflaeche aktivieren
            material.SetFloat("_Surface", 1f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;

            if (useAlphaBlend)
            {
                // Alpha-Blending fuer Rauch, Staub, Partikel mit useAlpha-Flag
                material.SetFloat("_Blend", 0f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            else
            {
                // Additive Blending fuer Tracer, Flash, Feuer-Effekte
                material.SetFloat("_Blend", 2f);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
            }

            return material;
        }

        /// <summary>
        /// Fallback-Material wenn Shader/Textur nicht verfuegbar.
        /// </summary>
        private Material GetFallbackMaterial()
        {
            Shader fallback = Shader.Find("Sprites/Default");
            Material mat = new(fallback) { name = "Effect_Fallback" };
            mat.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            return mat;
        }

        /// <summary>
        /// Spawnt einen datengetriebenen Explosions-Effekt an der angegebenen Position.
        /// Erstellt ParticleSystems fuer jedes Segment und zerstoert sich nach Ablauf.
        /// </summary>
        public void SpawnExplosion(Vector3 position, string effectId)
        {
            EffectDefinition definition = GetDefinition(effectId);
            if (definition?.Segments == null || definition.Segments.Count == 0)
            {
                // Fallback: einfacher Licht-Flash
                SpawnFallbackExplosion(position);
                return;
            }

            GameObject explosionObj = new($"Explosion_{definition.DisplayName}");
            explosionObj.transform.position = position;

            float maxLifetime = 0f;
            bool hasLightSegment = false;

            foreach (EffectSegment segment in definition.Segments)
            {
                if (segment.Type == "particle" || segment.Type == "tail" || segment.Type == "orientedParticle" || segment.Type == "line")
                {
                    GameObject psGo = new(segment.Name ?? "Particle");
                    psGo.transform.SetParent(explosionObj.transform, false);

                    ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ConfigureParticleSystem(ps, segment);

                    // Render-Modus je nach Segment-Typ anpassen
                    ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (psRenderer != null)
                    {
                        if (segment.Type == "orientedParticle")
                        {
                            psRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                        }
                        else if (segment.Type == "line")
                        {
                            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                            psRenderer.lengthScale = 4f;
                        }
                    }

                    ps.Play();

                    float segLife = segment.Particle != null
                        ? segment.Particle.LifetimeMax + segment.Particle.DelayMax
                        : (segment.Trail?.Lifetime ?? 1f);
                    maxLifetime = Mathf.Max(maxLifetime, segLife);
                }
                else if (segment.Type == "decal")
                {
                    // Explosion-Decal: Raycast nach unten um Boden-Normal zu finden
                    if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit decalHit, 10f))
                    {
                        SpawnDecal(decalHit.point, decalHit.normal, segment);
                    }
                }
                else if (segment.Type == "light")
                {
                    hasLightSegment = true;
                    SpawnExplosionLight(explosionObj.transform, segment);
                }
                else if (segment.Type == "cameraShake")
                {
                    ApplyCameraShake(position, segment);
                }
            }

            // Fallback-Licht wenn kein Light-Segment vorhanden
            if (!hasLightSegment)
            {
                SpawnExplosionLight(explosionObj.transform, null);
            }

            Object.Destroy(explosionObj, maxLifetime + 1f);
        }

        /// <summary>
        /// Spawnt ein Decal (Scorch-Mark / Einschussloch) auf der Oberflaeche.
        /// Verwendet ein flaches Quad mit Alpha-Blending.
        /// Unterstuetzt feste Groesse (Size) und Groessen-Bereiche (SizeMin/SizeMax).
        /// </summary>
        private void SpawnDecal(Vector3 position, Vector3 normal, EffectSegment segment)
        {
            EffectDecalDefinition decal = segment.Decal;

            // Groesse: SizeMin/SizeMax (Impact) oder Size (Explosion) oder Fallback
            float size;
            if (decal != null && decal.SizeMin > 0f)
            {
                size = Random.Range(decal.SizeMin, decal.SizeMax);
            }
            else
            {
                size = decal?.Size ?? 4.572f;
            }

            float delay = decal?.Delay ?? 0f;
            float lifetime = decal?.Lifetime ?? 30f;
            float rotation = Random.Range(
                decal?.RotationMin ?? 0f,
                decal?.RotationMax ?? 360f);

            // Alpha: AlphaMin/AlphaMax oder volle Opazitaet
            float alpha = 1f;
            if (decal != null && decal.AlphaMin < 1f)
            {
                alpha = Random.Range(decal.AlphaMin, decal.AlphaMax);
            }

            GameObject decalObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decalObj.name = segment.Name ?? "Decal";

            // Collider entfernen (nur visuell)
            Collider col = decalObj.GetComponent<Collider>();
            if (col != null)
            {
                Object.Destroy(col);
            }

            // Quad auf Oberflaeche positionieren (leicht darueber fuer Z-Fighting)
            decalObj.transform.position = position + normal * 0.02f;

            // Quad-Face (-Z) zur Oberflaeche ausrichten
            Vector3 forward = -normal;
            Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.forward;
            decalObj.transform.rotation = Quaternion.LookRotation(forward, up)
                * Quaternion.Euler(0f, 0f, rotation);

            decalObj.transform.localScale = new Vector3(size, size, 1f);

            // Material: Alpha-Blending fuer Scorch / Einschussloch
            Renderer renderer = decalObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = GetDecalMaterial(segment.Texture);
                if (alpha < 1f)
                {
                    mat = new Material(mat);
                    Color matColor = mat.color;
                    matColor.a = alpha;
                    mat.color = matColor;
                }
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Start-Verzoegerung: initial unsichtbar, dann einblenden
            if (delay > 0f)
            {
                decalObj.SetActive(false);
                decalObj.SetActive(true);
            }

            Object.Destroy(decalObj, lifetime);
        }

        /// <summary>
        /// Erstellt ein Material mit Alpha-Blending fuer Decals (keine Additive-Blending).
        /// </summary>
        private Material GetDecalMaterial(string texturePath)
        {
            string decalKey = "decal_" + (texturePath ?? "");

            if (m_MaterialCache.TryGetValue(decalKey, out Material cached))
            {
                return cached;
            }

            Shader shader = Shader.Find("Sprites/Default");
            Material material = new(shader) { name = $"Decal_{texturePath}" };

            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager != null && !string.IsNullOrEmpty(texturePath))
            {
                TextureData textureData = textureManager.GetTextureData(texturePath);
                if (textureData != null && textureData.HasTexture())
                {
                    material.mainTexture = textureData.Texture;
                }
                else
                {
                    Debug.LogWarning($"[EffectFactory] Decal texture not found: {texturePath}");
                }
            }

            m_MaterialCache[decalKey] = material;
            return material;
        }

        /// <summary>
        /// Einfacher Fallback-Explosions-Effekt wenn keine Definition vorhanden.
        /// </summary>
        private static void SpawnFallbackExplosion(Vector3 position)
        {
            GameObject flashObj = new("Explosion_Fallback");
            flashObj.transform.position = position;

            Light flash = flashObj.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.6f, 0.1f);
            flash.intensity = 8f;
            flash.range = 10f;

            Object.Destroy(flashObj, 0.3f);
        }

        /// <summary>
        /// Spawnt ein datengetriebenes Explosions-Licht auf einem Child-GameObject.
        /// Wenn kein Segment vorhanden, werden Fallback-Werte verwendet.
        /// URP-kompatibel: Light auf eigenem GameObject, ganzes GO wird destroyed.
        /// </summary>
        private static void SpawnExplosionLight(Transform parent, EffectSegment segment)
        {
            EffectLightDefinition def = segment?.Light;

            float lifetime = def?.Lifetime ?? 0.15f;
            float range = def?.Range ?? 12f;
            float intensity = def?.Intensity ?? 8f;

            GameObject lightObj = new("ExplosionLight");
            lightObj.transform.SetParent(parent, false);

            Light flash = lightObj.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.6f, 0.1f);
            flash.intensity = intensity;
            flash.range = range;

            Object.Destroy(lightObj, lifetime);
        }

        /// <summary>
        /// Wendet CameraShake auf den lokalen Spieler an, basierend auf Entfernung zur Explosion.
        /// Findet den AimCameraController des lokalen Spielers und ruft AddExplosionShake auf.
        /// Intensitaet faellt linear mit Entfernung ab.
        /// </summary>
        private static void ApplyCameraShake(Vector3 explosionPosition, EffectSegment segment)
        {
            EffectCameraShakeDefinition def = segment?.CameraShake;
            if (def == null)
            {
                return;
            }

            // AimCameraController direkt suchen (lebt auf dem Player-Prefab,
            // nicht in Camera.main-Hierarchie — Cinemachine-Kameras sind virtuelle Kameras)
            AimCameraController controller = Object.FindAnyObjectByType<AimCameraController>();
            if (controller == null)
            {
                return;
            }

            float distance = Vector3.Distance(controller.transform.position, explosionPosition);

            // Ausserhalb des Radius: kein Shake
            if (def.Radius <= 0f || distance > def.Radius)
            {
                return;
            }

            // Intensitaet linear abfallend mit Entfernung
            float distanceRatio = 1f - (distance / def.Radius);
            float scaledIntensity = def.Intensity * distanceRatio;

            controller.AddExplosionShake(scaledIntensity, def.Duration);
        }

        /// <summary>
        /// Spawnt einen datengetriebenen Impact-Effekt an der Einschlagstelle.
        /// Partikel werden relativ zur Oberflaechen-Normalen orientiert (weg von der Wand).
        /// Erzeugt Staub, Funken, Decal etc. je nach Surface-Typ und Munitionstyp.
        /// </summary>
        public void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal, string effectId)
        {
            EffectDefinition definition = GetDefinition(effectId);
            if (definition?.Segments == null || definition.Segments.Count == 0)
            {
                return;
            }

            // Impact-Root: orientiert an der Oberflaechen-Normalen
            // Partikel-Velocities in der JSON sind in Lokal-Space definiert (Z = weg von Wand)
            GameObject impactObj = new($"Impact_{definition.DisplayName}");
            impactObj.transform.position = hitPoint;

            // Rotation: Z-Achse zeigt entlang der Surface-Normal (weg von der Wand)
            Vector3 upHint = Mathf.Abs(Vector3.Dot(hitNormal, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.forward;
            impactObj.transform.rotation = Quaternion.LookRotation(hitNormal, upHint);

            float maxLifetime = 0f;

            foreach (EffectSegment segment in definition.Segments)
            {
                if (segment.Type == "particle" || segment.Type == "orientedParticle"
                    || segment.Type == "line" || segment.Type == "tail")
                {
                    GameObject psGo = new(segment.Name ?? "ImpactParticle");
                    psGo.transform.SetParent(impactObj.transform, false);

                    ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ConfigureParticleSystem(ps, segment);

                    // Render-Modus je nach Segment-Typ
                    ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (psRenderer != null)
                    {
                        if (segment.Type == "orientedParticle")
                        {
                            psRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                        }
                        else if (segment.Type == "line")
                        {
                            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                            psRenderer.lengthScale = 4f;
                        }
                    }

                    ps.Play();

                    float segLife = segment.Particle != null
                        ? segment.Particle.LifetimeMax + segment.Particle.DelayMax
                        : (segment.Trail?.Lifetime ?? 1f);
                    maxLifetime = Mathf.Max(maxLifetime, segLife);
                }
                else if (segment.Type == "decal")
                {
                    SpawnDecal(hitPoint, hitNormal, segment);
                }
            }

            Object.Destroy(impactObj, maxLifetime + 1f);
        }

        /// <summary>
        /// Spawnt einen datengetriebenen Muzzle-Effekt (Flash oder Smoke) an Position/Rotation.
        /// Laedt EffectDefinition per effectId, erstellt ParticleSystems + Licht fuer jedes Segment.
        /// Falls keine EffectDefinition vorhanden: Spawnt Fallback (kurzer heller Flash + Punktlicht).
        /// </summary>
        public void SpawnMuzzleEffect(Vector3 position, Quaternion rotation, string effectId)
        {
            EffectDefinition definition = GetDefinition(effectId);
            if (definition?.Segments == null || definition.Segments.Count == 0)
            {
                SpawnFallbackMuzzleFlash(position);
                return;
            }

            GameObject effectObj = new($"MuzzleEffect_{definition.DisplayName}");
            effectObj.transform.position = position;
            effectObj.transform.rotation = rotation;

            float maxLifetime = 0f;

            foreach (EffectSegment segment in definition.Segments)
            {
                if (segment.Type is "particle" or "tail" or "orientedParticle" or "line")
                {
                    GameObject psGo = new(segment.Name ?? "MuzzleParticle");
                    psGo.transform.SetParent(effectObj.transform, false);

                    ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ConfigureParticleSystem(ps, segment);

                    ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                    if (psRenderer != null)
                    {
                        if (segment.Type == "orientedParticle")
                        {
                            psRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                        }
                        else if (segment.Type == "line")
                        {
                            psRenderer.renderMode = ParticleSystemRenderMode.Stretch;
                            psRenderer.lengthScale = 4f;
                        }
                    }

                    ps.Play();

                    float segLife = segment.Particle != null
                        ? segment.Particle.LifetimeMax + segment.Particle.DelayMax
                        : (segment.Trail?.Lifetime ?? 1f);
                    maxLifetime = Mathf.Max(maxLifetime, segLife);
                }
                else if (segment.Type == "light")
                {
                    SpawnExplosionLight(effectObj.transform, segment);
                }
            }

            Object.Destroy(effectObj, maxLifetime + 0.5f);
        }

        /// <summary>
        /// Fallback Muzzle-Flash wenn keine EffectDefinition vorhanden.
        /// Kurzer heller Flash + Punktlicht (SoF2-Stil: additive gelbe Partikel).
        /// </summary>
        private static void SpawnFallbackMuzzleFlash(Vector3 position)
        {
            GameObject flashObj = new("MuzzleFlash_Fallback");
            flashObj.transform.position = position;

            Light flash = flashObj.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.85f, 0.4f);
            flash.intensity = 5f;
            flash.range = 6f;

            Object.Destroy(flashObj, 0.08f);
        }

        /// <summary>
        /// Spawnt eine SoF2-authentische Patronenhuelse am Eject-Bone.
        /// Datengetrieben aus EffectDefinition (Emitter-Segment): Modell, Velocity, Spin, Bounce, Gravity.
        /// Laedt 3D-Modell via PrefabManager (Addressables-Key aus Emitter-Definition).
        /// Fallback: kleiner Quader mit Messing-Farbe.
        /// </summary>
        public void SpawnShellCasing(Vector3 position, Quaternion rotation, string effectId)
        {
            EffectDefinition definition = GetDefinition(effectId);
            EffectEmitterDefinition emitter = null;

            if (definition?.Segments != null)
            {
                foreach (EffectSegment segment in definition.Segments)
                {
                    if (segment.Type == "emitter" && segment.Emitter != null)
                    {
                        emitter = segment.Emitter;
                        break;
                    }
                }
            }

            GameObject shellObj = null;

            // Modell aus Emitter-Definition laden (Addressables-Key ohne .md3)
            if (emitter?.Models != null && emitter.Models.Count > 0)
            {
                string modelKey = emitter.Models[Random.Range(0, emitter.Models.Count)];
                PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();
                if (prefabManager != null)
                {
                    GameObject prefab = prefabManager.LoadPrefab<GameObject>(modelKey);
                    if (prefab != null)
                    {
                        shellObj = Object.Instantiate(prefab, position, rotation);
                    }
                }
            }

            // Fallback: kleiner Quader als Patronenhuelse
            if (shellObj == null)
            {
                shellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shellObj.name = "ShellCasing_Fallback";
                shellObj.transform.position = position;
                shellObj.transform.rotation = rotation;
                shellObj.transform.localScale = new Vector3(0.008f, 0.008f, 0.02f);

                Renderer renderer = shellObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = new Color(0.82f, 0.68f, 0.21f);
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }

            // Rigidbody fuer Physik-Simulation
            Rigidbody rb = shellObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = shellObj.AddComponent<Rigidbody>();
            }

            rb.mass = 0.01f;
            rb.linearDamping = 0.3f;
            rb.angularDamping = 0.3f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            if (emitter != null)
            {
                // Datengetriebene Auswurf-Geschwindigkeit aus Emitter-Definition (bereits m/s)
                Vector3 localVelocity = new(
                    Random.Range(emitter.VelocityMin[0], emitter.VelocityMax[0]),
                    Random.Range(emitter.VelocityMin[1], emitter.VelocityMax[1]),
                    Random.Range(emitter.VelocityMin[2], emitter.VelocityMax[2])
                );
                rb.linearVelocity = rotation * localVelocity;

                // Datengetriebener Spin aus angleDelta (Grad/s → Rad/s)
                rb.angularVelocity = new Vector3(
                    Random.Range(emitter.AngleDeltaMin[0], emitter.AngleDeltaMax[0]) * Mathf.Deg2Rad,
                    Random.Range(emitter.AngleDeltaMin[1], emitter.AngleDeltaMax[1]) * Mathf.Deg2Rad,
                    Random.Range(emitter.AngleDeltaMin[2], emitter.AngleDeltaMax[2]) * Mathf.Deg2Rad
                );

                // Datengetriebenes Bounce-Material
                Collider col = shellObj.GetComponent<Collider>();
                if (col != null)
                {
                    PhysicsMaterial shellMat = new()
                    {
                        bounciness = Random.Range(emitter.BounceMin, emitter.BounceMax),
                        dynamicFriction = 0.5f,
                        staticFriction = 0.5f,
                        bounceCombine = PhysicsMaterialCombine.Maximum
                    };
                    col.material = shellMat;
                }

                // SoF2-authentische Gravitation (SoF2 shells fallen ~1.5-2× schneller als Unity-Standard)
                float desiredGravity = Random.Range(emitter.GravityMin, emitter.GravityMax);
                float extraAcceleration = desiredGravity - Physics.gravity.y;
                if (Mathf.Abs(extraAcceleration) > 0.1f)
                {
                    ConstantForce cf = shellObj.AddComponent<ConstantForce>();
                    cf.force = new Vector3(0f, extraAcceleration * rb.mass, 0f);
                }

                // SoF2 impactFx: Beim ersten Aufprall Impact-Effekt spawnen
                // (z.B. shell_bouce_brass → LOD-Huelse die weiterhuepft)
                if (!string.IsNullOrEmpty(emitter.ImpactFx))
                {
                    bool impactKills = definition.Segments[0].Flags != null
                        && definition.Segments[0].Flags.Contains("impactKills");
                    ShellCasingBehaviour behaviour = shellObj.AddComponent<ShellCasingBehaviour>();
                    behaviour.Initialize(emitter.ImpactFx, impactKills);
                }

                // Lebensdauer aus Definition (gekappt auf 5s fuer Performance)
                float lifetime = Mathf.Min(
                    Random.Range(emitter.LifetimeMin, emitter.LifetimeMax), 5f);
                Object.Destroy(shellObj, lifetime);
            }
            else
            {
                // Fallback: Hardcoded SoF2 shell_brass Werte
                Vector3 localEjectVelocity = new(
                    Random.Range(50f, 100f) * SOF2_UNIT_SCALE,
                    Random.Range(-10f, 10f) * SOF2_UNIT_SCALE,
                    Random.Range(80f, 120f) * SOF2_UNIT_SCALE
                );
                rb.linearVelocity = rotation * localEjectVelocity;

                rb.angularVelocity = new Vector3(
                    Random.Range(50f, 100f) * Mathf.Deg2Rad,
                    Random.Range(20f, 50f) * Mathf.Deg2Rad,
                    0f
                );

                Collider col = shellObj.GetComponent<Collider>();
                if (col != null)
                {
                    PhysicsMaterial shellMat = new()
                    {
                        bounciness = 0.3f,
                        dynamicFriction = 0.5f,
                        staticFriction = 0.5f,
                        bounceCombine = PhysicsMaterialCombine.Maximum
                    };
                    col.material = shellMat;
                }

                Object.Destroy(shellObj, 3f);
            }
        }

        /// <summary>SoF2 Unit-Skalierung: 1 Quake Unit = 0.0254 Meter.</summary>
        private const float SOF2_UNIT_SCALE = 0.0254f;
    }
}