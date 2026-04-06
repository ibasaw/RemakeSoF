# Server Configuration System

JSON-basierte SoF2-authentische Server-Konfiguration.
Geladen aus `Resources/Data/SoF2_Server_Configuration.json` via `ServerConfigurationLoader`.

---

## Architektur-Überblick

```
ApplicationEntryPoint.Awake() (Server)
  └─ ServerConfigurationLoader() → JsonUtility.FromJson<ServerConfiguration>(json)
       └─ ServiceLocator.Register(serverConfigLoader)

Zugriff überall via:
  ServiceLocator.Get<ServerConfigurationLoader>().Configuration
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **ServerConfiguration** | `Assets/Scripts/Runtime/Management/DataManagement/ServerConfiguration.cs` | POCO mit allen CVARs |
| **ServerConfigurationLoader** | `Assets/Scripts/Runtime/Management/DataManagement/ServerConfigurationLoader.cs` | Lädt JSON, stellt Configuration bereit |
| **SoF2_Server_Configuration.json** | `Assets/Resources/Data/SoF2_Server_Configuration.json` | JSON-Datei mit allen Werten |
| **ApplicationEntryPoint** | `Assets/Scripts/Runtime/ApplicationLifecycle/ApplicationEntryPoint.cs` | Registriert Loader im ServiceLocator |

---

## Alle CVARs

### Server-CVARs

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `sv_hostname` | `string` | `"SoF2 Remake Server"` | Servername |
| `sv_description` | `string` | — | Server-Beschreibung (mit Quake-Farbcodes) |
| `sv_motd` | `string` | — | Message of the Day |
| `sv_maxclients` | `int` | `16` | Maximale Spieleranzahl |
| `sv_minclients` | `int` | `1` | Minimale Spieleranzahl für Match-Start |
| `sv_port` | `int` | `7777` | Server-Port |
| `sv_ip` | `string` | `"127.0.0.1"` | Bind-Adresse |
| `sv_tickrate` | `int` | `20` | Server-Tickrate |
| `sv_allowdownload` | `bool` | `true` | Downloads erlauben |
| `sv_pure` | `bool` | `false` | Pure-Server-Modus |
| `sv_password` | `string` | `""` | Server-Passwort |
| `sv_privatePassword` | `string` | `""` | Reservierte Slots Passwort |
| `rconPassword` | `string` | `""` | Remote-Console-Passwort |
| `sv_master` | `string` | `"http://localhost:8000"` | Master-Server-URL |

### Game-CVARs

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `g_gametype` | `string` | `"hideandseek"` | Aktiver Gametype (tdm, ctf, hideandseek) |
| `g_mapname` | `string` | `"maps/mp_kam2"` | Start-Map |
| `g_friendlyfire` | `bool` | `false` | Friendly Fire |
| `g_gravity` | `int` | `800` | Gravitation |
| `g_speed` | `int` | `320` | Spieler-Geschwindigkeit |
| `g_knockback` | `int` | `1000` | Knockback-Multiplikator |
| `g_weaponrespawn` | `int` | `5` | Waffen-Respawn-Zeit (Sekunden) |
| `g_forcerespawn` | `int` | `0` | Automatisches Respawn erzwingen |
| `g_inactivity` | `int` | `120` | Inaktivitäts-Timeout (Sekunden) |
| `g_warmup` | `int` | `10` | Warmup-Zeit (Sekunden) |
| `g_allowvote` | `bool` | `true` | Voting erlauben |
| `g_teamAutoJoin` | `bool` | `false` | Auto-Team-Join |
| `g_teamForceBalance` | `bool` | `true` | Team-Balance erzwingen |

### Limits

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `timelimit` | `int` | `300` | Zeitlimit (Sekunden) |
| `fraglimit` | `int` | `0` | Frag-Limit (DM/TDM) |
| `scorelimit` | `int` | `50` | Score-Limit (CTF/Objective) |
| `roundlimit` | `int` | `0` | Runden-Limit |
| `roundtimelimit` | `int` | `180` | Rundenzeit-Limit (Sekunden) |
| `dmflags` | `int` | `0` | DM-Flags Bitfeld |

### Map-Rotation

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `sv_mapRotation` | `string[]` | `["maps/mp_kam2", ...]` | Map-Rotation (Round-Robin) |

### Hide and Seek

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `hideandseek_hidetime` | `int` | `30` | Versteckzeit (Sekunden) |
| `hideandseek_seektime` | `int` | `120` | Suchzeit (Sekunden) |
| `hideandseek_seekercount` | `int` | `1` | Anzahl Seeker |
| `hideandseek_hidermodel` | `string` | `""` | Erzwungenes Hider-Model |
| `hideandseek_seekermodel` | `string` | `""` | Erzwungenes Seeker-Model |
| `hideandseek_hiderweapons` | `bool` | `false` | Hider dürfen Waffen benutzen |
| `hideandseek_seekerweapons` | `bool` | `true` | Seeker dürfen Waffen benutzen |
| `hideandseek_roundlimit` | `int` | `5` | Runden-Limit für HideAndSeek |

### Bot-Konfiguration

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `sv_botcount` | `int` | `1` | Anzahl AI-Bots beim Start (0 = keine) |
| `sv_botnames` | `string[]` | 15 SoF2-Namen | Bot-Namen (Round-Robin) |
| `sv_botskins` | `string[]` | `["mullins_jungle"]` | Bot-Skins (Round-Robin) |

---

## Nutzung in Code

```csharp
// Pure Service via ServiceLocator
ServerConfigurationLoader loader = ServiceLocator.Get<ServerConfigurationLoader>();
ServerConfiguration config = loader.Configuration;

// Beispiele:
int port = config.sv_port;
string gametype = config.g_gametype;
int botCount = config.sv_botcount;
```

### Gametype-spezifischer Zugriff

Gametypes erhalten `ServerConfiguration` via `Initialize(definition, serverConfig)`:
```csharp
public class HideAndSeekGametype : BaseGametype
{
    int HideTime => ServerConfig.hideandseek_hidetime > 0 ? ServerConfig.hideandseek_hidetime : 30;
}
```
