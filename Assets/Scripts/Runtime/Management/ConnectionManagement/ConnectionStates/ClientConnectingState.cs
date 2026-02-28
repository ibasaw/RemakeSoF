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

        public void Configure(string iPAddress, ushort port)
        {
            m_IPAddress = iPAddress;
            m_Port = port;
        }

        public override void Enter()
        {
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Connecting });
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
                applicationVersion = Application.version
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
