using System.Collections.Generic;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ChatManagement;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Tolik.RemakeSoF.Runtime.Game.Networked;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a listening server. Handles incoming client connections. When shutting down or
    /// being timed out, transitions to the Offline state.
    /// </summary>
    class ServerListeningState : OnlineState
    {
        // used in ApprovalCheck. This is intended as a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
        const int k_MaxConnectPayload = 1024;
        bool m_MinPlayerConnected = false;

        /// <summary>
        /// Speichert den ConnectionPayload pro ClientId, damit in OnClientConnected
        /// Name und Skin auf dem gespawnten NetworkedCharacterState gesetzt werden können.
        /// </summary>
        readonly Dictionary<ulong, ConnectionPayload> m_ClientPayloads = new();

        public override void Enter()
        {
            // todo setup gsh to receive matchmaker tickets
            m_MinPlayerConnected = false;
            m_ClientPayloads.Clear();
        }

        public override void Exit() { }
        
        public override void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected to the server.");
            Manager.EventManager.Broadcast(new ClientConnectedEvent());

            // Spieler-Identitätsdaten aus gecachtem Payload auf NetworkedCharacterState setzen
            if (m_ClientPayloads.TryGetValue(clientId, out ConnectionPayload payload))
            {
                NetworkObject playerObject = Manager.NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
                if (playerObject != null && playerObject.TryGetComponent(out NetworkedCharacterState characterState))
                {
                    characterState.SetCharacterName(payload.playerName);
                    characterState.SetCurrentSkinName(payload.skinName);

                    // Team-Zuweisung via GametypeManager
                    GametypeManager gametypeManager = ServiceLocator.Get<GametypeManager>();
                    if (gametypeManager != null)
                    {
                        (int redCount, int blueCount) = CountTeams();
                        GametypeTeam assignedTeam = gametypeManager.AssignTeam(redCount, blueCount);
                        characterState.SetTeam((uint)assignedTeam);

                        // Gametype-spezifische Waffen
                        string[] weapons = gametypeManager.GetStartWeapons(assignedTeam);
                        if (weapons != null)
                        {
                            foreach (string weapon in weapons)
                            {
                                // Ammo-Override VOR AddWeapon/SetCurrentWeaponName im Cache hinterlegen
                                (int clip, int reserve, int altClip, int altReserve)? ammoOverride = gametypeManager.GetStartAmmo(weapon);
                                if (ammoOverride.HasValue)
                                {
                                    characterState.PreloadWeaponAmmo(weapon, ammoOverride.Value.clip, ammoOverride.Value.reserve, ammoOverride.Value.altClip, ammoOverride.Value.altReserve);
                                }

                                characterState.AddWeapon(weapon);
                            }
                        }
                        else
                        {
                            AssignDefaultWeapons(characterState);
                        }

                        characterState.SetCurrentWeaponName("knife");
                        Debug.Log($"[ServerListeningState] Client {clientId}: Name='{payload.playerName}', Skin='{payload.skinName}', Team={assignedTeam}, Weapons={weapons?.Length ?? 24}");
                    }
                    else
                    {
                        AssignDefaultWeapons(characterState);
                        characterState.SetCurrentWeaponName("knife");
                        Debug.Log($"[ServerListeningState] Client {clientId}: Name='{payload.playerName}', Skin='{payload.skinName}', Weapon='knife' (no GametypeManager)");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ServerListeningState] No NetworkedCharacterState found for client {clientId}");
                }
            }

            if (!m_MinPlayerConnected && Manager.NetworkManager.ConnectedClientsIds.Count >= NetworkedGameState.Singleton.MinPlayers)
            {
                m_MinPlayerConnected = true;
                Manager.EventManager.Broadcast(new MinNumberPlayersConnectedEvent());
            }

            // MOTD an den neuen Client senden
            NetworkedChatBridge chatBridge = NetworkedChatBridge.Instance;
            if (chatBridge != null)
            {
                chatBridge.SendMotdToClient(clientId);
            }

            UpdateMasterServerPlayerCount();
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected from the server.");
            m_ClientPayloads.Remove(clientId);
            Manager.EventManager.Broadcast(new ClientDisconnectedEvent());
            UpdateMasterServerPlayerCount();
            if (Manager.NetworkManager.ConnectedClientsIds.Count == 1 && Manager.NetworkManager.ConnectedClients.ContainsKey(clientId))
            {
                // This callback is invoked by the last client disconnecting from the server
                // Here the networked session is shut down immediately, but if we wanted to allow reconnection, we could
                // include a delay in a coroutine that could get cancelled when a client reconnects
                Debug.Log("All clients have disconnected from the server. Shutting down");
                Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.ServerEndedSession });
                Manager.ChangeState(Manager.m_Offline);
            }
        }

        public override void OnUserRequestedShutdown()
        {
            var reason = JsonUtility.ToJson(ConnectStatus.ServerEndedSession);
            for (var i = 0; i < Manager.NetworkManager.ConnectedClientsIds.Count; i++)
            {
                var id = Manager.NetworkManager.ConnectedClientsIds[i];

                Manager.NetworkManager.DisconnectClient(id, reason);
            }
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.ServerEndedSession });
            Manager.ChangeState(Manager.m_Offline);
        }

        public override void OnServerStopped()
        {
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.GenericDisconnect });
            Manager.ChangeState(Manager.m_Offline);
        }

        /// <summary>
        /// This logic plugs into the "ConnectionApprovalResponse" exposed by Netcode.NetworkManager. It is run every time a client connects to us.
        /// The complementary logic that runs when the client starts its connection can be found in ClientConnectingState.
        /// </summary>
        /// <remarks>
        /// Multiple things can be done here, some asynchronously. For example, it could authenticate your user against an auth service like UGS' auth service. It can
        /// also send custom messages to connecting users before they receive their connection result (this is useful to set status messages client side
        /// when connection is refused, for example).
        /// </remarks>
        /// <param name="request"> The initial request contains, among other things, binary data passed into StartClient. In our case, this is the client's GUID,
        /// which is a unique identifier for their install of the game that persists across app restarts.
        ///  <param name="response"> Our response to the approval process. In case of connection refusal with custom return message, we delay using the Pending field.
        public override void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var connectionData = request.Payload;
            if (connectionData.Length > k_MaxConnectPayload)
            {
                // If connectionData too high, deny immediately to avoid wasting time on the server. This is intended as
                // a bit of light protection against DOS attacks that rely on sending silly big buffers of garbage.
                response.Approved = false;
                return;
            }

            var payload = System.Text.Encoding.UTF8.GetString(connectionData);
            var connectionPayload = JsonUtility.FromJson<ConnectionPayload>(payload); // https://docs.unity3d.com/2020.2/Documentation/Manual/JSONSerialization.html
            var gameReturnStatus = GetConnectStatus(connectionPayload);

            if (gameReturnStatus == ConnectStatus.Success)
            {
                // Payload für OnClientConnected cachen, damit Name + Skin gesetzt werden können
                m_ClientPayloads[request.ClientNetworkId] = connectionPayload;

                // connection approval will create a player object for you
                response.Approved = true;
                response.CreatePlayerObject = true;
                response.Position = Vector3.zero;
                response.Rotation = Quaternion.identity;
                return;
            }

            response.Approved = false;
            response.Reason = JsonUtility.ToJson(gameReturnStatus);
        }
        
        ConnectStatus GetConnectStatus(ConnectionPayload connectionPayload)
        {
            if (Manager.NetworkManager.ConnectedClientsIds.Count >= NetworkedGameState.Singleton.MaxPlayers)
            {
                return ConnectStatus.ServerFull;
            }

            if (connectionPayload.applicationVersion != Application.version)
            {
                return ConnectStatus.IncompatibleVersions;
            }

            return ConnectStatus.Success;
            //todo add support to deny connection if map or game version is different
        }

        /// <summary>
        /// Aktualisiert den Spielerstand beim Master-Server nach Connect/Disconnect.
        /// </summary>
        void UpdateMasterServerPlayerCount()
        {
            MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
            if (masterService == null || !masterService.IsRegistered)
            {
                return;
            }

            int playerCount = Manager.NetworkManager.ConnectedClientsIds.Count;
            string currentMap = NetworkedGameState.Singleton != null
                ? NetworkedGameState.Singleton.currentMapName.Value.ToString()
                : "";
            masterService.UpdateServerInfo(playerCount, currentMap);
        }

        /// <summary>
        /// Zaehlt die aktuelle Teamverteilung aller verbundenen Spieler und AI-Bots.
        /// </summary>
        (int redCount, int blueCount) CountTeams()
        {
            int red = 0;
            int blue = 0;

            // Menschliche Spieler
            foreach (ulong cid in Manager.NetworkManager.ConnectedClientsIds)
            {
                NetworkObject obj = Manager.NetworkManager.SpawnManager.GetPlayerNetworkObject(cid);
                if (obj == null || !obj.TryGetComponent(out NetworkedCharacterState state))
                {
                    continue;
                }

                GametypeTeam team = (GametypeTeam)state.TeamId;
                if (team == GametypeTeam.Red)
                {
                    red++;
                }
                else if (team == GametypeTeam.Blue)
                {
                    blue++;
                }
            }

            // AI-Bots zaehlen
            AIBotSpawner botSpawner = NetworkedGameState.Singleton?.AIBotSpawner;
            if (botSpawner != null)
            {
                foreach (NetworkObject bot in botSpawner.SpawnedBots)
                {
                    if (bot == null || !bot.IsSpawned || !bot.TryGetComponent(out NetworkedCharacterState botState))
                    {
                        continue;
                    }

                    GametypeTeam botTeam = (GametypeTeam)botState.TeamId;
                    if (botTeam == GametypeTeam.Red)
                    {
                        red++;
                    }
                    else if (botTeam == GametypeTeam.Blue)
                    {
                        blue++;
                    }
                }
            }

            return (red, blue);
        }

        /// <summary>
        /// Weist das Standard-Waffenset (alle Waffen) zu. Fallback wenn kein Gametype aktiv.
        /// </summary>
        static void AssignDefaultWeapons(NetworkedCharacterState characterState)
        {
            characterState.AddWeapon("knife");
            characterState.AddWeapon("rpg7");
            characterState.AddWeapon("ak74");
            characterState.AddWeapon("mm1");
            characterState.AddWeapon("m4");
            characterState.AddWeapon("m590");
            characterState.AddWeapon("usas12");
            characterState.AddWeapon("msg90a1");
            characterState.AddWeapon("m60");
            characterState.AddWeapon("m1911a1");
            characterState.AddWeapon("f1");
            characterState.AddWeapon("ussocom");
            characterState.AddWeapon("m67");
            characterState.AddWeapon("microuzi");
            characterState.AddWeapon("oicw");
            characterState.AddWeapon("m3a1");
            characterState.AddWeapon("m84");
            characterState.AddWeapon("anm14");
            characterState.AddWeapon("l2a2");
            characterState.AddWeapon("m15");
            characterState.AddWeapon("mdn11");
            characterState.AddWeapon("smohg92");
            characterState.AddWeapon("mp5");
            characterState.AddWeapon("silver_talon");
        }
    }
}
