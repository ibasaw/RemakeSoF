using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tolik.RemakeSoF.Runtime.EffectManagement
{
    /// <summary>
    /// Root-Definition eines visuellen Effekts (z.B. Tracer, Projektil-Trail, MuzzleFlash).
    /// SoF2-Effekte bestehen aus einem oder mehreren Segmenten (Tail, Particle).
    /// Wird aus SoF2_Effects.json geladen.
    /// </summary>
    [Serializable]
    public class EffectDefinition
    {
        /// <summary>
        /// Eindeutige ID des Effekts (z.B. "effects/tracerTest2", "effects/rpg7_trail").
        /// Entspricht dem Pfad in der Waffen-JSON.
        /// </summary>
        [JsonProperty("id")]
        public string Id;

        /// <summary>
        /// Anzeigename fuer Debug/Editor.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName;

        /// <summary>
        /// Liste der Effekt-Segmente. SoF2-Effekte koennen mehrere Primitives enthalten
        /// (z.B. rpg7_trail = Smoke-Particle + Flash-Tail + Flash-Particle).
        /// </summary>
        [JsonProperty("segments")]
        public List<EffectSegment> Segments;
    }

    /// <summary>
    /// Ein einzelnes Segment eines SoF2-Effekts.
    /// Entspricht einem Primitive-Block (Tail oder Particle) in der .efx-Datei.
    /// </summary>
    [Serializable]
    public class EffectSegment
    {
        /// <summary>
        /// Segment-Typ: "tail" (TrailRenderer) oder "particle" (ParticleSystem).
        /// </summary>
        [JsonProperty("type")]
        public string Type;

        /// <summary>
        /// Optionaler Name des Segments (aus SoF2 "name"-Feld).
        /// </summary>
        [JsonProperty("name")]
        public string Name;

        /// <summary>
        /// Textur-Pfad (SoF2-Referenz, z.B. "gfx/misc/jk_tracer", "gfx/misc/gb_smoke").
        /// </summary>
        [JsonProperty("texture")]
        public string Texture;

        /// <summary>
        /// SoF2-Flags: "useAlpha", "usePhysics", "expensivePhysics", "impactKills".
        /// </summary>
        [JsonProperty("flags")]
        public List<string> Flags;

        /// <summary>
        /// SoF2-SpawnFlags: "evenDistribution", "rgbComponentInterpolation".
        /// </summary>
        [JsonProperty("spawnFlags")]
        public List<string> SpawnFlags;

        /// <summary>
        /// Trail-spezifische Parameter (nur fuer type "tail").
        /// </summary>
        [JsonProperty("trail")]
        public EffectTrailDefinition Trail;

        /// <summary>
        /// Particle-spezifische Parameter (nur fuer type "particle").
        /// </summary>
        [JsonProperty("particle")]
        public EffectParticleDefinition Particle;

        /// <summary>
        /// Alpha-Fade-Konfiguration.
        /// </summary>
        [JsonProperty("alpha")]
        public EffectAlphaDefinition Alpha;

        /// <summary>
        /// Farb-Konfiguration (RGB-Bereiche).
        /// </summary>
        [JsonProperty("color")]
        public EffectColorDefinition Color;

        /// <summary>
        /// Groessen-Konfiguration (Start/End mit Min/Max-Bereichen).
        /// </summary>
        [JsonProperty("size")]
        public EffectSizeDefinition Size;

        /// <summary>
        /// Decal-Parameter (nur fuer type "decal").
        /// </summary>
        [JsonProperty("decal")]
        public EffectDecalDefinition Decal;

        /// <summary>
        /// Light-Parameter (nur fuer type "light").
        /// </summary>
        [JsonProperty("light")]
        public EffectLightDefinition Light;

        /// <summary>
        /// CameraShake-Parameter (nur fuer type "cameraShake").
        /// </summary>
        [JsonProperty("cameraShake")]
        public EffectCameraShakeDefinition CameraShake;
    }

    // ===== Tail (TrailRenderer) =====

    /// <summary>
    /// Trail-Parameter fuer Tail-Segmente (SoF2 Tail-Primitive → Unity TrailRenderer).
    /// </summary>
    [Serializable]
    public class EffectTrailDefinition
    {
        /// <summary>
        /// Lebensdauer des Trails in Sekunden (SoF2 life / 1000).
        /// </summary>
        [JsonProperty("lifetime")]
        public float Lifetime;

        /// <summary>
        /// Minimale Partikelanzahl fuer Tail-Bursts.
        /// </summary>
        [JsonProperty("countMin")]
        public int CountMin;

        /// <summary>
        /// Maximale Partikelanzahl fuer Tail-Bursts.
        /// </summary>
        [JsonProperty("countMax")]
        public int CountMax;

        /// <summary>
        /// Startbreite des Trails in Unity-Metern.
        /// </summary>
        [JsonProperty("startWidth")]
        public float StartWidth;

        /// <summary>
        /// Endbreite des Trails in Unity-Metern.
        /// </summary>
        [JsonProperty("endWidth")]
        public float EndWidth;

        /// <summary>
        /// Minimale Trail-Laenge in Unity-Metern (SoF2 length end min × 0.0254).
        /// </summary>
        [JsonProperty("lengthMin")]
        public float LengthMin;

        /// <summary>
        /// Maximale Trail-Laenge in Unity-Metern (SoF2 length end max × 0.0254).
        /// </summary>
        [JsonProperty("lengthMax")]
        public float LengthMax;

        /// <summary>
        /// Geschwindigkeit des Tracer-Effekts in m/s (SoF2 velocity × 0.0254).
        /// </summary>
        [JsonProperty("speed")]
        public float Speed;

        /// <summary>
        /// Spawn-Radius in Unity-Metern (SoF2 radius × 0.0254).
        /// </summary>
        [JsonProperty("radius")]
        public float Radius;
    }

    // ===== Particle (ParticleSystem) =====

    /// <summary>
    /// Particle-Parameter fuer Particle-Segmente (SoF2 Particle-Primitive → Unity ParticleSystem).
    /// Alle Werte sind bereits in Unity-Einheiten konvertiert.
    /// </summary>
    [Serializable]
    public class EffectParticleDefinition
    {
        /// <summary>
        /// Minimale Partikelanzahl pro Emission (SoF2 count min).
        /// </summary>
        [JsonProperty("countMin")]
        public int CountMin;

        /// <summary>
        /// Maximale Partikelanzahl pro Emission (SoF2 count max).
        /// </summary>
        [JsonProperty("countMax")]
        public int CountMax;

        /// <summary>
        /// Minimale Partikel-Lebensdauer in Sekunden (SoF2 life min / 1000).
        /// </summary>
        [JsonProperty("lifetimeMin")]
        public float LifetimeMin;

        /// <summary>
        /// Maximale Partikel-Lebensdauer in Sekunden (SoF2 life max / 1000).
        /// </summary>
        [JsonProperty("lifetimeMax")]
        public float LifetimeMax;

        /// <summary>
        /// Minimale Spawn-Verzoegerung in Sekunden (SoF2 delay min / 1000).
        /// </summary>
        [JsonProperty("delayMin")]
        public float DelayMin;

        /// <summary>
        /// Maximale Spawn-Verzoegerung in Sekunden (SoF2 delay max / 1000).
        /// </summary>
        [JsonProperty("delayMax")]
        public float DelayMax;

        /// <summary>
        /// Minimale Start-Rotation in Grad (SoF2 rotation min).
        /// </summary>
        [JsonProperty("rotationMin")]
        public float RotationMin;

        /// <summary>
        /// Maximale Start-Rotation in Grad (SoF2 rotation max).
        /// </summary>
        [JsonProperty("rotationMax")]
        public float RotationMax;

        /// <summary>
        /// Minimale Rotationsgeschwindigkeit in Grad/Sek (SoF2 rotationDelta × 20fps).
        /// </summary>
        [JsonProperty("rotationSpeedMin")]
        public float RotationSpeedMin;

        /// <summary>
        /// Maximale Rotationsgeschwindigkeit in Grad/Sek (SoF2 rotationDelta × 20fps).
        /// </summary>
        [JsonProperty("rotationSpeedMax")]
        public float RotationSpeedMax;

        /// <summary>
        /// Unity ParticleSystem gravityModifier (SoF2 gravity QU/s² × 0.0254 / 9.81).
        /// </summary>
        [JsonProperty("gravityModifier")]
        public float GravityModifier;

        /// <summary>
        /// Spawn-Offset Minimum [x,y,z] in Unity-Metern (SoF2 origin min × 0.0254).
        /// </summary>
        [JsonProperty("originMin")]
        public float[] OriginMin;

        /// <summary>
        /// Spawn-Offset Maximum [x,y,z] in Unity-Metern (SoF2 origin max × 0.0254).
        /// </summary>
        [JsonProperty("originMax")]
        public float[] OriginMax;

        /// <summary>
        /// Geschwindigkeit Minimum [x,y,z] in m/s (SoF2 velocity min × 0.0254).
        /// </summary>
        [JsonProperty("velocityMin")]
        public float[] VelocityMin;

        /// <summary>
        /// Geschwindigkeit Maximum [x,y,z] in m/s (SoF2 velocity max × 0.0254).
        /// </summary>
        [JsonProperty("velocityMax")]
        public float[] VelocityMax;

        /// <summary>
        /// Burst-Modus: Alle Partikel sofort spawnen (Explosionen).
        /// Bei false wird rateOverDistance genutzt (Trail-Effekte).
        /// </summary>
        [JsonProperty("burst")]
        public bool Burst;
    }

    // ===== Shared Sub-Definitions =====

    /// <summary>
    /// Alpha-Konfiguration (SoF2 alpha-Block).
    /// </summary>
    [Serializable]
    public class EffectAlphaDefinition
    {
        /// <summary>
        /// Start-Alpha Minimum (0.0 - 1.0).
        /// </summary>
        [JsonProperty("startMin")]
        public float StartMin = 1f;

        /// <summary>
        /// Start-Alpha Maximum (0.0 - 1.0).
        /// </summary>
        [JsonProperty("startMax")]
        public float StartMax = 1f;

        /// <summary>
        /// End-Alpha Minimum (0.0 - 1.0).
        /// </summary>
        [JsonProperty("endMin")]
        public float EndMin;

        /// <summary>
        /// End-Alpha Maximum (0.0 - 1.0).
        /// </summary>
        [JsonProperty("endMax")]
        public float EndMax;

        /// <summary>
        /// SoF2 parm: Prozent der Lebensdauer bei dem der Uebergang beginnt (0-100).
        /// </summary>
        [JsonProperty("parm")]
        public int Parm;

        /// <summary>
        /// Verlaufskurve: "linear", "nonlinear", "linear nonlinear".
        /// </summary>
        [JsonProperty("curve")]
        public string Curve;
    }

    /// <summary>
    /// Farb-Konfiguration (SoF2 rgb-Block) mit Min/Max-Bereichen fuer Start und End.
    /// RGB-Werte 0.0-1.0.
    /// </summary>
    [Serializable]
    public class EffectColorDefinition
    {
        /// <summary>
        /// Start-Farbe Minimum [r,g,b] (0.0-1.0).
        /// </summary>
        [JsonProperty("startMin")]
        public float[] StartMin;

        /// <summary>
        /// Start-Farbe Maximum [r,g,b] (0.0-1.0).
        /// </summary>
        [JsonProperty("startMax")]
        public float[] StartMax;

        /// <summary>
        /// End-Farbe Minimum [r,g,b] (0.0-1.0).
        /// </summary>
        [JsonProperty("endMin")]
        public float[] EndMin;

        /// <summary>
        /// End-Farbe Maximum [r,g,b] (0.0-1.0).
        /// </summary>
        [JsonProperty("endMax")]
        public float[] EndMax;
    }

    /// <summary>
    /// Groessen-Konfiguration (SoF2 size-Block) mit Min/Max-Bereichen fuer Start und End.
    /// Alle Werte in Unity-Metern (SoF2 QU × 0.0254).
    /// </summary>
    [Serializable]
    public class EffectSizeDefinition
    {
        /// <summary>
        /// Start-Groesse Minimum in Unity-Metern.
        /// </summary>
        [JsonProperty("startMin")]
        public float StartMin;

        /// <summary>
        /// Start-Groesse Maximum in Unity-Metern.
        /// </summary>
        [JsonProperty("startMax")]
        public float StartMax;

        /// <summary>
        /// End-Groesse Minimum in Unity-Metern.
        /// </summary>
        [JsonProperty("endMin")]
        public float EndMin;

        /// <summary>
        /// End-Groesse Maximum in Unity-Metern.
        /// </summary>
        [JsonProperty("endMax")]
        public float EndMax;

        /// <summary>
        /// Verlaufskurve: "linear", "nonlinear".
        /// </summary>
        [JsonProperty("curve")]
        public string Curve;
    }

    // ===== Decal =====

    /// <summary>
    /// Decal-Parameter fuer Decal-Segmente (SoF2 Decal → Unity projected Quad auf Oberflaeche).
    /// Groessen in Unity-Metern (SoF2 QU × 0.0254).
    /// Unterstuetzt feste Groesse (Size, fuer Explosions-Scorch) und Groessen-Bereiche
    /// (SizeMin/SizeMax, fuer Impact-Decals wie Einschussloecher).
    /// </summary>
    [Serializable]
    public class EffectDecalDefinition
    {
        /// <summary>
        /// Spawn-Verzoegerung in Sekunden (SoF2 delay / 1000).
        /// </summary>
        [JsonProperty("delay")]
        public float Delay;

        /// <summary>
        /// Feste Groesse des Decals in Unity-Metern (SoF2 size start × 0.0254).
        /// Wird fuer Explosions-Scorch genutzt. Bei Impact-Decals stattdessen SizeMin/SizeMax verwenden.
        /// </summary>
        [JsonProperty("size")]
        public float Size;

        /// <summary>
        /// Minimale Decal-Groesse in Unity-Metern (Impact-Decals).
        /// Wenn groesser 0, wird Random.Range(SizeMin, SizeMax) statt Size verwendet.
        /// </summary>
        [JsonProperty("sizeMin")]
        public float SizeMin;

        /// <summary>
        /// Maximale Decal-Groesse in Unity-Metern (Impact-Decals).
        /// </summary>
        [JsonProperty("sizeMax")]
        public float SizeMax;

        /// <summary>
        /// Minimale Alpha-Transparenz des Decals (0.0-1.0). Standard 1.0 (volle Opazitaet).
        /// </summary>
        [JsonProperty("alphaMin")]
        public float AlphaMin = 1f;

        /// <summary>
        /// Maximale Alpha-Transparenz des Decals (0.0-1.0). Standard 1.0.
        /// </summary>
        [JsonProperty("alphaMax")]
        public float AlphaMax = 1f;

        /// <summary>
        /// Minimale Rotation in Grad.
        /// </summary>
        [JsonProperty("rotationMin")]
        public float RotationMin;

        /// <summary>
        /// Maximale Rotation in Grad.
        /// </summary>
        [JsonProperty("rotationMax")]
        public float RotationMax;

        /// <summary>
        /// Lebensdauer des Decals in Sekunden (wie lange der Scorch-Fleck sichtbar bleibt).
        /// </summary>
        [JsonProperty("lifetime")]
        public float Lifetime;
    }

    // ===== Light =====

    /// <summary>
    /// Light-Parameter fuer Light-Segmente (SoF2 Light → Unity Point Light).
    /// </summary>
    [Serializable]
    public class EffectLightDefinition
    {
        /// <summary>
        /// Lebensdauer des Lichts in Sekunden (SoF2 life / 1000).
        /// </summary>
        [JsonProperty("lifetime")]
        public float Lifetime;

        /// <summary>
        /// Licht-Reichweite in Unity-Metern (SoF2 size start × 0.0254).
        /// </summary>
        [JsonProperty("range")]
        public float Range;

        /// <summary>
        /// Licht-Intensitaet (0-20, Standard 8).
        /// </summary>
        [JsonProperty("intensity")]
        public float Intensity;
    }

    // ===== CameraShake =====

    /// <summary>
    /// CameraShake-Parameter fuer CameraShake-Segmente.
    /// Proximity-basiert: Kamera wird geschuettelt wenn Spieler innerhalb von Radius ist.
    /// </summary>
    [Serializable]
    public class EffectCameraShakeDefinition
    {
        /// <summary>
        /// Maximale Shake-Dauer in Sekunden (SoF2 life / 1000).
        /// </summary>
        [JsonProperty("duration")]
        public float Duration;

        /// <summary>
        /// Shake-Intensitaet / Amplitude (SoF2 bounce).
        /// </summary>
        [JsonProperty("intensity")]
        public float Intensity;

        /// <summary>
        /// Effektiver Radius in Unity-Metern (SoF2 radius × 0.0254).
        /// Spieler ausserhalb dieses Radius spueren keinen Shake.
        /// </summary>
        [JsonProperty("radius")]
        public float Radius;
    }
}
