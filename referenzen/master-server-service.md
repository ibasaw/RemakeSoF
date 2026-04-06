# Master Server Service

REST-basierter Service für Server-Registrierung, Heartbeat, Authentifizierung und Server-Browser.
Pure Service (kein MonoBehaviour), registriert im `ServiceLocator` via `ApplicationEntryPoint`.

---

## Architektur-Überblick

```
┌──────────────────────────────────────────────────────────────┐
│                     Server-Seite                              │
│                                                               │
│  ApplicationEntryPoint                                        │
│    └─ MasterServerService(sv_master URL)                      │
│         ├─ RegisterServerAsync(config, players)               │
│         │    ├─ POST /api/registerServer                      │
│         │    └─ StartHeartbeat() → 30s Loop                   │
│         ├─ UpdateServerInfo(players, mapName)                 │
│         │    └─ Aktualisiert lokale Werte für nächsten Beat   │
│         └─ DeregisterServerAsync()                            │
│              ├─ StopHeartbeat()                                │
│              └─ POST /api/deregisterServer                    │
└──────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│                     Client-Seite                              │
│                                                               │
│  JoinServerController                                         │
│    └─ MasterServerService.FetchServerPageAsync(offset, limit) │
│         └─ GET /api/servers?offset=X&limit=Y                  │
│              └─ ServerBrowserPageResponse (List<ServerEntry>)  │
│                                                               │
│  LoginController                                              │
│    └─ MasterServerService.LoginUserAsync(user, pass)          │
│         └─ POST /api/login                                    │
│              └─ MasterServerAuthResult                        │
│                                                               │
│  RegisterController                                           │
│    └─ MasterServerService.RegisterUserAsync(user, email, pass)│
│         └─ POST /api/register                                 │
│              └─ MasterServerRegisterResult                    │
└──────────────────────────────────────────────────────────────┘
```

---

## Datei

| Datei | Pfad | Rolle |
|-------|------|-------|
| **MasterServerService** | `Assets/Scripts/Runtime/Management/DataManagement/MasterServerService.cs` | Pure Service: REST-API-Kommunikation mit Master-Server |

---

## Konstruktor

```csharp
public MasterServerService(string masterServerUrl)
```

Der `masterServerUrl` wird aus der `sv_master` Konfiguration gelesen (z.B. `"http://localhost:3000"`).
Wird in `ApplicationEntryPoint.Awake()` instanziiert und im `ServiceLocator` registriert.

---

## Konstanten & Felder

| Feld / Konstante | Typ | Wert | Beschreibung |
|-------------------|-----|------|-------------|
| `k_HeartbeatIntervalMs` | `const int` | 30000 (30s) | Heartbeat-Intervall |
| `m_MasterServerUrl` | `readonly string` | — | Basis-URL des Master-Servers |
| `m_RegisteredServerId` | `string` | — | Server-ID nach Registrierung |
| `m_HeartbeatCts` | `CancellationTokenSource` | — | Token für Heartbeat-Abbruch |
| `m_ServerConfig` | `ServerConfiguration` | — | Aktuelle Server-Konfiguration |
| `m_CurrentPlayers` | `int` | — | Aktuelle Spieleranzahl |
| `m_RconPassword` | `string` | — | RCON-Passwort |
| `m_CurrentMapName` | `string` | — | Aktuelle Map |

**Properties**:

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `IsRegistered` | `bool` | `true` wenn Server beim Master registriert ist |
| `RconPassword` | `string` | RCON-Passwort (read-only) |

---

## Server-seitige Methoden

### RegisterServerAsync

```csharp
public async Task RegisterServerAsync(ServerConfiguration config, int currentPlayers)
```

Registriert den Dedicated Server beim Master-Server.

**Ablauf**:
1. POST `/api/registerServer` mit `ServerRegistrationPayload`
2. Empfängt `ServerRegistrationResponse` mit Server-ID
3. Speichert `m_RegisteredServerId`
4. Ruft `StartHeartbeat()` auf → startet 30s-Loop

**Payload** (gesendet):

| Feld | Beschreibung |
|------|-------------|
| hostname | Server-Name |
| port | Port-Nummer |
| map | Aktuelle Map |
| gametype | Spielmodus |
| maxPlayers | Max. Spieler |
| currentPlayers | Aktuelle Spieler |
| version | Build-Version |
| rconPassword | RCON-Passwort (optional) |

### UpdateServerInfo

```csharp
public void UpdateServerInfo(int currentPlayers, string currentMapName)
```

Aktualisiert lokale Werte (`m_CurrentPlayers`, `m_CurrentMapName`).
Diese Werte werden beim nächsten Heartbeat an den Master-Server gesendet.
**Kein sofortiger HTTP-Request** — nur lokale State-Änderung.

### DeregisterServerAsync

```csharp
public async Task DeregisterServerAsync()
```

Meldet den Server beim Master-Server ab.

**Ablauf**:
1. `StopHeartbeat()` — beendet den Heartbeat-Loop
2. POST `/api/deregisterServer` mit Server-ID
3. Setzt `m_RegisteredServerId` zurück

---

## Heartbeat-System

### Ablauf

```
RegisterServerAsync() erfolgreich
  └─ StartHeartbeat()
       └─ HeartbeatLoopAsync(CancellationToken)
            └─ while (!cancelled):
                 ├─ await Task.Delay(30000ms)
                 └─ SendHeartbeatAsync()
                      └─ POST /api/heartbeat
                           ├─ serverId
                           ├─ hostname
                           ├─ port
                           ├─ map (m_CurrentMapName)
                           ├─ gametype
                           ├─ currentPlayers (m_CurrentPlayers)
                           └─ version
```

### Methoden

| Methode | Sichtbarkeit | Beschreibung |
|---------|-------------|-------------|
| `StartHeartbeat()` | private | Erstellt `CancellationTokenSource`, startet `HeartbeatLoopAsync` |
| `StopHeartbeat()` | private | Cancelt Token → Loop bricht sauber ab |
| `HeartbeatLoopAsync(CancellationToken)` | private async | Endlos-Loop: 30s warten → `SendHeartbeatAsync()` |
| `SendHeartbeatAsync()` | private async | POST `/api/heartbeat` mit aktuellen Server-Daten |

### Timing

| Parameter | Wert |
|-----------|------|
| Intervall | 30 Sekunden (`k_HeartbeatIntervalMs = 30000`) |
| Erster Beat | 30s nach Registrierung (nicht sofort) |
| Abbruch | Bei `DeregisterServerAsync()` oder `ClearCache()` |

---

## Client-seitige Methoden

### FetchServerPageAsync

```csharp
public async Task<ServerBrowserPageResponse> FetchServerPageAsync(int offset, int limit)
```

Holt eine Seite der Server-Liste vom Master-Server.

**Request**: GET `/api/servers?offset={offset}&limit={limit}`

**Response**: `ServerBrowserPageResponse` mit:

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Servers | `List<ServerBrowserEntry>` | Server-Einträge der Seite |
| TotalCount | `int` | Gesamtanzahl registrierter Server |

**ServerBrowserEntry**:

| Feld | Typ | Beschreibung |
|------|-----|-------------|
| Hostname | `string` | Server-Name |
| Ip | `string` | IP-Adresse |
| Port | `int` | Port |
| Map | `string` | Aktuelle Map |
| Gametype | `string` | Spielmodus |
| Players | `int` | Aktuelle Spieler |
| MaxPlayers | `int` | Max. Spieler |

### LoginUserAsync

```csharp
public async Task<MasterServerAuthResult> LoginUserAsync(string username, string password)
```

**Request**: POST `/api/login` mit `{ username, password }`

**Response**: `MasterServerAuthResult`

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| IsSuccess | `bool` | Login erfolgreich? |
| Response | `AuthenticationResponse` | Auth-Token + UserData bei Erfolg |
| ResponseCode | `long` | HTTP-Status-Code |
| ErrorMessage | `string` | Fehlermeldung bei Misserfolg |

**Factory-Methoden**: `CreateSuccess(response)`, `CreateFailure(code, error)`

### RegisterUserAsync

```csharp
public async Task<MasterServerRegisterResult> RegisterUserAsync(string username, string email, string password)
```

**Request**: POST `/api/register` mit `{ username, email, password }`

**Response**: `MasterServerRegisterResult`

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| IsSuccess | `bool` | Registrierung erfolgreich? |
| Message | `string` | Erfolgsmeldung |
| ErrorMessage | `string` | Fehlermeldung bei Misserfolg |

**Innere Klasse**: `RegisterResponse` (JSON-Deserialisierung):
```csharp
[Serializable]
public class RegisterResponse
{
    public bool success;
    public string message;
    public string userId;
}
```

**Factory-Methoden**: `CreateSuccess(message)`, `CreateFailure(error)`

---

## ClearCache

```csharp
public void ClearCache()
```

Wird von `ServiceLocator.ClearAll()` beim Teardown aufgerufen.
Stoppt den Heartbeat-Loop und setzt den registrierten Server-ID zurück.

---

## Lifecycle

```
ApplicationEntryPoint.Awake()
  └─ ServiceLocator.Register(new MasterServerService(sv_master))

Server startet:
  └─ RegisterServerAsync(config, 0) → Heartbeat startet

Map-Wechsel / Spieler-Join:
  └─ UpdateServerInfo(players, mapName) → nächster Heartbeat sendet neue Werte

Server stoppt:
  └─ DeregisterServerAsync() → Heartbeat stoppt → Server wird aus Liste entfernt

ApplicationEntryPoint.OnDestroy()
  └─ ServiceLocator.ClearAll() → ClearCache() → Heartbeat-Abbruch
```

---

## API-Endpunkte (Zusammenfassung)

| Methode | HTTP | Endpunkt | Richtung |
|---------|------|----------|----------|
| RegisterServerAsync | POST | `/api/registerServer` | Server → Master |
| SendHeartbeatAsync | POST | `/api/heartbeat` | Server → Master |
| DeregisterServerAsync | POST | `/api/deregisterServer` | Server → Master |
| FetchServerPageAsync | GET | `/api/servers?offset=X&limit=Y` | Client → Master |
| LoginUserAsync | POST | `/api/login` | Client → Master |
| RegisterUserAsync | POST | `/api/register` | Client → Master |
