# HUD System

UIToolkit-basiertes Heads-Up-Display mit MVC-Pattern.
Events von `NetworkedCharacterState` und `ClientPlayerCharacter` treiben die Anzeige.

---

## Architektur-Überblick

```
Server: NetworkedCharacterState (NetworkVariables ändern sich)
        ↓
Client: NetworkVariable.OnValueChanged Callbacks
        ↓
Events: OnAmmoChanged, OnWeaponChanged, OnHealthChanged, etc.
        ↓
MatchController (MVC Controller)
  ├─ Abonniert Events in AddListener()
  └─ Ruft MatchView-Methoden auf
        ↓
MatchView (MVC View)
  ├─ UIToolkit VisualElements (UXML)
  └─ UpdateWeaponHud(), UpdateAmmoHud(), UpdateHealthHud()
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **MatchView** | `Assets/Scripts/Runtime/Game/Views/MatchView.cs` | UIToolkit View: Labels + Update-Methoden |
| **MatchController** | `Assets/Scripts/Runtime/Game/Controllers/MatchController.cs` | MVC Controller: Event-Subscriptions + View-Calls |
| **MatchView.uxml** | `Assets/UIToolkit/MatchView/MatchView.uxml` | UXML Layout: WeaponHudContainer, HealthHudContainer |
| **NetworkedCharacterState** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs` | Events: OnAmmoChanged, OnWeaponChanged, OnHealthChanged |
| **ClientPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Events: OnWeaponSwapRaiseStarted, OnWeaponSwapTargetChanged |

---

## Events → HUD Mapping

| Event | Quelle | Controller-Handler | View-Methode |
|-------|--------|-------------------|-------------|
| `OnAmmoChanged(clip, reserve)` | NetworkedCharacterState | `OnAmmoChanged()` | `UpdateAmmoHud(clip, reserve)` |
| `OnAltAmmoChanged(altClip, altReserve)` | NetworkedCharacterState | `OnAltAmmoChanged()` | `UpdateAltAmmoHud(altClip, altReserve)` |
| `OnWeaponChanged` | NetworkedCharacterState | `OnWeaponChangedHud()` | `UpdateWeaponHud()` |
| `OnHealthChanged` | NetworkedCharacterState | `OnHealthChanged()` | `UpdateHealthHud(health)` |
| `OnWeaponSwapRaiseStarted` | ClientPlayerCharacter | `OnWeaponSwapRaiseStarted()` | Visual Feedback |
| `OnWeaponSwapTargetChanged` | ClientPlayerCharacter | `OnWeaponSwapTargetChanged()` | Sofortiges HUD-Update (Prediction) |

---

## HUD-Elemente

### Weapon HUD

```
┌────────────────────────────┐
│ [Weapon Icon]              │
│ M4 Carbine                 │
│ 5.56mm                     │
│ Clip: 30  Reserve: 120     │
│ Alt: 1 / 3  (wenn vorhanden)│
└────────────────────────────┘
```

| Label | Inhalt |
|-------|--------|
| Weapon Name | `WeaponDefinition.DisplayName` |
| Ammo Type | `WeaponAmmoDefinition.Type` |
| Clip Ammo | `NetworkedCharacterState.CurrentClipAmmo` |
| Reserve Ammo | `NetworkedCharacterState.ReserveAmmo` |
| Alt Clip | `NetworkedCharacterState.AltClipAmmo` (nur wenn > 0) |
| Alt Reserve | `NetworkedCharacterState.AltReserveAmmo` |

### Health HUD

```
┌──────────────┐
│ Health: 100  │
└──────────────┘
```

---

## HUD Prediction (Instant Scroll)

Problem: Weapon-Swap dauert mehrere Frames (Drop → Raise). Das HUD soll aber sofort reagieren.

Lösung via zwei Events:
1. **OnWeaponSwapTargetChanged**: Feuert sofort beim Scrollen → HUD zeigt neue Waffe instant
2. **OnWeaponSwapRaiseStarted**: Feuert wenn Raise-Phase beginnt → Bestätigung + Visual-Update

Das erlaubt dem Spieler, durch Waffen zu scrollen und sofort die Zielwaffe im HUD zu sehen,
auch wenn die Drop-Phase der aktuellen Waffe noch läuft (Rapid Cycling).

---

## MVC-Pattern

- **Model**: `NetworkedCharacterState` — NetworkVariables als Single Source of Truth
- **View**: `MatchView` — UIToolkit, reine Darstellung, keine Logik
- **Controller**: `MatchController` — Event-Bridge: abonniert Model-Events, ruft View-Methoden auf

Controller abonniert/deabonniert Events sauber in `AddListener()`/`RemoveListeners()`:
```
OnViewEnabled → Subscribe Events
OnViewDisabled → Unsubscribe Events
```
