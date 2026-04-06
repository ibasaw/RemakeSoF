# Console System

In-Game-Konsole mit MVC-Pattern, DontDestroyOnLoad, QuakeColorLabel-Output und 
Server-Command-Bridge. Verfügbar in allen Scenes (Metagame + Game).

---

## Architektur

```
ConsoleManager : BaseApplication<ConsoleModel, ConsoleView, ConsoleController>
  │  (Singleton, DontDestroyOnLoad)
  │
  ├─ ConsoleModel   — Empty (kein State)
  ├─ ConsoleView     — UIToolkit: Input, Output, Toggle
  └─ ConsoleController — Input-Binding + Command-Dispatch
         ↓
NetworkedCommandBridge.Instance.SendCommandToServer(command)
```

---

## Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **ConsoleManager** | `Assets/Scripts/Runtime/Management/ConsoleManagement/ConsoleManager.cs` | Application Root, Singleton |
| **ConsoleModel** | `Assets/Scripts/Runtime/Management/ConsoleManagement/Models/ConsoleModel.cs` | Leerer Model |
| **ConsoleView** | `Assets/Scripts/Runtime/Management/ConsoleManagement/Views/ConsoleView.cs` | UI: Input/Output/Toggle |
| **ConsoleController** | `Assets/Scripts/Runtime/Management/ConsoleManagement/Controllers/ConsoleController.cs` | Event-Handling + Command-Dispatch |
| **ConsoleView.uxml** | `Assets/UIToolkit/ConsoleView/ConsoleView.uxml` | UXML Layout |

---

## UI-Layout

```
┌─────────────────────────────────────────────────────┐
│  ═══════════════ [Glowline] ═══════════════════════ │
│  ═══════════════ [GlowlineWide] ═══════════════════ │
│                                                      │
│  > sv_hostname "Test Server"                         │  (weiß)
│  Server hostname set to: Test Server                 │  (weiß)
│  > invalid_command                                   │  (weiß)
│  Unknown command: invalid_command                    │  (rot, bold)
│  > status                                            │  (weiß)
│  Warning: Not connected to server                    │  (gelb, bold)
│                                                      │
│  ┌─────────────────────────────────────────────────┐ │
│  │ > _                                             │ │  (Input TextField)
│  └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

---

## Toggle-Mechanik

```
Input: toggleConsoleAction (InputActionReference, z.B. Tilde ~)
  → ConsoleController.OnToggleConsoleAction(CallbackContext)
  → Broadcast ToggleConsoleEvent
  → ConsoleController.OnToggleConsole()
  → ConsoleView.Toggle()
    → IsOpen = !IsOpen
    → Show()/Hide() + Focus-Management
```

---

## Command-Flow

```
User tippt Befehl + Enter
  → ConsoleView.OnInputSubmit(KeyUpEvent)
    → if (KeyCode.Return)
      → Trim Input
      → Broadcast SubmitConsoleCommandEvent { command }
      → Clear Input + Refocus
  → ConsoleController.OnConsoleCommand()
    → NetworkedCommandBridge.Instance.SendCommandToServer(command)
```

---

## Output-Typen

| Methode | Farbe | Stil | Verwendung |
|---------|-------|------|-----------|
| `AddOutput(string)` | Weiß | Normal | Standard-Ausgabe |
| `AddWarningOutput(string)` | Gelb | Bold | Warnungen |
| `AddErrorOutput(string)` | Rot | Bold | Fehler |
| `ClearConsole()` | — | — | Alle Output-Labels entfernen |

Jede Ausgabe wird als neues `Label` dem `m_Output` ScrollView hinzugefügt.
`ScrollToBottom()` scrollt nach jeder neuen Ausgabe automatisch zum Ende.

---

## Texturen

| Element | Textur-Key | Beschreibung |
|---------|-----------|-------------|
| `m_Glowline` | `glowline` | Schmale leuchtende Linie oben |
| `m_GlowlineWide` | `glowlineWide` | Breite leuchtende Linie |
| Background | `consoleBackground` | Konsolen-Hintergrund |

Geladen über `TextureManager` in `OnEnable()`.

---

## Lebenszyklus

- **ConsoleManager.Awake():** Singleton setzen, `DontDestroyOnLoad(gameObject)` — überlebt Scene-Wechsel
- **ConsoleView.OnEnable():** UI-Elemente binden, Texturen laden, Callbacks registrieren
- **ConsoleView.OnDisable():** Callbacks deregistrieren
- **ConsoleController.OnEnable():** `toggleConsoleAction` aktivieren
- **ConsoleController.OnDisable():** `toggleConsoleAction` deaktivieren
