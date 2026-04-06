# Authentication System

Login- und Registrierungs-System mit Master-Server-Anbindung, AuthenticationManager StateMachine
und MVC Views/Controllers. Teil der Metagame-Scene.

---

## Architektur-Überblick

```
LoginView / RegisterView
  ↓ (Broadcast)
LoginController / RegisterController
  ↓
AuthenticationManager (StateMachine)
  │  States: Unauthenticated → Authenticating → Authenticated / SessionExpired
  │
  ├─ Authenticate(username, password)
  │    → MasterServerService.AuthenticateAsync()
  │    → AuthenticationEvent (Success / InvalidCredentials / NetworkError)
  │
  └─ RegisterViaService(username, email, password)
       → MasterServerService.RegisterUserAsync()
       → Success / Error Message in View
```

---

## Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **LoginView** | `Assets/Scripts/Runtime/Metagame/Views/LoginView.cs` | Login-Formular |
| **LoginController** | `Assets/Scripts/Runtime/Metagame/Controllers/LoginController.cs` | Login-Logik + Auth-Events |
| **RegisterView** | `Assets/Scripts/Runtime/Metagame/Views/RegisterView.cs` | Registrierungs-Formular |
| **RegisterController** | `Assets/Scripts/Runtime/Metagame/Controllers/RegisterController.cs` | Register-Logik + MasterServer |
| **AuthenticationManager** | StateMachine | Authentifizierungs-Lifecycle |

---

## Login View

### UI-Layout

```
┌──────────────────────────────────────┐
│  [SoF2 Logo]                         │
│                                      │
│  ┌──────────────────────────────┐    │
│  │  Username: [_______________] │    │
│  │  Password: [_______________] │    │
│  │                              │    │
│  │  [Login]     [Register]      │    │
│  │                              │    │
│  │  Error: Invalid credentials  │    │ (Status Label, rot/grün)
│  └──────────────────────────────┘    │
│                                      │
│  [Quit]                              │
└──────────────────────────────────────┘
```

### Texturen

| Element | Textur-Key | Beschreibung |
|---------|-----------|-------------|
| Background | `menuBackground` | Hintergrundbild |
| Logo | `logoImage` | SoF2-Logo |
| TextFields | `textFieldBackground` | Input-Hintergrund |
| Buttons | `buttonBackground` | Button-Hintergrund |

### Validierung (`ValidateLoginData()`)

- Username: Nicht leer
- Password: Nicht leer
- Bei Fehler: `SetStatusMessage(message, isError: true)` → Rotes Label

### Login-in-Progress

`SetLoginInProgress(true)`:
- Deaktiviert alle Inputs + Buttons
- Zeigt Modal-Spinner (`m_loginModal`)

---

## Register View

### UI-Layout

```
┌──────────────────────────────────────┐
│  Username:         [_______________] │
│  Email:            [_______________] │
│  Password:         [_______________] │
│  Confirm Password: [_______________] │
│                                      │
│  [Back to Login]        [Register]   │
│                                      │
│  Error: Passwords don't match        │ (Status Label)
└──────────────────────────────────────┘
```

### Validierung (`ValidateRegistrationData()`)

| Check | Bedingung | Fehlermeldung |
|-------|-----------|---------------|
| Username | Nicht leer | "Username is required" |
| Username | Min. 3 Zeichen | "Username must be at least 3 characters" |
| Email | Nicht leer | "Email is required" |
| Email | Regex-Validierung | "Please enter a valid email" |
| Password | Nicht leer | "Password is required" |
| Confirm | Passwörter identisch | "Passwords do not match" |

---

## Login Controller Flow

### Event-Subscriptions

| Event | Handler | Aktion |
|-------|---------|--------|
| `PlayerLoginEvent` | `OnPlayerLogin()` | Validierung → AuthenticationManager.Authenticate() |
| `ChangeToRegisterEvent` | `OnChangeToRegister()` | LoginView ausblenden |
| `ChangeToLoginEvent` | `OnChangeToLogin()` | LoginView anzeigen + Texturen laden |
| `AuthenticationEvent` | `OnAuthenticationEvent()` | Status-Updates in View |
| `UserUnauthenticatedEvent` | `OnUserUnauthenticatedEvent()` | LoginView anzeigen |
| `UserAuthenticatedEvent` | `OnUserAuthenticatedEvent()` | LoginView ausblenden |

### Authentication-Status Handling

```
AuthenticationEvent:
  Authenticating    → View.SetLoginInProgress(true)
  Success           → View.SetStatusMessage("Login successful!", false)
                    → View.SetLoginInProgress(false)
  InvalidCredentials → View.SetStatusMessage("Invalid credentials", true)
                    → View.SetLoginInProgress(false)
  NetworkError      → View.SetStatusMessage("Network error...", true)
                    → View.SetLoginInProgress(false)
```

### Texture Loading (`PrepareViewTexturesAndShow()`)

Lädt SoF2-Texturen über `TextureManager` und wendet sie auf Login-UI an:
- Background, Logo, Button-Texturen, Input-Texturen

---

## Register Controller Flow

### Event-Subscriptions

| Event | Handler | Aktion |
|-------|---------|--------|
| `PlayerRegisterEvent` | `OnPlayerRegister()` | Validierung → RegisterViaService() |
| `ChangeToRegisterEvent` | `OnChangeToRegister()` | RegisterView anzeigen |
| `ChangeToLoginEvent` | `OnChangeToLogin()` | RegisterView ausblenden |
| `ConnectionEvent` | `OnConnectionEvent()` | RegisterView ausblenden bei Connecting |

### Registration Flow (Async)

```csharp
async RegisterViaService(username, email, password):
  View.SetRegisterInProgress(true)
  MasterServerService masterService = ServiceLocator.Get<MasterServerService>()
  await masterService.RegisterUserAsync(username, email, password)
  → Success: View.SetStatusMessage("Registration successful!", false)
  → Error:   View.SetStatusMessage(error.Message, true)
  View.SetRegisterInProgress(false)
```

---

## Status-Messages

| Kontext | Farbe | Beispiel |
|---------|-------|---------|
| Erfolg | Grün | "Login successful!", "Registration successful!" |
| Fehler | Rot | "Invalid credentials", "Passwords do not match" |
| Info | Standard | "Authenticating..." |

Methode: `SetStatusMessage(string message, bool isError)`
- `isError = true` → Rot
- `isError = false` → Grün
