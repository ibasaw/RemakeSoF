# Sound System

Daten-getriebenes Sound-System nach SoF2-Vorbild (`trap_S_StartSound`, `CG_FireWeapon`, `CG_EjectBrass`).
Alle Sounds werden aus JSON-Definitionen geladen — keine hartkodierten AudioClips.
Zentraler Manager (`SoundManager`) scannt Audio-Dateien, `SurfaceImpactDataLoader` liefert
Surface-abhängige Pfade, `EffectFactory` spielt Effect-eingebettete Sounds ab.

---

## Architektur-Überblick

```
Audio-Dateien (Assets/Art/sound/, StreamingAssets/sound/, Extern)
        ↓
SoundManager (Pure Service, ServiceLocator)
  ├─ ScanSoundDirectory() → SoundRegistry (Cache + Lazy-Load)
  ├─ GetClip(key)         → AudioClip nach SoF2-Pfad
  ├─ GetNumberedClip(basePath) → Zufälliger Clip mit Nummer-Suffix
  └─ SetMixer(MasterMixer) → SFX/Music MixerGroup-Routing
        ↓
Konsumenten:
  ├─ ClientFootstepHandler    → Footstep + Landing Sounds
  ├─ NetworkedPlayerCharacter → Shellsound (MuzzleEffectsClientRpc)
  │                           → Impact Sound (TracerClientRpc)
  │                           → Fire Sound (Waffenschuss)
  └─ EffectFactory            → Effect-eingebettete Sounds (type: "sound")
```

---

## Sound-Kategorien

### 1. Impact Sounds (Einschlag am Trefferpunkt)

| Quelle | Effects JSON (`SoF2_Effects_impact_*.json`) |
|--------|------|
| **JSON-Feld** | Effect-Segment mit `"type": "sound"`, `"files": [...]` |
| **Auslösung** | Automatisch via `EffectFactory.SpawnImpactEffect()` → `PlayEffectSound()` |
| **Position** | Am Trefferpunkt (`hitPoint`) |
| **RPC** | `TracerClientRpc` spawnt Impact-Effect → EffectFactory spielt Sound |
| **Selektion** | Zufällig aus `files`-Array |

**Beispiel** (SoF2_Effects_impact_flesh.json):
```json
{
  "type": "sound",
  "name": "Sound",
  "sound": {
    "files": [
      "sound/player/bullet_impacts/gore/body_impact0.wav",
      "sound/player/bullet_impacts/gore/body_impact1.wav"
    ],
    "delay": 0.0
  }
}
```

**Zusätzlich**: `ammoTypes.[type].sound` in `SoF2_data_per_surface.json` als Fallback
für Munitionstypen ohne eigenen visuellen Effect mit Sound-Segment (z.B. `"blunt"` → Pistolenschlag).
Abgespielt via `PlayImpactSoundAtPosition()` in `TracerClientRpc`.

---

### 2. Shellsounds (Hülse trifft Boden — nahe Schütze)

| Quelle | Surface JSON (`SoF2_data_per_surface.json`) |
|--------|------|
| **JSON-Feld** | `ammoTypes.[type].shellsound` (z.B. `"sound/player/bullet_impacts/casings/casing_default"`) |
| **Auslösung** | `MuzzleEffectsClientRpc` — einmal pro Schuss |
| **Position** | Am `ejectBone` des Schützen |
| **Lookup** | `SurfaceImpactDataLoader.GetShellsoundPath(surfaceType, ammoType)` |
| **Selektion** | `SoundManager.GetNumberedClip()` → `casing_default0.wav`, `casing_default1.wav`, etc. |
| **Surface** | Boden unter Schütze via `DetectSurfaceAtPosition(transform.position)` |

**Beispiel** (SoF2_data_per_surface.json):
```json
"ammoTypes": {
  "0.45 ACP": {
    "shellsound": "sound/player/bullet_impacts/casings/casing_default",
    "effect": "effects/impact_default",
    "debris": "effects/chunks/debris_rock"
  }
}
```

**SoF2-Referenz**: `CG_EjectBrass` — Hülse wird am `ejection_X` Bone ausgeworfen,
Shell-Casing-Effekt spawnt visuelles 3D-Modell mit Physik (`ShellCasingBehaviour`),
der Shellsound spielt separat den Klang des Metalls auf dem Boden.

---

### 3. Footstep Sounds (Schritte am Boden)

| Quelle | Surface JSON (`SoF2_data_per_surface.json`) |
|--------|------|
| **JSON-Feld** | `footstep.sound`, `footstepStealth.sound`, `footstepProne.sound` |
| **Auslösung** | Timer-basiert in `ClientFootstepHandler.Update()` (Run: 0.35s, Walk: 0.55s) |
| **Position** | An Spielerfüßen (`m_PlayerRoot.position`) |
| **Lookup** | `SurfaceImpactDataLoader.GetSurfaceSoundPath(surfaceType, fieldName)` |
| **Selektion** | `SoundManager.GetNumberedClip()` → `concrete0.wav`, `concrete1.wav`, etc. |
| **Surface** | Raycast nach unten vom Player-Root → `SurfaceTypeMarker` |

**Trigger-Bedingungen**:
- `IsGrounded == true`
- `HorizontalSpeed > 0.5`
- `IsWalking` → `"footstepStealth"` (langsamer Interval), sonst `"footstep"` (schneller)

**Beispiel** (SoF2_data_per_surface.json):
```json
"default": {
  "footstep": { "sound": "sound/player/steps/concrete/concrete" },
  "footstepStealth": { "sound": "sound/player/steps/concrete/concrete_walk" },
  "footstepProne": { "sound": "sound/player/steps/prone/default_prone" }
}
```

**SoF2-Referenz**: `PM_CrashLand` + `PM_Footsteps` in `bg_pmove.c` —
Step-Events pro Animations-Frame, je nach Surface (`groundEntityShaderNum` → Material-Lookup).

#### Footstep-Decals (Fußabdrücke)

Zusätzlich zum Sound spawnt `ClientFootstepHandler` bei jedem Schritt ein visuelles Fußabdruck-Decal
über `EffectFactory.SpawnFootstepDecal()`:

```csharp
public void SpawnFootstepDecal(
    Vector3 position,      // Füße-Position (Raycast-Hit)
    Vector3 normal,        // Oberflächen-Normale
    string texturePath,    // SoF2-Textur-Pfad aus SurfaceImpactDataLoader
    float yawDegrees,      // Laufrichtung (Y-Rotation des Spielers)
    bool isLeftFoot        // Links/Rechts-Alternierung
)
```

| Konstante | Wert | Beschreibung |
|-----------|------|-------------|
| `FOOTSTEP_SIZE` | 0.22 m | Quad-Größe (Breite × Höhe) |
| `FOOTSTEP_ALPHA` | 0.45 | Transparenz (Alpha-Blending) |
| `FOOTSTEP_LIFETIME` | 15 s | Auto-Destroy nach 15 Sekunden |
| `SURFACE_OFFSET` | 0.02 m | Z-Fighting-Prävention über Boden |

**Mechanik**:
- Projected Quad wird an der Oberflächen-Normale ausgerichtet, dann per Yaw in Laufrichtung rotiert
- Links/Rechts-Alternierung per X-Scale-Spiegelung (`isLeftFoot ? -SIZE : SIZE`)
- Material: `GetDecalMaterial(texturePath)` → Alpha-Blending, `_BaseColor.a = 0.45`
- `shadowCastingMode = Off`, `receiveShadows = false`

**Textur-Pfad**: Kommt aus `SoF2_data_per_surface.json` → `footstep.decal` Feld pro Surface-Typ.

---

### 4. Landing Sounds (Landung nach Sprung/Fall)

| Quelle | Surface JSON (`SoF2_data_per_surface.json`) |
|--------|------|
| **JSON-Feld** | `land.sound`, `land_pain.sound`, `land_death.sound` |
| **Auslösung** | `ClientPlayerCharacter.CheckLandingSound()` bei `JustLanded == true` |
| **Position** | An Spielerfüßen (`m_PlayerRoot.position`) |
| **Lookup** | `SurfaceImpactDataLoader.GetSurfaceSoundPath(surfaceType, landType)` |
| **Selektion** | `SoundManager.GetNumberedClip()` mit Fallback auf nicht-nummerierte Datei |
| **Schwellen** | `FullFallHeight > 10` → `"land_death"`, `> 3` → `"land_pain"`, sonst `"land"` |

**Beispiel** (SoF2_data_per_surface.json):
```json
"default": {
  "land": { "sound": "sound/player/jumps/concrete" },
  "land_pain": { "sound": "sound/player/jumps/concrete_pain" },
  "land_death": { "sound": "sound/player/jumps/concrete_death" }
}
```

**Wichtig**: Landing-Sounds sind NICHT nummeriert (`concrete.mp3`, nicht `concrete0.mp3`).
`GetNumberedClip()` hat einen Fallback: wenn keine nummerierten Varianten gefunden,
wird `basePath.wav` / `basePath.mp3` direkt versucht.

**SoF2-Referenz**: `PM_CrashLand` — Fallhöhe bestimmt Sound-Typ,
`DAMAGE_FALL_MINIMUM` (130 Units) für `land_pain`, höher für `land_death`.

---

### 5. Weapon Fire Sounds (Waffenschuss — ✅ Implementiert)

| Quelle | Weapons JSON (`SoF2_Weapons_New.json`) |
|--------|------|
| **JSON-Feld** | `sounds.fire` (String oder Array), `sounds.altFire` |
| **Status** | ✅ **Implementiert** |
| **Auslösung** | via `MuzzleEffectsClientRpc` (6. Parameter: `fireSoundPath`) |
| **Position** | Am Schützen (Flash-Bone Position), 3D spatialized |
| **Auflösung** | `ResolveWeaponSoundPath()` — String direkt, JArray → zufällige Auswahl |

**JSON-Struktur** — `sounds.fire` kann String oder Array sein:
```json
// Einzelner Sound (RPG-7):
"sounds": { "fire": "sound/weapons/rpg7/fire01" }

// Mehrere Varianten (M4):
"sounds": {
  "fire": [
    "sound/weapons/m4/m4fire01",
    "sound/weapons/m4/m4fire02",
    "sound/weapons/m4/m4fire03"
  ],
  "altFire": "sound/weapons/m4/glaunch"
}
```

**Weitere Sound-Keys im JSON**: `ready`, `clipOut`, `clipIn`, `boltPull`, `boltRelease`,
`slideRelease`, `slap`, `drumOpen`, `drumClose`, `reload`.

**SoF2-Referenz** (`cg_weapons.c`, `cg_weaponinit.c`):
```
CG_FireWeapon():
  → trap_S_StartSound(NULL, entity, CHAN_WEAPON, attackInfo->flashSound[random])
  → Sound auf CHAN_WEAPON = wird am Entity abgespielt, kein separater Positions-Sound
  → flashSound[0..2] = bis zu 3 Varianten, zufällig ausgewählt
  → Lautstärke via attack.Volume (SoF2-Feld, nicht in allen Definitionen)

cg_weaponinit.c:
  → Loop durch weapon.mSoundNames[]
  → "fire" → weaponInfo.attack[ATTACK_NORMAL].flashSound[0..2]
  → "altfire" → weaponInfo.attack[ATTACK_ALTERNATE].flashSound[0..2]
  → Alle anderen → weaponInfo.otherWeaponSounds[group][slot]
  → MAX_WEAPON_SOUNDS = 12, MAX_WEAPON_SOUND_SLOTS = 3
```

**Implementierung** (NetworkedPlayerCharacter.cs):
1. Server: `ResolveWeaponSoundPath(weapon, "fire")` extrahiert Pfad (String direkt, JArray → Random)
2. Pfad wird als 6. Parameter in `MuzzleEffectsClientRpc` gesendet
3. Client: `SoundManager.GetClip(fireSoundPath)` → AudioClip laden
4. 3D AudioSource am Flash-Bone mit `SfxGroup` Mixer-Routing
5. `spatialBlend = 1f`, `maxDistance = 50f`, Linear Rolloff
6. `sounds.altFire` wird bei Alt-Fire analog aufgelöst

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **SoundManager** | `Assets/Scripts/Runtime/Management/SoundManagement/SoundManager.cs` | Pure Service: Audio-Scan, Registry, GetClip/GetNumberedClip, Mixer-Routing |
| **SoundRegistry** | `Assets/Scripts/Runtime/Management/SoundManagement/SoundRegistry.cs` | Cache: SoF2-Pfad → SoundData (Lazy-Load AudioClip) |
| **SoundData** | `Assets/Scripts/Runtime/Management/SoundManagement/SoundData.cs` | DTO: Key, FilePath, AudioClip |
| **SurfaceImpactDataLoader** | `Assets/Scripts/Runtime/Management/DataManagement/SurfaceImpactDataLoader.cs` | Pure Service: Surface-Sounds (Footstep, Land, Shellsound, Impact) |
| **ClientFootstepHandler** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientFootstepHandler.cs` | Timer-basierte Footsteps + Landing-Sounds |
| **ClientPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Setzt Handler-State (IsGrounded, HorizontalSpeed, FallHeight) |
| **NetworkedPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` | RPCs: Shellsound, Impact-Sound, Muzzle-Effects |
| **EffectFactory** | `Assets/Scripts/Runtime/Game/Effects/EffectFactory.cs` | PlayEffectSound() für Effect-eingebettete Impact-Sounds |
| **WeaponDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponDefinition.cs` | DTO: `Sounds` Dictionary mit fire/altFire/ready/etc. |
| **SurfaceTypeMarker** | `Assets/Scripts/Runtime/Game/Effects/SurfaceTypeMarker.cs` | MonoBehaviour: Surface-Typ auf World-Geometry |

---

## JSON-Datenquellen

| Datei | Pfad | Inhalt |
|-------|------|--------|
| **SoF2_data_per_surface.json** | `Assets/Resources/Data/` | Footstep, Land, Shellsound, Impact-Sound pro Surface + Ammo |
| **SoF2_Weapons_New.json** | `Assets/Resources/Data/` | Fire-Sounds, AltFire, Ready, Reload pro Waffe |
| **SoF2_Effects_impact_*.json** | `Assets/Resources/Data/Effects/` | Impact-Effekte mit eingebetteten Sound-Segmenten |

---

## SoundManager — Key-Format & Scan

**Key-Format**: SoF2-relativer Pfad mit Extension → `"sound/weapons/m4/m4fire01.wav"`

**Scan-Reihenfolge** (Priorität):
1. `Assets/Art/sound/` (Editor & Build — primär)
2. `StreamingAssets/sound/` (Build-Fallback)
3. Externer Pfad (Entwicklung: `sof2_extract/base/sound/`)

**Extension-Fallback**: `.wav` ↔ `.mp3` automatisch (SoF2-JSONs referenzieren oft `.wav`,
Dateien liegen aber als `.mp3` vor). Alias-Keys werden beim Scan registriert.

**GetNumberedClip(basePath)**: Sucht `basePath` + `0..7` + `.wav/.mp3`.
Fallback auf `basePath.wav/.mp3` ohne Nummer (für nicht-nummerierte Sounds wie Landing).

---

## Audio-Routing

```
MasterMixer (AudioMixer)
  ├─ SFX (AudioMixerGroup)
  │   ├─ Footsteps (ClientFootstepHandler)
  │   ├─ Landing (ClientFootstepHandler)
  │   ├─ Shellsounds (MuzzleEffectsClientRpc)
  │   ├─ Impact Sounds (EffectFactory + TracerClientRpc Fallback)
  │   └─ Fire Sounds (MuzzleEffectsClientRpc)
  └─ Music (AudioMixerGroup)
```

**Alle Gameplay-Sounds** werden über die `SFX` MixerGroup geroutet (`SoundManager.SfxGroup`).
Sounds via `AudioSource.PlayClipAtPoint()` umgehen den Mixer — darum verwenden alle
Sound-Systeme eigene `AudioSource`-Setups mit `outputAudioMixerGroup = SfxGroup`.

---

## SoF2-Referenz: Sound-Kanäle

| Kanal | SoF2-Konstante | Beschreibung |
|-------|----------------|-------------|
| Weapon | `CHAN_WEAPON` | Waffenschuss — am Entity, unterbricht vorherigen Sound auf diesem Kanal |
| Voice | `CHAN_VOICE` | Spieler-Stimme (Schmerzschreie, Funksprüche) |
| Body | `CHAN_BODY` | Footsteps, Landing, Shellsound |
| Auto | `CHAN_AUTO` | Automatischer Kanal (Impact-Sounds, Ambient) |

In Unity wird die Kanal-Trennung implizit über separate `AudioSource`-Instanzen realisiert.
Jeder Sound bekommt ein eigenes temporäres `GameObject` mit `AudioSource`, das nach Ablauf zerstört wird.

---

## Surface-Detection

Alle surface-abhängigen Sounds (Footstep, Landing, Shellsound) nutzen Raycasts
zur Oberflächen-Erkennung:

| System | Raycast-Ursprung | Richtung | Distanz | Lookup |
|--------|-----------------|----------|---------|--------|
| Footstep | `PlayerRoot + 0.1f Up` | Down | 2m | `SurfaceTypeMarker` → `GetSurfaceSoundPath()` |
| Landing | `PlayerRoot + 0.1f Up` | Down | 2m | `SurfaceTypeMarker` → `GetSurfaceSoundPath()` |
| Shellsound | `transform.position` | Down | 3m | `SurfaceTypeMarker` → `GetShellsoundPath()` |

**Fallback**: Wenn kein `SurfaceTypeMarker` gefunden → `"default"` Surface.
`SurfaceTypeMarker` wird beim Map-Loading von `MapColliderApplier.ApplySurfaceTypeMarker()`
auf World-Geometry platziert (aus `q3map_material` der Shader-Definitionen).
