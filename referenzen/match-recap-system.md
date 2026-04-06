# Match Recap System

End-of-Match Screen nach Game Over. Zeigt Ergebnis und Map-Switch-Countdown.

---

## Architektur

```
Server: RoundFlowStateMachine → SwitchingMapState
  ↓ mapSwitchCountdown.OnValueChanged
  ↓ nextMapDisplayName.OnValueChanged
Client: MatchRecapController → MatchRecapView
  ├─ OnClientEndMatch()      → "Game Over!"
  └─ UpdateMapSwitchCountdown() → "Next map: mp_shop in 5..."
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **MatchRecapView** | `Assets/Scripts/Runtime/Game/Views/MatchRecapView.cs` | View: "Game Over" + Map-Switch-Countdown |
| **MatchRecapView.uxml** | `Assets/UIToolkit/MatchRecapView/MatchRecapView.uxml` | UXML Layout |
| **GameView** | `Assets/Scripts/Runtime/Game/Views/GameView.cs` | Container: `MatchRecap` Property |

---

## UI-Elemente

| UXML-ID | Typ | Inhalt |
|---------|-----|--------|
| `resultLabel` | Label | "Game Over!" |
| `mapSwitchLabel` | Label | "Next map: {name} in {seconds}..." |

---

## Methoden

### `OnClientEndMatch()`
- Aktiviert den MatchRecap-Screen (`gameObject.SetActive(true)`)
- Setzt `resultLabel.text = "Game Over!"`
- Leert `mapSwitchLabel`

### `UpdateMapSwitchCountdown(string nextMapDisplayName, uint secondsRemaining)`
- `secondsRemaining > 0`: `"Next map: {name} in {seconds}..."`
- `secondsRemaining == 0`: `"Loading {name}..."`

---

## Ablauf

```
1. Match endet → Server sendet OnMatchEnded
2. MatchController.OnMatchEnded() → Broadcast(EndMatchEvent)
3. MatchRecapView.OnClientEndMatch() → "Game Over!" anzeigen
4. Server startet Map-Switch-Countdown (SwitchingMapState)
5. mapSwitchCountdown.OnValueChanged → UpdateMapSwitchCountdown()
6. Countdown erreicht 0 → Server lässt alle Clients neue Map laden
```
