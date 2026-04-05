using System;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when a client is attempting to connect to a server. Starts the client when
    /// entering. If successful, transitions to the ClientConnected state. If not, transitions to the Offline state.
    /// </summary>
    class ClientConnectingState : ConnectionState
    {
        string m_IPAddress;
        ushort m_Port;
        string m_PlayerName;
        string m_SkinName;
        string m_ServerName;
        string m_ServerDescription;

        public void Configure(string iPAddress, ushort port, string playerName, string skinName, string serverName = "", string serverDescription = "")
        {
            m_IPAddress = iPAddress;
            m_Port = port;
            m_PlayerName = playerName;
            m_SkinName = skinName;
            m_ServerName = serverName;
            m_ServerDescription = serverDescription;
        }

        public override void Enter()
        {
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Connecting, serverAddress = $"{m_IPAddress}:{m_Port}", serverName = m_ServerName, serverDescription = m_ServerDescription });
            ConnectClient();
        }

        public override void Exit() { }

        public override void OnClientConnected(ulong clientId)
        {
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Success });
            Manager.ChangeState(Manager.m_ClientConnected);
        }

        public override void OnClientDisconnect(ulong clientId)
        {
            // client ID is for sure ours here
            StartingClientFailed();
        }

        void StartingClientFailed()
        {
            var disconnectReason = Manager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.StartClientFailed });
            }
            else
            {
                Debug.LogWarning($"Client failed to connect with disconnect reason: {disconnectReason}");
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                Manager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
            }
            Manager.ChangeState(Manager.m_Offline);
        }

        void ConnectClient()
        {
            try
            {
                // Setup NGO with current connection method
                SetConnectionPayload();
                var utp = (UnityTransport)Manager.NetworkManager.NetworkConfig.NetworkTransport;
                utp.SetConnectionData(m_IPAddress, m_Port);

                Debug.Log($"Attempting to connect to server on {m_IPAddress} with port {m_Port}");
                // NGO's StartClient launches everything
                if (!Manager.NetworkManager.StartClient())
                {
                    throw new Exception("NetworkManager StartClient failed");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error connecting client, see following exception");
                Debug.LogException(e);
                StartingClientFailed();
                throw;
            }
        }

        void SetConnectionPayload()
        {
            var payload = JsonUtility.ToJson(new ConnectionPayload()
            {
                applicationVersion = Application.version,
                playerName = m_PlayerName ?? string.Empty,
                skinName = m_SkinName ?? string.Empty
            });

            var payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

            Manager.NetworkManager.NetworkConfig.ConnectionData = payloadBytes;
        }

        public override void OnTransportFailure()
        {
            // This behaviour will be the same for every online state
            Manager.ChangeState(Manager.m_Offline);
        }

        public override void OnCancelClientConnectionAttempt()
        {
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.UserCancelledConnectionAttempt });
            Manager.ChangeState(Manager.m_Offline);
        }
    }
}
