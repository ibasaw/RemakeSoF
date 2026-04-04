using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// TODO: kein createserver controller sondern server overview controller
    /// </summary>
    internal class CreateServerController : Controller<MetagameApplication>
    {
        CreateServerView View => App.View.CreateServerView;
        //ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        void Awake()
        {
           // ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AddListener<CreateServerClickEvent>(OnCreateServerClick);
            //Debug.Log("[CreateServerController] Awake - CreateServerController initialized and listeners added");
        }

        void OnDestroy()
        {
            RemoveListeners();
            //Debug.Log("[CreateServerController] OnDestroy - CreateServerController destroyed and listeners removed");
        }

        internal override void RemoveListeners()
        {
            RemoveListener<CreateServerClickEvent>(OnCreateServerClick);
          //  ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
        }

        void OnCreateServerClick(CreateServerClickEvent evt)
        {
            Debug.Log("[CreateServerController] OnCreateServerClick - Create Server button clicked, starting server...");
            //ConnectionManager.StartServerIP("0.0.0.0", 7777);
        }

       /* void OnConnectionEvent(ConnectionEvent evt)
        {
            Debug.Log($"[CreateServerController] OnConnectionEvent - Received connection event with status: {evt.status}");
        }*/
    }
}