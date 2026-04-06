# Server-Authoritative Weapon Fire System (pm_debounce)

Server-autoritatives Feuer-Gating nach SoF2-Vorbild (`bg_pmove.c` PM_GetAttackButtons).
Verhindert clientseitiges Cheating (Rapid-Fire, Skip-Semi-Auto) und garantiert
korrekte Fire-Modes (Auto, Single, Burst) serverseitig.

---

## SoF2-Referenz

### Quell-Code (bg_pmove.c)

| Funktion | Zeilen | Beschreibung |
|----------|--------|-------------|
| `PM_GetAttackButtons()` | 2482–2580 | Debounce-Logik für Attack, AltAttack, FireMode |
| `PM_Weapon()` | 2850–3260 | Waffen-State-Machine inkl. Burst-Decrement |
| `BG_FindFireMode()` | bg_weapons.c:1172 | FireMode zyklisch wechseln |

### pm_debounce Bitfield (bg_public.h)

```c
#define PMD_JUMP       0x0001
#define PMD_ATTACK     0x0002
#define PMD_FIREMODE   0x0004
#define PMD_USE        0x0008
#define PMD_ALTATTACK  0x0010
#define PMD_GOGGLES    0x0020
```

### SoF2-Prinzip

1. Spieler drückt Attack → `pm_debounce |= PMD_ATTACK` wird gesetzt
2. Solange Button gehalten UND FireMode != Auto → Attack-Button wird **maskiert** (kein Re-Fire)
3. Button losgelassen → `pm_debounce &= ~PMD_ATTACK` → erneutes Drücken erlaubt
4. Auto-Waffen ignorieren den Debounce (dürfen bei gehaltener Taste weiter feuern)

---

## Unsere Implementierung

### Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **NetworkedPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` | Server: pm_debounce, Fire-Gating, Burst, FireMode |
| **PlayerCommand** | `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerCommand.cs` | CommandButtons inkl. FireMode |
| **ClientPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Client sendet FireMode-Button, Rising-Edge |
| **NetworkedCharacterState** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs` | TryConsumeAmmo, TryConsumeAltAmmo |

### Server-Felder (NetworkedPlayerCharacter)

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| `m_ServerDebounce` | `int` | Bitfield — SoF2 `pm_debounce` |
| `m_ServerFireMode` | `string` | Aktueller Primary FireMode (`"auto"`, `"single"`, `"burst"`) |
| `m_ServerAltFireMode` | `string` | Aktueller AltAttack FireMode |
| `m_ServerBurstShotsRemaining` | `int` | Verbleibende Schüsse im laufenden Burst |

### Konstanten

```csharp
private const int PMD_ATTACK    = 0x0002;  // SoF2 PMD_ATTACK
private const int PMD_FIREMODE  = 0x0004;  // SoF2 PMD_FIREMODE
private const int PMD_ALTATTACK = 0x0010;  // SoF2 PMD_ALTATTACK
```

### CommandButtons (PlayerCommand.cs)

```csharp
public const int FireMode = 1 << 8;  // SoF2 BUTTON_FIREMODE
```

---

## Debounce-Logik (ProcessServerCommandLogic)

Exakte Umsetzung von SoF2 `PM_GetAttackButtons()`:

### 1. FireMode Debounce (bg_pmove.c:2489–2500)

```
Button gedrückt + Debounce nicht gesetzt → setze Debounce + CycleServerFireMode()
Button gedrückt + Debounce bereits gesetzt → ignorieren (kein Re-Cycle)
Button losgelassen → clear Debounce
```

### 2. PMD_ATTACK Debounce (bg_pmove.c:2504–2512)

```
Debounce gesetzt + Button losgelassen → clear Debounce
Debounce gesetzt + Button gehalten + FireMode != "auto" → Attack maskieren
Debounce gesetzt + Button gehalten + FireMode == "auto" → Attack durchlassen
```

### 3. Burst-Fire (bg_pmove.c:2530–2545)

```
FireMode == "burst" + Attack gedrückt + BurstCount <= 0 → BurstCount = 3
BurstCount > 0 → Force Attack, maskiere AltAttack + Reload + Zoom + FireMode
```

### 4. PMD_ALTATTACK Debounce (bg_pmove.c:2558–2569)

Identisch mit PMD_ATTACK, aber für AltAttack.

### 5. Burst Decrement (bg_pmove.c:2930–2932)

```
noActionRunning + BurstCount > 0 → BurstCount--
```

Dekrementiert **pro Waffen-Tick** (wenn Waffe bereit), nicht pro Schuss.
Damit läuft der Burst auch bei leerem Magazin aus.

### 6. PMD_ATTACK setzen (bg_pmove.c:3255)

Nach jedem erfolgreichen Schuss:
```csharp
m_ServerDebounce |= PMD_ATTACK;   // in ProcessAttack
m_ServerDebounce |= PMD_ALTATTACK; // in ProcessAltAttack
```

---

## Burst-Fire Mask

Während eines laufenden Bursts werden folgende Buttons gesperrt:

```csharp
attackButtons |= CommandButtons.Attack;       // Force Attack
attackButtons &= ~CommandButtons.AltAttack;   // Block
attackButtons &= ~CommandButtons.Reload;      // Block
attackButtons &= ~CommandButtons.Zoom;        // Block
attackButtons &= ~CommandButtons.FireMode;    // Block
```

Exakt wie SoF2 `bg_pmove.c:2539–2544`:
`BUTTON_ATTACK | ~BUTTON_ALT_ATTACK | ~BUTTON_RELOAD | ~BUTTON_ZOOMIN | ~BUTTON_ZOOMOUT | ~BUTTON_FIREMODE`

---

## FireMode Cycling (CycleServerFireMode)

SoF2 `BG_FindFireMode()` — zykliert durch verfügbare FireModes:

```csharp
private void CycleServerFireMode()
{
    // weapon.Attack.FireModes = ["single", "burst", "auto"]
    // currentIndex + 1 % count → nächster Modus
    m_ServerBurstShotsRemaining = 0;  // Burst bei Modus-Wechsel abbrechen
}
```

### UpdateServerFireModeParameters (Waffenwechsel)

Bei Waffenwechsel (Drop→Raise Transition):
- `m_ServerFireMode` = weapon.Attack.FireMode (Default aus JSON)
- `m_ServerAltFireMode` = weapon.AltAttack.FireMode
- Projektilwaffen ohne FireMode-Feld → `"single"` (SoF2-Verhalten)
- `m_ServerDebounce = 0` (Reset)
- `m_ServerBurstShotsRemaining = 0` (Reset)

---

## Granaten-Integration

### Cook-Phase
- PMD_ATTACK wird beim Cook-Start gesetzt (via ProcessAttack → ProcessProjectileAttack)
- Grenade-Cook blockiert alle weiteren Actions (`m_ServerAttackFramesRemaining = 9999`)

### Throw-Phase
- Button-Release triggert den Throw (TickServerGrenadeCook)
- PMD_ATTACK wird dadurch automatisch cleared (Button nicht mehr gehalten)
- `m_ServerGrenadeThrowFramesRemaining > 0` blockiert Re-Fire (noActionRunning = false)
- Nach Throw: Auto-Reload, kein extra PMD

### SoF2-Unterschied
SoF2 nutzt `grenadeTimer` + `WEAPON_CHARGING` als Gate.
Unser System nutzt `noActionRunning` (ThrowFramesRemaining > 0) — funktional identisch.

---

## Client-Seite

### Rising-Edge (ClientPlayerCharacter)

Client hat zusätzlich eine **lokale** Rising-Edge-Sperre (`m_AttackButtonWasPressed`):
- Verhindert Attack bei kontinuierlich gehaltenem Button für Client-Prediction
- Dient nur der lokalen Prediction-Qualität, nicht der Sicherheit

### FireMode-Button

```csharp
m_FireModeSwitchRequested = true;  // OnSwitchFireModePerformed
// → buttons |= CommandButtons.FireMode in RunPhysicsStep
// → m_FireModeSwitchRequested = false nach Command gebaut
```

---

## Vergleich: SoF2 vs. Unsere Implementierung

| Feature | SoF2 | Unsere Impl. | Status |
|---------|------|-------------|--------|
| PMD_ATTACK Debounce | bg_pmove.c:2504 | ProcessServerCommandLogic | ✅ Identisch |
| PMD_ALTATTACK Debounce | bg_pmove.c:2558 | ProcessServerCommandLogic | ✅ Identisch |
| PMD_FIREMODE Debounce | bg_pmove.c:2489 | ProcessServerCommandLogic | ✅ Identisch |
| Burst Init (count=3) | bg_pmove.c:2532 | ProcessServerCommandLogic | ✅ Identisch |
| Burst Mask | bg_pmove.c:2539 | +Zoom +FireMode | ✅ Identisch |
| Burst Decrement | bg_pmove.c:2930 (pro Tick) | noActionRunning (pro Tick) | ✅ Identisch |
| PMD setzen nach Schuss | bg_pmove.c:3255 | ProcessAttack/ProcessAltAttack | ✅ Identisch |
| FireMode Cycling | BG_FindFireMode | CycleServerFireMode | ✅ Äquivalent |
| Grenade Gate | grenadeTimer + CHARGING | noActionRunning + ThrowFrames | ✅ Funktional identisch |

---

## Waffen-FireMode-Tabelle

| Waffe | Verfügbare Modi | Default |
|-------|----------------|---------|
| M4 | single, burst, auto | auto |
| AK-74 | single, burst, auto | auto |
| OICW | single, burst, auto | auto |
| MP5 | single, burst, auto | auto |
| Micro Uzi | single, auto | auto |
| MSG90A1 | single | single |
| M590 | — | auto |
| USAS-12 | auto | auto |
| M60 | auto | auto |
| Knife | auto | auto |
| Alle Granaten | single | single |
| RPG7 | single | single |
