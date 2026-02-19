using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Tolik.RemakeSoF.Runtime.ConnectionManagement
{
    /// <summary>
    /// Connection state corresponding to a server starting up. Starts the server when entering the state. If successful,
    /// transitions to the ServerListening state, if not, transitions back to the Offline state.
    /// </summary>
    class StartingServerState : OnlineState
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
            StartServer();
        }

        public override void Exit() { }

        public override void OnServerStarted()
        {
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.Success });
            Manager.ChangeState(Manager.m_ServerListening);
        }

        public override void OnServerStopped()
        {
            StartServerFailed();
        }

        void StartServerFailed()
        {
            Manager.EventManager.Broadcast(new ConnectionEvent { status = ConnectStatus.StartServerFailed });
            Manager.ChangeState(Manager.m_Offline);
        }

        public void StartServer()
        {
            UnityTransport utp = (UnityTransport)Manager.NetworkManager.NetworkConfig.NetworkTransport;
            utp.SetConnectionData(m_IPAddress, m_Port);
            Debug.Log($"Starting server on {m_IPAddress}:{m_Port} with target framerate {Application.targetFrameRate}");

            if (!Manager.NetworkManager.StartServer())
                StartServerFailed();
        }
    }
}
