# Metagame Scene

Hub-Scene für den Spieler vor dem Joinen eines Games. Enthält Login, Registrierung, Hauptmenü, 
Loadout/Skin-Auswahl, Server-Browser, DirectIP-Verbindung und Optionen.

---

## Architektur-Überblick

```
MetagameApplication : BaseApplication<MetagameModel, MetagameView, MetagameController>
  │
  ├─ MetagameModel
  │   ├─ ClientConnectingModel  — Timer, Server-Adresse, -Name, -Description
  │   └─ PlayerDataModel        — Username, PlayerId, SelectedSkin (aus AuthenticationResponse)
  │
  ├─ MetagameView (Container)
  │   ├─ LoginView              — Login-Formular
  │   ├─ RegisterView           — Registrierungs-Formular
  │   ├─ MainMenuView           — Hauptmenü mit Button-Leiste
  │   ├─ LoadoutView            — Skin-Auswahl mit 3D-Preview
  │   ├─ JoinServerView         — Server-Browser mit Pagination
  │   ├─ CreateServerView       — Server erstellen (TODO)
  │   ├─ DirectIPView           — Direkte IP-Verbindung
  │   ├─ ClientConnectingView   — Verbindungsbildschirm
  │   ├─ MatchmakerView         — Matchmaker-Queue (TODO)
  │   ├─ OptionsView            — Einstellungen (Stub)
  │   └─ LogoutView             — Logout (Stub)
  │
  └─ MetagameController
      ├─ LoginController
      ├─ RegisterController
      ├─ MainMenuController
      ├─ LoadoutController
      ├─ JoinServerController
      ├─ CreateServerController
      ├─ DirectIPController
      ├─ ClientConnectingController
      └─ MatchmakerController
```

---

## Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **MetagameApplication** | `Assets/Scripts/Runtime/Metagame/MetagameApplication.cs` | Root-Klasse, Singleton `Instance` |
| **MetagameModel** | `Assets/Scripts/Runtime/Metagame/Models/MetagameModel.cs` | Model-Container |
| **MetagameView** | `Assets/Scripts/Runtime/Metagame/Views/MetagameView.cs` | View-Container (11 Sub-Views) |
| **MetagameController** | `Assets/Scripts/Runtime/Metagame/Controllers/MetagameController.cs` | Controller: MatchEnteredEvent handling |
| **MetagameEvents** | `Assets/Scripts/Runtime/Metagame/MetagameEvents.cs` | Alle Metagame Events |
| **PlayerDataModel** | `Assets/Scripts/Runtime/Metagame/Models/PlayerDataModel.cs` | Spielerdaten (Username, Id, Skin) |
| **ClientConnectingModel** | `Assets/Scripts/Runtime/Metagame/Models/ClientConnectingModel.cs` | Verbindungs-Timer + Server-Info |

---

## Events (MetagameEvents.cs)

### Navigation Events

| Event | Beschreibung |
|-------|-------------|
| `EnterMatchmakerQueueEvent(queueName)` | Matchmaker-Queue betreten |
| `ExitMatchmakerQueueEvent` | Matchmaker verlassen |
| `EnterIPConnectionEvent` | DirectIP-View öffnen |
| `ExitIPConnectionEvent` | DirectIP-View schließen |
| `ChangeToRegisterEvent` | Von Login zu Register wechseln |
| `ChangeToLoginEvent` | Von Register zu Login wechseln |
| `MatchEnteredEvent` | Match betreten → alle Metagame-Views ausblenden |

### Action Events

| Event | Beschreibung |
|-------|-------------|
| `PlayerLoginEvent { username, password }` | Login-Versuch |
| `PlayerRegisterEvent { username, password, confirmPassword, email }` | Registrierungs-Versuch |
| `JoinThroughDirectIPEvent { ipAddress, port }` | DirectIP-Verbindung starten |
| `CancelConnectionEvent` | Verbindungsversuch abbrechen |
| `CreateServerClickEvent` | Server erstellen (TODO) |
| `JoinServerClickEvent` | Server joinen (veraltet) |
| `RefreshServerListEvent` | Server-Liste neu laden |
| `LoadMoreServersEvent` | Nächste Seite laden |
| `ConnectToServerEvent { ipAddress, port, serverName, serverDescription }` | Mit Server verbinden |

### Skin Events

| Event | Beschreibung |
|-------|-------------|
| `LoadNextSkinEvent` | Nächsten Skin laden |
| `LoadPreviousSkinEvent` | Vorherigen Skin laden |
| `ChangeSkinByNameEvent { skinName }` | Skin per Name wechseln |

### Console Events

| Event | Beschreibung |
|-------|-------------|
| `ToggleConsoleEvent` | Konsole ein/ausblenden |
| `SubmitConsoleCommandEvent { command }` | Konsolen-Befehl absenden |

---

## Hauptmenü (MainMenuView / MainMenuController)

### Button-Konfiguration

Das Hauptmenü nutzt eine `List<ButtonConfig>` mit dynamischem Button-Layout:

| Button | Target View | Funktion |
|--------|-------------|----------|
| `joinServerButton` | JoinServerView | Server-Browser öffnen |
| `createServerButton` | CreateServerView | Server erstellen (TODO) |
| `optionsButton` | OptionsView | Einstellungen (Stub) |
| `loadoutButton` | LoadoutView | Skin-Auswahl mit 3D-Preview |
| `logoutButton` | LogoutView | Logout (Stub) |

### ButtonConfig Klasse

```csharp
class ButtonConfig
{
    string Name;                          // Button-ID
    Texture2D HoverIconPath;              // Hover/Active-Textur
    Action<ButtonConfig> OnClick;         // Click-Handler
    View<MetagameApplication> TargetView; // Ziel-View
    Button ButtonRef;                     // UIToolkit Button-Referenz
    bool IsActive;                        // Aktiver Zustand
}
```

### SubView-Loading

```
MainMenuView.LoadSubView(ButtonConfig):
  1. Alle SubViews ausblenden
  2. TargetView.Show() aufrufen
  3. TargetView.LoadVisualElement() → VisualElement holen
  4. In m_ContentBackground hinzufügen
  5. SetButtonActive() — Aktiven Button hervorheben (Hover-Textur)
```

### Nach Authentication

`MainMenuController.OnUserAuthenticatedEvent()`:
1. `PlayerDataModel.InitializePlayer(authResponse)` — Spielerdaten setzen
2. `PlayerSkinManager.ChangeSkin(selectedSkinName)` — Letzten Skin laden
3. `MainMenuView.Show()` — Hauptmenü anzeigen
4. `MainMenuView.LoadSubViewByName("loadoutButton")` — LoadoutView auto-öffnen

---

## Datenmodelle

### PlayerDataModel

```
PlayerName  → AuthenticationResponse.username
PlayerId    → AuthenticationResponse.playerId
CurrentSelectedSkinName → AuthenticationResponse.selectedSkinName
```

Initialisiert über `InitializePlayer(AuthenticationResponse)` nach erfolgreicher Authentifizierung.

### ClientConnectingModel

```
ElapsedTime     → float, inkrementiert in Update()
ServerAddress   → string (IP:Port)
ServerName      → string (sv_hostname)
ServerDescription → string (sv_description)
```

Gesetzt über `SetServerAddress()`, `SetServerName()`, `SetServerDescription()`.
Timer-Reset via `InitializeTimer()`.

---

## UI-Sounds (UIMenuSoundPlayer)

Statische Utility-Klasse für Menü-Sounds. Liest aus `SoundConfiguration.MenuSounds`.

| Sound Key | Verwendung |
|-----------|-----------|
| `Hilite` | Button Hover |
| `Click` | Button Click |
| `Select` | Auswahl bestätigen |
| `ApplyChanges` | Einstellungen anwenden |
| `Invalid` | Ungültige Eingabe |
| `Printout` | Text-Ausgabe |
| `ObjUpdate` | Objekt-Update |
| `Rotary01` / `Rotary02` | Dreh-Sounds |
| `Lock` / `Unlock` | Sperr-Sounds |

Abspielen: `UIMenuSoundPlayer.Play(soundKey)` → Erstellt temporären AudioSource, spielt ab, zerstört sich.
