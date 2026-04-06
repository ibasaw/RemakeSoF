# Scoreboard System

SoF2-Style Team-Scoreboard. Wird per Tab-Taste ein-/ausgeblendet. Zeigt alle Spieler sortiert nach Kills.

---

## Architektur-Überblick

```
Input: Tab-Taste → ScoreboardShowEvent / ScoreboardHideEvent
         ↓
MatchController (EventManager)
  ├─ OnScoreboardShow() → ScoreboardView.ShowScoreboard()
  └─ OnScoreboardHide() → ScoreboardView.HideScoreboard()
         ↓
ScoreboardView
  ├─ RefreshScoreboard() — Liest NetworkedGameState + alle NetworkedCharacterStates
  ├─ UpdateScoreHeader() — "Red leads 3-1" / "Game Tied at 0"
  ├─ UpdateGametypeInfo() — Gametype-Name + Team-Namen aus GametypeDefinition
  ├─ RefreshPlayerLists() — Baut Red/Blue Spieler-Rows, sortiert nach Kills
  └─ CreatePlayerRow() — Einzelne Zeile mit QuakeColorLabel-Name + Stats
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **ScoreboardView** | `Assets/Scripts/Runtime/Game/Views/ScoreboardView.cs` | View: Layout + Daten-Refresh |
| **MatchController** | `Assets/Scripts/Runtime/Game/Controllers/MatchController.cs` | Controller: Tab-Event → Show/Hide |
| **ScoreboardView.uxml** | `Assets/UIToolkit/ScoreboardView/ScoreboardView.uxml` | UXML Layout |
| **QuakeColorLabel** | `Assets/Scripts/Runtime/Core/QuakeColorLabel.cs` | Farbcodierte Spielernamen |
| **NetworkedGameState** | `Assets/Scripts/Runtime/Game/Networked/NetworkedGameState.cs` | Team-Scores, Gametype, Phase |
| **NetworkedCharacterState** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs` | Spieler-Stats |
| **GametypeDefinitionLoader** | ServiceLocator | Gametype-Name + Team-Namen |
| **TextureConfiguration** | ServiceLocator (TextureManager) | Scoreboard-Texturen |

---

## Scoreboard Layout

```
┌─────────────────────────────────────────────────────┐
│        [ScoreHeaderBar — Green Metallic]            │
│          Red leads 3-1                              │
│ Gametype: Hide and Seek    Game Time: 05:32         │
├─────────────────────────────────────────────────────┤
│ [Red Logo] RED TEAM (Hiders)    Score: 3            │
├──────────────────────────────────────────────────────┤
│ Name                  │ Status │  K  │  D  │ Ping   │
├──────────────────────────────────────────────────────┤
│ ^1Sgt^7.Mayhem       │  💀    │  12 │  5  │  32ms  │
│ ^4Fr0stBit3          │        │   8 │  7  │  45ms  │
│ ^3xXDarkLordXx       │  💀    │   5 │  9  │  78ms  │
├──────────────────────────────────────────────────────┤
│ Players: 3                                           │
├─────────────────────────────────────────────────────┤
│ [Blue Logo] BLUE TEAM (Seekers)    Score: 1         │
├──────────────────────────────────────────────────────┤
│ Name                  │ Status │  K  │  D  │ Ping   │
├──────────────────────────────────────────────────────┤
│ ^2CampKing42         │        │   9 │  4  │  28ms  │
│ ^5R4g3Quit           │        │   6 │  6  │  55ms  │
├──────────────────────────────────────────────────────┤
│ Players: 2                                           │
└─────────────────────────────────────────────────────┘
```

---

## Texturen

| Textur-Key | Verwendung | Quelle |
|-----------|-----------|--------|
| `scoreHeader` | Score-Header Bar (grüner Metallbalken) | TextureConfiguration.scoreboard |
| `teamRedLogo` | Rotes Team-Logo (Helm + Schwerter) | TextureConfiguration.scoreboard |
| `teamBlueLogo` | Blaues Team-Logo | TextureConfiguration.scoreboard |
| `scorelineHeader` | Spaltenüberschriften (dunkles Grid) | TextureConfiguration.scoreboard |
| `scorelineFooter` | Footer pro Team | TextureConfiguration.scoreboard |
| `scoreline` | Zeilenhintergrund pro Spieler | TextureConfiguration.scoreboard |
| `bigcharsAtlas` | QuakeColorLabel Atlas für Spielernamen | TextureConfiguration.scoreboard |
| `deadIcon` | Totenkopf-Icon für Status-Spalte | TextureConfiguration.scoreboard |

---

## Spieler-Row Details

Jede Zeile enthält (programmatisch erstellt in `CreatePlayerRow()`):

| Element | Typ | Breite | Inhalt |
|---------|-----|--------|--------|
| Name | `QuakeColorLabel` | 44% | Farbcodierter Spielername (13×20px Glyphen) |
| Status Icon | `VisualElement` | 6% | Dead-Icon oder leer |
| Kills | `Label` | 14% | Kill-Count (zentriert) |
| Deaths | `Label` | 14% | Death-Count (zentriert) |
| Ping | `Label` | 16% | `{ping}ms` (zentriert) |

**Tote Spieler:** Opacity 0.4 + Textfarbe `(1, 1, 1, 0.4)` (ausgegraut).

**Sortierung:** Spieler werden per `Sort((a, b) => b.Kills.CompareTo(a.Kills))` nach Kills absteigend sortiert.

---

## Datenquellen

- **Team-Scores:** `NetworkedGameState.redTeamScore.Value` / `blueTeamScore.Value`
- **Gametype-Info:** `GametypeDefinitionLoader.GetByGametypeId(activeGametypeId)` → `gametypeName`, `team1Name`, `team2Name`
- **Game-Time:** `NetworkedGameState.matchCountdown.Value` → Formatiert als `M:SS`
- **Spieler:** Iteration über `NetworkManager.SpawnManager.SpawnedObjectsList` → Filter nach `NetworkedCharacterState`
- **Team-Zuordnung:** `NetworkedCharacterState.TeamId` → `GametypeTeam.Red` / `.Blue`

---

## Dynamische Zeilenberechnung

```
ROW_HEIGHT = 26px (pro Spieler-Zeile)
HEADER_OVERHEAD = 350px (Container-Padding, Header, Footer, etc.)

GetMaxVisibleRows():
  screenHeight = Screen.height
  topPadding = screenHeight * 0.1
  availableHeight = screenHeight - topPadding - HEADER_OVERHEAD
  maxRows = Floor(availableHeight / ROW_HEIGHT)
  return Max(maxRows, 4)  // Minimum 4 Zeilen
```

---

## Editor Mock-Spieler

Für visuelle Tests im Unity Editor enthält `ScoreboardView` ein `#if UNITY_EDITOR` Mock-System:
- `PopulateMockPlayers()` füllt bis zu `MaxPlayers` (128) Spieler
- `BuildMockRowCache()` generiert einmalig gecachte Mock-Rows mit zufälligen Stats
- Deterministic RNG (`Random(42)`) für konsistente Ergebnisse
- Mock-Namen: `^1Sgt^7.Mayhem`, `^4Fr0stBit3`, `^3xXDarkLordXx`, etc.
- Einkommentierbar über Kommentar-Block in `RefreshPlayerLists()`

---

## Show / Hide Ablauf

```
Tab gedrückt → InputAction → Broadcast(ScoreboardShowEvent)
  → MatchController.OnScoreboardShow()
    → ScoreboardView.ShowScoreboard()
      → RefreshScoreboard() (alle Daten neu geladen)
      → m_ScoreboardRoot.style.display = Flex

Tab losgelassen → Broadcast(ScoreboardHideEvent)
  → MatchController.OnScoreboardHide()
    → ScoreboardView.HideScoreboard()
      → m_ScoreboardRoot.style.display = None
```
