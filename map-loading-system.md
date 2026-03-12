# Map Loading System – Referenzdokumentation

## Übersicht

Das Map-Loading-System lädt SoF2-Maps (als FBX-Prefabs via Addressables), erstellt Collider, wendet Texturen an, baut die Skybox und erstellt ein Directional Light für die Sonne. Die Ghoul2Meta-Komponente auf jedem Mesh speichert dabei alle relevanten idTech3/SoF2-Shader-Properties als dynamische Key-Value-Paare.

---

## Pipeline (MapLoader.cs)

```
LoadMapAsync(mapName)
  │
  ├─ PrefabLoaded       → Addressable geladen
  ├─ Instantiated        → In Szene instanziiert
  ├─ CollidersApplied    → MapColliderApplier
  ├─ TexturesApplied     → MapTextureApplier
  ├─ SkyboxApplied       → MapSkyboxApplier (Skybox + Sun Light)
  └─ Complete            → SpawnPoints eingerichtet
```

### Interne Services im MapLoader

| Service              | Beschreibung                                      | Server | Client |
|----------------------|---------------------------------------------------|--------|--------|
| `MapColliderApplier` | MeshCollider für alle Renderer                    | ✅     | ✅     |
| `MapTextureApplier`  | Texturen, Cull, Transparenz, Sky-Surface-Hiding   | ❌     | ✅     |
| `MapSkyboxApplier`   | Skybox aus skyParms + Directional Light aus sun    | ❌     | ✅     |

---

## Ghoul2Meta Properties (FBX Custom Properties)

Alle Properties werden vom `FBXGhoul2PropsImporter` (AssetPostprocessor) automatisch aus dem FBX in die `Ghoul2Meta`-Komponente übertragen. Properties sind dynamisch und indexiert (suffix `_N`).

### Textur-Properties

| Property                | Beispielwert                              | Beschreibung                                        |
|-------------------------|-------------------------------------------|-----------------------------------------------------|
| `mapped_texture_N`      | `"textures/shop1/floor_wood2"`            | Textur-Key für Slot N, aufgelöst via TextureManager |
| `cull_N`                | `"disabled"`                              | Backface Culling deaktiviert → beide Seiten rendern |
| `is_transparent_N`      | `true`                                    | Surface transparent → Alpha-Rendering               |

### Surface-Type-Properties

| Property                  | Beispielwert             | Beschreibung                                    |
|---------------------------|--------------------------|-------------------------------------------------|
| `surface_types_json_N`    | `["sky"]`                | Surface-Typ; "sky" → Renderer wird unsichtbar   |

### Sky / Skybox Properties

| Property              | Beispielwert                                  | Beschreibung                                              |
|-----------------------|-----------------------------------------------|-----------------------------------------------------------|
| `sky_types_json_N`    | `["textures/skies/cemetary 0 -"]`             | skyParms: `<basepath> <cloudheight> <innerbox>`           |
| `sun_N`               | `["0.8 0.8 0.8 300 0 50"]`                   | `r g b intensity degrees elevation`                       |
| `surfacelight_N`      | `["70"]`                                      | Baked Surface Light Intensität (informational)            |

---

## MapColliderApplier

**Datei:** `Assets/Scripts/Runtime/Game/MapLoader/MapColliderApplier.cs`

- Erstellt `MeshCollider` für alle Renderer mit `MeshFilter`
- Markiert GameObjects als `isStatic = true`
- **Transparente Surfaces werden übersprungen:** `is_transparent_N` Prefix-Check via LINQ `.Any()`
- **Flache Meshes erhalten Collider** (kein Bounds-Threshold, da man in SoF2 darauf springen konnte)
- Ruft `Physics.SyncTransforms()` nach dem Erstellen auf

---

## MapTextureApplier

**Datei:** `Assets/Scripts/Runtime/Game/MapLoader/MapTextureApplier.cs`

### Textur-Auflösung
1. `mapped_texture_N` Keys aus Ghoul2Meta sammeln
2. Für jeden Key: `TextureManager.GetTextureData(key)` → falls null → `GetTextureDataByAlias(key)`
3. Material erstellen mit `Universal Render Pipeline/Unlit` Shader
4. Texture auf `_BaseMap` Property setzen

### Per-Slot Properties
- **Cull:** `cull_N` = `"disabled"/"off"/"disable"` → `material._Cull = 0` (Both)
- **Transparenz:** `is_transparent_N` → Surface Type Transparent, Alpha Blend, ZWrite off, AlphaClip

### Sky-Surface-Handling
- `IsSkyboxSurface()` prüft `surface_types_json_N` auf `"sky"` String
- Sky-Surfaces: `renderer.enabled = false` → unsichtbar, aber Collider bleibt aktiv als Barriere
- Unity Skybox scheint stattdessen durch

---

## MapSkyboxApplier

**Datei:** `Assets/Scripts/Runtime/Game/MapLoader/MapSkyboxApplier.cs`

### Skybox-Erstellung

1. **Discovery:** Erster Renderer mit `sky_types_json_N` Property wird gesucht
2. **Parsing:** `["textures/skies/cemetary 0 -"]` → Basispfad `textures/skies/cemetary`
3. **6 Face-Texturen laden** via TextureManager:

| Suffix | Shader Property | Beschreibung |
|--------|-----------------|--------------|
| `_ft`  | `_FrontTex`     | Front        |
| `_bk`  | `_BackTex`      | Back         |
| `_up`  | `_UpTex`        | Top          |
| `_dn`  | `_DownTex`      | Bottom       |
| `_rt`  | `_RightTex`     | Right        |
| `_lf`  | `_LeftTex`      | Left         |

4. **Rotation:** `_up` und `_dn` Faces werden um **90° CCW** (gegen den Uhrzeigersinn) rotiert wegen idTech3 (Z-up, rechtshändig) → Unity (Y-up, linkshändig) Koordinatensystem-Unterschied
5. Material: `Skybox/6 Sided` Shader → `RenderSettings.skybox` zuweisen
6. `DynamicGI.UpdateEnvironment()` für korrekte Beleuchtung

### Textur-Pfade

Die 6 Skybox-Texturen müssen im TextureManager registriert sein:
```
Assets/Art/Textures/textures/skies/cemetary_ft.jpg  → Key: "textures/skies/cemetary_ft"
Assets/Art/Textures/textures/skies/cemetary_bk.jpg  → Key: "textures/skies/cemetary_bk"
Assets/Art/Textures/textures/skies/cemetary_up.jpg  → Key: "textures/skies/cemetary_up"
Assets/Art/Textures/textures/skies/cemetary_dn.jpg  → Key: "textures/skies/cemetary_dn"
Assets/Art/Textures/textures/skies/cemetary_rt.jpg  → Key: "textures/skies/cemetary_rt"
Assets/Art/Textures/textures/skies/cemetary_lf.jpg  → Key: "textures/skies/cemetary_lf"
```

### Directional Light (Sonne)

1. **sun_N** wird gelesen: `["0.8 0.8 0.8 300 0 50"]`
   - `r g b` → Unity `Color(0.8, 0.8, 0.8)`
   - `intensity` → `300 × 0.01 = 3.0` (Unity URP Skala)
   - `degrees` → Kompassrichtung (Gegenuhrzeigersinn von Osten in idTech3)
   - `elevation` → Höhenwinkel über Horizont (0-90°)
2. **Unity Rotation:** `Quaternion.Euler(elevation, -degrees, 0)` — degrees wird negiert wegen CW/CCW Unterschied
3. **Directional Light:** "SoF2_SunLight", Soft Shadows, `HideFlags.DontSave`
4. **surfacelight_N** wird nur geloggt (relevant für Baked Lighting, nicht Runtime)

---

## idTech3/SoF2 → Unity Koordinatensystem

| Eigenschaft       | idTech3/SoF2          | Unity                    |
|--------------------|-----------------------|--------------------------|
| Up-Achse           | Z-up                 | Y-up                     |
| Händigkeit         | Rechtshändig          | Linkshändig              |
| Skybox Top/Bottom  | Z-orientiert          | Y-orientiert → 90° CCW   |
| Sun degrees        | Gegenuhrzeigersinn    | Uhrzeigersinn → negieren |

---

## MapLoadPhase Enum

```csharp
public enum MapLoadPhase
{
    Started,           // 0.1  – "Loading map data..."
    PrefabLoaded,      // 0.4  – "Map data loaded"
    Instantiated,      // 0.6  – "Building map..."
    CollidersApplied,  // 0.75 – "Creating collision..."
    TexturesApplied,   // 0.85 – "Applying textures..."
    SkyboxApplied,     // 0.9  – "Creating skybox..."
    SpawnPointsReady,  // 0.95 – "Preparing spawn points..."
    Complete,          // 1.0  – "Ready!"
    Failed             // 0.0  – "Failed to load map!"
}
```

---

## Abhängigkeiten

- **ServiceLocator:** `PrefabManager`, `TextureManager`
- **TextureManager:** Scannt `Assets/Art/Textures` (System) + `persistentDataPath/CustomTextures` (Custom)
- **TextureRegistry:** Cache-first, Lazy Loading via Custom Loaders
- **Ghoul2Meta:** Dynamischer Property-Container auf jedem Map-Mesh
- **FBXGhoul2PropsImporter:** Editor AssetPostprocessor, überträgt FBX Custom Props → Ghoul2Meta
