using System;
using Unity.Collections;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.Management.MapManagement;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Holds the logical state of a game and synchronizes it across the network
    /// </summary>
    public class NetworkedGameState : NetworkBehaviour
    {
        internal NetworkVariable<uint> matchCountdown = new();
        internal NetworkVariable<int> playersConnected = new();

        /// <summary>
        /// Der aktuell geladene Map-Name, synchronisiert über das Netzwerk.
        /// Nur der Server darf diesen Wert ändern.
        /// </summary>
        internal NetworkVariable<FixedString128Bytes> currentMapName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        bool m_MatchStarted;
        bool m_MatchEnded;

        internal event Action OnMatchStarted;
        internal event Action OnMatchEnded;

        const uint k_CountdownStartValue = 300;
        const float k_ShutdownDelayAfterCountdownEnd = 30;

        const string k_DefaultMapName = "maps/cem1"; //TODO: Platzhalter, bis Map-Auswahl implementiert ist

        Coroutine m_CountdownRoutine;

        /// <summary>
        /// Interner MapLoader für clientseitiges Laden der Map.
        /// </summary>
        MapLoader m_ClientMapLoader;

        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                m_MatchEnded = false;
                ConnectionManager.EventManager.AddListener<MinNumberPlayersConnectedEvent>(OnServerMinNumberPlayersConnected);
                ConnectionManager.EventManager.AddListener<ClientConnectedEvent>(OnServerClientConnected);
                ConnectionManager.EventManager.AddListener<ClientDisconnectedEvent>(OnServerClientDisconnected);
                playersConnected.Value = NetworkManager.ConnectedClientsIds.Count;
                currentMapName.Value = new FixedString128Bytes(k_DefaultMapName);
                Debug.Log($"[NetworkedGameState] CurrentMapName set to: {k_DefaultMapName}");
            }
            else
            {
                // Client: Map laden, falls Server bereits einen Map-Namen gesetzt hat
                m_ClientMapLoader = new MapLoader();
                string mapName = currentMapName.Value.ToString();
                if (!string.IsNullOrEmpty(mapName))
                {
                    Debug.Log($"[NetworkedGameState] Client loading initial map: {mapName}");
                    _ = m_ClientMapLoader.LoadMapAsync(mapName);
                }

                // Auf zukünftige Map-Wechsel reagieren
                currentMapName.OnValueChanged += OnClientMapNameChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                if (m_CountdownRoutine != null)
                {
                    StopCoroutine(m_CountdownRoutine);
                    m_CountdownRoutine = null;
                }
                ConnectionManager.EventManager.RemoveListener<MinNumberPlayersConnectedEvent>(OnServerMinNumberPlayersConnected);
                ConnectionManager.EventManager.RemoveListener<ClientConnectedEvent>(OnServerClientConnected);
                ConnectionManager.EventManager.RemoveListener<ClientDisconnectedEvent>(OnServerClientDisconnected);
            }
            else
            {
                currentMapName.OnValueChanged -= OnClientMapNameChanged;
            }
        }

        /// <summary>
        /// Client-Callback wenn der Server den Map-Namen ändert.
        /// Lädt die neue Map lokal auf dem Client.
        /// </summary>
        void OnClientMapNameChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
        {
            string mapName = newValue.ToString();
            if (string.IsNullOrEmpty(mapName))
            {
                return;
            }

            Debug.Log($"[NetworkedGameState] Client map changed: {previousValue} -> {mapName}");
            _ = m_ClientMapLoader.LoadMapAsync(mapName);
        }

        /// <summary>
        /// Setzt den aktuellen Map-Namen. Nur auf dem Server aufrufbar.
        /// </summary>
        /// <param name="mapName">Der Name der Map.</param>
        public void SetCurrentMapName(string mapName)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedGameState] SetCurrentMapName can only be called on the server.");
                return;
            }

            currentMapName.Value = new FixedString128Bytes(mapName);
            Debug.Log($"[NetworkedGameState] CurrentMapName set to: {mapName}");
        }

        /// <summary>
        /// Registriert einen Callback für Map-Änderungen.
        /// </summary>
        /// <param name="callback">Der Callback bei Änderung.</param>
        public void RegisterOnMapChanged(NetworkVariable<FixedString128Bytes>.OnValueChangedDelegate callback)
        {
            currentMapName.OnValueChanged += callback;
        }

        /// <summary>
        /// Deregistriert einen Callback für Map-Änderungen.
        /// </summary>
        /// <param name="callback">Der Callback.</param>
        public void UnregisterOnMapChanged(NetworkVariable<FixedString128Bytes>.OnValueChangedDelegate callback)
        {
            currentMapName.OnValueChanged -= callback;
        }

        void OnServerMinNumberPlayersConnected(MinNumberPlayersConnectedEvent evt)
        {
            if (m_MatchStarted)
            {
                throw new Exception("[Server] Match has already started and received an unexpected MinNumberPlayersConnectedEvent");
            }
            Debug.Log("[Server] Starting match!");
            m_MatchStarted = true;
            OnServerStartCountdown();
            ClientStartMatchRpc();
            OnMatchStarted?.Invoke();
        }

        void OnServerClientConnected(ClientConnectedEvent evt)
        {
            playersConnected.Value = NetworkManager.ConnectedClientsIds.Count;
        }

        void OnServerClientDisconnected(ClientDisconnectedEvent evt)
        {
            playersConnected.Value = NetworkManager.ConnectedClientsIds.Count;
        }

        void OnServerStartCountdown()
        {
            matchCountdown.Value = k_CountdownStartValue;
            m_CountdownRoutine = StartCoroutine(OnServerDoCountdown());
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientStartMatchRpc()
        {
            OnMatchStarted?.Invoke();
        }

        IEnumerator OnServerDoCountdown()
        {
            while (matchCountdown.Value > 0
                && !m_MatchEnded)
            {
                yield return CoroutinesHelper.OneSecond;
                matchCountdown.Value--;
                Debug.Log($"[Server] Countdown: {matchCountdown.Value} seconds remaining");
            }
            OnServerCountdownExpired();
        }

        void OnServerCountdownExpired()
        {
            m_MatchEnded = true;
            if (m_CountdownRoutine != null)
            {
                StopCoroutine(m_CountdownRoutine);
                m_CountdownRoutine = null;
            }

            ClientEndMatchRpc();
            //StartCoroutine(CoroutinesHelper.WaitAndDo(new WaitForSeconds(k_ShutdownDelayAfterCountdownEnd), () => ConnectionManager.RequestShutdown()));
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientEndMatchRpc()
        {
            OnMatchEnded?.Invoke();
        }
    }
}
