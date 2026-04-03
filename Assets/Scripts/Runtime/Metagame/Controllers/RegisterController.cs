using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.DataManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Controller fuer die Registrierungs-View. Delegiert HTTP-Kommunikation an MasterServerService.
    /// </summary>
    internal class RegisterController : Controller<MetagameApplication>
    {
        RegisterView View => App.View.RegisterView;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;

        void Awake()
        {
            AddListener<PlayerRegisterEvent>(OnPlayerRegister);
            AddListener<ChangeToRegisterEvent>(OnChangeToRegister);
            AddListener<ChangeToLoginEvent>(OnChangeToLogin);
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
        }

        /// <summary>
        /// Wird aufgerufen wenn der Benutzer den Register-Button klickt.
        /// Validiert die Eingaben und delegiert den HTTP-Request an MasterServerService.
        /// </summary>
        void OnPlayerRegister(PlayerRegisterEvent evt)
        {
            Debug.Log($"Attempting to register user: {evt.username} with email: {evt.email}");

            if (!View.ValidateRegistrationData())
            {
                return;
            }

            View.ClearStatusMessage();
            View.SetRegisterInProgress(true);

            RegisterViaService(evt.username, evt.email, evt.password);
        }

        /// <summary>
        /// Delegiert die Registrierung an den MasterServerService.
        /// </summary>
        async void RegisterViaService(string username, string email, string password)
        {
            MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
            if (masterService == null)
            {
                View.SetStatusMessage("Service not available.", true);
                View.SetRegisterInProgress(false, false);
                return;
            }

            try
            {
                MasterServerRegisterResult result = await masterService.RegisterUserAsync(username, email, password);

                if (result.IsSuccess)
                {
                    Debug.Log($"[RegisterController] Registration successful: {result.Message}");
                    View.SetStatusMessage("Registration successful! Please login.", false);
                    View.SetRegisterInProgress(false, false);
                }
                else
                {
                    View.SetStatusMessage($"Registration failed: {result.ErrorMessage}", true);
                    View.SetRegisterInProgress(false, false);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RegisterController] Register Exception: {e.Message}");
                View.SetStatusMessage($"Registration failed: {e.Message}", true);
                View.SetRegisterInProgress(false, false);
            }
        }

        void OnChangeToLogin(ChangeToLoginEvent evt)
        {
            View.Hide();
        }

        void OnChangeToRegister(ChangeToRegisterEvent evt)
        {
            View.Show();
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<PlayerRegisterEvent>(OnPlayerRegister);
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
