using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Controller fuer den Server-Browser. Fetcht Server-Liste paginiert vom Master-Server,
    /// leitet Auswahl und Verbindung an View und ConnectionManager weiter.
    /// Verwendet serverseitiges Infinite Scroll (offset/limit).
    /// </summary>
    internal class JoinServerController : Controller<MetagameApplication>
    {
        /// <summary>Anzahl Server-Eintraege pro Lade-Batch (serverseitig).</summary>
        const int k_PageSize = 10;

        JoinServerView View => App.View.JoinServerView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        PlayerSkinManager PlayerSkinManager => ApplicationEntryPoint.Singleton.PlayerSkinManager;

        /// <summary>Ob gerade eine Server-Seite geladen wird (verhindert Doppel-Requests).</summary>
        bool m_IsLoading;

        /// <summary>Aktueller Offset fuer die naechste Seite.</summary>
        int m_CurrentOffset;

        /// <summary>Gesamtanzahl aller Server (vom Server gemeldet).</summary>
        int m_TotalCount;

        /// <summary>Ob der Server weitere Seiten hat.</summary>
        bool m_HasMore;

        void Awake()
        {
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AddListener<RefreshServerListEvent>(OnRefreshServerList);
            AddListener<ConnectToServerEvent>(OnConnectToServer);
            AddListener<LoadMoreServersEvent>(OnLoadMoreServers);
            //Debug.Log("[JoinServerController] Awake - initialized and listeners added");
        }

        void OnDestroy()
        {
            RemoveListeners();
            //Debug.Log("[JoinServerController] OnDestroy - destroyed and listeners removed");
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
                PlayerSkinManager.CurrentSkinName,
                evt.serverName
            );
        }

        /// <summary>
        /// Laedt die erste Seite der Server-Liste vom Master-Server.
        /// </summary>
        async void FetchServerList()
        {
            if (m_IsLoading)
            {
                return;
            }

            m_IsLoading = true;
            m_CurrentOffset = 0;
            m_TotalCount = 0;
            m_HasMore = false;
            View.ShowLoading();

            try
            {
                MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
                if (masterService == null)
                {
                    View.ShowError("Master server service not available.");
                    m_IsLoading = false;
                    return;
                }

                ServerBrowserPageResponse page = await masterService.FetchServerPageAsync(0, k_PageSize);
                m_TotalCount = page.total;
                m_CurrentOffset = page.servers.Length;
                m_HasMore = page.hasMore;

                View.BeginServerList(m_TotalCount);
                View.AppendServerBatch(page.servers, m_CurrentOffset, m_TotalCount, m_HasMore);
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
        /// Laedt die naechste Seite vom Master-Server (Infinite Scroll).
        /// </summary>
        async void LoadNextBatch()
        {
            if (m_IsLoading || !m_HasMore)
            {
                return;
            }

            m_IsLoading = true;

            try
            {
                MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
                if (masterService == null)
                {
                    m_IsLoading = false;
                    return;
                }

                ServerBrowserPageResponse page = await masterService.FetchServerPageAsync(m_CurrentOffset, k_PageSize);
                m_TotalCount = page.total;
                m_CurrentOffset += page.servers.Length;
                m_HasMore = page.hasMore;

                View.AppendServerBatch(page.servers, m_CurrentOffset, m_TotalCount, m_HasMore);
            }
            catch (Exception e)
            {
                Debug.LogError($"[JoinServerController] LoadNextBatch failed: {e.Message}");
            }
            finally
            {
                m_IsLoading = false;
            }
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            Debug.Log($"[JoinServerController] OnConnectionEvent - status: {evt.status}");
        }
    }
}