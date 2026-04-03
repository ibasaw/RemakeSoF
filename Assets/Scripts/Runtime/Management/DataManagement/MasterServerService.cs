using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Tolik.RemakeSoF.Runtime.DataManagement
{
    /// <summary>
    /// Pure Service fuer die Kommunikation mit dem Master-Server (Authentication-API).
    /// Server-Seite: Registrierung, Heartbeat, Deregistrierung.
    /// Client-Seite: Server-Liste abrufen.
    /// Registriert im ServiceLocator.
    /// </summary>
    public class MasterServerService
    {
        /// <summary>Basis-URL des Master-Servers (aus sv_master oder Default).</summary>
        readonly string m_MasterServerUrl;

        /// <summary>Heartbeat-Intervall in Millisekunden (30 Sekunden).</summary>
        const int k_HeartbeatIntervalMs = 30000;

        /// <summary>
        /// Erstellt einen MasterServerService mit der Master-Server-URL aus der Konfiguration (sv_master).
        /// </summary>
        /// <param name="masterServerUrl">sv_master URL aus der Server-/Client-Konfiguration. Darf nicht leer sein.</param>
        public MasterServerService(string masterServerUrl)
        {
            if (string.IsNullOrEmpty(masterServerUrl))
            {
                throw new System.ArgumentException("Master server URL (sv_master) muss in der Konfiguration gesetzt sein.", nameof(masterServerUrl));
            }

            m_MasterServerUrl = masterServerUrl.TrimEnd('/');
        }

        /// <summary>Vom Master-Server vergebene Server-ID nach Registrierung.</summary>
        string m_RegisteredServerId;

        /// <summary>Cancellation-Token fuer den Heartbeat-Loop.</summary>
        CancellationTokenSource m_HeartbeatCts;

        /// <summary>Referenz auf die Server-Konfiguration (nur Server-Seite).</summary>
        ServerConfiguration m_ServerConfig;

        /// <summary>Aktueller Spieler-Count (wird vom Server aktualisiert).</summary>
        int m_CurrentPlayers;

        /// <summary>Aktuelle Map (wird vom Server aktualisiert).</summary>
        string m_CurrentMapName;

        /// <summary>Ob der Server registriert ist.</summary>
        public bool IsRegistered => !string.IsNullOrEmpty(m_RegisteredServerId);

        // â”€â”€ Server-Seite â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Registriert diesen Game-Server beim Master-Server.
        /// Startet automatisch den Heartbeat-Loop nach erfolgreicher Registrierung.
        /// </summary>
        /// <param name="config">Die aktive Server-Konfiguration.</param>
        /// <param name="currentPlayers">Aktuelle Spieleranzahl.</param>
        public async Task RegisterServerAsync(ServerConfiguration config, int currentPlayers)
        {
            m_ServerConfig = config;
            m_CurrentPlayers = currentPlayers;
            m_CurrentMapName = config.g_mapname;

            ServerRegistrationPayload payload = new()
            {
                hostname = config.sv_hostname,
                ip = config.sv_ip,
                port = config.sv_port,
                mapName = config.g_mapname,
                gametype = config.g_gametype,
                currentPlayers = currentPlayers,
                maxPlayers = config.sv_maxclients,
                hasPassword = !string.IsNullOrEmpty(config.sv_password),
                version = Application.version
            };

            string json = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = new($"{m_MasterServerUrl}/api/registerServer", "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ServerRegistrationResponse response = JsonUtility.FromJson<ServerRegistrationResponse>(request.downloadHandler.text);
                    m_RegisteredServerId = response.serverId;
                    Debug.Log($"[MasterServerService] Server registriert: id={m_RegisteredServerId}, message={response.message}");

                    StartHeartbeat();
                }
                else
                {
                    Debug.LogWarning($"[MasterServerService] Registrierung fehlgeschlagen: {request.responseCode} - {request.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MasterServerService] Registrierung Exception: {e.Message}");
            }
        }

        /// <summary>
        /// Aktualisiert den Spielerstand beim Master-Server (Heartbeat-Daten).
        /// </summary>
        /// <param name="currentPlayers">Aktuelle Spieleranzahl.</param>
        /// <param name="currentMapName">Aktuelle Map.</param>
        public void UpdateServerInfo(int currentPlayers, string currentMapName)
        {
            m_CurrentPlayers = currentPlayers;
            m_CurrentMapName = currentMapName;
        }

        /// <summary>
        /// Deregistriert diesen Server beim Master-Server und stoppt den Heartbeat.
        /// </summary>
        public async Task DeregisterServerAsync()
        {
            StopHeartbeat();

            if (string.IsNullOrEmpty(m_RegisteredServerId))
            {
                return;
            }

            try
            {
                using UnityWebRequest request = new($"{m_MasterServerUrl}/api/removeServer/{m_RegisteredServerId}", "DELETE");
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[MasterServerService] Server deregistriert: {m_RegisteredServerId}");
                }
                else
                {
                    Debug.LogWarning($"[MasterServerService] Deregistrierung fehlgeschlagen: {request.responseCode} - {request.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MasterServerService] Deregistrierung Exception: {e.Message}");
            }

            m_RegisteredServerId = null;
        }

        /// <summary>
        /// Startet den Heartbeat-Loop der regelmaessig den Master-Server ueber den Serverstatus informiert.
        /// </summary>
        void StartHeartbeat()
        {
            StopHeartbeat();
            m_HeartbeatCts = new CancellationTokenSource();
            _ = HeartbeatLoopAsync(m_HeartbeatCts.Token);
        }

        /// <summary>
        /// Stoppt den Heartbeat-Loop.
        /// </summary>
        void StopHeartbeat()
        {
            if (m_HeartbeatCts != null)
            {
                m_HeartbeatCts.Cancel();
                m_HeartbeatCts.Dispose();
                m_HeartbeatCts = null;
            }
        }

        /// <summary>
        /// Heartbeat-Loop: Sendet regelmaessig den aktuellen Serverstatus an den Master-Server.
        /// </summary>
        async Task HeartbeatLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(k_HeartbeatIntervalMs, ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (ct.IsCancellationRequested || string.IsNullOrEmpty(m_RegisteredServerId))
                {
                    break;
                }

                await SendHeartbeatAsync();
            }
        }

        /// <summary>
        /// Sendet einen einzelnen Heartbeat an den Master-Server.
        /// </summary>
        async Task SendHeartbeatAsync()
        {
            ServerRegistrationPayload payload = new()
            {
                hostname = m_ServerConfig.sv_hostname,
                ip = m_ServerConfig.sv_ip,
                port = m_ServerConfig.sv_port,
                mapName = m_CurrentMapName,
                gametype = m_ServerConfig.g_gametype,
                currentPlayers = m_CurrentPlayers,
                maxPlayers = m_ServerConfig.sv_maxclients,
                hasPassword = !string.IsNullOrEmpty(m_ServerConfig.sv_password),
                version = Application.version
            };

            string json = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            try
            {
                using UnityWebRequest request = new($"{m_MasterServerUrl}/api/heartbeat/{m_RegisteredServerId}", "PUT");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[MasterServerService] Heartbeat fehlgeschlagen: {request.responseCode} - {request.error}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MasterServerService] Heartbeat Exception: {e.Message}");
            }
        }

        // Client-Seite
        /// <summary>
        /// Ruft die aktuelle Server-Liste vom Master-Server ab.
        /// </summary>
        /// <returns>Array von ServerBrowserEntry oder leeres Array bei Fehler.</returns>
        public async Task<ServerBrowserEntry[]> FetchServerListAsync()
        {
            try
            {
                using UnityWebRequest request = UnityWebRequest.Get($"{m_MasterServerUrl}/api/servers");
                request.SetRequestHeader("Accept", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    Debug.Log($"[MasterServerService] FetchServerList raw response: {responseText}");

                    // Master-Server liefert entweder { "servers": [...] } oder direkt [...]
                    // Versuch Wrapper zuerst, dann direktes Array
                    if (responseText.TrimStart().StartsWith("{"))
                    {
                        ServerBrowserEntryList list = JsonUtility.FromJson<ServerBrowserEntryList>(responseText);
                        return list.servers ?? Array.Empty<ServerBrowserEntry>();
                    }
                    else
                    {
                        // Top-Level Array: JsonUtility Wrapper-Trick
                        string wrapped = "{\"servers\":" + responseText + "}";
                        ServerBrowserEntryList list = JsonUtility.FromJson<ServerBrowserEntryList>(wrapped);
                        return list.servers ?? Array.Empty<ServerBrowserEntry>();
                    }
                }
                else
                {
                    Debug.LogWarning($"[MasterServerService] Server-Liste abrufen fehlgeschlagen: {request.responseCode} - {request.error}");
                    return Array.Empty<ServerBrowserEntry>();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MasterServerService] FetchServerList Exception: {e.Message}");
                return Array.Empty<ServerBrowserEntry>();
            }
        }

        /// <summary>
        /// Raeumt Ressourcen auf: Stoppt Heartbeat und deregistriert den Server.
        /// Wird bei ServiceLocator.ClearAll() aufgerufen.
        /// </summary>
        public void ClearCache()
        {
            StopHeartbeat();
        }

        // â”€â”€ Authentication (Client-Seite) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Sendet Login-Request an den Master-Server.
        /// </summary>
        /// <param name="username">Benutzername.</param>
        /// <param name="password">Passwort.</param>
        /// <returns>Ergebnis mit AuthenticationResponse oder Fehler-Status.</returns>
        public async Task<MasterServerAuthResult> LoginUserAsync(string username, string password)
        {
            AuthenticationPayload payload = new()
            {
                username = username,
                password = password
            };

            string json = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            try
            {
                using UnityWebRequest request = new($"{m_MasterServerUrl}/api/loginUser", "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    AuthenticationResponse response = JsonUtility.FromJson<AuthenticationResponse>(request.downloadHandler.text);
                    Debug.Log($"[MasterServerService] Login erfolgreich: {response.username}");
                    return MasterServerAuthResult.CreateSuccess(response);
                }
                else
                {
                    long code = request.responseCode;
                    string error = request.error;
                    Debug.LogWarning($"[MasterServerService] Login fehlgeschlagen: {code} - {error}");
                    return MasterServerAuthResult.CreateFailure(code, error);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MasterServerService] Login Exception: {e.Message}");
                return MasterServerAuthResult.CreateFailure(0, e.Message);
            }
        }

        /// <summary>
        /// Sendet Register-Request an den Master-Server.
        /// </summary>
        /// <param name="username">Benutzername.</param>
        /// <param name="email">E-Mail-Adresse.</param>
        /// <param name="password">Passwort.</param>
        /// <returns>Ergebnis mit Nachricht oder Fehler.</returns>
        public async Task<MasterServerRegisterResult> RegisterUserAsync(string username, string email, string password)
        {
            AuthenticationPayload payload = new()
            {
                username = username,
                password = password,
                email = email
            };

            string json = JsonUtility.ToJson(payload);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            try
            {
                using UnityWebRequest request = new($"{m_MasterServerUrl}/api/registerUser", "POST");
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    MasterServerRegisterResult.RegisterResponse response =
                        JsonUtility.FromJson<MasterServerRegisterResult.RegisterResponse>(request.downloadHandler.text);
                    Debug.Log($"[MasterServerService] Registrierung erfolgreich: {response.message}");
                    return MasterServerRegisterResult.CreateSuccess(response.message);
                }
                else
                {
                    string error = request.error;
                    Debug.LogWarning($"[MasterServerService] Registrierung fehlgeschlagen: {request.responseCode} - {error}");
                    return MasterServerRegisterResult.CreateFailure(error);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MasterServerService] Register Exception: {e.Message}");
                return MasterServerRegisterResult.CreateFailure(e.Message);
            }
        }
    }

    /// <summary>
    /// Ergebnis eines Login-Requests an den Master-Server.
    /// </summary>
    public class MasterServerAuthResult
    {
        /// <summary>Ob der Login erfolgreich war.</summary>
        public bool IsSuccess { get; private set; }

        /// <summary>Die AuthenticationResponse bei Erfolg (null bei Fehler).</summary>
        public AuthenticationResponse Response { get; private set; }

        /// <summary>HTTP-Statuscode bei Fehler (0 bei Netzwerkfehler).</summary>
        public long ResponseCode { get; private set; }

        /// <summary>Fehlermeldung bei Fehler.</summary>
        public string ErrorMessage { get; private set; }

        /// <summary>Erstellt ein erfolgreiches Ergebnis.</summary>
        public static MasterServerAuthResult CreateSuccess(AuthenticationResponse response)
        {
            return new MasterServerAuthResult { IsSuccess = true, Response = response };
        }

        /// <summary>Erstellt ein fehlgeschlagenes Ergebnis.</summary>
        public static MasterServerAuthResult CreateFailure(long responseCode, string error)
        {
            return new MasterServerAuthResult { IsSuccess = false, ResponseCode = responseCode, ErrorMessage = error };
        }
    }

    /// <summary>
    /// Ergebnis eines Register-Requests an den Master-Server.
    /// </summary>
    public class MasterServerRegisterResult
    {
        /// <summary>Ob die Registrierung erfolgreich war.</summary>
        public bool IsSuccess { get; private set; }

        /// <summary>Nachricht vom Server.</summary>
        public string Message { get; private set; }

        /// <summary>Fehlermeldung bei Fehler.</summary>
        public string ErrorMessage { get; private set; }

        /// <summary>Erstellt ein erfolgreiches Ergebnis.</summary>
        public static MasterServerRegisterResult CreateSuccess(string message)
        {
            return new MasterServerRegisterResult { IsSuccess = true, Message = message };
        }

        /// <summary>Erstellt ein fehlgeschlagenes Ergebnis.</summary>
        public static MasterServerRegisterResult CreateFailure(string error)
        {
            return new MasterServerRegisterResult { IsSuccess = false, ErrorMessage = error };
        }

        /// <summary>Antwort-DTO fuer die Registrierung.</summary>
        [Serializable]
        public class RegisterResponse
        {
            public bool success;
            public string message;
            public string userId;
        }
    }
}
