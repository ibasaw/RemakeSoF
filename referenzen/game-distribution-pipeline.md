# Game Distribution Pipeline — Referenz

Dokumentation der Build-Verteilungs-Pipeline: Unity Build → Archiv → Server → Launcher → Spieler.

---

## Übersicht

```
Unity Build (IL2CPP, Release)
        ↓
Post-Build Script (Python):
  1. Optional: Code Signing (signtool)
  2. SHA256-Hashes generieren (Manifest)
  3. Archiv erstellen (.zip)
  4. Archiv-Hash berechnen
  5. Manifest + Archiv auf Server hochladen
        ↓
FastAPI Backend:
  - GET /api/game/version → aktuelle Version + Manifest
  - GET /api/game/download → Archiv streamen
        ↓
Launcher (Tauri/Rust):
  1. Version checken
  2. Archiv downloaden
  3. SHA256 vom Archiv verifizieren
  4. Entpacken in Install Directory
  5. Optional: einzelne File-Hashes verifizieren
```

---

## Build-Artefakt Struktur

### Client Build

```
RemakeSoF/
├── RemakeSoF.exe
├── UnityPlayer.dll
├── RemakeSoF_Data/
│   ├── Managed/
│   ├── Resources/
│   ├── StreamingAssets/
│   └── ...
├── MonoBleedingEdge/           (falls Mono statt IL2CPP)
└── Art/                         ← Manuell kopiert (Texturen, Sounds)
    ├── textures/
    ├── sound/
    └── models/
```

**Wichtig:** Der `Art/`-Ordner wird NICHT vom Unity-Build inkludiert, sondern manuell
zum Build kopiert. Das beschleunigt den Build und verkleinert das Unity-Build-Artefakt.
Texturen/Sounds werden zur Laufzeit über die Loader geladen (TextureManager, SoundManager).

### Server Build

```
RemakeSoF_Server/
├── RemakeSoF.exe
├── RemakeSoF_Data/
│   ├── Resources/
│   ├── StreamingAssets/
│   └── ...
└── Data/                        ← JSON-Definitionen (Waffen, Skins, etc.)
```

**Server braucht KEINEN Art/-Ordner** — keine Texturen, Sounds oder visuelle Assets.

---

## Manifest Format

```json
{
  "version": "1.2.0",
  "files": [
    {
      "path": "RemakeSoF.exe",
      "sha256": "a3f2b8c...",
      "size": 12345678
    },
    {
      "path": "UnityPlayer.dll",
      "sha256": "b7d1e4a...",
      "size": 8765432
    }
  ],
  "archive": {
    "url": "/api/game/download/1.2.0",
    "sha256": "c9e3f5d...",
    "size": 450000000
  }
}
```

---

## Post-Build Script (Python)

Erzeugt Manifest + ZIP aus Unity Build Output:

```python
import hashlib
import json
import os
import zipfile

def generate_manifest(build_dir: str, version: str) -> dict:
    files = []
    for root, _, filenames in os.walk(build_dir):
        for filename in filenames:
            filepath = os.path.join(root, filename)
            relative = os.path.relpath(filepath, build_dir).replace("\\", "/")
            sha256 = hashlib.sha256(open(filepath, "rb").read()).hexdigest()
            size = os.path.getsize(filepath)
            files.append({"path": relative, "sha256": sha256, "size": size})
    return {"version": version, "files": files}

def create_archive(build_dir: str, output_zip: str) -> str:
    with zipfile.ZipFile(output_zip, "w", zipfile.ZIP_DEFLATED) as zf:
        for root, _, filenames in os.walk(build_dir):
            for filename in filenames:
                filepath = os.path.join(root, filename)
                arcname = os.path.relpath(filepath, build_dir)
                zf.write(filepath, arcname)
    return hashlib.sha256(open(output_zip, "rb").read()).hexdigest()
```

---

## Code Signing

| Situation | Signierung nötig? |
|-----------|-------------------|
| Privater Kreis / Testing | Nein, SHA256-Hashes reichen |
| Öffentliche Distribution | Ja, sonst SmartScreen-Warnungen |
| Steam / Epic Store | Plattform übernimmt |

### Signierung ausführen (Windows)

```powershell
signtool sign /f MeinZertifikat.pfx /p Passwort /tr http://timestamp.digicert.com /td sha256 /fd sha256 RemakeSoF.exe
```

**Was signieren:** RemakeSoF.exe, remakesof-launcher.exe (optional alle DLLs).

**EV Code Signing** (~300-400€/Jahr): Sofort kein SmartScreen-Warning.
**Standard Code Signing** (~100-200€/Jahr): SmartScreen verschwindet nach genug Downloads/Reputation.

---

## Addressable-Gruppen

Assets sind in logische Addressable-Gruppen organisiert:

| Gruppe | Inhalt | Server | Client |
|--------|--------|--------|--------|
| Characters | Character-Modelle/Skeletons | Ja (Hitboxen) | Ja |
| Weapons | Waffen-Prefabs | Nein | Ja |
| Gore | Gore-Assets | Nein | Ja |
| Maps | Map-Prefabs | Ja (Collider) | Ja |
| Animators | Animator-Controller | Ja (Bone-Anim) | Ja |
| Chunks | Debris/Chunk-Meshes | Nein | Ja |

### Empfehlungen

- **Addressables Analyze** nutzen um Asset-Duplikation zwischen Gruppen zu prüfen
- Maps ggf. pro Map als eigene Gruppe (granulare Updates)
- Stabile Assets in "Core"-Gruppen, häufig geänderte separat

---

## Modding-System (SoF2-Stil)

Das Game hat ein dateibasiertes Mod-System ähnlich SoF2 pk3:

```
Game Installation/
├── RemakeSoF.exe
├── RemakeSoF_Data/              ← Unity Build + Addressables (Basis-Game)
├── Art/                          ← Texturen, Sounds (überschreibbar)
│   ├── textures/                ← Custom Textures overriden hier
│   ├── sound/                   ← Custom Sounds overriden hier
│   └── models/                  ← Custom Skins overriden hier
└── Data/                         ← JSON-Definitionen (erweiterbar)
    ├── SoF2_Weapons_New/        ← Waffen-Defs (neue Waffen hinzufügbar)
    ├── skin_definitions/        ← Skin-Defs (neue Skins hinzufügbar)
    └── effects/                 ← Effekt-Defs (neue Effekte hinzufügbar)
```

### Was ist moddbar?

| Asset-Typ | Moddbar? | Mechanismus |
|-----------|----------|-------------|
| Texturen | Ja | TextureRegistry → Art/textures/ override |
| Sounds | Ja | SoundManager → Art/sound/ Disk-Loading |
| Skins | Ja | SkinDefinitionLoader → Art/models/ |
| Shader-Defs | Ja | LegacyShaderLoader → .g2shader von Disk |
| Waffen-Defs | Ja | WeaponDataLoader → Data/SoF2_Weapons_New/ |
| Effekt-Defs | Ja | EffectDataLoader → Data/Effects/ JSON |
| Prefabs (3D) | Nein | Addressables sind Build-Zeit signiert |

### Zukunft: Custom Prefabs

Für 3D-Modell-Modding wäre ein separater AssetBundle-Loader aus einem `Mods/`-Ordner nötig.
Aktuell nicht implementiert — datengetriebene Erweiterung über JSON ist der primäre Weg.

---

## Delta/Patch-Updates (Zukunft)

Statt vollem Re-Download nur geänderte Dateien laden:

1. **File-Level Manifest vergleichen**: alte vs. neue SHA256-Hashes
2. Nur geänderte Dateien downloaden
3. Oder: binäre Diffs mit xdelta3/bsdiff für große Dateien

---

## Launcher-Projekt

Repository: `github.com/anatoli308/gamelauncher`
Stack: Tauri 1.x + React + Rust

### Komponenten-Zuordnung

| Komponente | Projekt |
|------------|---------|
| Post-Build-Script (ZIP + Manifest) | RemakeSoF / CI-Pipeline |
| Upload zum Server | Backend-API (FastAPI) |
| Version-Check + Download + Verify | Gamelauncher (Tauri/Rust) |
