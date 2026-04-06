# Server Browser System

SoF2-Style Server-Browser mit Master-Server-Anbindung, Pagination, DirectIP-Verbindung 
und Connecting-Screen. Teil der Metagame-Scene.

---

## Architektur-Überblick

```
┌─────────────────────────────────────────────────────────────┐
│                    Server-Browser Flow                       │
│                                                              │
│  JoinServerView ←→ JoinServerController ←→ MasterServerService
│    ↓ (select)                                                │
│  ConnectToServerEvent                                        │
│    ↓                                                         │
│  ConnectionManager.StartClient(ip, port, playerData)         │
│    ↓                                                         │
│  ClientConnectingView ← ClientConnectingController           │
│    ↓ (success)                                               │
│  MatchEnteredEvent → Metagame-Views ausblenden               │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    DirectIP Flow                             │
│                                                              │
│  DirectIPView → JoinThroughDirectIPEvent                     │
│    ↓                                                         │
│  DirectIPController → ConnectionManager.StartClient()        │
│    ↓                                                         │
│  ClientConnectingView (gleicher Flow wie oben)               │
└─────────────────────────────────────────────────────────────┘
```

---

## Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **JoinServerView** | `Assets/Scripts/Runtime/Metagame/Views/JoinServerView.cs` | Server-Liste, Auswahl, Verbindung |
| **JoinServerController** | `Assets/Scripts/Runtime/Metagame/Controllers/JoinServerController.cs` | Pagination, MasterServer-Requests |
| **DirectIPView** | `Assets/Scripts/Runtime/Metagame/Views/DirectIPView.cs` | IP + Port Eingabe |
| **DirectIPController** | `Assets/Scripts/Runtime/Metagame/Controllers/DirectIPController.cs` | DirectIP-Verbindung |
| **ClientConnectingView** | `Assets/Scripts/Runtime/Metagame/Views/ClientConnectingView.cs` | Verbindungsbildschirm |
| **ClientConnectingController** | `Assets/Scripts/Runtime/Metagame/Controllers/ClientConnectingController.cs` | Verbindungsstatus |
| **ServerBrowserEntry** | `Assets/Scripts/Runtime/Management/DataManagement/ServerBrowserEntry.cs` | Daten-POCOs |

---

## Server-Browser (JoinServerView / JoinServerController)

### UI-Layout

```
┌─────────────────────────────────────────────────────────┐
│  [Get List]  [Refresh]  [New Favorite]  [Add Fav]       │
│  [Server Info]  [Find Friend]                           │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ 🔒 │ ^1Sgt's^7 Server   │ mp_shop │ TDM │ 8/16│35│  │
│  │    │ ^4Fun^7Zone         │ mp_city │ CTF │ 12/24│22│  │
│  │ 🔒 │ ^2Pro^7 Arena       │ mp_dust │ DM  │ 6/16│78│  │
│  │    │ ^3NoobFriendly      │ mp_map1 │ HAS │ 4/8 │55│  │
│  │    │                     │         │     │     │   │  │
│  │    │  (infinite scroll — loads more at bottom)   │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  Selected: ^1Sgt's^7 Server            [Connect]         │
│  Showing 4 of 12 servers                                 │
└─────────────────────────────────────────────────────────┘
```

### Server-Row Details

Jede Zeile enthält (programmatisch in `CreateServerRow()`):

| Element | Breite | Inhalt |
|---------|--------|--------|
| Lock Icon | 4% | Rotes Schloss wenn `hasPassword` |
| Server Name | 40% | `QuakeColorLabel` (farbcodiert) |
| Map Name | 18% | Aktueller Mapname |
| Gametype | 10% | z.B. "TDM", "CTF", "HAS" |
| Players | 14% | `"{current}/{max}"` |
| Ping | 14% | `"{ping}ms"` |

### Interaktion

- **Single-Click:** Zeile selektieren → Highlight + Connect-Button aktivieren + `m_SelectedServerLabel` aktualisieren
- **Double-Click:** Sofort `ConnectToServerEvent` broadcasten

### Pagination (Infinite Scroll)

```
k_PageSize = 10 (Server pro Seite)

JoinServerController:
  FetchServerList():
    await masterService.FetchServerPageAsync(0, k_PageSize)
    → View.BeginServerList(totalCount)
    → View.AppendServerBatch(servers, loadedSoFar, totalCount, hasMore)

  LoadNextBatch():
    await masterService.FetchServerPageAsync(m_CurrentOffset, k_PageSize)
    → View.AppendServerBatch(...)

JoinServerView:
  OnScrollValueChanged(scrollOffset):
    if (scrollOffset nahe Ende) && m_HasMore → Broadcast LoadMoreServersEvent
```

### Custom Scrolling (Drag-System)

Die Server-Liste nutzt ein manuelles Drag-Scroll-System statt NaviveScrollView:

| Konstante | Wert | Beschreibung |
|-----------|------|-------------|
| `k_ScrollThreshold` | 50px | Scroll-Distanz bis LoadMore |
| `k_DragThreshold` | 5px | Minimum Drag-Bewegung |

Events: `PointerDown` → `PointerMove` → `PointerUp` / `PointerCaptureOut`

### Button-Aktionen

| Button | Aktion |
|--------|--------|
| Refresh | Broadcast `RefreshServerListEvent` → `FetchServerList()` |
| Connect | Broadcast `ConnectToServerEvent { ip, port, name, desc }` |
| Get List | (gleich wie Refresh) |
| New Favorite / Add Favorite / Server Info / Find Friend | (Platzhalter, TODO) |

---

## DirectIP-Verbindung (DirectIPView / DirectIPController)

### UI-Layout

```
┌────────────────────────────┐
│  IP Address: [127.0.0.1  ] │
│  Port:       [7777       ] │
│                             │
│  [Join]         [Cancel]    │
└────────────────────────────┘
```

### Input-Validierung

- `SanitizeAndSetIpAddress(string)` — Filtert: nur `[A-Za-z0-9.]` erlaubt (Regex)
- `SanitizeAndSetPort(string)` — Filtert gleich + Parse als `ushort`
- Port-Default: `7777`

### Flow

```
Enter IP → Enter Port → Click Join
  → JoinThroughDirectIPEvent { ipAddress, port }
  → DirectIPController.OnJoinGame()
  → ConnectionManager.StartClient(ip, port, playerSkinName, playerName, displayName)
```

---

## Client Connecting Screen (ClientConnectingView / ClientConnectingController)

### UI-Layout

```
┌──────────────────────────────────┐
│  [Logo]                          │
│                                  │
│  Connecting to 192.168.1.5:7777  │
│  ^1Sgt's^7 Server               │ (QuakeColorLabel)
│  ^7A fun server for all^7        │ (QuakeColorLabel)
│                                  │
│  [🔫 Loading... 0:12]            │ (Bullet/Clip Animation)
│                                  │
│  [Cancel]                        │
└──────────────────────────────────┘
```

### Features

- **Timer:** `ClientConnectingModel.ElapsedTime` — Zeigt vergangene Zeit seit Verbindungsversuch
- **Server-Info:** `ServerAddress`, `ServerName`, `ServerDescription` aus Model (QuakeColorLabels)
- **Update-Loop:** `Update()` liest Model-Daten jeden Frame und aktualisiert Labels
- **Cancel:** Broadcast `CancelConnectionEvent` → `ConnectionManager.CancelClientConnectionAttempt()`

### Controller-Logik

```
ConnectionEvent.Connecting  → View anzeigen
ConnectionEvent.* (andere)  → View ausblenden
CancelConnectionEvent       → ConnectionManager.CancelClientConnectionAttempt()
```

### Texturen

| Textur | Verwendung |
|--------|-----------|
| `menuBackground` | Hintergrundbild |
| `logoImage` | SoF2-Logo |
| `joinIcon` | Join-Symbol |
| `bulletLoadingIndicator` | Loading-Animation (Patronen) |
| `clipLoadingIndicator` | Loading-Animation (Magazin) |
| `bigcharsAtlas` | QuakeColorLabel für Server-Name/Description |

---

## Datenmodelle (ServerBrowserEntry.cs)

### ServerBrowserEntry

```csharp
[Serializable]
class ServerBrowserEntry
{
    string id;              // Server-ID (vom Master-Server)
    string hostname;        // sv_hostname (QuakeColorLabel-kompatibel)
    string ip;              // Server-IP
    int port;               // Server-Port
    string mapName;         // Aktuelle Map
    string gametype;        // Gametype-ID (z.B. "tdm", "hideandseek")
    int currentPlayers;     // Aktuelle Spieleranzahl
    int maxPlayers;         // Maximum
    int ping;               // Latenz in ms (0 = unbekannt)
    bool hasPassword;       // sv_password gesetzt
    string description;     // sv_description
    string version;         // Applikations-Version
}
```

### ServerBrowserPageResponse (Pagination)

```csharp
[Serializable]
class ServerBrowserPageResponse
{
    ServerBrowserEntry[] servers;  // Aktuelle Seite
    int total;                     // Gesamt-Anzahl
    int offset;                    // Aktueller Offset
    bool hasMore;                  // Weitere Seiten verfügbar
}
```

### ServerRegistrationPayload (Server → Master-Server)

```csharp
[Serializable]
class ServerRegistrationPayload
{
    string hostname, ip, mapName, gametype, description, version;
    int port, currentPlayers, maxPlayers;
    bool hasPassword;
    string password, rconPassword;
}
```

### ServerRegistrationResponse

```csharp
[Serializable]
class ServerRegistrationResponse
{
    string serverId;       // Zugewiesene Server-ID
    string message;        // Status-Nachricht
    string rconPassword;   // RCON-Passwort
}
```

---

## Ping

| Konstante | Wert |
|-----------|------|
| `k_PingTimeoutMs` | 2000ms |

ICMP-Ping wird pro Server gemessen. Timeout nach 2 Sekunden → Ping bleibt `0`.
