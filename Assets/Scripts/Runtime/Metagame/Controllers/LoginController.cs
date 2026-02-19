using System;
using System.Collections;
using System.Text;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.AuthenticationManagement;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace Tolik.RemakeSoF.Runtime
{
    /// <summary>
    /// Zuständig für die Orchestrierung von Login-bezogenen Ereignissen und Interaktionen
    /// zwischen der LoginView und dem AuthenticationManager.
    /// </summary>
    internal class LoginController : Controller<MetagameApplication>
    {
        LoginView View => App.View.LoginView;
        AuthenticationManager AuthenticationManager => ApplicationEntryPoint.Singleton.AuthenticationManager;
        void Awake()
        {
            AddListener<PlayerLoginEvent>(OnPlayerLogin);
            AddListener<ChangeToRegisterEvent>(OnChangeToRegister);
            AddListener<ChangeToLoginEvent>(OnChangeToLogin);
            AuthenticationManager.EventManager.AddListener<AuthenticationEvent>(OnAuthenticationEvent);
            AuthenticationManager.EventManager.AddListener<UserUnauthenticatedEvent>(OnUserUnauthenticatedEvent);
            AuthenticationManager.EventManager.AddListener<UserAuthenticatedEvent>(OnUserAuthenticatedEvent);
        }

        void Start()
        {
            if(AuthenticationManager.IsAuthenticated())
            {
                Debug.Log("User already authenticated at LoginController Start, hiding login view");
                View.Hide();
                return;
            }
            PrepareViewTexturesAndShow();
        }

        // Called when the user clicks the "Login" button on the login view to attempt to log in
        void OnPlayerLogin(PlayerLoginEvent evt)
        {
            Debug.Log($"Attempting to log in user: {evt.username} with password: {evt.password}");

            // Validate all login data
            if (!View.ValidateLoginData())
                return; // Validation failed, error message already shown

            View.SetLoginInProgress(true);

            AuthenticationManager.Authenticate(evt.username, evt.password);
        }

        /// <summary>
        /// Called when the user clicks the "Login" button on the registration view to switch to the login view
        /// </summary>
        void OnChangeToLogin(ChangeToLoginEvent evt)
        {
            PrepareViewTexturesAndShow();
        }

        /// <summary>
        /// Called when the user clicks the "Register" button on the login view to switch to the registration view
        /// </summary>
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
            AuthenticationManager.EventManager.RemoveListener<AuthenticationEvent>(OnAuthenticationEvent);
            AuthenticationManager.EventManager.RemoveListener<UserUnauthenticatedEvent>(OnUserUnauthenticatedEvent);
            AuthenticationManager.EventManager.RemoveListener<UserAuthenticatedEvent>(OnUserAuthenticatedEvent);
        }

        void OnAuthenticationEvent(AuthenticationEvent evt)
        {
            Debug.Log($"Authentication event received with status: {evt.status}");
            switch (evt.status)
            {
                case AuthenticationStatus.Authenticating:
                    View.SetLoginInProgress(true);
                    break;
                case AuthenticationStatus.Success:
                    View.SetLoginInProgress(false);
                    View.SetStatusMessage("Login successful");
                    break;
                case AuthenticationStatus.NetworkError:
                    View.SetLoginInProgress(false);
                    View.SetStatusMessage("Login failed: Authserver unreachable", isError: true);
                    break;
                case AuthenticationStatus.InvalidCredentials:
                    View.SetLoginInProgress(false);
                    View.SetStatusMessage("Login failed: Invalid credentials", isError: true);
                    break;
                default:
                    View.SetLoginInProgress(false);
                    View.SetStatusMessage($"Login failed: {evt.status}", isError: true);
                    break;
            }
        }

        void OnUserUnauthenticatedEvent(UserUnauthenticatedEvent evt)
        {
            Debug.Log("User unauthenticated event received, showing login view");
            PrepareViewTexturesAndShow();
        }

        private void PrepareViewTexturesAndShow()
        {
            View.Show();

            TextureConfiguration configuration = ServiceLocator.Get<TextureManager>().Configuration;

            // Texturen vom Manager holen (aus Art/Textures oder persistentDataPath/CustomTextures)
            Texture2D backgroundTexture = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.login.background).Texture;
            Texture2D logoTexture = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.login.logo).Texture;
            Texture2D buttonBackgroundTexture = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.login.buttonBackground).Texture;
            Texture2D textFieldBackgroundTexture = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.login.inputBackground).Texture;

            // An View übergeben
            View.SetBackgroundTexture(backgroundTexture);
            View.SetLogoTexture(logoTexture);
            View.SetAllButtonBackgroundTexture(buttonBackgroundTexture);
            View.SetAllTextFieldInputsBackgroundTexture(textFieldBackgroundTexture);
        }

        void OnUserAuthenticatedEvent(UserAuthenticatedEvent evt)
        {
            Debug.Log("User authenticated event received, hiding login view");
            View.Hide();
        }
    }
}
