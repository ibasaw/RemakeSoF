using System;
using System.Collections;
using System.Text;
using Unity.DedicatedGameServerSample.Runtime.ApplicationLifecycle;
using Unity.DedicatedGameServerSample.Runtime.AuthenticationManagement;
using Unity.DedicatedGameServerSample.Runtime.ConnectionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.DedicatedGameServerSample.Runtime
{
    /// <summary>
    /// Zuständig für die Orchestrierung von Login-bezogenen Ereignissen und Interaktionen
    /// zwischen der LoginView und dem AuthenticationManager.
    /// </summary>
    internal class LoginController : Controller<MetagameApplication>
    {
        LoginView View => App.View.LoginView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        AuthenticationManager AuthenticationManager => ApplicationEntryPoint.Singleton.AuthenticationManager;

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
            Debug.Log($"Attempting to log in user: {evt.username} with password: {evt.password}");

            // Validate all login data
            //if (!View.ValidateLoginData())
            //    return; // Validation failed, error message already shown

            // Clear any previous status message
            //View.ClearStatusMessage();

            // Set UI to loading logging in state
            //View.SetLoginInProgress(true);

            AuthenticationManager.Authenticate(evt.username, evt.password);
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
