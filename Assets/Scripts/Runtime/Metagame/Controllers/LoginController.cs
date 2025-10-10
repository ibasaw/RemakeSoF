using System;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;

namespace Unity.DedicatedGameServerSample.Runtime
{
    internal class LoginController : Controller<MetagameApplication>
    {
        LoginView View => App.View.LoginView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        void Awake()
        {
            AddListener<PlayerLoginEvent>(OnPlayerLogin);
            AddListener<ChangeToRegisterEvent>(OnChangeToRegister);
            AddListener<ChangeToLoginEvent>(OnChangeToLogin);
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
        }

        // Called when the user clicks the "Login" button on the login view to attempt to log in
        void OnPlayerLogin(PlayerLoginEvent evt)
        {
            // Handle player login logic here, e.g., authenticate with a server
            Debug.Log($"Attempting to log in user: {evt.username} with password: {evt.password}");
        }
        
        // Called when the user clicks the "Login" button on the registration view to switch back to the login view
        void OnChangeToLogin(ChangeToLoginEvent evt)
        {
            View.Show();
        }

        // Called when the user clicks the "Register" button on the login view to switch to the registration view
        void OnChangeToRegister(ChangeToRegisterEvent evt)
        {
            View.Hide();
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<PlayerLoginEvent>(OnPlayerLogin);
            RemoveListener<ChangeToRegisterEvent>(OnChangeToRegister);
            RemoveListener<ChangeToLoginEvent>(OnChangeToLogin);
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
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
