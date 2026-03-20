# Map Loading & Match Lifecycle – Referenzdokumentation

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

- **ServiceLocator:** `PrefabManager`, `TextureManager`, `MapDataLoader`
- **TextureManager:** Scannt `Assets/Art/Textures` (System) + `persistentDataPath/CustomTextures` (Custom)
- **TextureRegistry:** Cache-first, Lazy Loading via Custom Loaders
- **Ghoul2Meta:** Dynamischer Property-Container auf jedem Map-Mesh
- **FBXGhoul2PropsImporter:** Editor AssetPostprocessor, überträgt FBX Custom Props → Ghoul2Meta

---

## Loading-Screen UI (MapLoadingView / MapLoadingController)

**Dateien:**
- `Assets/Scripts/Runtime/Game/Views/MapLoadingView.cs`
- `Assets/Scripts/Runtime/Game/Controllers/MapLoadingController.cs`
- `Assets/UI/Game/MapLoadingView.uxml`

### Funktionsweise

Der Loading-Screen zeigt pro Map-Wechsel einen LevelShot-Hintergrund, Map-Namen, Status-Text und einen animierten Fortschrittsbalken.

**Queue-basierte Animation:**

MapLoadPhases werden intern nicht sofort angezeigt, sondern in eine `Queue<MapLoadPhase>` eingereiht. Der Balken animiert per `Mathf.MoveTowards` (Speed `0.55` Einheiten/s) zum Ziel des aktuellen Segments. Erst wenn ein Segment erreicht ist, wird die nächste Phase aus der Queue geholt. Das stellt sicher, dass jede Phase sichtbare Bildschirmzeit bekommt — auch wenn alle Phasen in < 1 Frame vom MapLoader gefeuert werden.

**Mindestanzeigedauer:** `k_MinDisplayTime = 3f` Sekunden — der Screen bleibt mindestens so lange sichtbar, auch wenn das Laden schneller fertig ist.

**OnReadyToHide-Event:** Wenn der Balken bei 100% ankommt UND die Mindestzeit erreicht ist, feuert `OnReadyToHide`. Der Controller reagiert darauf:

1. `View.Hide()` — Loading-Screen weg
2. `App.View.Match.Show()` — Match-HUD sichtbar
3. `NetworkedGameState.NotifyGameplayVisibleOnClient()` — Client meldet dem Server "bin bereit"

### LevelShot-Hintergrund

`SetMapInfo(MapDefinition)` wird bei `OnMapChangeStarting` aufgerufen:

1. `m_TitleLabel.text = "Loading {mapName}"`
2. `TextureManager.GetTextureData(mapDef.levelShotBackgroundTexturePath)` → Texture auf `m_Background.style.backgroundImage`
3. Fortschritt wird auf 0 zurückgesetzt, Queue geleert
4. `m_ShowTimestamp = Time.time` für Mindestanzeigedauer

### UXML

```xml
<ui:VisualElement name="mapLoading" style="flex-grow:1; height:100%; -unity-background-scale-mode:scale-and-crop;">
```

Vollbild-Hintergrund mit `scale-and-crop` für korrekte Skalierung.

---

## Match Recap UI (MatchRecapView / MatchRecapController)

**Dateien:**
- `Assets/Scripts/Runtime/Game/Views/MatchRecapView.cs`
- `Assets/Scripts/Runtime/Game/Controllers/MatchRecapController.cs`
- `Assets/UI/Game/MatchRecapView.uxml`

### Funktionsweise

Nach Ablauf des Match-Countdowns zeigt der Recap-Screen:

1. **"Game Over!"** — sofort sichtbar
2. **Map-Switch-Countdown:** "Next map: {mapName} in 5/4/3/2/1..." → "Loading {mapName}..."

**Controller** abonniert `EndMatchEvent` (gebroadcastet vom `MatchController`), dann:

- `mapSwitchCountdown.OnValueChanged` → aktualisiert Sekundenanzeige
- `nextMapName.OnValueChanged` → holt Anzeigenamen via `MapDataLoader.GetByMapId()`

### UXML

```xml
<ui:Label name="mapSwitchLabel" />
```

Ersetzte den vorherigen "Continue"-Button — Spieler steuern den Map-Wechsel nicht mehr manuell.

---

## RoundFlowStateMachine (Serverseitiger Match-Lifecycle)

**Datei:** `Assets/Scripts/Runtime/Game/Networked/RoundFlowStateMachine.cs`

Die `RoundFlowStateMachine` erbt von `StateMachine<RoundFlowState, RoundFlowStateMachine>` (Core CRTP-Pattern) und steuert den gesamten Match/Map-Lifecycle serverseitig.

### State-Diagramm

```
                    MapLoadPhase.Started
         ┌───────────────────────────────────┐
         ▼                                   │
   ┌──────────┐   MapLoadPhase.Complete  ┌───┴──────────┐
   │ Loading  │ ──────────────────────→  │ WaitingFor   │
   └──────────┘                          │   Ready      │
                                         └──────┬───────┘
                                                │ MinPlayers + alle Clients ready
                                                ▼
                                         ┌──────────────┐
                                         │ StartingRound│
                                         │  (3.1s Delay)│
                                         └──────┬───────┘
                                                │ Delay abgelaufen
                                                ▼
                                         ┌──────────────┐
                                         │   Running    │
                                         │  (Countdown) │
                                         └──────┬───────┘
                                                │ Countdown = 0
                                                ▼
                                         ┌──────────────┐
                                         │ SwitchingMap │
                                         │ (5s Countdown│
                                         │  + Map laden)│
                                         └──────────────┘
```

### States im Detail

| State | Enter | Exit | Events | Transition |
|---|---|---|---|---|
| **LoadingState** | Cleared ready-set, `matchCountdown = 0` | — | `OnMapLoadPhase(Complete)` | → WaitingForReady |
| **WaitingForReadyState** | `RespawnAllConnectedPlayers()`, Host auto-ready | — | `OnClientReadyForRound()`, `OnMinPlayersReached()`, `OnClientConnected()` | → StartingRound (wenn MinPlayers + alle ready) |
| **StartingRoundState** | Startet 3.1s Delay-Coroutine | Stoppt Coroutine | `OnClientDisconnected()` | → Running oder zurück zu WaitingForReady |
| **SwitchingMapState** | `BroadcastMatchEnded()`, 5s Countdown-Coroutine | Stoppt Coroutine | `OnMapLoadPhase(Started)` | → Loading |
| **RunningState** | `BroadcastMatchStarted()`, Match-Countdown-Coroutine (aus MapDefinition.countdownStartValue, Default 300s) | Stoppt Coroutine | — | → SwitchingMap (Countdown = 0) |

### Shared Data in StateMachine

- `MinPlayersReached: bool` — wird bei Connect/Disconnect aktualisiert
- `ClientsReadyForRound: HashSet<ulong>` — Client-IDs die "Gameplay sichtbar" gemeldet haben
- `GameState: NetworkedGameState` — Referenz auf Netzwerk-Logik (NetworkVariables, RPCs)
- `RoundStartDelaySeconds = 3.1f` — Pause zwischen "alle bereit" und Match-Start

### Event-Delegation

`NetworkedGameState` leitet Server-Events direkt an die StateMachine weiter:

| NGS Event-Source | SM Methode | Beschreibung |
|---|---|---|
| `MapLoader.OnProgress` | `OnMapLoadPhase(phase)` | Map-Ladephasen |
| `NotifyGameplayVisibleServerRpc` | `OnClientReadyForRound(clientId)` | Client meldet Loading-Screen weg |
| `MinNumberPlayersConnectedEvent` | `OnMinPlayersReached()` | MinPlayers-Schwelle erreicht |
| `ClientConnectedEvent` | `OnClientConnected()` | Spieler verbindet sich |
| `ClientDisconnectedEvent` | `OnClientDisconnected()` | Spieler trennt sich |

---

## NetworkedGameState (Match-Netzwerk-Synchronisation)

**Datei:** `Assets/Scripts/Runtime/Game/Networked/NetworkedGameState.cs`

### NetworkVariables (Server → Client sync)

| Variable | Typ | Beschreibung |
|---|---|---|
| `matchCountdown` | `NetworkVariable<uint>` | Verbleibende Match-Sekunden |
| `playersConnected` | `NetworkVariable<int>` | Aktuelle Spieleranzahl |
| `mapSwitchCountdown` | `NetworkVariable<uint>` | Sekunden bis Map-Wechsel (0 = inaktiv) |
| `nextMapName` | `NetworkVariable<FixedString128Bytes>` | Nächste Map im Rotation |
| `currentMapName` | `NetworkVariable<FixedString128Bytes>` | Aktuell geladene Map |

### Events (lokal)

| Event | Beschreibung | Ausgelöst von |
|---|---|---|
| `OnMatchStarted` | Match-Runde gestartet | `BroadcastMatchStarted()` via RunningState |
| `OnMatchEnded` | Match-Runde beendet | `BroadcastMatchEnded()` via SwitchingMapState |
| `OnMapLoadProgress` | MapLoadPhase für UI | `MapLoader.OnProgress` |
| `OnMapChangeStarting` | Neue Map wird geladen (mit MapDefinition) | `currentMapName.OnValueChanged` |

### RPCs

| RPC | Richtung | Beschreibung |
|---|---|---|
| `NotifyGameplayVisibleServerRpc` | Client → Server | "Loading-Screen weg, bin bereit" |
| `ClientStartMatchRpc` | Server → Clients | Match-Start broadcasten |
| `ClientEndMatchRpc` | Server → Clients | Match-Ende broadcasten |

### Server Lifecycle (OnNetworkSpawn)

1. Registriert ConnectionManager-Events (MinPlayers, Connect, Disconnect)
2. `m_RoundFlowStateMachine.Initialize(this)` — SM startet in LoadingState
3. `currentMapName = k_DefaultMapName` → löst Map-Laden aus

### Client Lifecycle (OnNetworkSpawn)

1. Prüft ob `currentMapName` bereits gesetzt → manuell `FireMapChangeStarting()` (weil `OnValueChanged` nicht für initialen Sync feuert)
2. `m_MapLoader.LoadMapAsync(mapName)` — Map für Visuals laden
3. `currentMapName.OnValueChanged` → reagiert auf zukünftige Map-Wechsel

---

## Spieler-Respawn bei Map-Wechsel

**Dateien:**
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs`
- `Assets/Scripts/Runtime/Game/Characters/Server/ServerPlayerCharacter.cs`

### RespawnAtNextSpawnPoint() (Server-only)

1. `ServerPlayerSpawnPoints.Instance.ConsumeNextSpawnPoint()` → Position + Rotation
2. `transform.SetPositionAndRotation(position, rotation)` — Teleport
3. `m_ServerPosition.Value = position` — NetworkVariable sync
4. `CorrectionClientRpc(position, rotation)` — Client hart korrigieren (keine alte Prediction sichtbar)
5. `m_ServerPlayerCharacter.ResetForRespawn()` — Physik-State zurücksetzen
6. `m_ServerPlayerCharacter.SetReady()` — Character wieder aktiv

### ResetForRespawn()

```csharp
m_Simulation.SetState(Vector3.zero, true, false, false, 0f);
```

Setzt Velocity auf Zero, `isGrounded = true`, Jump/Crouch auf false, Timer auf 0 — verhindert dass Spieler mit alter Velocity weiterfliegen oder durch die Map fallen bevor Collider bereit sind.

### Wann wird respawned?

Der `WaitingForReadyState` ruft `RespawnAllConnectedPlayers()` in `Enter()` auf. Das passiert:
- Beim ersten Map-Load (nach `MapLoadPhase.Complete`)
- Bei jedem Map-Wechsel (nach neuem `MapLoadPhase.Complete`)

---

## Match-Timer Gating

Der Match-Countdown startet **nicht** sofort wenn MinPlayers erreicht sind, sondern erst wenn **alle** Bedingungen erfüllt sind:

1. **MinPlayers erreicht** — `MinPlayersReached = true`
2. **Server-Map fertig** — `MapLoadPhase.Complete` empfangen → WaitingForReadyState aktiv
3. **Alle Clients ready** — Jeder Client hat `NotifyGameplayVisibleOnClient()` gesendet (Loading-Screen weg)
4. **Host-Mode** — Host-Client wird automatisch als ready registriert in `WaitingForReadyState.Enter()`
5. **3.1s Verzögerung** — `StartingRoundState` wartet kurz, damit UI-Transitionen smooth sind

Erst dann wechselt die SM in `RunningState` → `BroadcastMatchStarted()` → Countdown beginnt.

### Bei Disconnect

- **Während WaitingForReady/StartingRound:** SM bleibt/geht zurück zu `WaitingForReadyState`, abgetrennte Client-IDs werden aus `ClientsReadyForRound` entfernt
- **Während Running:** Match läuft weiter (Countdown stoppt nicht)

---

## Map-Rotation

Gesteuert durch `SwitchingMapState`:

1. `MapDataLoader.GetNextMapId(currentMap)` → nächste Map in Rotation
2. `nextMapName.Value = nextMap` → Clients sehen "Next map: X"
3. `mapSwitchCountdown` zählt von 5 auf 0 (1x pro Sekunde)
4. `currentMapName.Value = nextMap` → löst `OnMapNameChanged` aus
5. `MapLoader.LoadMapAsync()` startet → `MapLoadPhase.Started` → SM geht in `LoadingState`
6. Cycle beginnt von vorne

### Map-Definitionen

Aus `Assets/Resources/Data/SoF2_Maps.json`:

```json
{
    "mapId": "maps/mp_hos1",
    "mapName": "Hospital",
    "countdownStartValue": 15,
    "levelShotBackgroundTexturePath": "levelshots/mp_hos1"
}
```

---

## Controller-Architektur (MVC)

| Controller | View | Verantwortung |
|---|---|---|
| `MapLoadingController` | `MapLoadingView` | Loading-Screen, Fortschrittsbalken, LevelShot |
| `MatchRecapController` | `MatchRecapView` | Game Over + Map-Switch-Countdown |
| `MatchController` | `MatchView` | Match-Timer, HUD, Input-Aktivierung |

### Event-Flow (vereinfacht)

```
[Server] LoadMapAsync → MapLoadPhase.Complete
           ↓
    RoundFlowStateMachine: Loading → WaitingForReady
           ↓                         (RespawnAllPlayers)
    [Client] Loading-Screen animiert → OnReadyToHide
           ↓
    NotifyGameplayVisibleServerRpc
           ↓
    RoundFlowStateMachine: WaitingForReady → StartingRound → Running
           ↓
    BroadcastMatchStarted → ClientStartMatchRpc
           ↓
    MatchController.OnMatchStarted → SetInputsActive(true) + StartMatchEvent
           ↓
    ... Countdown läuft ...
           ↓
    RunningState → SwitchingMapState
           ↓
    BroadcastMatchEnded → ClientEndMatchRpc
           ↓
    MatchController.OnMatchEnded → EndMatchEvent → MatchRecapView
           ↓
    5s Map-Switch-Countdown → currentMapName gesetzt → Cycle neu
```
