# Custom Skins / Workshop — Architektur & Machbarkeit

## Aktuelle Lade-Pipeline (Runtime)

### Textur-Loading (TextureManager)
- **Kein** Unity Asset Pipeline / Addressables für Texturen
- Alles per `File.ReadAllBytes()` + `Texture2D.LoadImage()` von Disk
- **Zwei Scan-Verzeichnisse** (beim Start):
  1. `Application.dataPath + "/Art/Textures"` (System-Texturen)
  2. `Application.persistentDataPath + "/CustomTextures"` (Custom, überschreibt System)
- Unterstützte Formate: `.png`, `.jpg`, `.jpeg`, `.tga`, `.tif`, `.tiff`
- **Key** = relativer Pfad ohne Extension (z.B. `models/characters/average_sleeves/body`)
- **Lazy Loading**: TextureData wird registriert mit FilePath, Texture2D wird erst bei erstem Zugriff geladen

### Skin-Definitionen (SkinDefinitionLoader)
- JSON-Dateien aus `StreamingAssets/Data/skin_data/*.json`
- Geladen per `File.ReadAllText()` + Newtonsoft JSON
- Definiert: Model-Name, Material-Gruppen (texture1, shader1, texture2, ...)

### Shader-Definitionen (LegacyShaderLoader)
- `.g2shader` Dateien aus `StreamingAssets/Data/shaders/`
- Geladen per `File.ReadAllText()` + eigener Parser (ShaderDataReader)
- Enthält: MainTexture, EditorImage, CullDisabled, HitLocation, HitMaterial

### Character-Prefabs + Animator
- **Addressables** (einzige Addressables-Nutzung in der Skin-Pipeline)
- Key: `characters/models/{model_name}`
- Animator: `models/animator/{name}`

### Material-Erstellung (Runtime)
- `Shader.Find("SoF2/MapSurface")` → neues Material
- Texture2D von TextureManager → `material.SetTexture("_BaseMap", tex)`
- Materialien werden gecacht (Key = Textur-Key)

## End-to-End Flow: Skin laden

```
ChangeSkin("mullins_non_combat")
  → SkinDefinitionLoader.GetByName() → SkinDefinition (JSON)
  → skinDefinition.GetModelName() → "average_sleeves"
  → PrefabManager.LoadPrefab("characters/models/average_sleeves") → GameObject (Addressables)
  → PlayerSkinApplier.ApplyAnimatorController() (Addressables)
  → TextureManager.CreateMaterialsFromSkinDefinition()
      → Für jede Material-Gruppe:
          → Key = texture1 ?? shader1
          → TextureRegistry.GetTextureData(key) → LazyLoad von Disk
          → Shader.Find("SoF2/MapSurface") + SetTexture → Material
  → PlayerSkinApplier.ApplySurfaceDefinitions() → Material auf Renderer
  → PlayerSkinApplier.DisableAndEnableSurfaces() → Sichtbarkeit
```

## Custom Skins — Was bereits funktioniert ✅

1. **Override-Mechanismus**: Datei in `persistentDataPath/CustomTextures/` mit gleichem relativen Pfad überschreibt System-Textur
2. **Disk-basiertes Loading**: Keine Unity-Importierung nötig, reine Bilddateien reichen
3. **ITextureLoader Interface**: Custom Loader können registriert werden
4. **TextureRegistry.RegisterTextureData()**: Programmatisch neue Textur-Keys mit beliebigem Dateipfad

## Was für Upload-Feature fehlt

### Clientseitig (nur eigener Skin sichtbar)
| Feature | Aufwand | Details |
|---------|---------|---------|
| TextureManager.Reload() | Klein | Cache clearen + Verzeichnisse neu scannen. Methode existiert auskommentiert |
| Custom Skin-Definitionen | Klein | SkinDefinitionLoader um `persistentDataPath` Scan-Pfad erweitern |
| UI für Skin-Auswahl | Mittel | Datei-Dialog oder Drag&Drop im Metagame |

### Multiplayer (andere sehen den Skin)
| Feature | Aufwand | Details |
|---------|---------|---------|
| Authserver Upload-Endpoint | Mittel | `POST /api/workshop/submit` — Dateien validieren + speichern |
| Download-on-Demand | Mittel | HTTP-Download → persistentDataPath speichern → TextureRegistry registrieren |
| Skin-Sync Protokoll | Mittel | Server broadcastet Skin-Info, Clients laden fehlende Skins nach |
| Server-seitige Validierung | Wichtig | Dateigröße, Format, Dimensionen prüfen. Kein Schadcode |
| Skin-Katalog Endpoint | Klein | `GET /api/workshop/skins` — Liste verfügbarer Custom Skins |

### Security-Überlegungen
- **Dateigrößen-Limit**: Max 4MB pro Textur (4K PNG unkomprimiert ~48MB, komprimiert ~4MB)
- **Dimensionen-Limit**: Max 4096x4096 Pixel
- **Format-Validierung**: Nur PNG/JPG, Header-Check nicht nur Extension
- **Sanitized Filenames**: Keine Path-Traversal (`../`) in Dateinamen
- **Rate Limiting**: Max N Uploads pro User pro Stunde
- **Content Moderation**: Optional — manuelles Review oder Hash-basierte Duplikat-Erkennung

## Architektur-Diagramm (Ziel)

```
┌─────────────────────────────────────────────┐
│  Game Client (Unity)                        │
│                                             │
│  persistentDataPath/CustomTextures/         │
│    └── models/characters/custom_skin/       │
│         ├── body.png    ← lokal oder        │
│         └── head.png      heruntergeladen   │
│                                             │
│  StreamingAssets/Data/skin_data/            │
│    └── custom_skin.json  ← Skin-Definition  │
│                                             │
│  TextureManager scannt beide Ordner         │
│  → LazyLoad → Material → Renderer          │
└──────────────┬──────────────────────────────┘
               │ Upload / Download
     ┌─────────▼──────────┐
     │  Authserver (API)  │
     │  /api/workshop/    │
     │  submit / list /   │
     │  download          │
     └────────────────────┘
```
