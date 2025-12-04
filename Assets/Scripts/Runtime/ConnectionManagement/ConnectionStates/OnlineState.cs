namespace Unity.DedicatedGameServerSample.Runtime.ConnectionManagement
{
    /// <summary>
    /// Base class representing an online connection state.
    /// </summary>
    abstract class OnlineState : ConnectionState
    {
        public override void OnUserRequestedShutdown()
        {
            // This behaviour will be the same for every online state
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.UserRequestedDisconnect });
            Manager.ChangeState(Manager.m_Offline);
        }

        public override void OnTransportFailure()
        {
            // This behaviour will be the same for every online state
            Manager.ChangeState(Manager.m_Offline);
        }
    }
}
