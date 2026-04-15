using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ChatManagement
{
    /// <summary>
    /// NetworkBehaviour bridge for chat messages. Allows clients to send chat messages
    /// that the server broadcasts to all connected clients.
    /// Must be placed on a networked GameObject (e.g. alongside NetworkedGameState).
    /// </summary>
    public class NetworkedChatBridge : NetworkBehaviour
    {
        /// <summary>
        /// Singleton instance for easy access from ChatController.
        /// </summary>
        public static NetworkedChatBridge Instance { get; private set; }

        /// <summary>
        /// Maximum allowed message length to prevent abuse.
        /// Matches original SoF2 limit of 128 bytes.
        /// </summary>
        const int k_MaxMessageLength = 128;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;

            string role = IsServer ? "SERVER" : "CLIENT";
            Debug.Log($"[NetworkedChatBridge] {role} spawned and ready.");
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public override void OnNetworkDespawn()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Called by the client (or host) to send a chat message.
        /// </summary>
        public void SendChatMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // Truncate to max length
            if (message.Length > k_MaxMessageLength)
            {
                message = message.Substring(0, k_MaxMessageLength);
            }

            if (IsServer)
            {
                // Already on server: resolve local name and broadcast
                string senderName = ResolvePlayerName(NetworkManager.LocalClientId);
                BroadcastChatMessageRpc(senderName, message);
            }
            else
            {
                SubmitChatMessageServerRpc(message);
            }
        }

        /// <summary>
        /// Client → Server: submits a chat message for validation and broadcast.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void SubmitChatMessageServerRpc(string message, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            // Server-side validation
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.Length > k_MaxMessageLength)
            {
                message = message.Substring(0, k_MaxMessageLength);
            }

            string senderName = ResolvePlayerName(senderClientId);
            Debug.Log($"[NetworkedChatBridge] Chat from {senderName} (client {senderClientId}): {message}");

            // Broadcast to all clients including the sender
            BroadcastChatMessageRpc(senderName, message);
        }

        /// <summary>
        /// Server → All Clients: delivers a chat message to all connected clients.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        void BroadcastChatMessageRpc(string senderName, string message)
        {
            ChatManager chatManager = ChatManager.Instance;
            if (chatManager != null)
            {
                chatManager.Broadcast(new ChatMessageReceivedEvent
                {
                    senderName = senderName,
                    message = message
                });
            }
        }

        /// <summary>
        /// Resolves a client ID to a player name using the player's NetworkedCharacterState.
        /// Falls back to "Player {clientId}" if the character state is not found.
        /// </summary>
        string ResolvePlayerName(ulong clientId)
        {
            NetworkObject playerObject = NetworkManager.SpawnManager.GetPlayerNetworkObject(clientId);
            if (playerObject != null && playerObject.TryGetComponent(out NetworkedCharacterState characterState))
            {
                string name = characterState.CharacterName;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return $"Player {clientId}";
        }

        /// <summary>
        /// Server: Sends the MOTD to a specific client as a system chat message.
        /// Called from ServerListeningState when a client connects.
        /// </summary>
        /// <param name="clientId">The client to send the MOTD to.</param>
        public void SendMotdToClient(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            ServerConfigurationLoader configLoader = ServiceLocator.Get<ServerConfigurationLoader>();
            if (configLoader?.Configuration == null)
            {
                return;
            }

            string motd = configLoader.Configuration.sv_motd;
            if (string.IsNullOrWhiteSpace(motd))
            {
                return;
            }

            MotdClientRpc(motd, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Server → specific Client: delivers the MOTD as a system message.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        void MotdClientRpc(string motd, RpcParams rpcParams = default)
        {
            ChatManager chatManager = ChatManager.Instance;
            if (chatManager != null)
            {
                chatManager.Broadcast(new MotdReceivedEvent
                {
                    message = motd
                });
            }
        }

        /// <summary>
        /// Server: Broadcasts a kill feed message to all clients.
        /// Called from ServerCharacterController when a character is killed.
        /// </summary>
        /// <param name="killerName">Name of the killer (with color codes).</param>
        /// <param name="victimName">Name of the victim (with color codes).</param>
        /// <param name="weaponName">Display name of the weapon used.</param>
        /// <param name="hitRegion">Formatted hit region string (e.g. "HEAD", "CHEST").</param>
        public void BroadcastKillFeed(string killerName, string victimName, string weaponName, string hitRegion)
        {
            if (!IsServer)
            {
                return;
            }

            BroadcastKillFeedRpc(killerName, victimName, weaponName, hitRegion);
        }

        /// <summary>
        /// Server → All Clients: delivers a kill feed message to all connected clients.
        /// </summary>
        [Rpc(SendTo.ClientsAndHost)]
        void BroadcastKillFeedRpc(string killerName, string victimName, string weaponName, string hitRegion)
        {
            ChatManager chatManager = ChatManager.Instance;
            if (chatManager != null)
            {
                chatManager.Broadcast(new KillFeedReceivedEvent
                {
                    killerName = killerName,
                    victimName = victimName,
                    weaponName = weaponName,
                    hitRegion = hitRegion
                });
            }
        }
    }
}
