# Textur-System Referenz

## Übersicht

Das Textur-System lädt, cached und wendet Texturen auf alle 3D-Modelle an —
Character-Skins, Waffen, Gore-Stücke, Hülsen, Projektile und Map-Geometry.
Texturen liegen als Bilddateien im Dateisystem und werden **lazy** (on-demand) geladen.

---

## Architektur

```
Art/Textures/ (System)                CustomTextures/ (Spieler)
       │                                       │
       └───────────┬───────────────────────────┘
                   ▼
           TextureManager (Pure Service, ServiceLocator)
                   │
                   ├── ScanAllTextureDirectories() → Registry vorregistrieren
                   ├── GetTextureData(key) → Cache → LazyTextureLoader → LegacyShaderLoader
                   └── CreateMaterialsFromSkinDefinition() → Skin-Materialien
                   │
                   ▼
           TextureRegistry (Dictionary<string, TextureData>)
                   │
                   ├── TextureCache (Case-insensitive)
                   ├── LazyTextureLoader (Default Custom Loader)
                   └── AliasKeys (alternative Schlüssel pro TextureData)
                   │
                   ▼
    ┌──────────────┼──────────────────────────┐
    ▼              ▼                          ▼
PrefabTexture   MapTexture              SkinDefinition
  Applier        Applier                  Materialien
(Prefabs)     (Map-Geometry)          (Character-Skins)
```

---

## Beteiligte Klassen

| Klasse | Pfad | Rolle |
|--------|------|-------|
| **TextureManager** | TextureManagement/TextureManager.cs | Pure Service: Scannt Verzeichnisse, cached Texturen, erstellt Materialien |
| **TextureRegistry** | TextureManagement/TextureRegistry.cs | Dictionary-Cache + Custom Loader Verwaltung |
| **TextureData** | TextureManagement/TextureData.cs | DTO: Id, FilePath, Texture2D, Material, Source, AliasKeys |
| **TextureDataFactory** | TextureManagement/TextureData.cs | Factory für TextureData-Erstellung |
| **LazyTextureLoader** | TextureManagement/LazyTextureLoader.cs | On-demand Laden von vorregistrierten Texturen aus Dateien |
| **LegacyShaderLoader** | DataManagement/LegacyShaderLoader.cs | Parst .g2shader Dateien → ShaderEntry Dictionaries |
| **ShaderDataReader** | Shared/ShaderDataReader.cs | Parser für .g2shader Text-Format |
| **ShaderEntry** | DTOs/SkinManagement/ShaderEntry.cs | DTO: MainTexture, EditorImage, HitLocation, CullDisabled |
| **PrefabTextureApplier** | TextureManagement/PrefabTextureApplier.cs | Statischer Helper: Texturen auf Prefabs (Ghoul2Meta + modelKey-Fallback) |
| **TextureConfiguration** | TextureManagement/TextureConfiguration.cs | Textur-Einstellungen (Filter, Qualität) |

---

## Textur-Quellen

### System-Texturen (Priorität: Standard)
```
Assets/Art/Textures/
  ├── models/characters/       → Character-Skins
  ├── models/characters/gore/  → Gore-Stücke (brain, bone, hand, etc.)
  ├── models/weapons/          → Waffen-Modelle
  ├── gfx/                     → Effekt-Texturen
  └── textures/                → Map-Texturen
```

### Custom-Texturen (Priorität: Überschreibt System)
```
{Application.persistentDataPath}/CustomTextures/
  └── Spieler können System-Texturen mit gleichen Keys überschreiben
```

### Key-Format
- Relativer Pfad ohne Extension: `models/characters/gore/brain/brain`
- Forward-Slashes: `models/weapons/shells/shell_brass_hires`
- Case-insensitive (StringComparer.OrdinalIgnoreCase)

---

## Lade-Pipeline

### 1. Startup: Verzeichnis-Scan

```
TextureManager.Initialize()
  ├── ScanDirectory(Art/Textures/, TextureSource.System)
  │   └── Für jede Datei (.png, .jpg, .jpeg, .tga, .tif, .tiff):
  │       └── TextureData vorregistrieren (Key + FilePath, OHNE geladene Texture)
  └── ScanDirectory(CustomTextures/, TextureSource.Custom)
      └── Custom überschreibt gleiche Keys
```

**Ergebnis:** Registry enthält ~5000+ Keys mit FilePaths, aber **keine geladenen Texturen**.

### 2. On-Demand: Lazy Loading

```
TextureManager.GetTextureData("models/characters/gore/hand")
  │
  ├── 1. Registry-Cache: Key vorhanden + HasTexture()?
  │   └── Ja → Return (sofort, kein Laden)
  │
  ├── 2. LazyTextureLoader.Load(key):
  │   ├── TextureCache hat Key? → FilePath bekannt
  │   ├── File.ReadAllBytes(filePath) → Texture2D.LoadImage()
  │   └── Registry.UpdateTextureData(key, texture)
  │
  └── 3. LegacyShaderLoader Fallback (via TextureManager):
      ├── Alle .g2shader Dictionaries durchsuchen
      ├── ShaderEntry gefunden? → MainTexture / EditorImage Key
      └── CreateMaterialFromShaderEntry() → Material erstellen + cachen
```

### 3. Material-Erstellung

| Szenario | Material-Quelle |
|----------|----------------|
| Character-Skin | `CreateMaterialsFromSkinDefinition()` → SkinDefinition.materials |
| Ghoul2Meta Prefab | `PrefabTextureApplier` → mapped_texture_0..N Keys |
| Gore/Shell/Chunk Prefab | `PrefabTextureApplier` → modelKey-Fallback |
| Map-Geometry | `MapTextureApplier` → q3map_material → Shader-Zuordnung |

---

## PrefabTextureApplier — Zwei Pfade

### Pfad 1: Ghoul2Meta (Standard)

Für Prefabs die aus `.glm`-Dateien konvertiert wurden und eine `Ghoul2Meta`-Komponente haben:

```
instance.GetComponentsInChildren<Renderer>()
  └── renderer.TryGetComponent(out Ghoul2Meta meta)?
      └── Ja: mapped_texture_0..N → TextureManager → Material[]
          ├── cull_N → Doppelseitig
          ├── is_transparent_N → Alpha-Cutout
          └── renderer.materials = Material[]
```

**Verwendet von:** Character-Modelle, Waffen, Projektile (RPG7-Rakete etc.)

### Pfad 2: modelKey-Fallback (Neu)

Für einfache 3D-Modelle ohne Ghoul2Meta (Gore-Stücke, Hülsen, Debris):

```
ApplyTextureFromModelKey(renderers, modelKey, textureManager)
  ├── modelKey: "models/characters/gore/hand.md3"
  ├── Extension entfernen: "models/characters/gore/hand"
  ├── TextureManager.GetTextureData(key)
  │   → Registry-Cache → LazyTextureLoader → LegacyShaderLoader
  ├── Material erstellen: SoF2/MapSurface Shader
  │   ├── _BaseMap = Texture2D
  │   ├── _Smoothness = 0
  │   ├── _Cull = 0 (doppelseitig)
  │   └── doubleSidedGI = true
  └── Alle Renderer: sharedMaterials = [material]
```

**Verwendet von:** EffectFactory.SpawnEmitterChunks, SpawnShellCasing, GoreApplier.SpawnBoltOn

### Aufrufer

| Aufrufer | Datei | modelKey Beispiel |
|----------|-------|-------------------|
| SpawnEmitterChunks | EffectFactory.cs | `"models/characters/gore/hand.md3"` |
| SpawnShellCasing | EffectFactory.cs | `"models/weapons/shells/shell_brass_hires"` |
| SpawnBoltOn | GoreApplier.cs | `"models/characters/gore/brain/g2brain.glm"` |
| ClientProjectileVisual | ClientProjectileVisual.cs | *(nutzt Ghoul2Meta, kein modelKey)* |
| FenceBarrier | FenceBarrier.cs | *(nutzt Ghoul2Meta, kein modelKey)* |

---

## Legacy Shader System (.g2shader)

### Überblick

SoF2 verwendete `.g2shader`-Dateien um Texturen, Cull-Mode, HitLocation und Transparenz
pro Model-Surface zu definieren. Diese Dateien werden beim Start geladen und dienen als
Fallback für die Textur-Auflösung.

### Dateien

```
Assets/StreamingAssets/Data/shaders/
  ├── gore.g2shader          → Gore-Stücke (brain, bone_long, hand, etc.)
  ├── average_face.g2shader  → Standard-Gesichter
  ├── average_body.g2shader  → Standard-Körper
  ├── chem_suit.g2shader     → Chemie-Anzug
  └── ... (weitere Model-Shader)
```

### Struktur einer .g2shader Datei

```
models/characters/gore/brain
{
    map   models/characters/gore/brain/brain
    cull  disable
}

models/characters/gore/hand/hand
{
    map   models/characters/gore/hand
}
```

### ShaderEntry Felder

| Feld | Beschreibung |
|------|-------------|
| `MainTexture` | Textur-Pfad (aus `map` Zeile) |
| `EditorImage` | Fallback-Textur (aus `qer_editorimage`) |
| `CullDisabled` | Doppelseitig rendern (aus `cull disable`) |
| `HitLocation` | Hit-Detection-Textur (aus `hitLocation`) |
| `HitMaterial` | Material-Hit-Textur (aus `hitMaterial`) |

---

## TextureData DTO

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Id` | `string` | Eindeutiger Key (relativer Pfad ohne Extension) |
| `FilePath` | `string` | Absoluter Dateipfad der Textur |
| `Texture` | `Texture2D` | Geladene Textur (null bis Lazy Load) |
| `Material` | `Material` | Zugehöriges Material (null bis Erstellung) |
| `Source` | `TextureSource` | `System` oder `Custom` |
| `AliasKeys` | `List<string>` | Alternative Schlüssel für diese Textur |
| `ReferenceCount` | `int` | Referenz-Zähler |
| `LoadedAt` | `DateTime` | Zeitpunkt der Erstellung |

---

## Unterstützte Formate

| Format | Extension |
|--------|-----------|
| PNG | `.png` |
| JPEG | `.jpg`, `.jpeg` |
| TGA | `.tga` |
| TIFF | `.tif`, `.tiff` |

---

## Shader-Zuordnung

| Kontext | Unity Shader |
|---------|-------------|
| Character-Skins | `SoF2/MapSurface` |
| Gore-Stücke | `SoF2/MapSurface` (doppelseitig) |
| Waffen | `SoF2/MapSurface` |
| Hülsen | `SoF2/MapSurface` |
| Map Standard | `SoF2/MapSurface` |
| Map Wasser | `SoF2/Water` |
| Map Glas | `SoF2/Glass` |
| Map Metall | `SoF2/Metal` |
| Map Eis | `SoF2/Ice` |
| Map Poliert | `SoF2/Polished` |
| Decals | `SoF2/Decal` |
| Effekt-Partikel | `SoF2/EffectParticle` |

---

## Gore-Textur-Inventar

Texturen in `Art/Textures/models/characters/gore/`:

| Ordner | Dateien | Status |
|--------|---------|--------|
| `brain/` | `brain.jpg` | ✅ |
| `bone_long/` | `bone_long.png` | ✅ |
| `hand/` | — | ✅ Via gore.g2shader |
| `lung/` | — | ✅ Via gore.g2shader |
| `stomach/` | — | ✅ Via gore.g2shader |
| `guts/` | — | ✅ Via gore.g2shader |
| `chunk_lrg/` | chunk_lrg Texturen | ✅ |
| `chunk_med/` | chunk_med Texturen | ✅ |
| `chunk_smll/` | chunk_smll Texturen | ✅ |
| `chunk_gib_lrg/` | chunk_gib_lrg Texturen | ✅ |
| `chunk_gib_med/` | chunk_gib_med Texturen | ✅ |
| `exit_wound/` | exit_wound Texturen | ✅ |
| `rib_cage/` | rib_cage Texturen | ✅ |
| `shoulder_bone/` | — | ⚠️ Keine Textur vorhanden |
| `bone_small/` | — | ⚠️ Keine Textur vorhanden |

---

## ServiceLocator-Integration

```
ApplicationEntryPoint.Awake()
  ├── ServiceLocator.Register(new LegacyShaderLoader())   → .g2shader Parsing
  ├── ServiceLocator.Register(new TextureManager())        → Textur-Cache + Lazy Loading
  └── ServiceLocator.Register(new PrefabManager())         → Prefab-Cache + Addressables

// Abruf:
TextureManager tm = ServiceLocator.Get<TextureManager>();
TextureData data = tm.GetTextureData("models/characters/gore/brain/brain");
```

---

## Lifecycle

| Phase | Aktion |
|-------|--------|
| `Awake` | LegacyShaderLoader parst alle .g2shader Dateien |
| `Awake` | TextureManager scannt Verzeichnisse → Registry vorregistriert |
| Runtime | GetTextureData() → Lazy Load bei erstem Zugriff |
| Runtime | PrefabTextureApplier → Material-Erstellung bei Prefab-Instantiation |
| `OnDestroy` | ServiceLocator.ClearAll() → TextureRegistry.ClearCache() → Destroy Textures/Materials |
