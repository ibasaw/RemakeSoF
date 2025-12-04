using Unity.Netcode;
using Unity.DedicatedGameServerSample.Runtime.Core;

namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Base class representing a connection state.
    /// </summary>
    public abstract class ConnectionState : State<ConnectionManager>
    {
        public override abstract void Enter();

        public override abstract void Exit();

        public virtual void OnClientConnected(ulong clientId) { }

        public virtual void OnClientDisconnect(ulong clientId) { }

        public virtual void OnServerStarted() { }

        public virtual void StartClient(string ipaddress, ushort port) { }

        public virtual void StartServerIP(string ipaddress, ushort port) { }

        public virtual void StartServerMatchmaker() { }

        public virtual void OnUserRequestedShutdown() { }

        public virtual void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) { }

        public virtual void OnTransportFailure() { }

        public virtual void OnServerStopped() { }
    }
}
