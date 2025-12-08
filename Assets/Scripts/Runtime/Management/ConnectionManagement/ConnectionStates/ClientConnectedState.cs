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
            var disconnectReason = Manager.NetworkManager.DisconnectReason;
            if (string.IsNullOrEmpty(disconnectReason))
            {
                Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.GenericDisconnect });
            }
            else
            {
                var connectStatus = JsonUtility.FromJson<ConnectStatus>(disconnectReason);
                Manager.EventManager.Broadcast(new ConnectionEvent { status = connectStatus });
            }
            Manager.ChangeState(Manager.m_Offline);
        }
    }
}
