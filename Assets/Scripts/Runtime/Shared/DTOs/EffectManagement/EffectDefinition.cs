using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tolik.RemakeSoF.Runtime.EffectManagement
{
    /// <summary>
    /// Root-Definition eines visuellen Effekts (z.B. Tracer, Projektil-Trail, MuzzleFlash).
    /// SoF2-Effekte bestehen aus einem oder mehreren Segmenten (Tail, Particle).
    /// Wird aus den JSON-Dateien im Effects/-Unterordner geladen.
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
        /// Textur-Pfade (SoF2-Referenzen). Manche Segmente haben mehrere Shaders
        /// (z.B. ["gfx/misc/bp_smoke01", "gfx/misc/bp_smoke02"]), andere nur einen.
        /// Der JsonConverter akzeptiert sowohl einen einzelnen String als auch ein Array.
        /// </summary>
        [JsonProperty("texture")]
        [JsonConverter(typeof(StringOrStringArrayConverter))]
        public List<string> Textures;

        /// <summary>
        /// Erster Textur-Pfad (Convenience fuer Single-Texture-Segmente).
        /// Bei Multi-Texture-Segmenten wird der erste Eintrag zurueckgegeben.
        /// </summary>
        [JsonIgnore]
        public string Texture => Textures != null && Textures.Count > 0 ? Textures[0] : null;

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

        /// <summary>
        /// Emitter-Parameter (nur fuer type "emitter").
        /// SoF2 Emitter-Primitive: physikalische Objekte mit 3D-Modellen (z.B. Patronenhuelsen).
        /// </summary>
        [JsonProperty("emitter")]
        public EffectEmitterDefinition Emitter;

        /// <summary>
        /// Sound-Parameter (nur fuer type "sound").
        /// SoF2 Sound-Primitive: spielt Audio ab bei Effekt-Ausloesung.
        /// </summary>
        [JsonProperty("sound")]
        public EffectSoundDefinition Sound;

        /// <summary>
        /// Impact-Effekt-Referenz (SoF2 impactfx). Sub-Effekt der bei Kollision spawnt.
        /// Nur fuer Particle- und Emitter-Segmente relevant.
        /// Bei Emitter-Segmenten wird impactFx stattdessen in EffectEmitterDefinition gespeichert.
        /// </summary>
        [JsonProperty("impactFx")]
        public string ImpactFx;

        /// <summary>
        /// PlayFx-Referenz (SoF2 fxRunner). Referenz auf einen Sub-Effekt der abgespielt wird.
        /// Nur fuer type "fxRunner" relevant.
        /// </summary>
        [JsonProperty("playFx")]
        public string PlayFx;

        /// <summary>
        /// FxRunner-Timing-Parameter (Delay, Count). Nur fuer type "fxRunner" relevant.
        /// </summary>
        [JsonProperty("runner")]
        public EffectFxRunnerDefinition Runner;

        /// <summary>
        /// Laengen-Konfiguration fuer Tail-Segmente (Start/End-Laenge mit Curve).
        /// Nur fuer type "tail" relevant.
        /// </summary>
        [JsonProperty("length")]
        public EffectLengthDefinition Length;
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

        /// <summary>
        /// Maximale Distanz in Metern, ab der Partikel nicht gerendert werden (SoF2 cullrange × 0.0254).
        /// </summary>
        [JsonProperty("cullRange")]
        public float CullRange;
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
        public float Parm;

        /// <summary>
        /// SoF2 parmMax: Obere Grenze fuer Parm-Randomisierung (0-100).
        /// Jedes Partikel waehlt einen zufaelligen Wert zwischen Parm und ParmMax.
        /// </summary>
        [JsonProperty("parmMax")]
        public float ParmMax;

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
        /// SoF2 parm: Prozent der Lebensdauer bei dem der Endwert erreicht wird (0-100).
        /// Nur relevant bei Curve = "clamp".
        /// </summary>
        [JsonProperty("parm")]
        public float Parm;

        /// <summary>
        /// SoF2 parmMax: Obere Grenze fuer Parm-Randomisierung (0-100).
        /// Jedes Partikel waehlt einen zufaelligen Wert zwischen Parm und ParmMax.
        /// </summary>
        [JsonProperty("parmMax")]
        public float ParmMax;

        /// <summary>
        /// Verlaufskurve: "linear", "nonlinear", "clamp".
        /// Bei "clamp" wird der Endwert bei Parm% der Lebensdauer erreicht und gehalten.
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

        /// <summary>
        /// Licht-Farbe als RGB-Array [r, g, b] (0.0-1.0).
        /// Fallback: warmes Orange (1.0, 0.6, 0.1) wenn nicht definiert.
        /// Beispiel: Flashbang=[1,1,1], Phosphor=[0.3,1,0.2], Standard=[1,0.6,0.1].
        /// </summary>
        [JsonProperty("color")]
        public float[] Color;
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

    // ===== Emitter (3D-Modell mit Physik) =====

    /// <summary>
    /// Emitter-Parameter fuer physikalische 3D-Objekte (SoF2 Emitter-Primitive).
    /// Verwendet fuer Patronenhuelsen und andere ausgeworfene Objekte mit Rigidbody-Physik.
    /// Alle Werte sind bereits in Unity-Einheiten konvertiert.
    /// </summary>
    [Serializable]
    public class EffectEmitterDefinition
    {
        /// <summary>
        /// Liste der Modell-Keys fuer Addressables (SoF2 models-Block, ohne .md3-Endung!).
        /// Bei mehreren Modellen wird zufaellig eines gewaehlt.
        /// </summary>
        [JsonProperty("models")]
        public List<string> Models;

        /// <summary>
        /// Auswurf-Geschwindigkeit Minimum [x,y,z] in m/s (SoF2 velocity min × 0.0254).
        /// Lokal-Space: x=rechts, y=oben, z=vorwaerts.
        /// </summary>
        [JsonProperty("velocityMin")]
        public float[] VelocityMin;

        /// <summary>
        /// Auswurf-Geschwindigkeit Maximum [x,y,z] in m/s (SoF2 velocity max × 0.0254).
        /// </summary>
        [JsonProperty("velocityMax")]
        public float[] VelocityMax;

        /// <summary>
        /// Rotations-Geschwindigkeit Minimum [pitch,yaw,roll] in Grad/Sekunde
        /// (SoF2 angleDelta min × 20 fps).
        /// </summary>
        [JsonProperty("angleDeltaMin")]
        public float[] AngleDeltaMin;

        /// <summary>
        /// Rotations-Geschwindigkeit Maximum [pitch,yaw,roll] in Grad/Sekunde
        /// (SoF2 angleDelta max × 20 fps).
        /// </summary>
        [JsonProperty("angleDeltaMax")]
        public float[] AngleDeltaMax;

        /// <summary>
        /// Gravitation Minimum in m/s² (SoF2 gravity min × 0.0254). Negativ = nach unten.
        /// </summary>
        [JsonProperty("gravityMin")]
        public float GravityMin;

        /// <summary>
        /// Gravitation Maximum in m/s² (SoF2 gravity max × 0.0254). Negativ = nach unten.
        /// </summary>
        [JsonProperty("gravityMax")]
        public float GravityMax;

        /// <summary>
        /// Abprall-Koeffizient Minimum (SoF2 bounce min, 0.0 - 1.0).
        /// </summary>
        [JsonProperty("bounceMin")]
        public float BounceMin;

        /// <summary>
        /// Abprall-Koeffizient Maximum (SoF2 bounce max, 0.0 - 1.0).
        /// </summary>
        [JsonProperty("bounceMax")]
        public float BounceMax;

        /// <summary>
        /// Minimale Lebensdauer in Sekunden (SoF2 life min / 1000).
        /// </summary>
        [JsonProperty("lifetimeMin")]
        public float LifetimeMin;

        /// <summary>
        /// Maximale Lebensdauer in Sekunden (SoF2 life max / 1000).
        /// </summary>
        [JsonProperty("lifetimeMax")]
        public float LifetimeMax;

        /// <summary>
        /// Maximale Sichtweite in Unity-Metern (SoF2 cullrange × 0.0254).
        /// </summary>
        [JsonProperty("cullRange")]
        public float CullRange;

        /// <summary>
        /// Minimale Anzahl an gleichzeitig gespawnten Emitter-Objekten (SoF2 count min).
        /// Standard 1 (z.B. eine einzelne Patronenhuelse).
        /// </summary>
        [JsonProperty("countMin")]
        public int CountMin = 1;

        /// <summary>
        /// Maximale Anzahl an gleichzeitig gespawnten Emitter-Objekten (SoF2 count max).
        /// Standard 1 (z.B. eine einzelne Patronenhuelse).
        /// </summary>
        [JsonProperty("countMax")]
        public int CountMax = 1;

        /// <summary>
        /// Optionale Referenz auf Impact-Effekt beim Aufprall (SoF2 impactfx).
        /// </summary>
        [JsonProperty("impactFx")]
        public string ImpactFx;

        /// <summary>
        /// Optionale Referenz auf Sub-Effekt der waehrend der Flugzeit emittiert wird (SoF2 emitfx).
        /// Wird periodisch am Emitter-Objekt gespawnt (z.B. Rauchschweif hinter Truemmer).
        /// </summary>
        [JsonProperty("emitFx")]
        public string EmitFx;
    }

    // ===== Sound =====

    /// <summary>
    /// Sound-Parameter fuer Sound-Segmente (SoF2 Sound-Primitive → Unity AudioSource.PlayOneShot).
    /// SoF2-Sounds werden relativ zur Effektposition als 3D-Sound abgespielt.
    /// </summary>
    [Serializable]
    public class EffectSoundDefinition
    {
        /// <summary>
        /// Pfade zu den Sound-Dateien (SoF2 sound-Block).
        /// Bei mehreren Pfaden wird zufaellig einer gewaehlt.
        /// </summary>
        [JsonProperty("files")]
        public List<string> Files;

        /// <summary>
        /// Spawn-Verzoegerung in Sekunden (SoF2 delay / 1000). 0 = sofort.
        /// </summary>
        [JsonProperty("delay")]
        public float Delay;
    }

    // ===== Length (Tail-Laenge) =====

    /// <summary>
    /// Laengen-Konfiguration fuer Tail-Segmente (SoF2 length-Block).
    /// Definiert Start- und End-Laenge des Trails mit optionaler Kurve.
    /// Alle Werte in Unity-Metern (SoF2 QU × 0.0254).
    /// </summary>
    [Serializable]
    public class EffectLengthDefinition
    {
        /// <summary>
        /// Start-Laenge Minimum in Unity-Metern.
        /// </summary>
        [JsonProperty("startMin")]
        public float StartMin;

        /// <summary>
        /// Start-Laenge Maximum in Unity-Metern.
        /// </summary>
        [JsonProperty("startMax")]
        public float StartMax;

        /// <summary>
        /// End-Laenge Minimum in Unity-Metern.
        /// </summary>
        [JsonProperty("endMin")]
        public float EndMin;

        /// <summary>
        /// End-Laenge Maximum in Unity-Metern.
        /// </summary>
        [JsonProperty("endMax")]
        public float EndMax;

        /// <summary>
        /// Verlaufskurve: "linear", "nonlinear", "clamp".
        /// </summary>
        [JsonProperty("curve")]
        public string Curve;
    }

    // ===== FxRunner =====

    /// <summary>
    /// Timing-Parameter fuer FxRunner-Segmente (SoF2 FxRunner-Primitive).
    /// FxRunner spawnen andere Effekte zeitversetzt (z.B. arterielle Blutspritzer-Sequenzen).
    /// </summary>
    [Serializable]
    public class EffectFxRunnerDefinition
    {
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
        /// Minimale Anzahl an Spawns (SoF2 count min). Standard 1.
        /// </summary>
        [JsonProperty("countMin")]
        public float CountMin = 1f;

        /// <summary>
        /// Maximale Anzahl an Spawns (SoF2 count max). Standard 1.
        /// </summary>
        [JsonProperty("countMax")]
        public float CountMax = 1f;
    }

    // ===== JsonConverter =====

    /// <summary>
    /// Konvertiert JSON-Werte die entweder ein einzelner String oder ein String-Array sein koennen
    /// zu einer List&lt;string&gt;.
    /// SoF2-Effekte haben manchmal eine einzelne Textur ("gfx/misc/jk_tracer")
    /// und manchmal ein Array von Texturen (["gfx/misc/bp_smoke01", "gfx/misc/bp_smoke02"]).
    /// </summary>
    public class StringOrStringArrayConverter : JsonConverter<List<string>>
    {
        /// <summary>
        /// Liest einen JSON-Wert der entweder ein String oder ein String-Array ist
        /// und gibt immer eine List&lt;string&gt; zurueck.
        /// </summary>
        public override List<string> ReadJson(JsonReader reader, Type objectType, List<string> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                return new List<string> { (string)reader.Value };
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                return serializer.Deserialize<List<string>>(reader);
            }

            throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing string or string array");
        }

        /// <summary>
        /// Schreibt die Liste als einzelnen String (bei einem Element) oder als Array.
        /// </summary>
        public override void WriteJson(JsonWriter writer, List<string> value, JsonSerializer serializer)
        {
            if (value == null || value.Count == 0)
            {
                writer.WriteNull();
            }
            else if (value.Count == 1)
            {
                writer.WriteValue(value[0]);
            }
            else
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}
