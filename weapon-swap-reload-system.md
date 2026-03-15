# Weapon Swap & Reload System

Server-autoritatives Waffenwechsel- und Nachladesystem nach SoF2-Vorbild.
Beide Systeme nutzen Frame-diskretes Timing (wie SoF2 weaponTime) für Cooldowns.

---

## Architektur-Überblick

```
Client: ScrollWheel / Zahlenkeys
        ↓
ClientPlayerCharacter: Bestimmt nächste/vorherige Waffe
        ↓
NetworkedCharacterState.OnWeaponSwapRequested (Event)
        ↓
Server: NetworkedPlayerCharacter.OnServerWeaponSwapRequested()
  ├─ Gating: noActionRunning
  ├─ Phase 1: Drop (alte Waffe weglegen — mp_drop Animation)
  ├─ Phase 2: Raise (neue Waffe ziehen — mp_raise Animation)
  └─ Ammo-Cache: Speichert/Restauriert Munition pro Waffe
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **NetworkedPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` | Server: Swap-Logik, Frame-Counting, Reload-Logik |
| **NetworkedCharacterState** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs` | Ammo-NetworkVariables, AmmoCache, Events |
| **ClientPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Input-Handling, Prediction-Events |
| **WeaponSwapPhase** | `Assets/Scripts/Runtime/Game/Characters/Shared/WeaponSwapPhase.cs` | Enum: None, Drop, Raise |
| **ShellReloadPhase** | `Assets/Scripts/Runtime/Game/Characters/Shared/ShellReloadPhase.cs` | Enum: None, Start, Shell, End |
| **WeaponLoader** | `Assets/Scripts/Runtime/Game/WeaponLoader/WeaponLoader.cs` | Visual: Prefab laden + an Bone attachen |

---

## Weapon Swap

### Phasen (WeaponSwapPhase)

```
None → Drop → Raise → None
```

| Phase | Aktion | Animation |
|-------|--------|-----------|
| **Drop** | Alte Waffe weglegen | `mp_drop` (Frames + FPS aus JSON) |
| **Raise** | Neue Waffe ziehen | `mp_raise` (Frames + FPS aus JSON) |

### Server-Felder

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| `m_ServerIsSwapping` | `bool` | Swap aktiv? |
| `m_ServerSwapPhase` | `WeaponSwapPhase` | Aktuelle Phase |
| `m_ServerSwapFramesRemaining` | `int` | Verbleibende Frames in Phase |
| `m_ServerSwapFps` | `int` | FPS der aktuellen Phase |
| `m_ServerSwapTargetWeapon` | `string` | Zielwaffe |

### Server-Flow

1. **OnServerWeaponSwapRequested(targetWeapon)**:
   - Gating: Läuft bereits ein Swap in Drop-Phase? → Target updaten (Rapid Cycling)
   - Ammo der aktuellen Waffe in Cache speichern
   - Drop-Phase starten mit `mp_drop` Animation-Daten

2. **TickServerWeaponSwap(deltaTime)**:
   - Frame-Akkumulator + Countdown (wie Attack-Frames)
   - **Drop abgeschlossen**: `SetCurrentWeaponName(target)` → Raise-Phase starten
   - **Raise abgeschlossen**: `m_ServerIsSwapping = false`, Swap fertig

### Rapid Cycling (m_PendingSwapTarget)

Wenn der Spieler während der Drop-Phase erneut scrollt:
- `m_ServerSwapTargetWeapon` wird aktualisiert (kein Neustart der Drop-Phase)
- Die Drop-Phase läuft weiter, aber die Raise-Phase nutzt die neue Zielwaffe

### Client-Events (HUD Prediction)

| Event | Wann | Zweck |
|-------|------|-------|
| `OnWeaponSwapTargetChanged` | Drop-Phase: Target ändert sich | HUD zeigt sofort neue Waffe |
| `OnWeaponSwapRaiseStarted` | Raise-Phase beginnt | Visual-Update, Weapon-Icon |

### Ammo-Cache

Beim Swap wird die aktuelle Munition im `m_AmmoCache` gespeichert:
```
m_AmmoCache["m4"] = (clipAmmo, reserveAmmo, altClip, altReserve)
```

Beim Wechsel zur neuen Waffe wird der Cache restauriert.
Falls kein Cache existiert → `StartClip` / `StartReserve` aus JSON.

---

## Reload System

### Standard-Reload (Magazin)

```
Client: R-Taste → Attack-Button "Reload"
        ↓
Server: noActionRunning && HasAmmoToReload
        ↓
m_ServerReloadFramesRemaining = mp_reload.Duration
m_ServerReloadFps = mp_reload.Fps
        ↓
Frame-Countdown → Abgeschlossen:
NetworkedCharacterState.CompleteReload()
  → Reserve → Clip Transfer
```

### Shell-by-Shell Reload (Schrotflinten)

Waffen mit `mp_reload_start`, `mp_reload_shell`, `mp_reload_end` nutzen Shell-Reload:

```
ShellReloadPhase:
  None → Start → Shell (×N) → End → None
```

| Phase | Animation | Aktion |
|-------|-----------|--------|
| **Start** | `mp_reload_start` | Waffe öffnen |
| **Shell** | `mp_reload_shell` | Pro Patrone: TransferOneShell() |
| **End** | `mp_reload_end` | Waffe schließen |

### Server-Felder (Shell Reload)

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| `m_ServerIsShellReload` | `bool` | Shell-Reload-Modus aktiv? |
| `m_ServerShellReloadPhase` | `ShellReloadPhase` | Aktuelle Phase |
| `m_ServerReloadStartFrames` | `int` | Frames für Start-Animation |
| `m_ServerReloadShellFrames` | `int` | Frames pro Shell |
| `m_ServerReloadEndFrames` | `int` | Frames für End-Animation |
| `m_ServerShellsRemaining` | `int` | Verbleibende Patronen |

### Shell-Reload Flow

1. **Start-Phase**: `mp_reload_start` Animation abspielen
2. **Shell-Phase** (wiederholt):
   - `TransferOneShell()` → 1 Patrone von Reserve in Clip
   - `m_ServerShellsRemaining--`
   - Wenn Clip voll ODER Reserve leer → End-Phase
3. **End-Phase**: `mp_reload_end` Animation abspielen
4. **Fertig**: Alle Reload-Felder zurücksetzen

### Waffen mit Shell-Reload

| Waffe | Patronen | Magazin | Shell-Reload? |
|-------|----------|---------|--------------|
| M590 | 12gauge | 7 | Ja |
| MM1 | 40mm Grenade | 12 | Ja |
| Alle anderen | — | — | Nein (Standard Magazin-Reload) |

---

## Ammo System

### NetworkVariables (synchronisiert)

| Variable | Typ | Beschreibung |
|----------|-----|-------------|
| `m_CurrentClipAmmo` | `NetworkVariable<int>` | Munition im Magazin |
| `m_ReserveAmmo` | `NetworkVariable<int>` | Reserve-Munition |
| `m_AltClipAmmo` | `NetworkVariable<int>` | Alt-Attack Clip (z.B. M203) |
| `m_AltReserveAmmo` | `NetworkVariable<int>` | Alt-Attack Reserve |

### Ammo-Events

| Event | Parameter | Beschreibung |
|-------|-----------|-------------|
| `OnAmmoChanged` | `(int clip, int reserve)` | Clip oder Reserve geändert |
| `OnAltAmmoChanged` | `(int altClip, int altReserve)` | Alt-Ammo geändert |

### Ammo-Methoden

| Methode | Beschreibung |
|---------|-------------|
| `TryConsumeAmmo()` | Server: 1 Patrone abziehen, `true` wenn erlaubt (infinite oder clip > 0) |
| `TryConsumeAltAmmo()` | Server: 1 Alt-Patrone abziehen |
| `CompleteReload()` | Transfer Reserve → Clip (min(maxClip - clip, reserve)) |
| `TransferOneShell()` | Transfer 1 Patrone Reserve → Clip |

### Infinite Ammo

`WeaponAmmoDefinition.Infinite = true` → Knife hat unendliche Munition.
`TryConsumeAmmo()` gibt sofort `true` zurück ohne Decrement.

---

## Attack Gating (Zusammenfassung)

Alle Combat-Actions werden server-seitig gegated:

```csharp
bool noActionRunning = m_ServerAttackFramesRemaining <= 0
                    && m_ServerReloadFramesRemaining <= 0
                    && m_ServerAltAttackFramesRemaining <= 0
                    && !m_ServerIsSwapping;
```

Nur wenn **keine** Action läuft, kann Attack/AltAttack/Reload/Swap starten.
Das verhindert gleichzeitiges Feuern+Nachladen und dient als Anti-Cheat.

---

## Animator-Integration

Waffen-Swap setzt den Animator-Parameter `CurrentWeapon` auf den `AnimatorIndex`:
- 0 = Knife, 1 = RPG-7, 2 = M4, 3 = AK-74, 4 = MM-1, 5 = M590, ...
- Animator-StateMachine wechselt Idle/Attack/Reload-Animationen basierend auf dem Index
- `SwapSpeed` Parameter steuert die Geschwindigkeit der Raise-Animation
