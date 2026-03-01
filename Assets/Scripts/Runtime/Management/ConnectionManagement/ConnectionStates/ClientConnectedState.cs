using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a connected client. When being disconnected, transitions to the Offline state.
    /// </summary>
    class ClientConnectedState : OnlineState
    {
        public override void Enter() { }

        public override void Exit() { }

        public override void OnClientDisconnect(ulong clientId)
        {
            string disconnectReason = Manager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.GenericDisconnect });
            }
            else
            {
                try
                {
                    ConnectStatus connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                    Manager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
                }
                catch
                {
                    // DisconnectReason ist kein gültiges JSON (z.B. TransportShutdown beim Exit Play Mode)
                    Debug.LogWarning($"[ClientConnectedState] Could not parse disconnect reason as ConnectStatus: {disconnectReason}");
                    Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.GenericDisconnect });
                }
            }
            Manager.ChangeState(Manager.m_Offline);
        }
    }
}
