using System;
using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.AuthenticationManagement;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime
{
    internal class MainMenuController : Controller<MetagameApplication>
    {
        MainMenuView View => App.View.MainMenu;
        ConnectionManager ConnectionManager => ApplicationEntryPoint.Singleton.ConnectionManager;
        AuthenticationManager AuthenticationManager => ApplicationEntryPoint.Singleton.AuthenticationManager;
        PlayerSkinManager PlayerSkinManager => ApplicationEntryPoint.Singleton.PlayerSkinManager;
        void Awake()
        {
            AddListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            AddListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            AddListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            AddListener<ExitIPConnectionEvent>(OnExitIPConnection);
            AddListener<UserRequestedLogoutEvent>(OnUserRequestedLogout);
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AuthenticationManager.EventManager.AddListener<UserAuthenticatedEvent>(OnUserAuthenticatedEvent);
            AuthenticationManager.EventManager.AddListener<UserUnauthenticatedEvent>(OnUserUnauthenticatedEvent);
        }

        void Start()
        {
            if (AuthenticationManager.IsAuthenticated())
            {
                OnUserAuthenticatedEvent(new UserAuthenticatedEvent(AuthenticationManager.m_Authenticated.AuthResponse));
            }
        }

        void OnDestroy()
        {
            RemoveListeners();
        }

        internal override void RemoveListeners()
        {
            RemoveListener<EnterMatchmakerQueueEvent>(OnEnterMatchmakerQueue);
            RemoveListener<ExitMatchmakerQueueEvent>(OnExitMatchmakerQueue);
            RemoveListener<EnterIPConnectionEvent>(OnEnterIPConnection);
            RemoveListener<ExitIPConnectionEvent>(OnExitIPConnection);
            RemoveListener<UserRequestedLogoutEvent>(OnUserRequestedLogout);
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
            AuthenticationManager.EventManager.RemoveListener<UserAuthenticatedEvent>(OnUserAuthenticatedEvent);
            AuthenticationManager.EventManager.RemoveListener<UserUnauthenticatedEvent>(OnUserUnauthenticatedEvent);
        }

        void OnEnterMatchmakerQueue(EnterMatchmakerQueueEvent evt)
        {
            View.Hide();
        }

        void OnExitMatchmakerQueue(ExitMatchmakerQueueEvent evt)
        {
            View.Show();
        }

        void OnEnterIPConnection(EnterIPConnectionEvent evt)
        {
            View.Hide();
        }

        void OnExitIPConnection(ExitIPConnectionEvent evt)
        {
            View.Show();
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            switch (evt.status)
            {
                case ConnectStatus.Connecting:
                    // Block logout mid-handshake to avoid leaving the session in a half-state.
                    View.SetLogoutEnabled(false);
                    break;
                case ConnectStatus.Success:
                case ConnectStatus.ServerFull:
                case ConnectStatus.IncompatibleVersions:
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.GenericDisconnect:
                case ConnectStatus.ServerEndedSession:
                case ConnectStatus.StartClientFailed:
                    View.SetLogoutEnabled(true);
                    View.Show();
                    break;
            }
        }

        void OnUserAuthenticatedEvent(UserAuthenticatedEvent evt)
        {
            Debug.Log("User authenticated event received, showing main menu view");
            App.Model.PlayerData.InitializePlayer(evt.AuthResponse);

            PlayerSkinManager.ChangeSkin(App.Model.PlayerData.CurrentSelectedSkinName);

            View.Show();
            View.LoadSubViewByName("loadoutButton"); // start show Loadout view by default when player is authenticated and main menu is shown
        }

        /// <summary>
        /// MainMenuView confirmed the logout via its inline overlay. Delegate to the manager,
        /// which clears the persisted session and transitions to UnauthenticatedState.
        /// </summary>
        void OnUserRequestedLogout(UserRequestedLogoutEvent evt)
        {
            Debug.Log("[MainMenuController] User confirmed logout — delegating to AuthenticationManager.");
            AuthenticationManager.Logout();
        }

        /// <summary>
        /// Fired by UnauthenticatedState on entry (post-logout or post-expiry). Hide the menu
        /// so the LoginView (re-shown by LoginController) is not occluded, and reset the
        /// cached player data so a different user does not briefly inherit the previous skin / name.
        /// </summary>
        void OnUserUnauthenticatedEvent(UserUnauthenticatedEvent evt)
        {
            Debug.Log("[MainMenuController] User unauthenticated — hiding menu and resetting player data.");
            App.Model.PlayerData.Reset();
            View.Hide();
        }
    }
}
