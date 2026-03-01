using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    internal class JoinServerController : Controller<MetagameApplication>
    {
        JoinServerView View => App.View.JoinServerView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        void Awake()
        {
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AddListener<JoinServerClickEvent>(OnJoinServerClick);
            Debug.Log("[JoinServerController] Awake - JoinServerController initialized and listeners added");
        }

        void OnDestroy()
        {
            RemoveListeners();
            Debug.Log("[JoinServerController] OnDestroy - JoinServerController destroyed and listeners removed");
        }

        internal override void RemoveListeners()
        {
            RemoveListener<JoinServerClickEvent>(OnJoinServerClick);
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
        }

        void OnJoinServerClick(JoinServerClickEvent evt)
        {
            Debug.Log("[JoinServerController] OnJoinServerClick - Join Server button clicked, joining server...");
            ConnectionManager.StartClient(
                "127.0.0.1",
                7777,
                App.Model.PlayerData.PlayerName,
                App.Model.PlayerData.CurrentSelectedSkinName
            );
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            Debug.Log($"[JoinServerController] OnConnectionEvent - Received connection event with status: {evt.status}");
        }
    }
}