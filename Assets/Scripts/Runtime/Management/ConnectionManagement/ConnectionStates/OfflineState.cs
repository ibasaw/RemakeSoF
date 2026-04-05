namespace Tolik.RemakeSoF.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to when the NetworkManager is shut down.
    ///  From this state we can transition to the ClientConnecting state, if starting as a client,
    ///  or the StartingServer state, if starting as a server.
    /// </summary>
    class OfflineState : ConnectionState
    {
        public override void Enter()
        {
            Manager.NetworkManager.Shutdown();
        }

        public override void Exit() { }

        public override void StartClient(string ipaddress, ushort port, string playerName, string skinName, string serverName = "", string serverDescription = "")
        {
            Manager.m_ClientConnecting.Configure(ipaddress, port, playerName, skinName, serverName, serverDescription);
            Manager.ChangeState(Manager.m_ClientConnecting);
        }

        public override void StartServerIP(string ipaddress, ushort port)
        {
            Manager.m_StartingServer.Configure(ipaddress, port);
            Manager.ChangeState(Manager.m_StartingServer);
        }
    }
}
