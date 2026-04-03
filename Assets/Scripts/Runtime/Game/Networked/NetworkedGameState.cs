using System;
using Unity.Collections;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Tolik.RemakeSoF.Runtime.Management.MapManagement;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.Game.Networked
{
    /// <summary>
    /// Holds the logical state of a game and synchronizes it across the network
    /// </summary>
    public class NetworkedGameState : NetworkBehaviour
    {
        /// <summary>Singleton-Instanz fuer globalen Zugriff auf Spielkonfiguration.</summary>
        public static NetworkedGameState Singleton { get; private set; }

        /// <summary>Minimale Spieleranzahl um ein Match zu starten (aus Server-Config).</summary>
        internal int MinPlayers { get; private set; }

        /// <summary>Maximale Spieleranzahl pro Match (aus Server-Config).</summary>
        internal int MaxPlayers { get; private set; }

        internal NetworkVariable<uint> matchCountdown = new();
        internal NetworkVariable<int> playersConnected = new();

        /// <summary>
        /// Countdown vor Rundenbeginn (3, 2, 1, 0), synchronisiert uebers Netzwerk.
        /// 0 = inaktiv oder "GO!". Clients zeigen den Wert als zentriertes Overlay.
        /// </summary>
        internal NetworkVariable<uint> roundStartCountdown = new();

        /// <summary>
        /// Countdown bis zum Map-Wechsel (in Sekunden), synchronisiert uebers Netzwerk.
        /// 0 = kein Map-Wechsel aktiv.
        /// </summary>
        internal NetworkVariable<uint> mapSwitchCountdown = new();

        /// <summary>
        /// Name der naechsten Map fuer den Map-Wechsel-Countdown, synchronisiert uebers Netzwerk.
        /// </summary>
        internal NetworkVariable<FixedString128Bytes> nextMapName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Der aktuell geladene Map-Name, synchronisiert über das Netzwerk.
        /// Nur der Server darf diesen Wert ändern.
        /// </summary>
        internal NetworkVariable<FixedString128Bytes> currentMapName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// Der aktive Gametype-Identifier, synchronisiert ueber das Netzwerk.
        /// Clients nutzen diesen Wert fuer UI und gametype-spezifisches Rendering.
        /// </summary>
        internal NetworkVariable<FixedString64Bytes> activeGametypeId = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        internal event Action OnMatchStarted;
        internal event Action OnMatchEnded;

        /// <summary>
        /// Wird gefeuert wenn der Round-Start-Countdown beginnt (3, 2, 1).
        /// Clients sollen Input deaktivieren und Countdown-UI anzeigen.
        /// </summary>
        internal event Action OnRoundStarting;

        /// <summary>
        /// Event das bei jeder Map-Ladephase gefeuert wird (für UI-Fortschrittsanzeige).
        /// </summary>
        internal event Action<MapLoadPhase> OnMapLoadProgress;

        /// <summary>
        /// Event das vor einem Map-Wechsel gefeuert wird (fuer Loading-Screen mit LevelShot).
        /// Uebergibt die MapDefinition der neuen Map.
        /// </summary>
        internal event Action<MapDefinition> OnMapChangeStarting;

        /// <summary>Geladene Server-Konfiguration (nur Server, lazy resolved).</summary>
        ServerConfigurationLoader m_ServerConfigLoader;

        RoundFlowStateMachine m_RoundFlowStateMachine;

        /// <summary>
        /// Server-seitiger AI-Bot-Spawner. Wird wie die RoundFlowStateMachine am selben GameObject verwaltet.
        /// </summary>
        AIBotSpawner m_AIBotSpawner;

        /// <summary>
        /// Oeffentlicher Zugriff auf den AIBotSpawner fuer die RoundFlowStateMachine.
        /// </summary>
        internal AIBotSpawner AIBotSpawner => m_AIBotSpawner;

        /// <summary>
        /// MapLoader wird auf Server UND Client verwendet.
        /// Server: Braucht SpawnPoints, Kollision, Trigger-Zonen.
        /// Client: Braucht Visuals + Struktur.
        /// </summary>
        MapLoader m_MapLoader;

        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        /// <summary>
        /// Stellt sicher, dass eine RoundFlowStateMachine am selben GameObject vorhanden ist.
        /// </summary>
        void EnsureRoundFlowStateMachine()
        {
            if (m_RoundFlowStateMachine != null)
            {
                return;
            }

            m_RoundFlowStateMachine = GetComponent<RoundFlowStateMachine>();
            if (m_RoundFlowStateMachine == null)
            {
                m_RoundFlowStateMachine = gameObject.AddComponent<RoundFlowStateMachine>();
            }
        }

        /// <summary>
        /// Stellt sicher, dass ein AIBotSpawner am selben GameObject vorhanden ist (nur Server).
        /// </summary>
        void EnsureAIBotSpawner()
        {
            if (m_AIBotSpawner != null)
            {
                return;
            }

            m_AIBotSpawner = GetComponent<AIBotSpawner>();
            if (m_AIBotSpawner == null)
            {
                m_AIBotSpawner = gameObject.AddComponent<AIBotSpawner>();
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Singleton = this;
            EnsureRoundFlowStateMachine();

            m_MapLoader = new MapLoader();
            m_MapLoader.OnProgress += OnMapLoaderProgress;

            if (IsServer)
            {
                ConnectionManager.EventManager.AddListener<MinNumberPlayersConnectedEvent>(OnServerMinNumberPlayersConnected);
                ConnectionManager.EventManager.AddListener<ClientConnectedEvent>(OnServerClientConnected);
                ConnectionManager.EventManager.AddListener<ClientDisconnectedEvent>(OnServerClientDisconnected);
                playersConnected.Value = NetworkManager.ConnectedClientsIds.Count;
                EnsureAIBotSpawner();

                // Server-Konfiguration auflösen: Map, Spieleranzahl, Gametype
                m_ServerConfigLoader = ServiceLocator.Get<ServerConfigurationLoader>();
                if (m_ServerConfigLoader == null)
                {
                    Debug.LogError("[NetworkedGameState] ServerConfigurationLoader nicht im ServiceLocator registriert!");
                    return;
                }

                ServerConfiguration config = m_ServerConfigLoader.Configuration;
                string startMap = config.g_mapname;
                MinPlayers = config.sv_minclients;
                MaxPlayers = config.sv_maxclients;
                if (!string.IsNullOrEmpty(config.g_gametype))
                {
                    activeGametypeId.Value = new FixedString64Bytes(config.g_gametype);
                }

                m_RoundFlowStateMachine.Initialize(this);
                currentMapName.Value = new FixedString128Bytes(startMap);

                // Server lädt Map für SpawnPoints, Kollision, etc.
                Debug.Log($"[NetworkedGameState] Server loading map: {startMap} (gametype={activeGametypeId.Value})");
                _ = m_MapLoader.LoadMapAsync(startMap);
            }

            if (!IsServer)
            {
                // Client: Map laden, falls Server bereits einen Map-Namen gesetzt hat
                string mapName = currentMapName.Value.ToString();
                if (!string.IsNullOrEmpty(mapName))
                {
                    // OnValueChanged feuert nicht fuer den initialen Sync,
                    // daher OnMapChangeStarting manuell ausloesen
                    FireMapChangeStarting(mapName);

                    Debug.Log($"[NetworkedGameState] Client loading initial map: {mapName}");
                    _ = m_MapLoader.LoadMapAsync(mapName);
                }
            }

            // Beide: Auf zukünftige Map-Wechsel reagieren (Client für Visuals, Server für neue SpawnPoints)
            currentMapName.OnValueChanged += OnMapNameChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (Singleton == this)
            {
                Singleton = null;
            }

            currentMapName.OnValueChanged -= OnMapNameChanged;
            if (m_MapLoader != null)
            {
                m_MapLoader.OnProgress -= OnMapLoaderProgress;
            }

            if (IsServer)
            {
                ConnectionManager.EventManager.RemoveListener<MinNumberPlayersConnectedEvent>(OnServerMinNumberPlayersConnected);
                ConnectionManager.EventManager.RemoveListener<ClientConnectedEvent>(OnServerClientConnected);
                ConnectionManager.EventManager.RemoveListener<ClientDisconnectedEvent>(OnServerClientDisconnected);
            }
        }

        /// <summary>
        /// Leitet MapLoader-Phasen an die UI weiter und delegiert serverseitig an die StateMachine.
        /// </summary>
        void OnMapLoaderProgress(MapLoadPhase phase)
        {
            OnMapLoadProgress?.Invoke(phase);

            if (IsServer)
            {
                m_RoundFlowStateMachine.OnMapLoadPhase(phase);
            }
        }

        /// <summary>
        /// Callback wenn der Map-Name sich ändert.
        /// Server: Lädt neue Map für SpawnPoints/Kollision.
        /// Client: Lädt neue Map für Visuals.
        /// </summary>
        void OnMapNameChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
        {
            string mapName = newValue.ToString();
            if (string.IsNullOrEmpty(mapName))
            {
                return;
            }

            Debug.Log($"[NetworkedGameState] Map changed: {previousValue} -> {mapName} (IsServer={IsServer})");

            FireMapChangeStarting(mapName);
            _ = m_MapLoader.LoadMapAsync(mapName);
        }

        /// <summary>
        /// Wird vom Client aufgerufen sobald der Loading-Screen weg ist
        /// und der Spieler das Gameplay sieht.
        /// </summary>
        public void NotifyGameplayVisibleOnClient()
        {
            if (!IsClient || IsServer || !IsSpawned)
            {
                return;
            }

            NotifyGameplayVisibleServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void NotifyGameplayVisibleServerRpc(RpcParams rpcParams = default)
        {
            if (!IsServer)
            {
                return;
            }

            ulong senderClientId = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.ContainsKey(senderClientId))
            {
                return;
            }

            m_RoundFlowStateMachine.OnClientReadyForRound(senderClientId);
        }

        /// <summary>
        /// Feuert das OnMapChangeStarting-Event mit der MapDefinition der angegebenen Map.
        /// </summary>
        void FireMapChangeStarting(string mapName)
        {
            MapDataLoader mapDataLoader = ServiceLocator.Get<MapDataLoader>();
            MapDefinition mapDef = mapDataLoader?.GetByMapId(mapName);
            if (mapDef != null)
            {
                OnMapChangeStarting?.Invoke(mapDef);
            }
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
            m_RoundFlowStateMachine.OnMinPlayersReached();
        }

        void OnServerClientConnected(ClientConnectedEvent evt)
        {
            m_RoundFlowStateMachine.OnClientConnected();
        }

        void OnServerClientDisconnected(ClientDisconnectedEvent evt)
        {
            m_RoundFlowStateMachine.OnClientDisconnected();
        }

        /// <summary>
        /// Broadcastet Match-Start ueber RPC an alle Clients und feuert das lokale Event.
        /// Wird von RunningState aufgerufen.
        /// </summary>
        internal void BroadcastMatchStarted()
        {
            ClientStartMatchRpc();
            OnMatchStarted?.Invoke();
        }

        /// <summary>
        /// Broadcastet den Round-Start-Countdown-Beginn an alle Clients.
        /// Wird von StartingRoundState aufgerufen.
        /// </summary>
        internal void BroadcastRoundStarting()
        {
            ClientRoundStartingRpc();
            OnRoundStarting?.Invoke();
        }

        /// <summary>
        /// Broadcastet Match-Ende ueber RPC an alle Clients.
        /// Wird von SwitchingMapState aufgerufen.
        /// </summary>
        internal void BroadcastMatchEnded()
        {
            ClientEndMatchRpc();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientRoundStartingRpc()
        {
            OnRoundStarting?.Invoke();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientStartMatchRpc()
        {
            OnMatchStarted?.Invoke();
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ClientEndMatchRpc()
        {
            OnMatchEnded?.Invoke();
        }
    }
}
