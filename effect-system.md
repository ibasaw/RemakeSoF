# Visual Effect System

Daten-getriebenes Visual-Effect-System nach SoF2-Vorbild (`.efx`-Dateien → JSON).
Alle Effekte werden aus JSON-Definitionen geladen — keine hartkodierten Partikel-Prefabs.
Zentrale Factory (`EffectFactory`) baut ParticleSystems, TrailRenderer, Decals, Lights,
CameraShake und physikalische 3D-Emitter zur Laufzeit aus den Definitionen.

---

## Architektur-Überblick

```
JSON-Dateien (Resources/Data/ + Resources/Data/Effects/)
        ↓
EffectDataLoader (Pure Service, ServiceLocator)
  ├─ Lädt SoF2_Effects.json (Tracer, Trails, Explosionen)
  └─ Lädt ALLE JSON aus Effects/ (Impacts, Muzzle, Shell, Debris)
        ↓
EffectDefinition (DTO-Hierarchie)
  ├─ EffectSegment (Particle, Trail, Decal, Light, CameraShake, Emitter)
  ├─ EffectParticleDefinition
  ├─ EffectTrailDefinition
  ├─ EffectDecalDefinition
  ├─ EffectLightDefinition
  ├─ EffectCameraShakeDefinition
  └─ EffectEmitterDefinition (3D-Modelle mit Physik)
        ↓
EffectFactory (MonoBehaviour, ServiceLocator)
  ├─ SpawnImpactEffect()      → Einschlag-Partikel + Decal
  ├─ SpawnMuzzleEffect()      → Mündungsfeuer + Rauch
  ├─ SpawnShellCasing()       → Hülse (3D-Modell + Physik)
  ├─ SpawnDebris()            → Trümmer (Partikel + 3D-Chunks)
  ├─ SpawnExplosion()         → Explosion + Licht + CameraShake + Debris
  ├─ SpawnEmitterChunks()     → Physikalische 3D-Objekte (intern)
  └─ GetMaterial() / GetDefinition() → Material-Cache + Loader-Zugriff
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **EffectFactory** | `Assets/Scripts/Runtime/Game/Effects/EffectFactory.cs` | Zentrale Factory: baut ParticleSystems, Trails, Decals, Lights, Emitter aus JSON |
| **EffectDataLoader** | `Assets/Scripts/Runtime/Management/DataManagement/EffectDataLoader.cs` | Pure Service: JSON → EffectDefinition Dictionary |
| **EffectDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/EffectManagement/EffectDefinition.cs` | DTO-Hierarchie für alle Effekt-Typen |
| **SurfaceImpactDataLoader** | `Assets/Scripts/Runtime/Management/DataManagement/SurfaceImpactDataLoader.cs` | Pure Service: Surface + Ammo → Impact/Debris EffectId |
| **SurfaceTypeMarker** | `Assets/Scripts/Runtime/Game/Effects/SurfaceTypeMarker.cs` | MonoBehaviour-Marker: Oberflächen-Typ auf World-Geometry |
| **TracerVisual** | `Assets/Scripts/Runtime/Game/Effects/TracerVisual.cs` | Hitscan-Tracer (Trail von Start → End) |
| **ShellCasingBehaviour** | `Assets/Scripts/Runtime/Game/Effects/ShellCasingBehaviour.cs` | Hülsen-Physik + Impact-Bounce-Effekt |
| **ClientProjectileVisual** | `Assets/Scripts/Runtime/Game/Projectiles/ClientProjectileVisual.cs` | Client-seitige Projektil-Visuals (Trail, Modell, Explosion) |

---

## DTO-Struktur (EffectDefinition)

### EffectDefinition (Root)

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Id | `string` | Unique Key, z.B. `"effects/impact_concrete"` |
| DisplayName | `string` | Debug-Name |
| Segments | `List<EffectSegment>` | Liste der Effekt-Primitiven |

### EffectSegment

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Type | `string` | `"particle"`, `"orientedParticle"`, `"tail"`, `"decal"`, `"light"`, `"cameraShake"`, `"emitter"` |
| Name | `string` | Optionaler Segment-Name |
| Texture | `string` | SoF2 Textur-Pfad, z.B. `"gfx/misc/jk_mflash_m4"` |
| Flags | `List<string>` | `"useAlpha"`, `"usePhysics"`, `"expensivePhysics"`, `"impactKills"`, `"impactFx"` |
| Particle | `EffectParticleDefinition` | Partikel-Parameter |
| Trail | `EffectTrailDefinition` | Trail-Parameter |
| Alpha | `EffectAlphaDefinition` | Alpha-Verlauf |
| Color | `EffectColorDefinition` | Farb-Verlauf |
| Size | `EffectSizeDefinition` | Größen-Verlauf |
| Decal | `EffectDecalDefinition` | Decal-Parameter |
| Light | `EffectLightDefinition` | Licht-Parameter |
| CameraShake | `EffectCameraShakeDefinition` | Screen-Shake |
| Emitter | `EffectEmitterDefinition` | 3D-Modell-Physik |

### Segment-Typen im Detail

#### Particle / OrientedParticle

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| CountMin/Max | `int` | Anzahl Partikel |
| LifetimeMin/Max | `float` | Lebensdauer (Sekunden) |
| DelayMin/Max | `float` | Spawn-Verzögerung |
| RotationMin/Max | `float` | Start-Rotation (Grad) |
| RotationSpeedMin/Max | `float` | Rotationsgeschwindigkeit (°/s) |
| GravityModifier | `float` | Schwerkraft-Multiplikator |
| Burst | `bool` | Alle sofort spawnen (Explosion) vs. kontinuierlich |
| OriginMin/Max | `float[3]` | Lokaler Offset-Bereich (Spawn-Box) |
| VelocityMin/Max | `float[3]` | Geschwindigkeits-Range (3D) |

#### Trail (Typ "tail")

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| CountMin/Max | `int` | Anzahl Trails |
| Lifetime | `float` | Trail-Lebensdauer |
| StartWidth/EndWidth | `float` | Breite (Start → Ende) |
| LengthMin/Max | `float` | Länge |
| Speed | `float` | Geschwindigkeit |
| Radius | `float` | Spawning-Radius |

#### Decal

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Size / SizeMin/Max | `float` | Feste Größe oder Range |
| AlphaMin/Max | `float` | Decke/Transparenz (0.0–1.0) |
| RotationMin/Max | `float` | Zufällige Rotation |
| Delay | `float` | Verzögerung |
| Lifetime | `float` | Lebensdauer |

#### Light

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Lifetime | `float` | Dauer (Sekunden) |
| Range | `float` | Reichweite (Unity Meter) |
| Intensity | `float` | Licht-Intensität |

#### CameraShake

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Duration | `float` | Dauer |
| Intensity | `float` | Amplitude |
| Radius | `float` | Effektiver Radius |

#### Emitter (3D-Modelle mit Physik)

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Models | `List<string>` | Addressable-Keys für 3D-Modelle |
| CountMin/Max | `int` | Anzahl zu spawnender Objekte (default: 1) |
| VelocityMin/Max | `float[3]` | Geschwindigkeits-Range |
| AngleDeltaMin/Max | `float[3]` | Rotationsgeschwindigkeit (°/s) |
| GravityMin/Max | `float` | Schwerkraft-Range |
| BounceMin/Max | `float` | Bounce-Koeffizient (0.0–1.0) |
| LifetimeMin/Max | `float` | Lebensdauer |
| CullRange | `float` | Max. Sichtweite |
| ImpactFx | `string` | Optionaler Bounce-Effekt-ID |

---

## SoF2 → Unity Konvertierung

### Einheiten

| SoF2 | Unity | Faktor | Beispiel |
|------|-------|--------|----------|
| 1 QU (Quake Unit) | 0.0254 m | × 0.0254 | 100 QU = 2.54 m |
| Velocity (QU/s) | m/s | × 0.0254 | 1000 QU/s = 25.4 m/s |
| Lifetime (ms) | Sekunden | ÷ 1000 | 500 ms = 0.5 s |
| Size (QU) | Unity Meter | × 0.0254 | 4 QU = 0.1016 m |
| Gravity (QU/s²) | m/s² (negativ) | × 0.0254 | -800 QU/s² = -20.32 m/s² |

### .efx → JSON Konvertierungsmethodik

SoF2 verwendet `.efx`-Dateien (Text-basiert) in `effects/`-Ordnern.
Die Konvertierung zu JSON folgt dieser Methodik:

1. **Segment-Typen** werden 1:1 übernommen: `particle`, `tail`, `decal`, `emitter`
2. **Wertebereiche** (Min/Max) werden aus SoF2 `start`/`end` oder `min`/`max` Feldern gelesen
3. **Einheiten** werden mit obigen Faktoren umgerechnet
4. **Texturen** behalten den SoF2-Pfad (z.B. `"gfx/misc/bp_smoke01"`) — TextureManager löst auf
5. **Flags** werden als String-Array übernommen: `useAlpha`, `usePhysics`, `impactKills`, etc.
6. **Farben** werden von 0–255 auf 0.0–1.0 normalisiert

### Blending-Modi

| SoF2 Flag | Unity Shader Blend | Einsatz |
|-----------|-------------------|---------|
| (kein useAlpha) | Additive: SrcAlpha → One | Tracer, Mündungsfeuer, Feuer |
| `useAlpha` | Alpha: SrcAlpha → OneMinusSrcAlpha | Rauch, Staub, Funken |

**Shader:** `Universal Render Pipeline/Particles/Unlit` (Fallback: `Particles/Standard Unlit`)

---

## Effekt-Kategorien

### 1. Muzzle-Effekte (Mündung)

Beim Schuss wird pro Waffe ein Muzzle-Flash + Muzzle-Smoke gespawnt.
Die Effect-IDs kommen aus `WeaponAttackDefinition.MuzzleFlash` und `MuzzleSmoke`.

```
Waffe feuert (Server)
  └─ ProcessAttack()
       ├─ MuzzleFlash-ID aus WeaponAttackDefinition
       ├─ MuzzleSmoke-ID aus WeaponAttackDefinition
       └─ TracerClientRpc() sendet IDs an Clients
             ├─ EffectFactory.SpawnMuzzleEffect(pos, rot, muzzleFlashId)
             └─ EffectFactory.SpawnMuzzleEffect(pos, rot, muzzleSmokeId)
```

**JSON-Datei:** `SoF2_Effects_muzzle_flashes.json` — 9 Effekte

| Effect-ID | Waffe |
|-----------|-------|
| `effects/muzzle_flashes/mflash_m4` | M4 Carbine |
| `effects/muzzle_flashes/mflash_ak74` | AK-74 |
| `effects/muzzle_flashes/mflash_m590` | M590 Shotgun |
| `effects/muzzle_flashes/mflash_usas12` | USAS-12 |
| `effects/muzzle_flashes/mflash_m60` | M60 |
| `effects/muzzle_flashes/mflash_microuzi` | Micro Uzi |
| `effects/muzzle_flashes/mflash_m19` | M19 Pistol |
| `effects/muzzle_flashes/mflash_silvertalon` | Silver Talon |
| `effects/muzzle_flashes/mflash_mm1` | MM1 Grenade Launcher |

**JSON-Datei:** `SoF2_Effects_muzzle_smoke.json` — 5 Effekte

| Effect-ID | Waffe(n) |
|-----------|----------|
| `effects/muzzle_flashes/smoke_m4` | M4, AK-74, Micro Uzi, M19, Silver Talon |
| `effects/muzzle_flashes/smoke_ak74` | AK-74 (alternativ) |
| `effects/muzzle_flashes/smoke_mm1` | MM1 |
| `effects/muzzle_flashes/smoke_usas12` | USAS-12, M590 |
| `effects/muzzle_flashes/smoke_m60` | M60 |

---

### 2. Shell Casings (Hülsenauswurf)

Beim Schuss wird eine 3D-Hülse vom `EjectBone` der Waffe gespawnt.
Die Effect-ID kommt aus `WeaponAttackDefinition.ShellCasingEject`.

```
Waffe feuert (Client)
  └─ TracerClientRpc()
       └─ EffectFactory.SpawnShellCasing(ejectBonePos, ejectBoneRot, shellCasingEjectId)
             ├─ Segment Typ "emitter" → SpawnEmitterChunks()
             │   ├─ PrefabManager.Get(models[random])
             │   ├─ Rigidbody (velocity, angularVelocity, gravity)
             │   └─ ShellCasingBehaviour (impactFx, impactKills)
             └─ Hülse fällt, rotiert, prallt
                    └─ OnCollisionEnter → SpawnShellCasing(impactFx)
                           └─ Bounce-Partikel-Effekt (Funken/Klirren)
```

**JSON-Datei:** `SoF2_Effects_shell_casings.json` — 2 Effekte

| Effect-ID | Modell | Bounce-Effekt |
|-----------|--------|---------------|
| `effects/shell_brass` | `models/weapons/shells/shell_brass_hires` | `shells/shell_bouce_brass` |
| `effects/shell_shotgun` | `models/weapons/shells/shell_shotgun_hires` | `shells/shell_bouce_shotgun` |

**JSON-Datei:** `SoF2_Effects_shell_bounce.json` — 4 Effekte

| Effect-ID | Beschreibung |
|-----------|-------------|
| `shells/shell_bouce_brass` | Messing-Hülse prallt auf |
| `shells/shell_bouce_brass_large` | Große Messing-Hülse |
| `shells/shell_bouce_brass_small` | Kleine Messing-Hülse |
| `shells/shell_bouce_shotgun` | Schrotflinten-Hülse prallt auf |

### Emitter-Parameter (Shell Casings)

| Parameter | Brass | Shotgun | Einheit |
|-----------|-------|---------|---------|
| VelocityMin | [1.27, -0.254, 2.032] | [1.27, -0.254, 2.032] | m/s |
| VelocityMax | [2.54, 0.254, 3.048] | [2.54, 0.254, 3.048] | m/s |
| AngleDeltaMin | [1000, 400, 0] | [1000, 400, 0] | °/s |
| AngleDeltaMax | [2000, 1000, 0] | [2000, 1000, 0] | °/s |
| GravityMin/Max | -15.24 / -20.32 | -15.24 / -20.32 | m/s² |
| BounceMin/Max | 0.2 / 0.4 | 0.2 / 0.4 | Koeffizient |
| Lifetime | 3.0 s | 3.0 s | Sekunden |
| CullRange | 7.62 | 7.62 | Meter |

---

### 3. Impact-Effekte (Einschlag)

Pro Treffer wird ein oberflächen- und munitionsspezifischer Einschlag-Effekt gespawnt.
Die Zuordnung erfolgt über `SurfaceImpactDataLoader` aus `SoF2_data_per_surface.json`.

```
Server: Raycast trifft Geometry
  ├─ SurfaceTypeMarker.SurfaceType auslesen (z.B. "concrete")
  ├─ SurfaceImpactDataLoader.GetImpactEffectId(surfaceType, ammoType)
  │   Fallback-Kette: Exact → "default" → "effects/impact_default"
  ├─ SurfaceImpactDataLoader.GetDebrisEffectId(surfaceType, ammoType)
  └─ TracerClientRpc(start, end, hitNormal, tracerEffectId, impactEffectId, debrisEffectId)
        ↓
Client: TracerClientRpc()
  ├─ EffectFactory.SpawnImpactEffect(hitPoint, hitNormal, impactEffectId)
  │   ├─ Partikel-Segmente: Staub, Funken, Rauch (orientiert an hitNormal)
  │   └─ Decal-Segment: Einschussloch auf Oberfläche
  └─ EffectFactory.SpawnDebris(hitPoint, impactRotation, debrisEffectId)
       ├─ Partikel-Segmente: Staub-Puff
       └─ Emitter-Segmente: 3D-Chunks (Steine, Holzsplitter, etc.)
```

**JSON-Dateien:** 19 Impact-Dateien in `Resources/Data/Effects/`

| JSON-Datei | Oberflächen-Typ | Effekt-Anzahl |
|-----------|----------------|---------------|
| `SoF2_Effects_impact_concrete.json` | Beton | 4 |
| `SoF2_Effects_impact_metal.json` | Metall | 6+ |
| `SoF2_Effects_impact_wood.json` | Holz | 4 |
| `SoF2_Effects_impact_dirt.json` | Erde | — |
| `SoF2_Effects_impact_grass.json` | Gras | — |
| `SoF2_Effects_impact_short_grass.json` | Kurzes Gras | — |
| `SoF2_Effects_impact_gravel.json` | Kies | — |
| `SoF2_Effects_impact_rock.json` | Stein | — |
| `SoF2_Effects_impact_mud.json` | Schlamm | — |
| `SoF2_Effects_impact_ice.json` | Eis | — |
| `SoF2_Effects_impact_snow.json` | Schnee | — |
| `SoF2_Effects_impact_water.json` | Wasser | — |
| `SoF2_Effects_impact_glass.json` | Glas | — |
| `SoF2_Effects_impact_flesh.json` | Fleisch | — |
| `SoF2_Effects_impact_canvas.json` | Stoff | — |
| `SoF2_Effects_impact_leaves.json` | Blätter | — |
| `SoF2_Effects_impact_misc.json` | Sonstiges | — |
| `SoF2_Effects_impact_default.json` | Fallback | 1 |
| `SoF2_Effects_impact_knife.json` | Messer | 5 |

### Knife-Impact-Effekte

| Effect-ID | Oberfläche |
|-----------|-----------|
| `effects/impacts/knife_concrete` | Beton/Stein |
| `effects/impacts/knife_default` | Standard-Fallback |
| `effects/impacts/knife_dirt` | Erde |
| `effects/impacts/knife_flesh` | Fleisch |
| `effects/impacts/knife_metal` | Metall |

### SurfaceImpactDataLoader — Fallback-Kette

```csharp
// Für Impact-Effekte:
string GetImpactEffectId(string surfaceType, string ammoType)
// Für Debris-Effekte:
string GetDebrisEffectId(string surfaceType, string ammoType)

// Fallback-Logik (identisch für beide):
// 1. Exakte Suche: surfaceType + ammoType
// 2. Fallback: "default" + ammoType
// 3. Letzer Fallback: "effects/impact_default" (bzw. "" für Debris)
```

### SoF2_data_per_surface.json — Struktur

```json
{
  "default": {
    "density": 0.6,
    "loudness": 1,
    "footstep": { "sound": "sound/player/steps/concrete/concrete" },
    "ammoTypes": {
      "0.45 ACP": {
        "shellsound": "sound/player/bullet_impacts/casings/casing_default",
        "effect": "effects/impact_default",
        "debris": "effects/chunks/debris_rock"
      },
      "5.56mm": {
        "effect": "effects/impact_default",
        "debris": "effects/chunks/debris_rock"
      }
    }
  },
  "concrete": { ... },
  "metal": { ... }
}
```

---

### 4. Debris-System (Trümmer / Splitter)

Debris-Effekte bestehen aus zwei Teilen:
1. **Partikel-Segmente:** Staub-Puff (particle mit useAlpha)
2. **Emitter-Segmente:** Physikalische 3D-Chunks (Rigidbody, Gravity, Bounce)

```
EffectFactory.SpawnDebris(position, rotation, effectId)
  ├─ GetDefinition(effectId) → EffectDefinition
  ├─ Für jedes Segment:
  │   ├─ type == "particle" → ConfigureParticleSystem() → Staub-Puff
  │   └─ type == "emitter" → SpawnEmitterChunks()
  │         ├─ N = Random(CountMin, CountMax)
  │         ├─ Für jedes Chunk:
  │         │   ├─ PrefabManager.Get(models[random]) → 3D-Modell
  │         │   ├─ Rigidbody: velocity, angularVelocity, useGravity
  │         │   ├─ AutoDestroy nach Lifetime
  │         │   └─ Optional: ShellCasingBehaviour (wenn impactFx gesetzt)
  │         └─ Zufällige Werte aus Min/Max-Ranges
  └─ Ergebnis: Staubwolke + fliegende Trümmer-Stücke
```

#### Surface Debris (Einschlag-Trümmer)

**JSON-Datei:** `SoF2_Effects_debris_chunks.json` — 11 Effekte

| Effect-ID | Oberfläche | CountMin/Max |
|-----------|-----------|-------------|
| `effects/chunks/debris_rock` | Stein/Beton | 1–4 |
| `effects/chunks/debris_rock_small` | Kl. Stein | 1–3 |
| `effects/chunks/debris_wood` | Holz | 1–3 |
| `effects/chunks/debris_metal` | Metall | 1–3 |
| `effects/chunks/debris_hollowmetal` | Hohlmetall | 1–3 |
| `effects/chunks/debris_hollowwood` | Hohlholz | 1–3 |
| `effects/chunks/debris_glass` | Glas | 1–4 |
| `effects/chunks/debris_ice` | Eis | 1–3 |
| `effects/chunks/debris_snow` | Schnee | 1–3 |
| `effects/chunks/debris_computer` | Computer | 2–5 |
| `effects/chunks/debris_water` | Wasser | 1–2 |

Jeder Surface-Debris-Effekt enthält:
- 1× `particle`-Segment: Staub-Puff (`gfx/misc/bp_smoke01`, useAlpha)
- 1× `emitter`-Segment: 3D-Chunk-Modelle mit Physik

#### Explosion Debris (Explosions-Trümmer)

**JSON-Datei:** `SoF2_Effects_debris_explosion.json` — 3 Effekte

| Effect-ID | Beschreibung | CountMin/Max |
|-----------|-------------|-------------|
| `effects/debris_largeExplosion` | Große Explosion (RPG7) | 3–6 |
| `effects/debris_mediumExplosion` | Mittlere Explosion (MM1) | 2–4 |
| `effects/debris_smallExplosion` | Kleine Explosion (Granate) | 1–3 |

Explosions-Debris-Modelle werden zur Laufzeit aus der Umgebung bestimmt
(z.B. SurfaceType des Bodens). Das `models[]`-Array in der JSON ist leer —
der Game-Code füllt es basierend auf dem Kontext.

---

### 5. Tracer-System (Hitscan-Spuren)

Hitscan-Tracer fliegen visuell von Mündung zum Einschlagpunkt.
Die Effect-ID kommt aus `WeaponAttackDefinition.TracerEffect`.

```
Server: ProcessAttack() → Raycast
  └─ TracerClientRpc(serverStart, end, hitNormal, tracerEffectId, impactEffectId, debrisEffectId)
        ↓
Client: TracerVisual.Create(start, end, tracerEffectId)
  ├─ GetDefinition() → Sucht "tail"-Segment
  ├─ Daten-getriebener TrailRenderer (Lifetime, Width, Colors, Alpha)
  ├─ Rotation zum Ziel
  └─ Update(): Linearer Flug → Auto-Destroy nach Ankunft + Trail-Fadeout
```

**Fallback:** Gelb/Orange Standard-Tracer wenn keine Definition vorhanden.

**JSON-Datei:** `SoF2_Effects.json` — Tracer-Effekte

| Effect-ID | Beschreibung |
|-----------|-------------|
| `effects/tracerTest2` | Standard-Kugel-Tracer |

---

### 6. Projektil-Visuals (Trails + Modelle)

Projektile (RPG7, MM1, Granaten, Messer) haben eigene Client-seitige Visuals.

```
Server: ProcessProjectileAttack()
  └─ ProjectileSpawnClientRpc(pos, dir, speed, gravity, bounce, detonation,
                              timer, projectileId, effectId, explosionEffectId, modelKey)
        ↓
Client: ClientProjectileVisual
  ├─ TryLoadModel() → PrefabManager.Get(modelKey) → 3D-Modell
  ├─ CreateDataDrivenVisuals() → Trail + Partikel aus EffectDefinition
  ├─ CreateFallbackTrail() → Farbiger Trail wenn kein effektId
  ├─ Update(): SoF2-Physik (Gravity 20.32 m/s², Raycast, Bounce)
  ├─ Knife: Roll-Spin 1750°/s auf knifeworldbase-Bone
  └─ Detonate(): SpawnExplosion(explosionEffectId)
```

**JSON-Datei:** `SoF2_Effects.json` — Trail-Effekte

| Effect-ID | Projektil |
|-----------|----------|
| `effects/m203_trail` | M203 / MM1 Granate |
| `effects/grenade_trail` | F1 Handgranate |
| `effects/rpg7_trail` | RPG7 Rakete |

---

### 7. Explosions-Effekte

Explosionen bestehen aus Partikel-Segmenten + optionalen Decals, Light, CameraShake und Emitter.

```
EffectFactory.SpawnExplosion(position, effectId)
  ├─ GetDefinition(effectId)
  ├─ Für jedes Segment:
  │   ├─ "particle" → ConfigureParticleSystem()
  │   ├─ "decal" → SpawnDecal()
  │   ├─ "light" → SpawnExplosionLight()
  │   ├─ "cameraShake" → ApplyCameraShake()
  │   └─ "emitter" → SpawnEmitterChunks() (Explosions-Debris)
  └─ Ergebnis: Feuerball + Rauch + Scorch-Mark + Blitz + Screen-Shake + Trümmer
```

**JSON-Datei:** `SoF2_Effects.json` — Explosions-Effekte

| Effect-ID | Beschreibung |
|-----------|-------------|
| `effects/explosions/fragment_explosion` | Standard-Explostion (Granate) |
| `effects/explosions/m203_explosion` | M203/MM1 Explosion |
| `effects/explosions/mushroom_explosion_no_fire` | RPG7 Pilzwolke |

---

## EffectFactory — Implementierungsdetails

### Material-Caching

```csharp
// Einmaliges Erstellen, dann Cache:
Material GetMaterial(string texturePath, bool useAlphaBlend)
// Key: texturePath + "_alpha" oder texturePath + "_additive"
// Shader: Universal Render Pipeline/Particles/Unlit
// TextureManager löst SoF2-Pfade zu Unity-Texturen auf
```

### ParticleSystem-Konfiguration

`ConfigureParticleSystem()` setzt alle Unity ParticleSystem-Module:

| Modul | Quelle |
|-------|--------|
| Main | Lifetime, StartSize, StartRotation, GravityModifier, StartSpeed, SimulationSpace |
| Emission | Burst-Count oder Rate over Distance |
| Shape | Sphere/Cone basierend auf OriginMin/Max |
| Velocity over Lifetime | VelocityMin/Max → Random Between Two Constants |
| Color over Lifetime | BuildGradient() aus Color + Alpha Definitionen |
| Size over Lifetime | StartSize → EndSize Interpolation via BuildSizeCurve(). Unterstuetzt `clamp` (Endwert bei parm% erreicht und gehalten), `nonlinear` (EaseInOut), `linear` (Default) |

### CameraShake

```csharp
// Distanz-basiert: Intensität nimmt mit Entfernung ab
float distance = Vector3.Distance(position, camera.position);
if (distance < radius)
{
    float scale = 1f - (distance / radius);
    aimCamera.AddViewPunch(intensity * scale, duration);
}
```

---

## TracerClientRpc — Vollständige Signatur

```csharp
[Rpc(SendTo.Everyone)]
private void TracerClientRpc(
    Vector3 serverStart,     // Mündungsposition (Server)
    Vector3 end,             // Einschlagpunkt
    Vector3 hitNormal,       // Oberflächen-Normale
    string tracerEffectId,   // Tracer-Visual (Trail)
    string impactEffectId,   // Einschlag-Effekt (Partikel + Decal)
    string debrisEffectId    // Trümmer-Effekt (Chunks + Staub)
)
```

Der Client spawnt aus diesen 3 Effect-IDs:
1. **TracerVisual** → Trail von Start zu End
2. **SpawnImpactEffect** → Einschlag-Partikel + Decal am Einschlagpunkt
3. **SpawnDebris** → Trümmer-Chunks + Staub-Puff am Einschlagpunkt

---

## JSON-Datei-Inventar

### Haupt-Datei

| Datei | Pfad | Effekte |
|-------|------|---------|
| `SoF2_Effects.json` | `Resources/Data/` | 7 (Tracer, Trails, Explosionen) |

### Effects/-Unterordner

| Datei | Kategorie | Effekte |
|-------|-----------|---------|
| `SoF2_Effects_muzzle_flashes.json` | Mündungsfeuer | 9 |
| `SoF2_Effects_muzzle_smoke.json` | Mündungsrauch | 5 |
| `SoF2_Effects_shell_casings.json` | Hülsenauswurf | 2 |
| `SoF2_Effects_shell_bounce.json` | Hülsen-Aufprall | 4 |
| `SoF2_Effects_debris_chunks.json` | Surface-Debris | 11 |
| `SoF2_Effects_debris_explosion.json` | Explosions-Debris | 3 |
| `SoF2_Effects_impact_*.json` (19 Dateien) | Oberflächen-Einschläge | variabel |

**Gesamt:** 25 JSON-Dateien mit 40+ individuellen Effekt-Definitionen.

---

## Datenfluss-Diagramm (Gesamtübersicht)

```
┌──────────────────────────────────────────────────────────────┐
│                    JSON-Definitionen                          │
│  SoF2_Weapons_New.json    SoF2_Effects.json                  │
│  SoF2_data_per_surface.json   Effects/*.json (25 Dateien)    │
└───────────────────────────┬──────────────────────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         ▼                  ▼                  ▼
  WeaponDataLoader   EffectDataLoader   SurfaceImpactDataLoader
  (Waffen-Defs)     (Effekt-Defs)      (Surface→Effect Mapping)
         │                  │                  │
         └──────────────────┼──────────────────┘
                            │
                            ▼
              NetworkedPlayerCharacter (Server)
              ├─ ProcessAttack() / ProcessAltAttack()
              │   ├─ Raycast → SurfaceTypeMarker
              │   ├─ tracerEffectId  ← WeaponAttackDefinition
              │   ├─ impactEffectId  ← SurfaceImpactDataLoader
              │   ├─ debrisEffectId  ← SurfaceImpactDataLoader
              │   └─ TracerClientRpc(6 Parameter)
              │
              └─ ProcessProjectileAttack()
                  └─ ProjectileSpawnClientRpc(11 Parameter)
                            │
                            ▼
              ┌─────────────────────────────┐
              │      Client (alle Spieler)   │
              │                              │
              │  TracerClientRpc:            │
              │  ├─ TracerVisual.Create()    │
              │  ├─ SpawnMuzzleEffect() ×2   │
              │  ├─ SpawnShellCasing()       │
              │  ├─ SpawnImpactEffect()      │
              │  └─ SpawnDebris()            │
              │                              │
              │  ProjectileSpawnClientRpc:   │
              │  └─ ClientProjectileVisual   │
              │      ├─ 3D-Modell + Trail    │
              │      └─ Detonate() →         │
              │          SpawnExplosion()     │
              └─────────────────────────────┘
```

---

## Bekannte Abweichungen von SoF2

| Aspekt | SoF2 Original | Unity Umsetzung |
|--------|--------------|-----------------|
| Effekt-Format | `.efx` Textdateien | JSON-Dateien |
| Rendering | Software/OpenGL Sprites | URP ParticleSystem + TrailRenderer |
| Decals | BSP Surface Marks | Projected Quads mit Alpha-Blending |
| Emitter-Physik | Eigene Physik-Engine | Unity Rigidbody + Collider |
| Licht | Vertex-basiert | URP Point Light |
| CameraShake | cl_screen.c | AimCameraController.AddViewPunch() |
| Textur-Lookup | WAL/TGA direkt | TextureManager mit Addressables-Bridge |
| Sound | Inline in .efx | Noch nicht implementiert (TODO) |

---

## Performance-Hinweise

- **Material-Cache:** Materialien werden einmalig erstellt und wiederverwendet.
  `ClearCache()` zerstört alle bei Scene-Wechsel.
- **ParticleSystem-Pooling:** Aktuell kein Pooling — jeder Effekt instanziiert neue GameObjects.
  Bei vielen gleichzeitigen Treffern kann das zu GC-Spikes führen.
- **Emitter-Chunks:** 3D-Modelle werden per Addressables geladen (async, gecacht via PrefabManager).
  Erste Nutzung kann Frame-Drop verursachen.
- **Tracer-Lifetime:** TracerVisual zerstört sich selbst nach Ankunft + Trail-Fadeout.
  Kein manuelles Cleanup nötig.
- **CullRange:** Emitter-Chunks haben eine maximale Sichtweite.
  Chunks außerhalb werden nicht gespawnt.

---

## SoF2 Effect Flags — Referenz

Flags stammen 1:1 aus den originalen `.efx`-Dateien und steuern Rendering- und Physik-Verhalten
der einzelnen Effekt-Segmente. Sie werden als `string[]` im JSON-Feld `flags` gespeichert.

### Vollständige Flag-Liste

| Flag | Vorkommen | Status | Beschreibung |
|------|-----------|--------|-------------|
| `useAlpha` | 65× | **Implementiert** | Alpha-Blending statt Additive Blending. Rauch/Staub/Dreck verdecken statt zu leuchten. `EffectFactory.cs` prüft dieses Flag und setzt `SrcAlpha → OneMinusSrcAlpha` statt `SrcAlpha → One`. |
| `depthHack` | 46× | **Implementiert** | Muzzle-Flash-Partikel werden vor Viewmodel-Geometrie gerendert. `renderQueue = 3100` + `sortingOrder = 10` verhindern Z-Fighting. Nur auf Particle-Segmenten (alle Muzzle-Flashes). |
| `usePhysics` | 31× | **Implementiert** | Partikel kollidieren mit der Welt (Debris-Bits, Impact-Splitter). `ParticleSystem.CollisionModule` mit World-Collision, Medium Quality, Bounce 0.2-0.5.  |
| `useModel` | 22× | **Implementiert** | Alle 22 Vorkommen sind Emitter-Segmente — werden bereits via `SpawnEmitterChunks()` als 3D-Modelle per PrefabManager geladen. Flag ist Metadaten-Bestätigung. |
| `impactKills` | 5× | **Implementiert** | Zerstört das Emitter-Objekt beim ersten Aufprall. `ShellCasingBehaviour.OnCollisionEnter()` nutzt dieses Flag — Patronenhülse verschwindet und spawnt den Impact-FX. |
| `expensivePhysics` | 4× | **Implementiert** | Höhere Kollisionsgenauigkeit: Particle-Segmente → `CollisionQuality.High`, Emitter-Segmente → `CollisionDetectionMode.Continuous`. |
| `useBBox` | 4× | **Implementiert** | Particle-Segmente: über `originMin/originMax → Box Shape` im ShapeModule. Emitter-Segmente: Rigidbody-Collider liefern BBox-Kollision implizit. |
| `impactFx` | 4× | **Implementiert** (indirekt) | Markiert Emitter die beim Aufprall einen referenzierten Sub-Effekt spawnen. Wird über `EffectEmitterDefinition.ImpactFx` als String-Feld verarbeitet, nicht direkt aus dem Flag gelesen. |
| `emitFx` | 2× | **Implementiert** | Emitter spawnt periodisch Sub-Effekte während des Flugs via `EmitFxBehaviour` MonoBehaviour. Sub-Effekte: `embers_for_emitter` (Incendiary-Glutfunken), `underwater_chunk_trail` (Unterwasser-Blasenspur). |

### Implementierungs-Status Zusammenfassung

- **9 von 9 Flags aktiv implementiert**: Alle SoF2-Effekt-Flags werden verarbeitet
- Alle Flags bleiben in den JSON-Definitionen erhalten (keine Daten gehen verloren)

---

## .efx → JSON Konvertierungs-Bugfixes (Session 2025)

### Behobene Konvertierungsfehler

Folgende Bugs wurden im Python-Parser (`efx_parser.py`) und den generierten JSON-Dateien gefunden und behoben:

#### 1. Gravity-Werte ~9.81× zu groß + falsches Vorzeichen

**Problem:** Parser rechnete SoF2-Gravity direkt mit `× 0.0254`, aber Unity's `ParticleSystem.gravityModifier`
multipliziert intern mit `Physics.gravity` (9.81 m/s²).

**Formel-Korrektur:**
```
Particle gravity:   -(SoF2_value × 0.0254 / 9.81)
Emitter gravity:     SoF2_value × 0.0254  (direkt, da Rigidbody ConstantForce)
```

**Betroffene Dateien:** SoF2_Effects.json (141 Fixes), muzzle_flashes.json (9), muzzle_smoke.json (9)

#### 2. ParticleSystem Endlos-Loop

**Problem:** `EffectFactory.ConfigureParticleSystem()` setzte nie `main.loop = false`.
Unity-Default ist `loop = true` → One-Shot-Effekte (Explosion, Muzzleflash) liefen endlos.

**Fix:** `main.loop = false` in `ConfigureParticleSystem()` und `ConfigureTailAsParticleSystem()` hinzugefügt.

#### 3. Achsen-Mapping fehlte

**Problem:** SoF2 Koordinatensystem [X=forward, Y=left, Z=up] ≠ Unity [X=right, Y=up, Z=forward].
Origin/Velocity-Vektoren für World-Space-Effekte waren falsch orientiert.

**Remap-Regel:** `SoF2 [A, B, C] → Unity [B, C, A]` (Y←X, Z←Y, X←Z) für World-Space-Effekte.
Muzzle-Effekte (Local-Space) brauchen kein Remap.

**Betroffene Dateien:** SoF2_Effects.json (Explosionen), shell_casings.json

#### 4. CameraShake/Decal Struktur falsch

**Problem:** Parser schrieb CameraShake/Decal-Properties flach auf das Segment statt in
verschachtelte `cameraShake: {}` / `decal: {}` Objekte.

**Betroffene Dateien:** SoF2_Effects.json (4 CameraShake + 3 Decal Segmente)

---

## Tracer-System — Detailreferenz

### tracerTest2 (Standard-Hitscan-Tracer)

| Parameter | SoF2 Original | Unity (konvertiert) |
|-----------|--------------|-------------------|
| Velocity | 5000 QU/s | 127.0 m/s |
| Lifetime | 500 ms | 0.5 s |
| Length End | 400–450 QU | 10.16–11.43 m (nicht von TrailRenderer genutzt) |
| Width Start | ~3 QU | 0.08 m |
| Width End | ~0.5 QU | 0.01 m |
| Color Start | (255, 217, 51) | (1.0, 0.85, 0.2) gelb-orange |
| Color End | (255, 128, 26) | (1.0, 0.5, 0.1) rötlich-orange |
| Alpha | 1.0 → 0.5 | 1.0 → 0.5 |
| Texture | `gfx/misc/jk_tracer` | Lazy-loaded via TextureManager |
| Blending | Additive | SrcAlpha → One (kein `useAlpha`-Flag) |

### Timing-Verhalten

Hitscan = **Schaden sofort** (Server-Raycast), Tracer = **visuell verzögert** (127 m/s Flugzeit).
Das ist identisch zum SoF2-Original.

| Distanz | Tracer-Ankunft |
|---------|---------------|
| 25 m | ~0.2 s |
| 50 m | ~0.4 s |
| 100 m | ~0.8 s |

### TracerVisual.cs Ablauf

1. `TracerVisual.Create(start, end, effectId)` — statische Factory
2. Holt `EffectDefinition`, sucht erstes `tail`-Segment
3. Erstellt GameObject mit `TrailRenderer`, konfiguriert via `EffectFactory.ConfigureTrailRenderer()`
4. Textur `gfx/misc/jk_tracer` → TextureManager → LazyTextureLoader → `Assets/Art/Textures/gfx/misc/jk_tracer.jpg`
5. Material: URP Particles/Unlit, Additive Blending, Transparent Surface
6. `Update()`: Linearer Flug von Start → End mit konstanter Geschwindigkeit
7. Auto-Destroy bei Ankunft + Trail-Fadeout (kurze Verzögerung für letzte Trail-Segmente)
8. Fallback-Werte: `FALLBACK_SPEED = 300 m/s`, `MAX_LIFETIME = 3 s`
