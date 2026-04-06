# HUD System

Vollständiges UIToolkit-basiertes Heads-Up-Display mit MVC-Pattern.
Umfasst: Gameplay-HUD, Scoreboard, Hit-Confirmation, Gametype-Messages, Round-Countdown, Debug-HUD, Match-Recap.
Events von `NetworkedCharacterState`, `ClientPlayerCharacter` und `NetworkedPlayerCharacter` treiben die Anzeige.

---

## Architektur-Überblick

```
Server: NetworkedCharacterState / NetworkedGameState (NetworkVariables)
        ↓
Client: NetworkVariable.OnValueChanged Callbacks
        ↓
Events: OnAmmoChanged, OnWeaponChanged, OnHealthChanged, OnHitConfirmed,
        OnGametypeMessage, OnStunnedChanged, OnRoundStarting, etc.
        ↓
MatchController (MVC Controller)
  ├─ Abonniert Model-Events + Character-Events
  ├─ Ruft MatchView-Methoden auf (Gameplay HUD)
  ├─ Ruft ScoreboardView auf (Tab-Scoreboard)
  └─ Ruft MatchRecapView auf (Game Over Screen)
        ↓
Views (MVC Views)
  ├─ MatchView        — Gameplay HUD (UIToolkit)
  ├─ ScoreboardView   — Tab-Scoreboard (UIToolkit)
  ├─ MatchRecapView   — Game Over + Map Switch (UIToolkit)
  └─ QuakeColorLabel  — Farbcodierter Text (Custom VisualElement)
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **MatchView** | `Assets/Scripts/Runtime/Game/Views/MatchView.cs` | Gameplay HUD: Waffe, Health, Status, Countdown, HitConfirm, Gametype Messages |
| **MatchController** | `Assets/Scripts/Runtime/Game/Controllers/MatchController.cs` | MVC Controller: Event-Subscriptions, View-Calls, Seeker-Freeze-Logik |
| **ScoreboardView** | `Assets/Scripts/Runtime/Game/Views/ScoreboardView.cs` | Tab-Scoreboard: Team-Scores, Spieler-Listen, Deaths/Kills |
| **MatchRecapView** | `Assets/Scripts/Runtime/Game/Views/MatchRecapView.cs` | Game Over Screen + Map-Switch-Countdown |
| **QuakeColorLabel** | `Assets/Scripts/Runtime/Core/QuakeColorLabel.cs` | Custom VisualElement: Quake/SoF2-Farbcodes (^0-^9, ^a-^z) |
| **GameView** | `Assets/Scripts/Runtime/Game/Views/GameView.cs` | Container: Match, Scoreboard, MatchRecap, Menu, MapLoading |
| **GameEvents** | `Assets/Scripts/Runtime/Game/GameEvents.cs` | ScoreboardShowEvent, ScoreboardHideEvent, StartMatchEvent, EndMatchEvent |
| **MatchView.uxml** | `Assets/UIToolkit/MatchView/MatchView.uxml` | UXML Layout: Gameplay HUD |
| **ScoreboardView.uxml** | `Assets/UIToolkit/ScoreboardView/ScoreboardView.uxml` | UXML Layout: Team-Scoreboard |
| **MatchRecapView.uxml** | `Assets/UIToolkit/MatchRecapView/MatchRecapView.uxml` | UXML Layout: Game Over Screen |
| **NetworkedCharacterState** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs` | Events: OnAmmoChanged, OnWeaponChanged, OnHealthChanged, OnKillsChanged, etc. |
| **NetworkedPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` | Events: OnHitConfirmed, OnGametypeMessage, OnStunnedChanged |
| **ClientPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Events: OnWeaponSwapRaiseStarted, OnWeaponSwapTargetChanged, OnFireModeChanged |

---

## Events → HUD Mapping (Vollständig)

### Character State Events (NetworkedCharacterState)

| Event | Controller-Handler | View-Methode | Beschreibung |
|-------|-------------------|-------------|-------------|
| `OnAmmoChanged(clip, reserve)` | `OnAmmoChanged()` | `UpdateAmmoHud(clip, reserve)` | Munitions-Anzeige |
| `OnAltAmmoChanged(altClip, altReserve)` | `OnAltAmmoChanged()` | `UpdateAltAmmoHud(altClip, altReserve)` | Alt-Ammo (z.B. M203) |
| `OnWeaponChanged` | `OnWeaponChangedHud()` | `UpdateWeaponHud(...)` | Waffen-Wechsel komplett |
| `OnHealthChanged` | `OnHealthChanged()` | `UpdateHealthHud(health)` | Health-Anzeige |
| `OnKillsChanged` | `OnKillsChanged()` | `UpdatePlayerStats(kills, deaths)` | K/D Ratio |
| `OnDeathsChanged` | `OnDeathsChanged()` | `UpdatePlayerStats(kills, deaths)` | K/D Ratio |
| `OnIsAliveChanged` | `OnIsAliveChanged()` | `UpdatePlayerStatus(isAlive, isStunned)` | ALIVE / DEAD / STUNNED |

### Client Player Events (ClientPlayerCharacter)

| Event | Controller-Handler | View-Methode | Beschreibung |
|-------|-------------------|-------------|-------------|
| `OnWeaponSwapRaiseStarted` | `OnWeaponSwapRaiseStarted()` | `UpdateWeaponHud(...)` | Raise-Phase Bestätigung |
| `OnWeaponSwapTargetChanged` | `OnWeaponSwapTargetChanged()` | `UpdateWeaponHud(...)` | Instant-Prediction beim Scrollen |
| `OnFireModeChanged` | `OnFireModeChanged()` | `UpdateFireModeHud(mode, hasMultiple)` | Feuermodus-Wechsel |

### Networked Player Events (NetworkedPlayerCharacter)

| Event | Controller-Handler | View-Methode | Beschreibung |
|-------|-------------------|-------------|-------------|
| `OnHitConfirmed(region, damage, isKill)` | `OnHitConfirmed()` | `ShowHitConfirm(...)` | Hit-Confirmation Overlay |
| `OnGametypeMessage(message)` | `OnGametypeMessage()` | `ShowGametypeMessage(message)` | Gametype Broadcast (QuakeColorLabel) |
| `OnStunnedChanged(isStunned)` | `OnStunnedChanged()` | `UpdatePlayerStatus(isAlive, isStunned)` | STUNNED Status (HideAndSeek) |

### Game State Events (NetworkedGameState)

| Event | Controller-Handler | View-Methode | Beschreibung |
|-------|-------------------|-------------|-------------|
| `matchCountdown.OnValueChanged` | `OnCountdownChanged()` | `OnCountdownChanged(value)` | Game-Timer (MM:SS) |
| `redTeamScore.OnValueChanged` | `OnTeamScoreChanged()` | `UpdateTeamScore(red, blue)` | Team-Scores |
| `blueTeamScore.OnValueChanged` | `OnTeamScoreChanged()` | `UpdateTeamScore(red, blue)` | Team-Scores |
| `roundStartCountdown.OnValueChanged` | `OnRoundStartCountdownChanged()` | `ShowRoundStartCountdown(value)` | 3, 2, 1 Overlay |
| `waitingForPlayers.OnValueChanged` | `OnWaitingForPlayersChanged()` | `ShowWaitingMessage(msg)` | "Waiting for players..." |
| `warmupCountdown.OnValueChanged` | `OnWarmupCountdownChanged()` | `ShowWaitingMessage(msg)` | "Round starts in X..." |
| `gametypePhase.OnValueChanged` | `OnGametypePhaseChanged()` | — | Seeker-Freeze Aufhebung |
| `phaseTimeRemaining.OnValueChanged` | `OnPhaseTimeRemainingChanged()` | `ShowRoundStartCountdown(value)` | Seeker-Warmup-Timer |
| `currentRound.OnValueChanged` | `OnRoundChanged()` | `UpdateRoundDisplay(round, limit)` | "Round X/Y" |
| `roundLimit.OnValueChanged` | `OnRoundChanged()` | `UpdateRoundDisplay(round, limit)` | Round-Limit Update |
| `OnRoundStarting` | `OnRoundStarting()` | `ShowRoundStartCountdown(value)` | Movement-Freeze + Countdown |
| `OnMatchStarted` | `OnMatchStarted()` | `ShowGoText()` | "GO!" Overlay + Unfreeze |
| `OnMatchEnded` | `OnMatchEnded()` | Broadcast `EndMatchEvent` | Spiel vorbei |

### MVC Events (EventManager)

| Event | Controller-Handler | Aktion |
|-------|-------------------|--------|
| `ScoreboardShowEvent` | `OnScoreboardShow()` | `ScoreboardView.ShowScoreboard()` |
| `ScoreboardHideEvent` | `OnScoreboardHide()` | `ScoreboardView.HideScoreboard()` |

---

## HUD-Elemente

### Weapon HUD (Rechts Unten)

```
┌────────────────────────────┐
│ [Weapon Icon]              │
│ M4 CARBINE                 │
│ SEMI-AUTO                  │ (nur wenn hasMultipleModes)
│ 5.56mm                     │
│ 30 | 120                   │ (Clip | Reserve)
│ M203    1 | 3              │ (Alt-Ammo, nur wenn vorhanden)
└────────────────────────────┘
```

| Label | Inhalt | Quelle |
|-------|--------|--------|
| Weapon Icon | `WeaponDefinition.HudIcon` | TextureManager |
| Weapon Name | `DisplayName.ToUpperInvariant()` | WeaponDefinition |
| Fire Mode | `SEMI-AUTO / FULL-AUTO / BURST` | ClientPlayerCharacter |
| Ammo Type | `WeaponAmmoDefinition.Type` | WeaponDefinition |
| Clip Ammo | Aktueller Clip | NetworkedCharacterState |
| Reserve Ammo | Reserve gesamt | NetworkedCharacterState |
| Alt Ammo | Clip + Reserve (z.B. M203) | NetworkedCharacterState |

**Infinite Ammo:** Wenn Waffe infinite Ammo hat, wird die gesamte Ammo-Row ausgeblendet.

### Health HUD (Links Unten)

```
┌──────────────┐
│ 100          │
│ Surface: ... │ (Debug: letzter getroffener Surface-Typ)
└──────────────┘
```

### Player Status (Links Oben)

```
┌──────────────────────────────────────┐
│ Your Team: RED/BLUE                  │ (farbcodiert)
│ K: 5  D: 2                          │ (Kills/Deaths)
│ ALIVE / DEAD / STUNNED              │ (farbcodiert)
│ Connected: 8/16                      │
│ Round 3/10                           │
└──────────────────────────────────────┘
```

| Status | Farbe |
|--------|-------|
| ALIVE | Grün `(0.4, 1.0, 0.4)` |
| DEAD | Rot `(1.0, 0.3, 0.3)` |
| STUNNED | Gelb `(1.0, 0.8, 0.2)` |

### Game Timer (Oben Mitte)

```
MM:SS
```
Formatierung: `{countdown/60:D2}:{countdown%60:D2}`

### Team Scores (Oben)

```
[Red Logo] 3       1 [Blue Logo]
```
Texturen aus `TextureConfiguration.ScoreboardTextures`.

### Round Start Countdown (Zentriert)

```
     3
     2
     1
    GO!
```

- `ShowRoundStartCountdown(uint value)` — Zeigt 3, 2, 1
- `ShowGoText()` — Zeigt "GO!" für 1 Sekunde
- `HideRoundStartCountdown()` — Blendet Overlay aus
- Sound: `sound/misc/c4/beep.mp3` (Countdown), `sound/radio/male/move.mp3` (GO!)

### Waiting / Warmup Messages (Zentriert)

```
Waiting for more Players to start round...
Round starts in 5...
```

### Hit Confirmation (Zentriert)

```
  HEAD
  -85 KILL
```

| Treffer | Region-Farbe | Damage-Text |
|---------|-------------|-------------|
| Normal | Rot `(1.0, 0.31, 0.31)` | `-{damage}` |
| Kill | Gold `(1.0, 0.85, 0.2)` | `-{damage} KILL` |

Anzeigedauer: `k_HitConfirmDisplayDuration = 1.0s`

### Gametype Message (Zentriert, QuakeColorLabel)

```
You stunned ^1SniperWolf^7!
```

- Nutzt `QuakeColorLabel` mit bigchars Atlas für farbcodierte Spielernamen
- CharWidth: 16px, CharHeight: 24px
- Anzeigedauer: `k_GametypeMessageDisplayDuration = 3.0s`
- Eingefügt als Child-Element in `GametypeMessageContainer`

### FPS Counter (Oben Rechts)

```
FPS: 144.0
```
Update-Intervall: `k_FpsUpdateInterval = 0.5s`

### Crosshair (Bildschirmmitte)

- Container: `CrosshairContainer`
- Vertikaler Offset: `CROSSHAIR_VERTICAL_OFFSET_PERCENT = 0.0f` (exakt Mitte)
- Anpassbar für Third-Person Parallax-Korrektur

### Bhop Display (Unten Mitte)

```
853.2
Bhop x5  |  Peak: 912.3  |  Dist: 45.2m
Last: x3  |  Peak: 780.1  |  Dist: 28.5m
```

- Aktuelle Horizontalgeschwindigkeit
- Chain-Counter (nur bei Chain > 1)
- Last-Chain-Info (nur wenn aktuelle Chain ≤ 1)

---

## Debug HUD (Links Mitte)

Toggle via `m_ShowDebugHud` (Inspector).
Update-Intervall: `k_DebugHudUpdateInterval = 0.1s`

```
┌─────────────────────────────────────┐
│ Grounded: true                      │
│ Attacking: false                    │
│ Crouching: false                    │
│ Horiz Speed: 320.0 u/s             │
│ Vert Speed: -12.5 u/s              │
│ Pos: (45.2, 12.8, -33.1)           │
│ Airtime: 0.32s                      │
│ Jump Phase: 0.15s                   │
│ Fall Phase: 0.17s                   │
│ Jump Height: 1.2m                   │
│ Fall Height: 0.8m                   │
│ Air Dist Horiz: 3.5m               │
│ Vert Path: 2.0m                     │
│ Full Airtime: 0.45s                 │
│ Full Jump Phase: 0.20s              │
│ Full Fall Phase: 0.25s              │
│ Full Jump Height: 1.5m              │
│ Full Fall Height: 1.2m              │
│ Full Dist Horiz: 5.2m              │
│ Full Vert Path: 2.7m                │
└─────────────────────────────────────┘
```

---

## HUD Prediction (Instant Scroll)

Problem: Weapon-Swap dauert mehrere Frames (Drop → Raise). Das HUD soll aber sofort reagieren.

Lösung via zwei Events:
1. **OnWeaponSwapTargetChanged**: Feuert sofort beim Scrollen → HUD zeigt neue Waffe instant
2. **OnWeaponSwapRaiseStarted**: Feuert wenn Raise-Phase beginnt → Bestätigung + Visual-Update

Das erlaubt dem Spieler, durch Waffen zu scrollen und sofort die Zielwaffe im HUD zu sehen,
auch wenn die Drop-Phase der aktuellen Waffe noch läuft (Rapid Cycling).

**Client Ammo Cache:** `MatchController` hält ein `Dictionary<string, (int clip, int reserve)> m_ClientAmmoCache`,
das bei jedem `OnAmmoChanged`/`OnWeaponChangedHud` aktualisiert wird. Bei Weapon-Swap-Prediction zeigt 
der HUD damit korrekte Ammo-Werte statt `StartClip`/`StartReserve`.

---

## Seeker-Freeze-Mechanik (HideAndSeek)

Spezialfall: In HideAndSeek hat der Seeker (Blue Team) während der Hiding-Phase eingefrorene Eingabe.

### Ablauf

1. `OnRoundStarting()` → Alle Spieler Movement-Freeze + Countdown (3, 2, 1)
2. `OnMatchStarted()` → Prüfung:
   - **Hider (Red):** Normaler "GO!" + Unfreeze
   - **Seeker (Blue):** `m_SeekerFrozen = true`, Movement bleibt frozen, zeigt `phaseTimeRemaining` als Countdown
3. `OnPhaseTimeRemainingChanged()` → Aktualisiert Seeker-Countdown in UI (nur wenn `m_SeekerFrozen`)
4. `OnGametypePhaseChanged(Hiding → Seeking)` → `m_SeekerFrozen = false`, "GO!" + Sound + Unfreeze

### Methode

```
IsLocalPlayerSeekerInHideAndSeek():
  - Prüft gametypeId == "hideandseek"
  - Prüft lokaler Spieler Team == Blue (Seeker)
```

---

## MVC-Pattern

- **Model**: `NetworkedCharacterState` / `NetworkedGameState` — NetworkVariables als Single Source of Truth
- **View**: `MatchView`, `ScoreboardView`, `MatchRecapView` — UIToolkit, reine Darstellung, keine Logik
- **Controller**: `MatchController` — Event-Bridge: abonniert Model-Events, ruft View-Methoden auf
- **Container**: `GameView` — Hält Referenzen zu allen Sub-Views: `Match`, `Scoreboard`, `MatchRecap`, `Menu`, `MapLoading`

### Event Lifecycle

```
Awake()   → Subscribe: NetworkedGameState Events, EventManager Events
OnEnable  → View bindet UI-Elemente, feuert OnViewEnabled
Update()  → Lazy SubscribeToCharacterState(), FPS/Debug Updates
OnDestroy → Unsubscribe: alle Events sauber deregistriert
```

### Character State Subscription (Lazy)

```
Update():
  if m_CharacterState == null → SubscribeToCharacterState()
    → Cached: m_CharacterState, m_PlayerCharacter, m_NetworkedPlayerCharacter
    → Subscribe: OnAmmoChanged, OnWeaponChanged, OnHealthChanged,
                 OnKillsChanged, OnDeathsChanged, OnIsAliveChanged,
                 OnWeaponSwapRaiseStarted, OnWeaponSwapTargetChanged, OnFireModeChanged,
                 OnHitConfirmed, OnGametypeMessage, OnStunnedChanged
    → TryInitializeHudFromCurrentState() (einmaliger Refresh)
```

---

## UI Sound Integration

| Sound | SoF2-Pfad | Trigger |
|-------|-----------|---------|
| Countdown Beep | `sound/misc/c4/beep.mp3` | Round-Start 3, 2, 1 |
| GO! Signal | `sound/radio/male/move.mp3` | Match/Phase Start |

Sounds werden über `PlayUiSound(string soundKey)` via `SoundManager` abgespielt.

---

## UXML Layout Dateien

| Datei | Elemente |
|-------|----------|
| `MatchView.uxml` | timerLabel, playersConnectedLabel, yourTeamLabel, playerStatsLabel, playerStatusLabel, hudRedTeamScoreLabel, hudBlueTeamScoreLabel, roundLabel, fpsLabel, DebugInfoBox (alle Debug-Labels), weaponIconImage, weaponNameLabel, fireModeLabel, ammoTypeLabel, clipAmmoLabel, reserveAmmoLabel, AmmoRow, AltAmmoRow, healthLabel, roundStartLabel, waitingLabel, HitConfirmContainer, GametypeMessageContainer, CrosshairContainer |
| `ScoreboardView.uxml` | ScoreboardRoot, ScoreboardContainer, ScoreHeaderBar, scoreHeaderLabel, gametypeLabel, gameTimeLabel, RedTeamHeader/BlueTeamHeader (Logo + Name + Score), RedTeamColumnHeaders/BlueTeamColumnHeaders, RedPlayersList/BluePlayersList, RedTeamFooter/BlueTeamFooter |
| `MatchRecapView.uxml` | resultLabel, mapSwitchLabel |
