using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ConsoleManagement
{
    /// <summary>
    /// NetworkBehaviour bridge that allows clients to send console commands to the server.
    /// The server executes the command via <see cref="ServerCommandListener"/> and sends the response back to the client.
    /// Must be placed on a networked GameObject (e.g. alongside NetworkedGameState or on a persistent network object).
    /// </summary>
    public class NetworkedCommandBridge : NetworkBehaviour
    {
        /// <summary>
        /// Singleton instance for easy access from ConsoleController.
        /// </summary>
        public static NetworkedCommandBridge Instance { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Instance = this;

            string role = IsServer ? "SERVER" : "CLIENT";
            Debug.Log($"[NetworkedCommandBridge] {role} spawned and ready.");
        }
        private void Awake()
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
            Debug.Log("[NetworkedCommandBridge] Despawned and instance cleared.");
        }

        /// <summary>
        /// Called by the client to send a command to the server for execution.
        /// </summary>
        public void SendCommandToServer(string command)
        {
            if (IsServer)
            {
                // Already on server, execute directly
                ExecuteOnServer(command, NetworkManager.LocalClientId);
            }
            else
            {
                // Send to server via RPC
                SubmitCommandServerRpc(command);
            }
        }

        /// <summary>
        /// Rpc: receives a command from a client and executes it on the server.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void SubmitCommandServerRpc(string command, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"[NetworkedCommandBridge] Received command from client {senderClientId}: {command}");
            ExecuteOnServer(command, senderClientId);
        }

        /// <summary>
        /// Executes a command on the server and sends the response back to the requesting client.
        /// </summary>
        void ExecuteOnServer(string command, ulong clientId)
        {
            ServerCommandListener listener = ServiceLocator.Get<ServerCommandListener>();
            if (listener != null)
            {
                string result = listener.ExecuteCommand(command);
                SendResponseToClient(result, clientId);
            }
            else
            {
                string errorMsg = "[Server] ServerCommandListener not available.";
                Debug.LogWarning(errorMsg);
                SendResponseToClient(errorMsg, clientId);
            }
        }

        /// <summary>
        /// Sends a response message back to a specific client.
        /// </summary>
        void SendResponseToClient(string message, ulong clientId)
        {
            ReceiveResponseRpc(message, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// Rpc: receives a response from the server and displays it in the client console.
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        void ReceiveResponseRpc(string message, RpcParams rpcParams = default)
        {
            Debug.Log($"[NetworkedCommandBridge] Server response: {message}");

            // Show response in the ConsoleView if available
            ConsoleManager consoleManager = ConsoleManager.Instance;
            if (consoleManager != null && consoleManager.View != null)
            {
                consoleManager.View.AddOutput(message);
            }
        }
    }
}
