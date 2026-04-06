# Hide and Seek Gametype

Rundenbasierter asymmetrischer Spielmodus: Hider (Rot) verstecken sich, Seeker (Blau) suchen und eliminieren.
Implementiert als `HideAndSeekGametype : BaseGametype` im Gametype-System.

---

## Architektur-Überblick

```
ServerConfiguration (JSON)
  └─ hideandseek_* CVARs
       └─ GametypeManager.Initialize()
            └─ HideAndSeekGametype.Initialize(definition, serverConfig)

Runden-Ablauf:
  OnRoundStart() → Hiding-Phase (HideTime Sekunden)
       └─ Seeker eingefroren (SetTeamMovementLocked)
       └─ OnPhaseTimeExpired()
            └─ Seeking-Phase (SeekTime Sekunden)
                 ├─ Seeker entsperrt
                 ├─ OnClientDeath() → Hider eliminiert → AliveHiderCount--
                 │    └─ AliveHiderCount <= 0 → Seeker gewinnen
                 └─ OnPhaseTimeExpired()
                      └─ Hider überleben → m_HidersSurvived = true
                           └─ OnTimeExpired() → Hider gewinnen + Survival-Kills
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **HideAndSeekGametype** | `Assets/Scripts/Runtime/Management/GametypeManagement/HideAndSeekGametype.cs` | Gametype-Logik, Phasen, Scoring |
| **BaseGametype** | `Assets/Scripts/Runtime/Management/GametypeManagement/BaseGametype.cs` | Basis: OnRoundStart (CurrentRound++), Defaults |
| **GametypeManager** | `Assets/Scripts/Runtime/Management/GametypeManagement/GametypeManager.cs` | Orchestrator, delegiert an ActiveGametype |
| **IGametype** | `Assets/Scripts/Runtime/Management/GametypeManagement/IGametype.cs` | Interface: alle Gametype-Methoden |
| **GametypeEventResult** | `Assets/Scripts/Runtime/Management/GametypeManagement/GametypeEventResult.cs` | Return-Struct: Score-Deltas, Restart, Messages |
| **RoundFlowStateMachine** | `Assets/Scripts/Runtime/Game/Networked/RoundFlowStateMachine.cs` | Runden-Lifecycle, Phase-Sync, Countdown |
| **NetworkedGameState** | `Assets/Scripts/Runtime/Game/Networked/NetworkedGameState.cs` | ApplyGametypeResult, AwardSurvivalKills |
| **ServerCharacterController** | `Assets/Scripts/Runtime/Game/Characters/Server/ServerCharacterController.cs` | OnDeath → OnClientDeath() → RequestRoundRestart |

---

## Server-Konfiguration (CVARs)

```json
{
    "hideandseek_hidetime": 30,
    "hideandseek_seektime": 120,
    "hideandseek_seekercount": 1,
    "hideandseek_hidermodel": "",
    "hideandseek_seekermodel": "",
    "hideandseek_hiderweapons": false,
    "hideandseek_seekerweapons": true,
    "hideandseek_roundlimit": 5
}
```

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `hideandseek_hidetime` | `int` | `30` | Versteckzeit (Sekunden) |
| `hideandseek_seektime` | `int` | `120` | Suchzeit (Sekunden) |
| `hideandseek_seekercount` | `int` | `1` | Anzahl Seeker pro Runde |
| `hideandseek_hiderweapons` | `bool` | `false` | Hider dürfen Waffen benutzen |
| `hideandseek_seekerweapons` | `bool` | `true` | Seeker dürfen Waffen benutzen |
| `hideandseek_roundlimit` | `int` | `5` | Max. Runden bevor Map-Wechsel |
| `hideandseek_hidermodel` | `string` | `""` | Erzwungenes Hider-Model (leer = frei) |
| `hideandseek_seekermodel` | `string` | `""` | Erzwungenes Seeker-Model (leer = frei) |

---

## Phasen-System

### HideAndSeekPhase Enum

| Phase | Wert | Beschreibung |
|-------|------|-------------|
| `Hiding` | `0` | Versteckphase — Seeker eingefroren |
| `Seeking` | `1` | Suchphase — Seeker aktiv |
| `RoundOver` | `2` | Runde beendet, Ergebnis wird angezeigt |

### Phasen-Synchronisation

Phase wird über `NetworkedGameState.gametypePhase` (NetworkVariable) an Clients synchronisiert.
`phaseTimeRemaining` (NetworkVariable) zeigt verbleibende Zeit pro Phase.

`RoundFlowRunningState.GameLoop()` prüft jeden Frame:
- `GetCurrentPhase()` → bei Änderung → `gametypePhase.Value = currentPhase`
- `GetPhaseTimeRemaining()` → `phaseTimeRemaining.Value` (auf Sekunden gerundet)
- Phasen-Übergang Hiding→Seeking: `SetTeamMovementLocked(Blue, false)` → Seeker entsperrt

---

## Scoring-System

### Seeker gewinnt (alle Hider eliminiert)

```
OnClientDeath(victim=Hider, killer=Seeker)
  ├─ AliveHiderCount--
  ├─ Killer bekommt +1 ClientScore
  └─ if AliveHiderCount <= 0:
       ├─ Blue TeamScore +1
       ├─ RestartRound = true (5s Delay)
       └─ BroadcastMessage: "Alle Hider gefunden!"
```

### Hider gewinnen (Zeit abgelaufen)

Zwei Timer laufen parallel:
1. **Phase-Timer** (`OnRunFrame` → `PhaseTimeRemaining -= deltaTime`)
2. **Countdown** (`RunCountdown` → `matchCountdown.Value--` jede Sekunde)

**Race Condition behoben** durch `m_HidersSurvived` Flag:

```
OnPhaseTimeExpired() (Seeking → RoundOver):
  └─ m_HidersSurvived = true
  └─ CurrentPhase = RoundOver

OnTimeExpired() (Countdown = 0):
  ├─ if RoundOver && m_HidersSurvived:
  │    ├─ Red TeamScore +1
  │    ├─ AwardSurvivalKillsToTeam = Red (alle lebenden Hider +1 Kill)
  │    └─ RestartRound = true
  ├─ if RoundOver && !m_HidersSurvived:
  │    └─ RestartRound = true (Score bereits bei Eliminierung vergeben)
  └─ if !RoundOver:
       ├─ Red TeamScore +1
       ├─ AwardSurvivalKillsToTeam = Red
       └─ RestartRound = true
```

### AwardSurvivalKills

`NetworkedGameState.AwardSurvivalKills(GametypeTeam.Red)`:
- Iteriert alle menschlichen Spieler + AI-Bots
- Prüft `TeamId == Red && IsAlive`
- Jeder lebende Hider bekommt `AddKill()` (+1 Kill-Score)

---

## Team-Zuweisung

```csharp
AssignTeam(redCount, blueCount):
  if blueCount < SeekerCount → Blue (Seeker)
  else → Red (Hider)
```

Seeker-Slots werden zuerst gefüllt, Rest wird Hider.

---

## Waffen-Konfiguration

```csharp
GetStartWeapons(team) → ["knife"]  // Alle starten nur mit Messer
HidersHaveWeapons() → hideandseek_hiderweapons (default false)
SeekersHaveWeapons() → hideandseek_seekerweapons (default true)
```

---

## Runden-Limit & Map-Wechsel

```csharp
IsRoundLimitReached() → RoundLimit > 0 && CurrentRound >= RoundLimit
GetRoundLimit() → hideandseek_roundlimit (default 5)
```

`ShouldSwitchMap()` in RoundFlowStateMachine prüft:
1. `IsRoundLimitReached()` → true → Map wechseln
2. `roundlimit == 0` → Timelimit entscheidet
3. Beides 0 → unendlich

---

## Movement-Locking

```
RoundFlowRunningState.Enter():
  └─ SetAllPlayersMovementLocked(false)  // Alle dürfen laufen
  └─ if HideAndSeek && Hiding:
       └─ SetTeamMovementLocked(Blue, true)  // Seeker eingefroren

GameLoop (Phase Hiding → Seeking):
  └─ SetTeamMovementLocked(Blue, false)  // Seeker entsperrt
```

---

## Stun-System (Damage-Hook)

```csharp
OnDamage(attacker, victim, attackerTeam, victimTeam, damage, weaponName):
  if victimTeam == Blue (Seeker):
    return { SuppressDamage = true, StunDuration = 3f }  // Kein Schaden, nur Stun
  else:
    return { SuppressDamage = false }  // Normaler Schaden
```

Seeker nehmen keinen Schaden, werden aber für 3 Sekunden gestunnt (Movement gesperrt).
