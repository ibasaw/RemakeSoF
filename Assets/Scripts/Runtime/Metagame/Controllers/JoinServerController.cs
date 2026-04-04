using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Controller fuer den Server-Browser. Fetcht Server-Liste vom Master-Server,
    /// leitet Auswahl und Verbindung an View und ConnectionManager weiter.
    /// Verwendet paginiertes Laden (10er-Pakete) fuer performante UI.
    /// </summary>
    internal class JoinServerController : Controller<MetagameApplication>
    {
        /// <summary>Anzahl Server-Eintraege pro Lade-Batch.</summary>
        const int k_PageSize = 10;

        JoinServerView View => App.View.JoinServerView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        PlayerSkinManager PlayerSkinManager => ApplicationEntryPoint.Singleton.PlayerSkinManager;

        /// <summary>Ob gerade eine Server-Liste geladen wird (verhindert Doppel-Requests).</summary>
        bool m_IsLoading;

        /// <summary>Alle geladenen Server-Eintraege (vollstaendige Liste).</summary>
        ServerBrowserEntry[] m_AllServers;

        /// <summary>Wie viele Server bereits an die View uebergeben wurden.</summary>
        int m_LoadedCount;

        void Awake()
        {
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AddListener<RefreshServerListEvent>(OnRefreshServerList);
            AddListener<ConnectToServerEvent>(OnConnectToServer);
            AddListener<LoadMoreServersEvent>(OnLoadMoreServers);
            Debug.Log("[JoinServerController] Awake - initialized and listeners added");
        }

        void OnDestroy()
        {
            RemoveListeners();
            Debug.Log("[JoinServerController] OnDestroy - destroyed and listeners removed");
        }

        internal override void RemoveListeners()
        {
            RemoveListener<RefreshServerListEvent>(OnRefreshServerList);
            RemoveListener<ConnectToServerEvent>(OnConnectToServer);
            RemoveListener<LoadMoreServersEvent>(OnLoadMoreServers);
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
        }

        /// <summary>
        /// Refresh-Button geklickt: Server-Liste neu laden.
        /// </summary>
        void OnRefreshServerList(RefreshServerListEvent evt)
        {
            FetchServerList();
        }

        /// <summary>
        /// View hat erkannt, dass der Nutzer ans Ende gescrollt hat.
        /// Naechstes Paket laden.
        /// </summary>
        void OnLoadMoreServers(LoadMoreServersEvent evt)
        {
            LoadNextBatch();
        }

        /// <summary>
        /// Server im Browser ausgewaehlt und Connect geklickt (oder Doppelklick).
        /// </summary>
        void OnConnectToServer(ConnectToServerEvent evt)
        {
            Debug.Log($"[JoinServerController] Connecting to server at {evt.ipAddress}:{evt.port}");
            ConnectionManager.StartClient(
                evt.ipAddress,
                evt.port,
                App.Model.PlayerData.PlayerName,
                PlayerSkinManager.CurrentSkinName
            );
        }

        /// <summary>
        /// Laedt die Server-Liste vom Master-Server und zeigt die erste Seite.
        /// </summary>
        async void FetchServerList()
        {
            if (m_IsLoading)
            {
                return;
            }

            m_IsLoading = true;
            View.ShowLoading();

            try
            {
#if MOCK_SERVER_BROWSER
                m_AllServers = GenerateMockServers(500);
                await System.Threading.Tasks.Task.Delay(200);
#else
                MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
                if (masterService == null)
                {
                    View.ShowError("Master server service not available.");
                    m_IsLoading = false;
                    return;
                }

                m_AllServers = await masterService.FetchServerListAsync();
#endif

                m_LoadedCount = 0;
                int totalCount = m_AllServers?.Length ?? 0;
                View.BeginServerList(totalCount);
                LoadNextBatch();
            }
            catch (Exception e)
            {
                Debug.LogError($"[JoinServerController] FetchServerList failed: {e.Message}");
                View.ShowError("Failed to fetch server list.");
            }
            finally
            {
                m_IsLoading = false;
            }
        }

        /// <summary>
        /// Uebergibt das naechste Paket (k_PageSize Eintraege) an die View.
        /// </summary>
        void LoadNextBatch()
        {
            if (m_AllServers == null || m_LoadedCount >= m_AllServers.Length)
            {
                return;
            }

            int remaining = m_AllServers.Length - m_LoadedCount;
            int batchSize = Math.Min(k_PageSize, remaining);
            ServerBrowserEntry[] batch = new ServerBrowserEntry[batchSize];
            Array.Copy(m_AllServers, m_LoadedCount, batch, 0, batchSize);
            m_LoadedCount += batchSize;

            bool hasMore = m_LoadedCount < m_AllServers.Length;
            View.AppendServerBatch(batch, m_LoadedCount, m_AllServers.Length, hasMore);
        }

#if MOCK_SERVER_BROWSER
        /// <summary>
        /// Generiert Mock-Server fuer Testing.
        /// </summary>
        ServerBrowserEntry[] GenerateMockServers(int count)
        {
            string[] mapNames = { "mp_col1", "mp_frostbite", "mp_compound", "mp_shop", "mp_kam3", "mp_raven", "mp_hk", "mp_pra2", "mp_jor3", "mp_col2" };
            string[] gameTypes = { "DM", "TDM", "CTF", "INF", "ELIM", "DEM" };
            string[] prefixes = { "[EU]", "[US]", "[RU]", "[DE]", "[UK]", "[FR]", "[PL]", "[CZ]", "[NL]", "[BR]" };
            string[] names = { "FragFest", "Warzone", "NightOps", "Elite", "Tactical", "Killbox", "Recon","LONG LONG LONG LONG LONG SKFIEWFWO FKOWEKFWEKFOWEF OKWEFO WKEFO", "Bravo", "Delta", "SoF2Classic", "OldSchool", "ProMode", "Casual", "Ranked", "Training" };

            ServerBrowserEntry[] servers = new ServerBrowserEntry[count];
            System.Random rng = new(42);

            for (int i = 0; i < count; i++)
            {
                int maxPl = rng.Next(2, 5) * 4;
                servers[i] = new ServerBrowserEntry
                {
                    id = $"mock-{i}",
                    hostname = $"{prefixes[rng.Next(prefixes.Length)]} {names[rng.Next(names.Length)]} #{i + 1}",
                    ip = $"192.168.{rng.Next(1, 255)}.{rng.Next(1, 255)}",
                    port = 7777 + rng.Next(0, 100),
                    mapName = mapNames[rng.Next(mapNames.Length)],
                    gametype = gameTypes[rng.Next(gameTypes.Length)],
                    currentPlayers = rng.Next(0, maxPl + 1),
                    maxPlayers = maxPl,
                    ping = rng.Next(5, 99999),
                    hasPassword = rng.Next(100) < 20,
                    version = "1.0.0"
                };
            }

            return servers;
        }
#endif

        void OnConnectionEvent(ConnectionEvent evt)
        {
            Debug.Log($"[JoinServerController] OnConnectionEvent - status: {evt.status}");
        }
    }
}