# Round Flow System

Server-autoritatives Runden-Lifecycle-Management als StateMachine.
Steuert Map-Loading, Warmup, Match-Start, Countdown und Map-Wechsel.

---

## Architektur-Überblick

```
RoundFlowStateMachine : StateMachine<RoundFlowState, RoundFlowStateMachine>
  ├─ LoadingState          → Map wird geladen
  ├─ WaitingForReadyState  → Warten auf Spieler + Bots
  ├─ WarmupState           → g_warmup Sekunden Warmup
  ├─ StartingRoundState    → 3-2-1 Countdown
  ├─ RunningState          → Runde aktiv, Match-Countdown + GameLoop
  └─ SwitchingMapState     → Map-Wechsel-Countdown (5s)
```

---

## State-Übergänge

```
Loading → WaitingForReady (Map geladen, alle Clients ready)
  └─ WaitingForReady → Warmup (MinPlayers erreicht + Teams ready)
       └─ Warmup → StartingRound (Warmup-Timer abgelaufen)
            └─ StartingRound → Running (3-2-1 Countdown abgelaufen)
                 └─ Running → WaitingForReady (nächste Runde, gleiche Map)
                 └─ Running → SwitchingMap (Rundenlimit/Timelimit erreicht)
                      └─ SwitchingMap → Loading (MapLoader startet nächste Map)
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **RoundFlowStateMachine** | `Assets/Scripts/Runtime/Game/Networked/RoundFlowStateMachine.cs` | Alle States + State Machine |
| **NetworkedGameState** | `Assets/Scripts/Runtime/Game/Networked/NetworkedGameState.cs` | NetworkVariables, ApplyGametypeResult, BroadcastMatchStarted |
| **GametypeManager** | `Assets/Scripts/Runtime/Management/GametypeManagement/GametypeManager.cs` | Gametype-Delegation |
| **AIBotSpawner** | `Assets/Scripts/Runtime/Game/Networked/AIBotSpawner.cs` | Bot Spawn/Despawn/Respawn |
| **ServerConfigurationLoader** | `Assets/Scripts/Runtime/Management/DataManagement/ServerConfigurationLoader.cs` | g_warmup, sv_mapRotation |

---

## States im Detail

### LoadingState

- **Enter**: Wartet auf `MapLoadPhase.Finished` Event vom MapLoader
- **Transition**: → `WaitingForReadyState` wenn Map fertig + alle Clients `ClientReadyForRound` gemeldet

### WaitingForReadyState

- **Enter**:
  - Spawnt AI-Bots via `AIBotSpawner.SpawnInitialBots()`
  - Subscribet auf `OnInitialBotsSpawned`
  - Prüft `MinPlayersReached` via `UpdatePlayerCounts()`
- **OnBotsSpawned()**: `UpdatePlayerCounts()` → `TryStartMatch()`
- **TryStartMatch()**: Prüft `MinPlayersReached` + `AreTeamsReady()` → Transition zu Warmup
- **Player-Count**: `GetTotalPlayerCount()` = Humans + `AIBotSpawner.SpawnedBotCount`

### WarmupState

- **Enter**: Startet Warmup-Countdown (`g_warmup`, Default 10s)
- **Ende**: `AIBotSpawner.RespawnAllBots()` + Spieler-Respawn
- **Transition**: → `StartingRoundState`

### StartingRoundState

- **Enter**: 3-2-1 Countdown, Spieler-Movement gesperrt
- **Transition**: → `RunningState`

### RunningState

Enthält **zwei parallele Coroutines**:

1. **GameLoop** (jeden Frame):
   - `GametypeManager.OnRunFrame(deltaTime)` — Gametype-Logik
   - Phase-Synchronisation (`gametypePhase`, `phaseTimeRemaining`)
   - HideAndSeek-spezifisch: Seeker-Unlock bei Phase-Wechsel
   - `m_RoundRestartPending` prüfen → `ShouldSwitchMap()` → WaitingForReady oder SwitchingMap

2. **RunCountdown** (jede Sekunde):
   - `matchCountdown.Value--` bis 0
   - `MatchElapsedTime += 1f`
   - Bei 0: `OnTimeExpired()` → `ApplyGametypeResult()` → Restart oder SwitchMap

**OnRoundRestartRequested(delay)**: Setzt `m_RoundRestartPending = true` für GameLoop

### SwitchingMapState

- **Enter**: 5s Countdown, nächste Map aus `sv_mapRotation` ermitteln
- **MapLoader**: Lädt nächste Map → `MapLoadPhase.Started` → zurück zu `LoadingState`

---

## Map-Wechsel-Logik (ShouldSwitchMap)

```
ShouldSwitchMap():
  1. IsRoundLimitReached() → true → Map wechseln
  2. roundlimit == 0 && timelimit > 0 && MatchElapsedTime >= timelimit → Map wechseln
  3. roundlimit == 0 && timelimit == 0 → nie wechseln (unendlich)
```

Map-Rotation: `sv_mapRotation` Array (Round-Robin via `GetNextMapFromRotation()`).

---

## Player-Count-System

```csharp
GetTotalPlayerCount():
  return ConnectedClientsIds.Count + AIBotSpawner.SpawnedBotCount

UpdatePlayerCounts():
  playersConnected.Value = GetTotalPlayerCount()
  MinPlayersReached = (playersConnected.Value >= sv_minclients)
```

Wird aufgerufen bei:
- `Initialize()` — initialer Count
- `OnClientConnected()` — Spieler beitritt
- `OnClientDisconnected()` — Spieler verlässt
- `OnBotsSpawned()` — Bots gespawnt

---

## Team-Counting

```csharp
CountTeams():
  foreach ConnectedClientsIds → NetworkedCharacterState.TeamId
  foreach AIBotSpawner.SpawnedBots → NetworkedCharacterState.TeamId
  return (redCount, blueCount)
```

---

## NetworkVariables (synchronisiert an Clients)

| Variable | Typ | Beschreibung |
|----------|-----|-------------|
| `matchCountdown` | `NetworkVariable<uint>` | Sekunden bis Rundenende |
| `playersConnected` | `NetworkVariable<int>` | Spieler + Bots verbunden |
| `currentRound` | `NetworkVariable<int>` | Aktuelle Rundennummer |
| `roundLimit` | `NetworkVariable<int>` | Max. Runden |
| `gametypePhase` | `NetworkVariable<int>` | Gametype-Phase (z.B. HideAndSeekPhase) |
| `phaseTimeRemaining` | `NetworkVariable<uint>` | Restzeit in aktueller Phase |
| `redTeamScore` | `NetworkVariable<int>` | Rot Team-Score |
| `blueTeamScore` | `NetworkVariable<int>` | Blau Team-Score |
