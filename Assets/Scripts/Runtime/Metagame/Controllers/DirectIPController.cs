using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    internal class DirectIPController : Controller<MetagameApplication>
    {
        DirectIPView View => App.View.DirectIP;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        PlayerSkinManager PlayerSkinManager => ApplicationEntryPoint.Singleton.PlayerSkinManager;

        void Awake()
        {
            AddListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            AddListener<ExitIPConnectionEvent>(OnExitIPConnection);
            AddListener<JoinThroughDirectIPEvent>(OnJoinGame);
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            RemoveListener<ExitIPConnectionEvent>(OnExitIPConnection);
            RemoveListener<JoinThroughDirectIPEvent>(OnJoinGame);
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
        }

        void OnEnterIPConnection(EnterIPConnectionEvent evt)
        {
            View.Show();
        }

        void OnExitIPConnection(ExitIPConnectionEvent evt)
        {
            View.Hide();
        }

        void OnJoinGame(JoinThroughDirectIPEvent evt)
        {
            ConnectionManager.StartClient(
                evt.ipAddress,
                evt.port,
                App.Model.PlayerData.PlayerName,
                PlayerSkinManager.CurrentSkinName
            );
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            if (evt.status == ConnectStatus.Connecting)
            {
                View.Hide();
            }
        }
    }
}
