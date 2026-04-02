using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.EffectManagement;
using Tolik.RemakeSoF.Runtime.Game.Camera;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Tolik.RemakeSoF.Runtime.SoundManagement;
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
        private const string SOF2_TRAIL_SHADER = "SoF2/ProjectileTrail";
        private const string SOF2_EFFECT_PARTICLE_SHADER = "SoF2/EffectParticle";
        private const string SOF2_DECAL_SHADER = "SoF2/Decal";
        private const string SOF2_DISTORTION_SHADER = "SoF2/Distortion";

        private static Shader s_CachedParticleShader;
        private static Shader s_CachedTrailShader;
        private static Shader s_CachedDecalShader;
        private static Shader s_CachedDistortionShader;

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

            trail.material = GetTrailMaterial(segment.Texture);

            EffectTrailDefinition def = segment.Trail;
            if (def != null)
            {
                trail.time = def.Lifetime;

                // Breite: Segment-Size-Block bevorzugen (korrekte SoF2-Width).
                // trail.StartWidth speichert oft length.start (initiale Trail-LAENGE, nicht Breite).
                EffectSizeDefinition size = segment.Size;
                if (size != null && size.StartMax > 0f)
                {
                    trail.startWidth = (size.StartMin + size.StartMax) * 0.5f;
                    trail.endWidth = size.EndMax > 0f
                        ? (size.EndMin + size.EndMax) * 0.5f
                        : trail.startWidth * 0.1f;
                }
                else
                {
                    trail.startWidth = def.StartWidth;
                    trail.endWidth = def.EndWidth;
                }
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
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(def.LifetimeMin, def.LifetimeMax);
            main.startSpeed = 0f; // Velocity ueber Velocity over Lifetime
            main.maxParticles = def.CountMax * 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // SoF2 gravity ist als m/s² gespeichert (SoF2_val × 0.0254).
            // Die JSON-Daten haben inkonsistente Vorzeichen: Gore-Effekte nutzen negative Werte
            // (negativ = fallend), Impact-Effekte nutzen positive Werte (positiv = fallend).
            // Daher immer Absolutwert: alle Partikel fallen nach unten, nur die Staerke variiert.
            // Unity gravityModifier ist Multiplikator fuer Physics.gravity.y (-9.81).
            // Beispiel: |±5.08| / 9.81 = 0.518 → Partikel fallen mit ~52% Erdbeschleunigung.
            main.gravityModifier = Mathf.Approximately(Physics.gravity.y, 0f)
                ? 0f
                : Mathf.Abs(def.GravityModifier) / Mathf.Abs(Physics.gravity.y);

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
                float avgStart = (size.StartMin + size.StartMax) * 0.5f;
                float avgEnd = (size.EndMin + size.EndMax) * 0.5f;

                // SoF2: When startSize == 0 but endSize > 0, the particle grows from 0 to endSize.
                // Unity startSize=0 would make the particle invisible, so use endSize as startSize
                // and drive Size over Lifetime from 0 → 1.
                if (avgStart <= 0f && avgEnd > 0f)
                {
                    main.startSize = new ParticleSystem.MinMaxCurve(size.EndMin, size.EndMax);

                    ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
                    sol.enabled = true;
                    AnimationCurve sizeCurve = BuildSizeCurve(1f, size.Curve, size.Parm);
                    // Curve from 0 → 1: particle grows from invisible to full endSize
                    AnimationCurve growCurve = new(
                        new Keyframe(0f, 0f, 0f, 2f),
                        new Keyframe(1f, 1f, 0f, 0f));
                    sol.size = new ParticleSystem.MinMaxCurve(1f, growCurve);
                }
                else
                {
                    main.startSize = new ParticleSystem.MinMaxCurve(size.StartMin, size.StartMax);

                    ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
                    sol.enabled = true;
                    float endRatio = avgStart > 0f ? avgEnd / avgStart : 1f;

                    // Wachstum begrenzen: extrem hohe Ratios erzeugen uebergrosse Partikel
                    // die in Unity unnatuerlich wirken. SoF2-Partikel nutzten niedrigere Bildschirm-
                    // aufloesung und Q3-Rendering wo 20-30x weniger auffiel.
                    // Gore-/Rauch-Effekte: 10-15x ist natuerlich (Nebel waechst von mm auf 15-25cm).
                    if (endRatio > 20f)
                    {
                        endRatio = 20f;
                    }

                    AnimationCurve sizeCurve = BuildSizeCurve(endRatio, size.Curve, size.Parm);
                    sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
                }
            }

            // === Emission Module ===
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;

            // SoF2 impactFx: Sub-Effekt der bei Partikel-Kollision spawnt.
            // Da Unity kein Per-Particle-Collision-Spawning hat, emulieren wir das als
            // zeitversetzte Bursts die die Bluttropfen-Regenschauer-Optik nachbilden.
            bool isImpactSubEffect = segment.Flags != null && segment.Flags.Contains("impactFx")
                && def.CountMin == 0 && def.CountMax == 0;

            if (isImpactSubEffect)
            {
                // Sub-Effekt: 3-6 Tropfen, zeitversetzt ueber die Lebensdauer der Eltern-Partikel
                int subCount = Random.Range(3, 7);
                float parentLifetime = def.LifetimeMax > 0f ? def.LifetimeMax : 0.9f;

                emission.rateOverTime = 0f;
                emission.rateOverDistance = 0f;
                ParticleSystem.Burst[] subBursts = new ParticleSystem.Burst[subCount];
                for (int b = 0; b < subCount; b++)
                {
                    float t = (b + 1f) / (subCount + 1f) * parentLifetime * 0.7f;
                    subBursts[b] = new ParticleSystem.Burst(t, 1);
                }
                emission.SetBursts(subBursts);

                // Scatter-Velocity wenn keine definiert (Tropfen fallen aus der Blut-Wolke)
                if (def.VelocityMin == null || def.VelocityMax == null)
                {
                    ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
                    vel.enabled = true;
                    vel.space = ParticleSystemSimulationSpace.Local;
                    vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
                    vel.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.5f);
                    vel.z = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
                }

                // Spawn-Offset vergroessern damit Tropfen nicht alle am gleichen Punkt starten
                ParticleSystem.ShapeModule subShape = ps.shape;
                subShape.enabled = true;
                subShape.shapeType = ParticleSystemShapeType.Sphere;
                subShape.radius = 0.1f;

                main.maxParticles = subCount * 2;
            }
            else if (def.Burst)
            {
                float delaySpan = def.DelayMax - def.DelayMin;
                int avgCount = Mathf.Max(1, (def.CountMin + def.CountMax) / 2);

                // SoF2 semantics: each particle picks a random spawn time in [delayMin, delayMax].
                // When the delay range is wide, particles are spread continuously over that window.
                // Threshold: if delaySpan > particle lifetime, treat as continuous emission.
                if (delaySpan > main.startLifetime.constantMax && delaySpan > 0.5f)
                {
                    // Continuous emission over the delay window
                    float rate = avgCount / delaySpan;
                    emission.rateOverTime = rate;
                    emission.rateOverDistance = 0f;
                    main.startDelay = new ParticleSystem.MinMaxCurve(def.DelayMin);
                    main.duration = delaySpan;
                    main.loop = false;
                }
                else
                {
                    bool hasEvenDist = segment.SpawnFlags != null
                        && segment.SpawnFlags.Contains("evenDistribution");

                    if (hasEvenDist && avgCount > 1 && delaySpan > 0.01f)
                    {
                        // evenDistribution: Partikel gleichmaessig ueber Delay-Range verteilen
                        // statt alle gleichzeitig (z.B. konzentrische Wasserripples).
                        emission.rateOverTime = 0f;
                        emission.rateOverDistance = 0f;
                        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[avgCount];
                        float step = delaySpan / Mathf.Max(1, avgCount - 1);
                        for (int i = 0; i < avgCount; i++)
                        {
                            bursts[i] = new ParticleSystem.Burst(def.DelayMin + i * step, 1);
                        }
                        emission.SetBursts(bursts);
                    }
                    else
                    {
                        // Standard burst: all particles spawn at once
                        emission.rateOverTime = 0f;
                        emission.rateOverDistance = 0f;
                        short burstCount = (short)avgCount;
                        emission.SetBursts(new ParticleSystem.Burst[] { new(0f, burstCount) });

                        // Start Delay fuer gestaffeltes Spawnen
                        if (def.DelayMin > 0f || def.DelayMax > 0f)
                        {
                            main.startDelay = new ParticleSystem.MinMaxCurve(def.DelayMin, def.DelayMax);
                        }
                    }
                }
            }
            else
            {
                // SoF2 burst=false: Partikel werden zeitlich gestreut ueber die Delay-Range emittiert.
                // In SoF2 wählt jeder Partikel eine zufaellige Spawn-Zeit in [delayMin, delayMax].
                // Unity rateOverDistance funktioniert hier NICHT (Emitter bewegt sich nicht).
                // Stattdessen: Burst mit zeitlicher Verteilung ueber delaySpan.
                int avgCount = Mathf.Max(1, (def.CountMin + def.CountMax) / 2);
                float delaySpan = def.DelayMax - def.DelayMin;

                if (avgCount > 1 && delaySpan > 0.01f)
                {
                    // Partikel ueber die Delay-Range verteilen
                    emission.rateOverTime = 0f;
                    emission.rateOverDistance = 0f;
                    float rate = avgCount / delaySpan;
                    emission.rateOverTime = rate;
                    main.startDelay = new ParticleSystem.MinMaxCurve(def.DelayMin);
                    main.duration = delaySpan;
                    main.loop = false;
                }
                else
                {
                    // Wenig Partikel oder kein Delay-Spread: als Burst spawnen
                    emission.rateOverTime = 0f;
                    emission.rateOverDistance = 0f;
                    short burstCount = (short)avgCount;
                    emission.SetBursts(new ParticleSystem.Burst[] { new(def.DelayMin, burstCount) });
                }
            }

            // === Shape Module (Spawn-Offset) ===
            // SoF2→Unity Achsen-Remap auch fuer Origins: X→Z, Y→-X, Z→Y
            ParticleSystem.ShapeModule shape = ps.shape;
            if (def.OriginMin != null && def.OriginMax != null && def.OriginMin.Length == 3)
            {
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                Vector3 originMin = new(-def.OriginMax[1], def.OriginMin[2], def.OriginMin[0]);
                Vector3 originMax = new(-def.OriginMin[1], def.OriginMax[2], def.OriginMax[0]);
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
            // SoF2 Koordinaten: X=forward, Y=left, Z=up
            // Unity Local Space: X=right, Y=up, Z=forward (Z zeigt entlang Surface-Normal)
            // Remap: SoF2_X→Unity_Z, SoF2_Y→Unity_-X, SoF2_Z→Unity_Y
            //
            // WICHTIG: SoF2-Velocity ist initiale Geschwindigkeit (Impulse), keine permanente Kraft.
            // Unity VelocityOverLifetime mit Konstanten = permanente Geschwindigkeit → Partikel fliegen
            // ewig in eine Richtung. Loesung: Impulse-Decay-Kurve die bei 100% startet und
            // innerhalb von 15% der Lebenszeit auf 0 faellt. Danach wirkt nur noch Schwerkraft.
            if (def.VelocityMin != null && def.VelocityMax != null && def.VelocityMin.Length == 3)
            {
                ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.Local;

                // Impulse-Decay: Geschwindigkeit faellt von 100% auf 0% innerhalb der ersten 15% der Lebenszeit
                AnimationCurve decayMin = new(
                    new Keyframe(0f, 1f, 0f, -10f),
                    new Keyframe(0.15f, 0f, -0.5f, 0f),
                    new Keyframe(1f, 0f, 0f, 0f));
                AnimationCurve decayMax = new(
                    new Keyframe(0f, 1f, 0f, -10f),
                    new Keyframe(0.15f, 0f, -0.5f, 0f),
                    new Keyframe(1f, 0f, 0f, 0f));

                float xMin = -def.VelocityMax[1];
                float xMax = -def.VelocityMin[1];
                float yMin = def.VelocityMin[2];
                float yMax = def.VelocityMax[2];
                float zMin = def.VelocityMin[0];
                float zMax = def.VelocityMax[0];

                vel.x = new ParticleSystem.MinMaxCurve(1f,
                    ScaleCurve(decayMin, xMin),
                    ScaleCurve(decayMax, xMax));
                vel.y = new ParticleSystem.MinMaxCurve(1f,
                    ScaleCurve(decayMin, yMin),
                    ScaleCurve(decayMax, yMax));
                vel.z = new ParticleSystem.MinMaxCurve(1f,
                    ScaleCurve(decayMin, zMin),
                    ScaleCurve(decayMax, zMax));
            }

            // === Color over Lifetime (Alpha-Fade + rgbComponentInterpolation) ===
            EffectAlphaDefinition alpha = segment.Alpha;
            bool hasRgbInterp = segment.SpawnFlags != null
                && segment.SpawnFlags.Contains("rgbComponentInterpolation");

            if (alpha != null || (hasRgbInterp && segment.Color != null))
            {
                ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
                col.enabled = true;

                if (hasRgbInterp && HasColorRange(segment.Color))
                {
                    // rgbComponentInterpolation: Per-Partikel Farbvariation zwischen Min/Max-Bereichen.
                    // Unity waehlt pro Partikel einen zufaelligen Lerp-Faktor zwischen beiden Gradienten.
                    Gradient gradientMin = BuildGradientFromRange(segment, false);
                    Gradient gradientMax = BuildGradientFromRange(segment, true);
                    col.color = new ParticleSystem.MinMaxGradient(gradientMin, gradientMax);
                }
                else
                {
                    Gradient gradient = BuildGradient(segment);
                    col.color = new ParticleSystem.MinMaxGradient(gradient);
                }
            }

            // === Collision (usePhysics / expensivePhysics) ===
            bool hasPhysics = segment.Flags != null && segment.Flags.Contains("usePhysics");
            bool hasExpensivePhysics = segment.Flags != null && segment.Flags.Contains("expensivePhysics");
            bool hasImpactFx = segment.Flags != null && segment.Flags.Contains("impactFx");
            bool hasImpactKills = segment.Flags != null && segment.Flags.Contains("impactKills");
            if (hasPhysics || hasExpensivePhysics)
            {
                ParticleSystem.CollisionModule collision = ps.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.bounce = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                collision.dampen = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
                collision.quality = hasExpensivePhysics
                    ? ParticleSystemCollisionQuality.High
                    : ParticleSystemCollisionQuality.Medium;

                // Hitbox- und Player-Layer von Partikel-Kollision ausschliessen,
                // damit Blutpartikel nicht auf Hitbox-Collidern oder dem Spieler-Capsule
                // Aufprall-Decals erzeugen. Nur Welt-Geometrie soll Splats erhalten.
                int hitboxLayer = LayerMask.NameToLayer("Hitbox");
                int playerLayer = LayerMask.NameToLayer("Player");
                int excludeMask = 0;
                if (hitboxLayer >= 0) excludeMask |= (1 << hitboxLayer);
                if (playerLayer >= 0) excludeMask |= (1 << playerLayer);
                collision.collidesWith = ~excludeMask;

                // SoF2 impactKills: Partikel stirbt beim ersten Aufprall
                collision.lifetimeLoss = hasImpactKills ? 1.0f : 0.1f;

                // SoF2 impactFx: Collision-Messages aktivieren damit OnParticleCollision
                // in BloodDropCollisionBehaviour feuert und blood_splat_mp Decals spawnt
                if (hasImpactFx)
                {
                    collision.sendCollisionMessages = true;
                }
            }

            // === Renderer ===
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = GetMaterial(segment.Texture, useAlpha);
                renderer.renderMode = ParticleSystemRenderMode.Billboard;

                // depthHack: Render in front of viewmodel geometry (muzzle flashes)
                bool depthHack = segment.Flags != null && segment.Flags.Contains("depthHack");
                if (depthHack)
                {
                    renderer.sortingOrder = 10;
                    renderer.material.renderQueue = 3100;
                }
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
            main.loop = false;
            main.startLifetime = trail.Lifetime;
            main.startSpeed = 0f;
            main.maxParticles = Mathf.Max(trail.CountMax, 1) * 4;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Size: Segment-Size-Block bevorzugen (korrekte SoF2-Width).
            // trail.StartWidth speichert oft length.start (initiale Trail-LAENGE, nicht Breite).
            EffectSizeDefinition sizeBlock = segment.Size;
            float startWidth;
            float endWidth;
            if (sizeBlock != null && sizeBlock.StartMax > 0f)
            {
                startWidth = (sizeBlock.StartMin + sizeBlock.StartMax) * 0.5f;
                endWidth = sizeBlock.EndMax > 0f
                    ? (sizeBlock.EndMin + sizeBlock.EndMax) * 0.5f
                    : startWidth;
            }
            else
            {
                startWidth = trail.StartWidth > 0f ? trail.StartWidth : 0.05f;
                endWidth = trail.EndWidth > 0f ? trail.EndWidth : startWidth * 0.2f;
            }

            main.startSize = new ParticleSystem.MinMaxCurve(startWidth, startWidth * 1.5f);

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

            // === Size over Lifetime: von Start- zu End-Width schrumpfen ===
            float endRatio = startWidth > 0f ? endWidth / startWidth : 0.2f;
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
                renderer.lengthScale = length > 0f ? length / Mathf.Max(startWidth, 0.001f) : 4f;
                renderer.velocityScale = 0.1f;
            }
        }

        /// <summary>
        /// Skaliert alle Keyframe-Werte einer AnimationCurve mit einem Faktor.
        /// Wird fuer Impulse-Decay-Kurven verwendet: Decay-Kurvenwerte (0..1) × tatsaechliche Geschwindigkeit.
        /// </summary>
        private static AnimationCurve ScaleCurve(AnimationCurve source, float scale)
        {
            Keyframe[] keys = source.keys;
            Keyframe[] scaled = new Keyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                scaled[i] = new Keyframe(keys[i].time, keys[i].value * scale, keys[i].inTangent * scale, keys[i].outTangent * scale);
                scaled[i].weightedMode = keys[i].weightedMode;
                scaled[i].inWeight = keys[i].inWeight;
                scaled[i].outWeight = keys[i].outWeight;
            }
            return new AnimationCurve(scaled);
        }

        /// <summary>
        /// Baut eine AnimationCurve fuer Size-Over-Lifetime basierend auf SoF2 Curve-Typ und Parm.
        /// Bei "clamp" wird endRatio bei parm% der Lebensdauer erreicht und dann konstant gehalten.
        /// Bei "nonlinear" wird eine Ease-In/Out-Kurve erzeugt.
        /// Bei "linear" (oder Default) wird linear interpoliert.
        /// </summary>
        private AnimationCurve BuildSizeCurve(float endRatio, string curve, float parm)
        {
            bool isClamp = !string.IsNullOrEmpty(curve) &&
                           curve.Contains("clamp", System.StringComparison.OrdinalIgnoreCase);
            bool isNonlinear = !string.IsNullOrEmpty(curve) &&
                               curve.Contains("nonlinear", System.StringComparison.OrdinalIgnoreCase);

            if (isClamp && parm > 0 && parm <= 100)
            {
                float clampTime = parm / 100f;
                Keyframe k0 = new(0f, 1f) { outTangent = (endRatio - 1f) / clampTime };
                Keyframe k1 = new(clampTime, endRatio) { inTangent = (endRatio - 1f) / clampTime, outTangent = 0f };
                Keyframe k2 = new(1f, endRatio) { inTangent = 0f };
                return new AnimationCurve(k0, k1, k2);
            }

            if (isNonlinear)
            {
                return AnimationCurve.EaseInOut(0f, 1f, 1f, endRatio);
            }

            return AnimationCurve.Linear(0f, 1f, 1f, endRatio);
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
                if (colorDef.StartMin != null && colorDef.StartMin.Length >= 1)
                {
                    startColor = ColorFromArray(colorDef.StartMin);
                }

                if (colorDef.EndMin != null && colorDef.EndMin.Length >= 1)
                {
                    endColor = ColorFromArray(colorDef.EndMin);
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
        /// Baut einen Gradient fuer eine Seite des Min/Max-Farbbereichs (fuer rgbComponentInterpolation).
        /// useMax=false liefert StartMin/EndMin-Farben, useMax=true liefert StartMax/EndMax-Farben.
        /// Unity interpoliert pro Partikel zufaellig zwischen den beiden resultierenden Gradienten.
        /// </summary>
        private Gradient BuildGradientFromRange(EffectSegment segment, bool useMax)
        {
            Gradient gradient = new();

            Color startColor = Color.white;
            Color endColor = Color.white;

            EffectColorDefinition colorDef = segment.Color;
            if (colorDef != null)
            {
                float[] startArr = useMax ? colorDef.StartMax : colorDef.StartMin;
                float[] endArr = useMax ? colorDef.EndMax : colorDef.EndMin;

                if (startArr != null && startArr.Length >= 1)
                {
                    startColor = ColorFromArray(startArr);
                }

                if (endArr != null && endArr.Length >= 1)
                {
                    endColor = ColorFromArray(endArr);
                }
                else
                {
                    endColor = startColor;
                }
            }

            float startAlpha = 1f;
            float endAlpha = 0f;
            float fadeStart = 0f;

            EffectAlphaDefinition alphaDef = segment.Alpha;
            if (alphaDef != null)
            {
                startAlpha = useMax ? alphaDef.StartMax : alphaDef.StartMin;
                endAlpha = useMax ? alphaDef.EndMax : alphaDef.EndMin;
                fadeStart = alphaDef.Parm > 0 ? alphaDef.Parm / 100f : 0f;
            }

            float[] endCheck = useMax ? colorDef?.EndMax : colorDef?.EndMin;
            GradientColorKey[] colorKeys = endCheck != null
                ? new GradientColorKey[] { new(startColor, 0f), new(endColor, 1f) }
                : new GradientColorKey[] { new(startColor, 0f), new(startColor, 1f) };

            GradientAlphaKey[] alphaKeys = fadeStart > 0f
                ? new GradientAlphaKey[] { new(startAlpha, 0f), new(startAlpha, fadeStart), new(endAlpha, 1f) }
                : new GradientAlphaKey[] { new(startAlpha, 0f), new(endAlpha, 1f) };

            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        /// <summary>
        /// Erstellt eine Unity-Color aus einem float-Array (1 Element = Graustufe, 3 Elemente = RGB).
        /// </summary>
        private static Color ColorFromArray(float[] arr)
        {
            if (arr.Length == 1)
            {
                return new Color(arr[0], arr[0], arr[0]);
            }

            if (arr.Length == 2)
            {
                return new Color(arr[0], arr[1], 0f);
            }

            return new Color(arr[0], arr[1], arr[2]);
        }

        /// <summary>
        /// Prueft ob eine ColorDefinition unterschiedliche Min/Max-Bereiche hat.
        /// Gibt true zurueck wenn mindestens ein RGB-Kanal zwischen StartMin und StartMax variiert.
        /// </summary>
        private static bool HasColorRange(EffectColorDefinition colorDef)
        {
            if (colorDef == null)
            {
                return false;
            }

            if (colorDef.StartMin != null && colorDef.StartMax != null
                && colorDef.StartMin.Length >= 1 && colorDef.StartMax.Length >= 1)
            {
                int channels = Mathf.Min(colorDef.StartMin.Length, colorDef.StartMax.Length);
                for (int i = 0; i < channels; i++)
                {
                    if (Mathf.Abs(colorDef.StartMin[i] - colorDef.StartMax[i]) > 0.001f)
                    {
                        return true;
                    }
                }
            }

            if (colorDef.EndMin != null && colorDef.EndMax != null
                && colorDef.EndMin.Length >= 1 && colorDef.EndMax.Length >= 1)
            {
                int channels = Mathf.Min(colorDef.EndMin.Length, colorDef.EndMax.Length);
                for (int i = 0; i < channels; i++)
                {
                    if (Mathf.Abs(colorDef.EndMin[i] - colorDef.EndMax[i]) > 0.001f)
                    {
                        return true;
                    }
                }
            }

            return false;
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
        /// Gibt eine gecachte Trail-Material-Instanz fuer den angegebenen Textur-Pfad zurueck.
        /// Nutzt SoF2/ProjectileTrail Shader mit Soft-Edge und HDR-Core-Glow.
        /// Fallback auf Standard-Partikel-Material wenn Trail-Shader nicht verfuegbar.
        /// </summary>
        public Material GetTrailMaterial(string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
            {
                return GetFallbackMaterial();
            }

            string cacheKey = texturePath + "_trail";

            if (m_MaterialCache.TryGetValue(cacheKey, out Material cached))
            {
                return cached;
            }

            Material material = BuildTrailMaterial(texturePath);
            m_MaterialCache[cacheKey] = material;
            return material;
        }

        /// <summary>
        /// Baut ein Trail-Material mit SoF2/ProjectileTrail Shader.
        /// Soft Edges, heller Kern, HDR-Emission fuer Bloom.
        /// Fallback auf BuildMaterialFromTexture wenn Shader nicht verfuegbar.
        /// </summary>
        private Material BuildTrailMaterial(string texturePath)
        {
            if (s_CachedTrailShader == null)
            {
                s_CachedTrailShader = Shader.Find(SOF2_TRAIL_SHADER);
            }

            // Fallback: normales Partikel-Material wenn Trail-Shader fehlt
            if (s_CachedTrailShader == null)
            {
                return BuildMaterialFromTexture(texturePath, false);
            }

            TextureManager textureManager = ServiceLocator.Get<TextureManager>();

            Material material = new(s_CachedTrailShader) { name = $"Trail_{texturePath}" };

            if (textureManager != null)
            {
                TextureData textureData = textureManager.GetTextureData(texturePath);
                if (textureData != null && textureData.HasTexture())
                {
                    material.SetTexture("_BaseMap", textureData.Texture);
                }
            }

            material.SetColor("_BaseColor", Color.white);
            material.renderQueue = 3000;

            // Additive Blending (Standard fuer Tracer / Flash Trails)
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_Cull", 0f);

            return material;
        }

        /// <summary>
        /// Baut ein Material aus einer SoF2-Textur via TextureManager.
        /// Nutzt SoF2/EffectParticle Shader mit Soft Particles und HDR-Emission.
        /// Fallback auf URP Particles/Unlit wenn Custom-Shader nicht verfuegbar.
        /// </summary>
        private Material BuildMaterialFromTexture(string texturePath, bool useAlphaBlend = false)
        {
            TextureManager textureManager = ServiceLocator.Get<TextureManager>();

            // SoF2/EffectParticle bevorzugen, URP Particle als Fallback
            if (s_CachedParticleShader == null)
            {
                s_CachedParticleShader = Shader.Find(SOF2_EFFECT_PARTICLE_SHADER);
                if (s_CachedParticleShader == null)
                {
                    s_CachedParticleShader = Shader.Find(URP_PARTICLE_SHADER);
                    if (s_CachedParticleShader == null)
                    {
                        s_CachedParticleShader = Shader.Find(BUILTIN_PARTICLE_SHADER);
                    }
                }
            }
            Shader shader = s_CachedParticleShader;
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
                    // URP Particles/Unlit verwendet _BaseMap, nicht _MainTex
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", textureData.Texture);
                    }
                    else
                    {
                        material.mainTexture = textureData.Texture;
                    }
                }
                else
                {
                    // Soft-Circle-Fallback: ohne Textur wuerden Alpha-Partikel als solide
                    // Kreise/Kugeln gerendert. Default-Particle ist ein weiches Kreis-Sprite.
                    Texture2D fallbackTex = Resources.GetBuiltinResource<Texture2D>("Default-Particle.psd");
                    if (fallbackTex != null)
                    {
                        if (material.HasProperty("_BaseMap"))
                        {
                            material.SetTexture("_BaseMap", fallbackTex);
                        }
                        else
                        {
                            material.mainTexture = fallbackTex;
                        }
                    }
                    Debug.LogWarning($"[EffectFactory] Texture not found: {texturePath}, using soft particle fallback");
                }
            }

            // URP: Transparente Oberflaeche aktivieren
            material.SetFloat("_Surface", 1f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;

            // ZWrite OFF: Ohne dies schreiben Partikel in den Depth-Buffer und
            // schneiden dahinterliegende Partikel weg → sichtbare Loecher/Rechtecke
            material.SetFloat("_ZWrite", 0f);
            material.DisableKeyword("_ZWRITE_ON");

            // Backface-Culling OFF: Partikel muessen von allen Blickwinkeln sichtbar sein
            material.SetFloat("_Cull", 0f);

            // Grundfarbe Weiss mit vollem Alpha als Basis fuer ParticleSystem-Tinting
            material.SetColor("_BaseColor", Color.white);

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

            // HDR Emission: hoeher fuer additive Effekte (Tracer, Flash), neutral fuer Alpha (Rauch)
            if (material.HasProperty("_EmissionIntensity"))
            {
                material.SetFloat("_EmissionIntensity", useAlphaBlend ? 1.0f : 1.5f);
            }

            // Soft-Particle-Range reduzieren: Default 0.5m im Shader blendet Partikel
            // aus die naeher als 50cm an Szenen-Geometrie sind. Gore-Effekte spawnen
            // direkt an Bolt-Positionen (Koerperoberflaeche), depthDiff ≈ 0 → komplett
            // unsichtbar. 2cm reicht fuer sanfte Uebergaenge an Waenden/Boden.
            if (material.HasProperty("_SoftParticleRange"))
            {
                material.SetFloat("_SoftParticleRange", 0.02f);
            }

            return material;
        }

        /// <summary>
        /// Fallback-Material wenn Shader/Textur nicht verfuegbar.
        /// Nutzt SoF2/EffectParticle als Fallback (korrekte URP-Transparenz).
        /// </summary>
        private Material GetFallbackMaterial()
        {
            Shader fallback = Shader.Find(SOF2_EFFECT_PARTICLE_SHADER)
                ?? Shader.Find(URP_PARTICLE_SHADER)
                ?? Shader.Find(BUILTIN_PARTICLE_SHADER);
            Material mat = new(fallback) { name = "Effect_Fallback" };
            mat.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 0.8f));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.renderQueue = 3000;
            return mat;
        }

        /// <summary>
        /// Gibt ein gecachtes Distortion-Material fuer Heat-Haze-Effekte zurueck.
        /// Nutzt SoF2/Distortion Shader (Scene-Color Sampling + UV-Verzerrung).
        /// Wird von Explosion-Segmenten mit type="distortion" verwendet.
        /// </summary>
        public Material GetDistortionMaterial()
        {
            const string cacheKey = "_distortion_haze";

            if (m_MaterialCache.TryGetValue(cacheKey, out Material cached))
            {
                return cached;
            }

            if (s_CachedDistortionShader == null)
            {
                s_CachedDistortionShader = Shader.Find(SOF2_DISTORTION_SHADER);
            }

            if (s_CachedDistortionShader == null)
            {
                Debug.LogWarning("[EffectFactory] SoF2/Distortion shader not found.");
                return GetFallbackMaterial();
            }

            Material material = new(s_CachedDistortionShader) { name = "Distortion_HeatHaze" };
            material.renderQueue = 3050;
            m_MaterialCache[cacheKey] = material;
            return material;
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

            // SoF2 Effekt-Koordinaten: X = "forward" (Surface-Normal), Y/Z = perpendicular
            // Fuer Boden-Explosionen: X = nach oben, Y/Z = horizontal
            // Rotation so setzen, dass lokale Z-Achse (= JSON-Achse fuer Q3-X) nach oben zeigt
            // Damit werden Origin/Velocity korrekt orientiert (Feuer horizontal, Rauch steigt auf)
            explosionObj.transform.rotation = Quaternion.LookRotation(Vector3.up);

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
                else if (segment.Type == "emitter" && segment.Emitter != null)
                {
                    SpawnEmitterChunks(position, explosionObj.transform.rotation, segment.Emitter, segment.Flags);
                    float emitterLife = Mathf.Max(segment.Emitter.LifetimeMin, segment.Emitter.LifetimeMax);
                    maxLifetime = Mathf.Max(maxLifetime, emitterLife);
                }
                else if (segment.Type == "sound")
                {
                    PlayEffectSound(position, segment);
                }
                else if (segment.Type == "distortion")
                {
                    // Heat-Haze Distortion: ParticleSystem mit SoF2/Distortion Shader
                    GameObject distGo = new(segment.Name ?? "Distortion");
                    distGo.transform.SetParent(explosionObj.transform, false);

                    ParticleSystem distPs = distGo.AddComponent<ParticleSystem>();
                    distPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ConfigureParticleSystem(distPs, segment);

                    ParticleSystemRenderer distRenderer = distPs.GetComponent<ParticleSystemRenderer>();
                    if (distRenderer != null)
                    {
                        distRenderer.material = GetDistortionMaterial();
                        distRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                    }

                    distPs.Play();

                    float distLife = segment.Particle != null
                        ? segment.Particle.LifetimeMax + segment.Particle.DelayMax
                        : 1.5f;
                    maxLifetime = Mathf.Max(maxLifetime, distLife);
                }
            }

            // Fallback-Licht wenn kein Light-Segment vorhanden
            if (!hasLightSegment)
            {
                SpawnExplosionLight(explosionObj.transform, null);
            }

            // M84 Flashbang: Bildschirm-Flash fuer lokalen Spieler (SoF2 damageType "flash")
            if (effectId.Contains("stun_flash"))
            {
                FlashbangScreenEffect.TriggerFlash(position);
            }

            // Incendiary: persistentes Bodenfeuer nach Explosion (SoF2 ANM14)
            if (effectId.Contains("incendiary_explosion"))
            {
                SpawnGroundFire(position, "effects/fire/ground_fire", 10f);
            }

            // Phosphorus: brennende Chunks fliegen vom Explosionszentrum weg (SoF2 M15 WP)
            if (effectId.Contains("phosphorus_explosion"))
            {
                SpawnPhosphorusScatter(position, 8);
            }

            Object.Destroy(explosionObj, maxLifetime + 1f);
        }

        /// <summary>
        /// Spawnt einen persistenten Bodenfeuer-Effekt (SoF2 Incendiary/ANM14).
        /// Partikel-Segmente werden als Looping-ParticleSysteme konfiguriert und nach Ablauf der Dauer gestoppt.
        /// </summary>
        private void SpawnGroundFire(Vector3 position, string effectId, float duration)
        {
            EffectDefinition definition = GetDefinition(effectId);
            if (definition?.Segments == null || definition.Segments.Count == 0)
            {
                return;
            }

            GameObject fireObj = new($"GroundFire_{definition.DisplayName}");

            // Ground-snap: Raycast nach unten um Feuer auf den Boden zu setzen
            if (Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, 10f))
            {
                fireObj.transform.position = groundHit.point + Vector3.up * 0.05f;
            }
            else
            {
                fireObj.transform.position = position;
            }

            fireObj.transform.rotation = Quaternion.LookRotation(Vector3.up);

            foreach (EffectSegment segment in definition.Segments)
            {
                if (segment.Type != "particle" && segment.Type != "orientedParticle")
                {
                    continue;
                }

                GameObject psGo = new(segment.Name ?? "FireParticle");
                psGo.transform.SetParent(fireObj.transform, false);

                ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ConfigureParticleSystem(ps, segment);

                // Looping aktivieren: Partikel emittieren kontinuierlich fuer die Dauer
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.duration = duration;

                // Emission ueber die Dauer verteilen statt Burst-only
                ParticleSystem.EmissionModule emission = ps.emission;
                int avgCount = segment.Particle != null
                    ? Mathf.Max((segment.Particle.CountMin + segment.Particle.CountMax) / 2, 1)
                    : 5;
                float avgLifetime = segment.Particle != null
                    ? Mathf.Max(segment.Particle.LifetimeMax, 0.5f)
                    : 1f;
                emission.rateOverTime = avgCount / avgLifetime;

                ParticleSystemRenderer psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null && segment.Type == "orientedParticle")
                {
                    psRenderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                }

                ps.Play();
            }

            Object.Destroy(fireObj, duration + 2f);
        }

        /// <summary>
        /// Spawnt brennende Phosphor-Chunks die vom Explosionszentrum wegfliegen (SoF2 M15 WP).
        /// Jeder Chunk ist ein unsichtbares Physik-Objekt mit EmitFxBehaviour fuer Rauch-/Glutschweif.
        /// </summary>
        private void SpawnPhosphorusScatter(Vector3 position, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject chunkObj = new($"PhosphorusChunk_{i}");
                chunkObj.transform.position = position;

                SphereCollider sphere = chunkObj.AddComponent<SphereCollider>();
                sphere.radius = 0.05f;

                Rigidbody rb = chunkObj.AddComponent<Rigidbody>();
                rb.mass = 0.02f;
                rb.useGravity = true;
                rb.linearDamping = 0.3f;

                // Zufaellige Richtung nach oben/aussen
                Vector3 randomDir = Random.onUnitSphere;
                randomDir.y = Mathf.Abs(randomDir.y) + 0.3f;
                rb.linearVelocity = randomDir.normalized * Random.Range(3f, 8f);

                // Bounce-Material
                PhysicsMaterial chunkMat = new()
                {
                    bounciness = 0.3f,
                    dynamicFriction = 0.6f,
                    staticFriction = 0.6f,
                    bounceCombine = PhysicsMaterialCombine.Average
                };
                sphere.material = chunkMat;

                // EmitFx: Rauchschweif waehrend Flugzeit (phosphorus_chunk = Glut + Rauchpuff)
                float lifetime = Random.Range(2f, 4f);
                EmitFxBehaviour emitBehaviour = chunkObj.AddComponent<EmitFxBehaviour>();
                emitBehaviour.Initialize("effects/explosions/phosphorus_chunk", lifetime);

                Object.Destroy(chunkObj, lifetime);
            }
        }

        /// <summary>
        /// Spawnt einen datengetriebenen Distortion-Effekt (Heat-Haze) an der angegebenen Position.
        /// Erstellt ein ParticleSystem mit SoF2/Distortion Shader fuer UV-Verzerrung.
        /// Kann direkt aufgerufen werden (z.B. nach SpawnExplosion fuer zusaetzlichen Polish).
        /// </summary>
        public void SpawnDistortionEffect(Vector3 position, float size = 3f, float lifetime = 1.5f)
        {
            if (s_CachedDistortionShader == null)
            {
                s_CachedDistortionShader = Shader.Find(SOF2_DISTORTION_SHADER);
            }
            if (s_CachedDistortionShader == null)
            {
                return;
            }

            GameObject distObj = new("Distortion_HeatHaze");
            distObj.transform.position = position;

            ParticleSystem ps = distObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSize = size;
            main.startSpeed = 0.3f;
            main.maxParticles = 3;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.1f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new(0f, 2) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = size * 0.3f;

            // Groesse waechst, dann schrumpft
            ParticleSystem.SizeOverLifetimeModule sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.3f, 0f, 3f),
                new Keyframe(0.3f, 1f, 0f, 0f),
                new Keyframe(1f, 0.5f, -1f, 0f)));

            // Alpha-Fade
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient gradient = new();
            gradient.SetKeys(
                new GradientColorKey[] { new(Color.white, 0f), new(Color.white, 1f) },
                new GradientAlphaKey[] { new(0f, 0f), new(0.8f, 0.15f), new(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = GetDistortionMaterial();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            ps.Play();
            Object.Destroy(distObj, lifetime + 0.5f);
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
                size = decal?.Size ?? 0.3f;
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

            // Blut-Decals (Pool, Splat) muessen auf den Boden projiziert werden,
            // da sie an Bolt-Positionen (Bones) gespawnt werden, nicht an Oberflaechen.
            bool isBloodDecal = !string.IsNullOrEmpty(segment.Texture)
                && (segment.Texture.Contains("blood") || segment.Texture.Contains("bloody"));

            Vector3 decalPosition;
            Vector3 decalNormal;

            if (isBloodDecal)
            {
                // Raycast nach unten: Ground-Projektion fuer Blut-Pfuetzen
                int layerMask = ~LayerMask.GetMask("Hitbox");
                if (Physics.Raycast(position, Vector3.down, out RaycastHit groundHit, 5f, layerMask, QueryTriggerInteraction.Ignore))
                {
                    decalPosition = groundHit.point + groundHit.normal * 0.02f;
                    decalNormal = groundHit.normal;
                }
                else
                {
                    // Kein Boden gefunden: direkt unter der Position auf Y=0 projizieren
                    decalPosition = new Vector3(position.x, 0.02f, position.z);
                    decalNormal = Vector3.up;
                }
            }
            else
            {
                // Standard-Decals (Scorch, Einschuss): auf der Oberflaeche platzieren
                decalPosition = position + normal * 0.02f;
                decalNormal = normal;
            }

            decalObj.transform.position = decalPosition;

            // Quad-Face (-Z) zur Oberflaeche ausrichten
            Vector3 forward = -decalNormal;
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
                    Color matColor = mat.HasProperty("_BaseColor")
                        ? mat.GetColor("_BaseColor")
                        : Color.white;
                    matColor.a = alpha;
                    mat.SetColor("_BaseColor", matColor);
                }
                renderer.material = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Start-Verzoegerung: Renderer verstecken, DelayedActivation blendet spaeter ein
            if (delay > 0f)
            {
                Renderer rend = decalObj.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.enabled = false;
                }
                DelayedActivation activator = decalObj.AddComponent<DelayedActivation>();
                activator.Initialize(delay);
            }

            Object.Destroy(decalObj, delay + lifetime);
        }

        /// <summary>
        /// Erstellt ein Material mit SoF2/Decal Shader fuer Scorch-Marks und Einschusslocher.
        /// Alpha-Blending, Depth-Bias gegen Z-Fighting, kein Lighting.
        /// Fallback auf SoF2/EffectParticle wenn Decal-Shader nicht verfuegbar.
        /// </summary>
        private Material GetDecalMaterial(string texturePath)
        {
            string decalKey = "decal_" + (texturePath ?? "");

            if (m_MaterialCache.TryGetValue(decalKey, out Material cached))
            {
                return cached;
            }

            if (s_CachedDecalShader == null)
            {
                s_CachedDecalShader = Shader.Find(SOF2_DECAL_SHADER);
            }

            Shader shader = s_CachedDecalShader;
            if (shader == null)
            {
                Debug.LogWarning("[EffectFactory] SoF2/Decal shader not found, falling back to EffectParticle.");
                shader = Shader.Find(SOF2_EFFECT_PARTICLE_SHADER)
                    ?? Shader.Find(URP_PARTICLE_SHADER);
            }

            Material material = new(shader) { name = $"Decal_{texturePath}" };

            TextureManager textureManager = ServiceLocator.Get<TextureManager>();
            if (textureManager != null && !string.IsNullOrEmpty(texturePath))
            {
                TextureData textureData = textureManager.GetTextureData(texturePath);
                if (textureData != null && textureData.HasTexture())
                {
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTexture("_BaseMap", textureData.Texture);
                    }
                    else
                    {
                        material.mainTexture = textureData.Texture;
                    }
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

            // Farbe aus JSON-Definition oder Fallback (warmes Orange)
            Color lightColor;
            if (def?.Color != null && def.Color.Length >= 3)
            {
                lightColor = new Color(def.Color[0], def.Color[1], def.Color[2]);
            }
            else
            {
                lightColor = new Color(1f, 0.6f, 0.1f);
            }

            GameObject lightObj = new("ExplosionLight");
            lightObj.transform.SetParent(parent, false);

            Light flash = lightObj.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = lightColor;
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
        /// Spielt einen 3D-Sound an der angegebenen Position ab.
        /// Waehlt zufaellig eine der Sound-Dateien aus und spielt sie ueber eine temporaere AudioSource.
        /// Routet den Sound ueber die SFX AudioMixerGroup des SoundManagers.
        /// Optional verzoegert (SoF2 Sound delay).
        /// </summary>
        private static void PlayEffectSound(Vector3 position, EffectSegment segment)
        {
            EffectSoundDefinition def = segment?.Sound;
            if (def?.Files == null || def.Files.Count == 0)
            {
                Debug.LogWarning("[EffectFactory] PlayEffectSound: No sound files in segment.");
                return;
            }

            SoundManager soundManager = ServiceLocator.Get<SoundManager>();
            if (soundManager == null)
            {
                Debug.LogWarning("[EffectFactory] PlayEffectSound: SoundManager not found in ServiceLocator.");
                return;
            }

            // Zufaellige Datei waehlen (SoF2: mehrere Sounds werden alterniert)
            string soundPath = def.Files[Random.Range(0, def.Files.Count)];
            AudioClip clip = soundManager.GetClip(soundPath);
            if (clip == null)
            {
                Debug.LogWarning($"[EffectFactory] PlayEffectSound: Clip null for '{soundPath}'. HasSound={soundManager.HasSound(soundPath)}");
                return;
            }

            // Temporaeres GameObject mit AudioSource fuer 3D-Sound + Mixer-Routing
            GameObject soundObj = new("EffectSound");
            soundObj.transform.position = position;
            AudioSource source = soundObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = 1f;
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.minDistance = 3f;
            source.maxDistance = 40f;
            source.rolloffMode = AudioRolloffMode.Linear;

            // SFX Mixer Group zuweisen (falls vorhanden)
            if (soundManager.SfxGroup != null)
            {
                source.outputAudioMixerGroup = soundManager.SfxGroup;
            }

            if (def.Delay > 0f)
            {
                source.PlayDelayed(def.Delay);
                Object.Destroy(soundObj, def.Delay + clip.length + 0.5f);
            }
            else
            {
                source.Play();
                Object.Destroy(soundObj, clip.length + 0.5f);
            }
        }

        /// <summary>
        /// Spawnt einen datengetriebenen Impact-Effekt an der Einschlagstelle.
        /// Partikel werden relativ zur Oberflaechen-Normalen orientiert (weg von der Wand).
        /// Erzeugt Staub, Funken, Decal etc. je nach Surface-Typ und Munitionstyp.
        /// </summary>
        public void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal, string effectId)
        {
            SpawnImpactEffect(hitPoint, hitNormal, effectId, 1f);
        }

        /// <summary>
        /// Spawnt einen datengetriebenen Impact-Effekt mit optionalem Skalierungsfaktor.
        /// Partikel werden relativ zur Oberflaechen-Normalen orientiert (weg von der Wand).
        /// </summary>
        public void SpawnImpactEffect(Vector3 hitPoint, Vector3 hitNormal, string effectId, float scale)
        {
            EffectDefinition definition = GetDefinition(effectId);
            if (definition?.Segments == null || definition.Segments.Count == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[EffectFactory] SpawnImpactEffect: no definition or segments for effectId='{effectId}' (definition={definition != null}, segments={definition?.Segments?.Count ?? 0})");
#endif
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[EffectFactory] SpawnImpactEffect: effectId='{effectId}', displayName='{definition.DisplayName}', segments={definition.Segments.Count}, pos={hitPoint}");
#endif

            // Impact-Root: orientiert an der Oberflaechen-Normalen
            // Partikel-Velocities in der JSON sind in Lokal-Space definiert (Z = weg von Wand)
            GameObject impactObj = new($"Impact_{definition.DisplayName}");
            impactObj.transform.position = hitPoint;

            // Rotation: Z-Achse zeigt entlang der Surface-Normal (weg von der Wand)
            Vector3 upHint = Mathf.Abs(Vector3.Dot(hitNormal, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.forward;
            impactObj.transform.rotation = Quaternion.LookRotation(hitNormal, upHint);

            // Optionale Skalierung fuer groessere/kleinere Blutpfuetzen
            if (!Mathf.Approximately(scale, 1f))
            {
                impactObj.transform.localScale = Vector3.one * scale;
            }

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

                    // SoF2 impactFx: BloodDropCollisionBehaviour anhaengen damit
                    // Blutpartikel beim Bodenaufprall blood_splat_mp Decals spawnen
                    bool segmentHasImpactFx = segment.Flags != null && segment.Flags.Contains("impactFx");
                    bool segmentHasPhysics = segment.Flags != null && segment.Flags.Contains("usePhysics");
                    int segCount = segment.Particle?.CountMax ?? 0;
                    if (segmentHasImpactFx && segmentHasPhysics && segCount > 0)
                    {
                        BloodDropCollisionBehaviour collisionBehaviour = psGo.AddComponent<BloodDropCollisionBehaviour>();
                        collisionBehaviour.Initialize("effects/blood_splat_mp");
                    }

                    float segLife = segment.Particle != null
                        ? segment.Particle.LifetimeMax + segment.Particle.DelayMax
                        : (segment.Trail?.Lifetime ?? 1f);
                    maxLifetime = Mathf.Max(maxLifetime, segLife);
                }
                else if (segment.Type == "decal")
                {
                    SpawnDecal(hitPoint, hitNormal, segment);
                }
                else if (segment.Type == "sound")
                {
                    PlayEffectSound(hitPoint, segment);
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
                else if (segment.Type == "sound")
                {
                    PlayEffectSound(position, segment);
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
        /// Spawnt einen datengetriebenen Debris-Effekt (z.B. effects/chunks/debris_rock).
        /// Verarbeitet Partikel-Segmente (Rauch/Staub) und Emitter-Segmente (3D-Model-Chunks mit Physik).
        /// Wird von Surface-Impact-Code und Explosion-Code aufgerufen.
        /// Bei Bullet-Impact werden Emitter-Chunks (3D-Modelle) uebersprungen — nur Partikel spawnen.
        /// Fuer Explosionen: SpawnExplosion nutzt SpawnEmitterChunks direkt.
        /// </summary>
        public void SpawnDebris(Vector3 position, Quaternion rotation, string effectId)
        {
            EffectDefinition definition = GetDefinition(effectId);
            if (definition?.Segments == null || definition.Segments.Count == 0)
            {
                return;
            }

            GameObject debrisRoot = new($"Debris_{definition.DisplayName}");
            debrisRoot.transform.position = position;
            debrisRoot.transform.rotation = rotation;

            float maxLifetime = 0f;

            foreach (EffectSegment segment in definition.Segments)
            {
                if (segment.Type == "particle" || segment.Type == "tail" || segment.Type == "line")
                {
                    GameObject psGo = new(segment.Name ?? "Particle");
                    psGo.transform.SetParent(debrisRoot.transform, false);

                    ParticleSystem ps = psGo.AddComponent<ParticleSystem>();
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ConfigureParticleSystem(ps, segment);
                    ps.Play();

                    float segLife = segment.Particle != null
                        ? segment.Particle.LifetimeMax + segment.Particle.DelayMax
                        : (segment.Trail?.Lifetime ?? 1f);
                    maxLifetime = Mathf.Max(maxLifetime, segLife);
                }
                // Emitter-Chunks (3D-Modelle) bei Bullet-Impact uebersprungen:
                // Die Partikel-Segmente (bp_rock, Staub, Rauch) reichen als visuelles Feedback.
                // 3D-Modell-Chunks spawnen nur via SpawnExplosion → SpawnEmitterChunks direkt.
                else if (segment.Type == "sound")
                {
                    PlayEffectSound(position, segment);
                }
            }

            Object.Destroy(debrisRoot, maxLifetime + 1f);
        }

        /// <summary>
        /// Spawnt mehrere 3D-Model-Chunks mit Rigidbody-Physik aus einer Emitter-Definition.
        /// Jeder Chunk bekommt zufaellige Geschwindigkeit, Spin, Gravitation und Bounce aus den definierten Bereichen.
        /// Modell wird zufaellig aus der Modell-Liste gewaehlt und via PrefabManager geladen.
        /// Unterstuetzt emitFx (Sub-Effekt-Spawning waehrend Flugzeit) und expensivePhysics (Continuous Collision).
        /// </summary>
        private void SpawnEmitterChunks(Vector3 position, Quaternion rotation, EffectEmitterDefinition emitter, List<string> flags = null)
        {
            bool hasModels = emitter.Models != null && emitter.Models.Count > 0;
            bool hasEmitFx = !string.IsNullOrEmpty(emitter.EmitFx);

            // Ohne Modelle und ohne emitFx nichts zu tun
            if (!hasModels && !hasEmitFx)
            {
                return;
            }

            int count = Random.Range(emitter.CountMin, emitter.CountMax + 1);
            PrefabManager prefabManager = ServiceLocator.Get<PrefabManager>();

            for (int i = 0; i < count; i++)
            {
                GameObject chunkObj = null;

                // 3D-Modell laden wenn vorhanden
                if (hasModels)
                {
                    string modelKey = emitter.Models[Random.Range(0, emitter.Models.Count)];

                    if (prefabManager != null)
                    {
                        GameObject prefab = prefabManager.LoadPrefab<GameObject>(modelKey);
                        if (prefab != null)
                        {
                            chunkObj = Object.Instantiate(prefab, position, rotation);
                            PrefabTextureApplier.ApplyTextures(chunkObj);
                        }
                    }

                    // Fallback: Modell nicht gefunden — ueberspringen.
                    // Log ausgeben damit klar ist welches Modell fehlt (Addressable-Build pruefen!).
                    if (chunkObj == null)
                    {
                        Debug.LogWarning($"[EffectFactory] Emitter model not loaded: '{modelKey}' — check Addressables build. Skipping chunk.");
                        continue;
                    }
                }
                else
                {
                    // emitFx-only: unsichtbares Physik-Objekt als Traeger fuer Sub-Effekte
                    chunkObj = new($"EmitFx_Carrier_{i}");
                    chunkObj.transform.position = position;
                    chunkObj.transform.rotation = rotation;
                    SphereCollider sphere = chunkObj.AddComponent<SphereCollider>();
                    sphere.radius = 0.05f;
                }

                // Rigidbody fuer Physik-Simulation
                Rigidbody rb = chunkObj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = chunkObj.AddComponent<Rigidbody>();
                }

                rb.mass = 0.05f;
                rb.useGravity = true;
                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.2f;

                // expensivePhysics: hoehere Kollisionsgenauigkeit (SoF2 rpg7 chunks, shotgun shells)
                bool expensive = flags != null && flags.Contains("expensivePhysics");
                rb.collisionDetectionMode = expensive
                    ? CollisionDetectionMode.Continuous
                    : CollisionDetectionMode.Discrete;

                // Datengetriebene Geschwindigkeit
                // SoF2→Unity Achsen-Remap: X(forward)→Z, Y(left)→-X, Z(up)→Y
                Vector3 localVelocity = new(
                    -Random.Range(emitter.VelocityMin[1], emitter.VelocityMax[1]),
                    Random.Range(emitter.VelocityMin[2], emitter.VelocityMax[2]),
                    Random.Range(emitter.VelocityMin[0], emitter.VelocityMax[0])
                );
                rb.linearVelocity = rotation * localVelocity;

                // Datengetriebener Spin (Grad/s → Rad/s) — optional, nicht alle Emitter definieren angleDelta
                // SoF2→Unity Achsen-Remap: X→Z, Y→-X, Z→Y
                if (emitter.AngleDeltaMin != null && emitter.AngleDeltaMax != null)
                {
                    rb.angularVelocity = new Vector3(
                        -Random.Range(emitter.AngleDeltaMin[1], emitter.AngleDeltaMax[1]) * Mathf.Deg2Rad,
                        Random.Range(emitter.AngleDeltaMin[2], emitter.AngleDeltaMax[2]) * Mathf.Deg2Rad,
                        Random.Range(emitter.AngleDeltaMin[0], emitter.AngleDeltaMax[0]) * Mathf.Deg2Rad
                    );
                }

                // Datengetriebenes Bounce-Material
                Collider col = chunkObj.GetComponent<Collider>();
                if (col != null)
                {
                    PhysicsMaterial chunkMat = new()
                    {
                        bounciness = Random.Range(emitter.BounceMin, emitter.BounceMax),
                        dynamicFriction = 0.5f,
                        staticFriction = 0.5f,
                        bounceCombine = PhysicsMaterialCombine.Maximum
                    };
                    col.material = chunkMat;
                }

                // SoF2-Gravitation (oft staerker als Unity-Standard)
                float desiredGravity = Random.Range(emitter.GravityMin, emitter.GravityMax);
                float extraAcceleration = desiredGravity - Physics.gravity.y;
                if (Mathf.Abs(extraAcceleration) > 0.1f)
                {
                    ConstantForce cf = chunkObj.AddComponent<ConstantForce>();
                    cf.force = new Vector3(0f, extraAcceleration * rb.mass, 0f);
                }

                // Lebensdauer aus Definition (gekappt auf 5s fuer Performance)
                float lifetime = Mathf.Min(
                    Random.Range(emitter.LifetimeMin, emitter.LifetimeMax), 5f);

                // emitFx: Sub-Effekt waehrend Flugzeit spawnen (z.B. Rauchschweif hinter Truemmern)
                if (!string.IsNullOrEmpty(emitter.EmitFx))
                {
                    EmitFxBehaviour emitBehaviour = chunkObj.AddComponent<EmitFxBehaviour>();
                    emitBehaviour.Initialize(emitter.EmitFx, lifetime);
                }

                Object.Destroy(chunkObj, lifetime);
            }
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

            // Modell aus Emitter-Definition laden (Addressables-Key ohne .md3!)
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
                        PrefabTextureApplier.ApplyTextures(shellObj);
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
                // SoF2→Unity Achsen-Remap: X(forward)→Z, Y(left)→-X, Z(up)→Y
                Vector3 localVelocity = new(
                    -Random.Range(emitter.VelocityMin[1], emitter.VelocityMax[1]),
                    Random.Range(emitter.VelocityMin[2], emitter.VelocityMax[2]),
                    Random.Range(emitter.VelocityMin[0], emitter.VelocityMax[0])
                );
                rb.linearVelocity = rotation * localVelocity;

                // Datengetriebener Spin aus angleDelta (Grad/s → Rad/s)
                // SoF2→Unity Achsen-Remap: X→Z, Y→-X, Z→Y
                rb.angularVelocity = new Vector3(
                    -Random.Range(emitter.AngleDeltaMin[1], emitter.AngleDeltaMax[1]) * Mathf.Deg2Rad,
                    Random.Range(emitter.AngleDeltaMin[2], emitter.AngleDeltaMax[2]) * Mathf.Deg2Rad,
                    Random.Range(emitter.AngleDeltaMin[0], emitter.AngleDeltaMax[0]) * Mathf.Deg2Rad
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