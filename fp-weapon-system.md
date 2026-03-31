# First-Person Weapon System

SoF2-authentisches First-Person-Waffensystem mit Ghoul2 Composite-Modell,
separatem viewModel-Prefab und per-Weapon viewOffset.

---

## SoF2 Original (cg_weapons.c)

```
CG_RegisterWeapon:
  Slot 0: viewG2Model   (FP-Waffe, detailliertes Mesh)
  Slot 1: buffer         (unsichtbares Skeleton mit Attachment-Bolts)
  Slot 2: rhand.glm      (rechte Hand, bolted an Buffer)
  Slot 3: lhand.glm      (linke Hand, bolted an Buffer)

CG_AddViewWeapon:
  → viewG2Model gerendert an vieworg (Kamera-Ursprung)
  → RF_FIRST_PERSON | RF_DEPTHHACK Flags
  → VectorScale(hand.axis[0], foreshorten) — Z-Skalierung
  → Per-Weapon viewoffset (Forward/Right/Up in QU)
```

---

## Architektur-Überblick

```
WeaponDefinition (JSON)
  ├─ worldModel    → TP: "models/weapons/m4/world/m4world"
  ├─ viewModel     → FP: "models/weapons/m4/m4"
  ├─ buffer.model  → "models/weapons/buffer/rifle/buffer"
  ├─ hands.left/right.boltToBone → "lhand_m4", "rhand_m4"
  ├─ viewOffset    → { forward, right, up } in QU
  ├─ foreshorten   → 0.6 (Z-Skalierung)
  ├─ fovX          → Waffen-FOV
  └─ inviewAnimations → { idle, fire, reload, ... } pro weapon/lhand/rhand

ClientPlayerCharacter (Orchestrierung)
  ├─ m_WeaponLoader      → TP: LoadAndAttachWeapon("m4") am rhang_tag_bone
  ├─ m_FpWeaponLoader    → FP: LoadAndAttachWeapon("m4", viewModel) am FP_WeaponHolder
  ├─ m_FpHandsLoader     → Buffer + lhand/rhand Composite-Modell
  ├─ FirstPersonCameraEffects → viewOffset, bob, landing, duck
  └─ Weapon Overlay Camera → separates FOV für FP-Waffen
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **WeaponLoader** | `Assets/Scripts/Runtime/Game/WeaponLoader/WeaponLoader.cs` | Prefab laden + attachen (TP & FP) |
| **FirstPersonHandsLoader** | `Assets/Scripts/Runtime/Game/WeaponLoader/FirstPersonHandsLoader.cs` | Buffer + Hands Composite-Modell |
| **ClientPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Orchestrierung TP/FP/Hands |
| **FirstPersonCameraEffects** | `Assets/Scripts/Runtime/Game/Camera/FirstPersonCameraEffects.cs` | ViewOffset, Bob, Landing |
| **WeaponDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponDefinition.cs` | Root-DTO |
| **WeaponViewOffsetDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponViewOffsetDefinition.cs` | ViewOffset DTO (Forward/Right/Up) |
| **SoF2_Weapons_New.json** | `Assets/Resources/Data/SoF2_Weapons_New.json` | Zentrale Waffen-Datenbank |

---

## Dual-Model-System (TP vs FP)

SoF2 verwendet zwei unabhängige Modelle pro Waffe:

| Aspekt | Third-Person (TP) | First-Person (FP) |
|--------|-------------------|--------------------|
| JSON-Key | `worldModel` | `viewModel` |
| Beispiel | `models/weapons/m4/world/m4world` | `models/weapons/m4/m4` |
| Mesh-Detail | Vereinfacht | Detailliert, mit Animations-Bones |
| Attachment | `rhang_tag_bone` (Hand-Bone) | `FP_WeaponHolder` (Camera-Child) |
| Loader-Call | `LoadAndAttachWeapon("m4")` | `LoadAndAttachWeapon("m4", viewModel)` |
| Scale-Kompensation | Ja (Bone FBX-Scale) | Nein (QU→Meter beabsichtigt) |
| Rotation | (0, 0, -90°) | (0, 0, 0°) |
| Animations | mp_idle, mp_attack (Torso) | inviewAnimations (weapon/lhand/rhand) |

### Prefab-Laden

```csharp
// TP-Waffe: weaponKey = Addressable-Key (gemappt auf worldModel)
m_WeaponLoader.LoadAndAttachWeapon(weaponName);

// FP-Waffe: viewModel-Pfad als Addressable-Key
string fpModelKey = definition.ViewModel;
m_FpWeaponLoader.LoadAndAttachWeapon(weaponName, fpModelKey);
```

---

## FP_WeaponHolder Setup

```csharp
// SoF2: viewG2Model gerendert an vieworg (Kamera-Ursprung) — kein Offset.
fpWeaponHolder.transform.localPosition = Vector3.zero;
```

- Parent: `Camera.main.transform`
- LocalPosition: `Vector3.zero` (SoF2-authentisch, kein Unity-Korrektur-Offset)
- Per-Weapon viewOffset wird über `FirstPersonCameraEffects` additiv angewendet

---

## Ghoul2 Composite-Modell (Buffer + Hands)

SoF2 Ghoul2 rendert bis zu 4 Slots als ein zusammengesetztes Modell:

```
FP_WeaponHolder (Camera-Child)
  └─ viewModel Prefab (Slot 0: Waffe)
       └─ [buffer.boltToBone] z.B. "gun" oder "handle"
            └─ Buffer Prefab (Slot 1: unsichtbares Skeleton)
                 ├─ [hands.right.boltToBone] z.B. "rhand_m4"
                 │    └─ rhand Prefab (Slot 2)
                 └─ [hands.left.boltToBone] z.B. "lhand_m4"
                      └─ lhand Prefab (Slot 3)
```

### Buffer
- Unsichtbares Skeleton mit Attachment-Bolts
- Enthält Bones für: Hände, Muzzle-Flash (`flash_*`), Shell-Ejektion
- Addressable-Key aus `definition.Buffer.Model` (z.B. `"models/weapons/buffer/rifle/buffer"`)
- 180° Z-Rotation (SoF2-Konvention)
- World-Scale-Kompensation gegen Parent-Bone-Scale

### Hände
- `lhand` und `rhand` sind gemeinsame Modelle für alle Waffen
- Addressable-Keys: `"lhand"`, `"rhand"` (konstant)
- Attachment-Bolts variieren pro Waffe (z.B. `"lhand_m4"`, `"rhand_m4"`)
- Manche Waffen nur einhändig (rhand = null)

---

## InviewAnimations

Separate Animations pro Composite-Slot (weapon, lhand, rhand):

```json
"inviewAnimations": {
    "idle": {
        "weapon": { "name": "idle", "startFrame": 0, "duration": 50, "fps": 20 },
        "lhand":  { "name": "idle", "startFrame": 0, "duration": 50, "fps": 20 },
        "rhand":  { "name": "idle", "startFrame": 0, "duration": 50, "fps": 20 }
    },
    "fire": {
        "weapon": { "name": "fire", "startFrame": 0, "duration": 6, "fps": 20 },
        "lhand":  { "name": "fire", "startFrame": 0, "duration": 6, "fps": 20 },
        "rhand":  { "name": "fire", "startFrame": 0, "duration": 6, "fps": 20 }
    },
    ...
}
```

**Wichtig:** Diese Animations existieren nur im **viewModel**-Prefab, nicht im worldModel.
Das ist der Hauptgrund warum FP das viewModel laden muss.

| State | Beschreibung |
|-------|-------------|
| `idle` | Idle-Pose |
| `fire` | Primärangriff |
| `altfire` | Alternativangriff |
| `reload` | Nachladen |
| `reload_start` | Shell-Reload Start (Shotguns) |
| `reload_loop` | Shell-Reload Loop |
| `reload_end` | Shell-Reload Ende |
| `raise` | Waffe ziehen |
| `drop` | Waffe weglegen |

---

## ViewOffset (Per-Weapon Camera Offset)

SoF2 verschiebt die Kamera pro Waffe in Forward/Right/Up (Quake-Units):

```json
"viewOffset": {
    "forward": 0,
    "right": 0,
    "up": 0
}
```

- Konvertierung: QU × 0.0254 = Unity-Meter
- Angewendet via `FirstPersonCameraEffects.SetWeaponViewOffset()`
- Additiv zu Bob, Landing, Duck und EyeHeight-Korrektur
- Wird in Cinemachine PostPipelineStageCallback als `PositionCorrection` angewendet

---

## Foreshorten (Z-Axis Scaling)

SoF2 `VectorScale(hand.axis[0], foreshorten)` — skaliert nur die Forward-Achse:

```json
"foreshorten": 0.6
```

- Default: 0.6 (60% der Z-Tiefe)
- Perspektivischer Trick: Waffe nimmt weniger Bildschirm ein
- Nur Z-Achse (Forward) wird skaliert — X/Y bleiben 1:1
- Angewendet nach dem Laden: `m_FpWeaponLoader.ApplyForeshorten(foreshorten)`

---

## Weapon Overlay Camera

SoF2 `CG_CalculateWeaponFov` — separates FOV für FP-Waffen:

- URP Overlay-Kamera rendert nur FPWeapon-Layer
- Main-Kamera hat FPWeapon-Layer aus CullingMask entfernt
- Per-Weapon FOV aus `definition.FovX` (0 = Default)
- Verhindert Verzerrung bei weitem Welt-FOV

---

## Visibility-Management

| Modus | TP-Waffe | TP-Body | FP-Waffe |
|-------|----------|---------|----------|
| First-Person | ShadowsOnly | ShadowsOnly | Visible |
| Third-Person | Visible | Visible | Hidden |

- `ApplyFirstPersonVisibility()` steuert ShadowCastingMode aller Visual-Renderer
- FP-Waffe über `SetActive(true/false)`
- `RefreshVisualRenderers()` re-cached nach Waffenwechsel

---

## Texturen

Beide Loader (TP und FP) nutzen den `viewModel`-Pfad für Texturen:

```
viewModel = "models/weapons/m4/m4"
  → Base-Textur:    "models/weapons/m4/m4"
  → Specular-Textur: "models/weapons/m4/m4_spec"
```

- Material: `SoF2/MapSurface` Shader mit `_LightBlend = 0.5`
- Backface-Culling OFF (SoF2: `cull disable`)
