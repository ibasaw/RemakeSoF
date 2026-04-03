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
    /// </summary>
    internal class JoinServerController : Controller<MetagameApplication>
    {
        JoinServerView View => App.View.JoinServerView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        PlayerSkinManager PlayerSkinManager => ApplicationEntryPoint.Singleton.PlayerSkinManager;

        /// <summary>Ob gerade eine Server-Liste geladen wird (verhindert Doppel-Requests).</summary>
        bool m_IsLoading;

        void Awake()
        {
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AddListener<RefreshServerListEvent>(OnRefreshServerList);
            AddListener<ConnectToServerEvent>(OnConnectToServer);
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
        /// Laedt die Server-Liste vom Master-Server und aktualisiert die View.
        /// </summary>
        async void FetchServerList()
        {
            if (m_IsLoading)
            {
                return;
            }

            m_IsLoading = true;
            View.ShowLoading();

            MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
            if (masterService == null)
            {
                View.ShowError("Master server service not available.");
                m_IsLoading = false;
                return;
            }

            try
            {
                ServerBrowserEntry[] servers = await masterService.FetchServerListAsync();
                View.PopulateServerList(servers);
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

        void OnConnectionEvent(ConnectionEvent evt)
        {
            Debug.Log($"[JoinServerController] OnConnectionEvent - status: {evt.status}");
        }
    }
}