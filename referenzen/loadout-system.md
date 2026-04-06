# Loadout System

Skin-Auswahl-View mit Live-3D-Character-Preview, QuakeColorLabel-Spielername, 
Skin-Thumbnail-Liste und Prev/Next-Navigation. Teil der Metagame-Scene.

---

## Architektur-Überblick

```
LoadoutController (MVC Controller)
  ├─ Listens: LoadNextSkinEvent, LoadPreviousSkinEvent, ChangeSkinByNameEvent
  ├─ Calls: PlayerSkinManager.LoadNextSkin() / LoadPreviousSkin() / ChangeSkin()
  ├─ Listens: PlayerSkinChangedEvent (from PlayerSkinManager.EventManager)
  └─ Updates: LoadoutView.SetCharacterPrefab() + UpdateSkinInfo()
         ↓
LoadoutView (MVC View)
  ├─ Player Info:  QuakeColorLabel Name, Player-ID
  ├─ Skin Info:    Name, Rarity, Description
  ├─ 3D Preview:   RenderTexture mit separater Camera + Light (Layer 30)
  ├─ Skin List:    Horizontale ScrollView mit klickbaren Thumbnails
  ├─ Navigation:   Prev/Next Buttons + Equip Button
  └─ Display Name: TextField mit Live-QuakeColorLabel-Overlay
```

---

## Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **LoadoutView** | `Assets/Scripts/Runtime/Metagame/Views/LoadoutView.cs` | View: 3D-Preview, Skin-Liste, Name-Input |
| **LoadoutController** | `Assets/Scripts/Runtime/Metagame/Controllers/LoadoutController.cs` | Controller: Skin-Wechsel-Logik |
| **PlayerSkinManager** | StateMachine | Skin-Lade/Anwende-Pipeline |

---

## UI-Layout

```
┌───────────────────────────────────────────────────────┐
│  [Player Name: ^1Sgt^7.Mayhem]    [Player ID: 12345] │
│                                                        │
│  ┌──────────────────────────────┐  Skin: mullins_jungle│
│  │                              │  Rarity: Common      │
│  │     3D Character Preview     │  Description: ...    │
│  │    (RenderTexture Layer 30)  │                      │
│  │                              │  Display Name:       │
│  │                              │  [____^1Test^7___]   │
│  │                              │  ^1Test^7  (overlay) │
│  └──────────────────────────────┘                      │
│                                                        │
│  [◄ Prev]                              [Next ►]       │
│                              [Equip]                   │
│                                                        │
│  ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐ ┌──┐      │
│  │🖼│ │🖼│ │🖼│ │🖼│ │🖼│ │🖼│ │🖼│ │🖼│ │🖼│      │
│  └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘ └──┘      │
│         ↑ Horizontale Skin-Thumbnail-Liste             │
└───────────────────────────────────────────────────────┘
```

---

## 3D Character Preview

### Stage-Setup (`CreateStage()`)

| Parameter | Wert |
|-----------|------|
| Preview Layer | `30` (eigener Layer, nur Preview-Camera sieht ihn) |
| Camera FOV | `34°` |
| Camera Offset | `(6, 0.3, 0)` |
| Clear Color | `(0, 0, 0, 0)` — Transparent |
| Character Rotation | `(0, 90, 0)` — Seitlich zum Kamerawinkel |
| Character Position | `(0, -0.95, 0)` — Vertikal zentriert |

### Rendering

- `RenderTexture` wird dynamisch erstellt/resized wenn Container-Größe sich ändert (`OnGeometryChanged`)
- `LateUpdate()` rendert Camera jeden Frame (`m_CharacterPreviewCamera.Render()`)
- RenderTexture wird auf `m_CharacterPreviewContainer.style.backgroundImage` gesetzt
- Beim Skin-Wechsel: alter Instance wird destroyed, neuer Prefab instantiiert + Layer rekursiv gesetzt

### Cleanup (`OnDisable()`)

- Unregisters alle Callbacks
- Released RenderTexture
- Destroys Preview-Stage (Camera, Light, Instanz)
- Cleared `m_LoadedIconTextures` (Thumbnail-Cache)

---

## Skin-Thumbnail-Liste

### Aufbau (`PopulateSkinList()`)

1. `SkinDefinitionLoader.GetAllSkinNames()` → Alle verfügbaren Skin-Namen
2. Icon-Dateien aus `Art/Textures/gfx/playericons/` laden
3. `BuildSkinIconMap(iconFileName)` — Regex-Parse: `"NPC_DisplayName ( skin_name ).jpg"` → `skin_name`
4. Pro Skin: Klickbares `VisualElement` mit Thumbnail-Textur erstellt
5. Gecacht in `m_SkinThumbnails : Dictionary<string, VisualElement>`
6. Texturen gecacht in `m_LoadedIconTextures : Dictionary<string, Texture2D>`

### Auswahl

- Klick auf Thumbnail → `OnSkinThumbnailClicked(skinName)` → Broadcast `ChangeSkinByNameEvent`
- `UpdateSkinListSelection(skinName)` → Highlight + ScrollIntoView

---

## QuakeColorLabel Integration

### Spielername-Anzeige

Das Spielername-Label wird dynamisch durch ein `QuakeColorLabel` ersetzt:
- Placeholder `Label` mit ID `playerNameLabel` wird aus dem Parent entfernt
- `QuakeColorLabel` wird an gleicher Stelle eingefügt
- Atlas wird aus `TextureConfiguration.scoreboard.bigcharsAtlas` geladen

### Display Name Input Overlay

Über dem `TextField` für den Display-Name liegt ein `QuakeColorLabel`-Overlay:
- Zeigt Live-Preview der Quake-Farbcodes während der Eingabe
- `OnDisplayNameChanged(ChangeEvent<string>)` → Aktualisiert beide QuakeColorLabels
- Input-Feld ist funktional (editierbar), Overlay zeigt farbige Version

---

## Controller-Logik (LoadoutController)

### Event-Subscriptions

| Event | Quelle | Handler |
|-------|--------|---------|
| `LoadNextSkinEvent` | EventManager (Metagame) | `OnClickLoadNextSkin()` → `PlayerSkinManager.LoadNextSkin()` |
| `LoadPreviousSkinEvent` | EventManager (Metagame) | `OnClickLoadPreviousSkin()` → `PlayerSkinManager.LoadPreviousSkin()` |
| `ChangeSkinByNameEvent` | EventManager (Metagame) | `OnChangeSkinByName()` → `PlayerSkinManager.ChangeSkin(name)` |
| `PlayerSkinChangedEvent` | PlayerSkinManager.EventManager | `OnPlayerSkinChanged()` → View aktualisieren |
| `ConnectionEvent` | ConnectionManager.EventManager | `OnConnectionEvent()` → View ausblenden bei Connecting |

### Skin-Wechsel-Flow

```
User klickt Next/Prev/Thumbnail
  → Event Broadcast
  → LoadoutController triggert PlayerSkinManager
  → PlayerSkinManager lädt Prefab + Texturen (StateMachine: Loading → Applied)
  → PlayerSkinChangedEvent gefeuert
  → LoadoutController.OnPlayerSkinChanged()
    → View.SetCharacterPrefab(newPrefab)  — 3D Preview aktualisiert
    → View.UpdateSkinInfo(skinName)       — Labels + Thumbnail-Highlight
```

---

## Texturen

| Textur-Key | Verwendung |
|-----------|-----------|
| `tabUnselectedIcon` / `tabSelectedIcon` | Tab-Icons |
| `scrollbarThumb` / `scrollbarTrack` / etc. | ScrollView-Customization |
| `bigcharsAtlas` | QuakeColorLabel Atlas (Spielername + Input-Overlay) |
