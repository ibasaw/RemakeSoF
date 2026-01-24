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
            ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            AuthenticationManager.EventManager.AddListener<UserAuthenticatedEvent>(OnUserAuthenticatedEvent);
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
            ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
            AuthenticationManager.EventManager.RemoveListener<UserAuthenticatedEvent>(OnUserAuthenticatedEvent);
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
                case ConnectStatus.Success:
                case ConnectStatus.ServerFull:
                case ConnectStatus.IncompatibleVersions:
                case ConnectStatus.UserRequestedDisconnect:
                case ConnectStatus.GenericDisconnect:
                case ConnectStatus.ServerEndedSession:
                case ConnectStatus.StartClientFailed:
                    View.Show();
                    break;
            }
        }

        void OnUserAuthenticatedEvent(UserAuthenticatedEvent evt)
        {
            Debug.Log("User authenticated event received, showing main menu view");
            App.Model.PlayerData.InitializePlayer(evt.AuthResponse);

            PlayerSkinManager.ChangeSkin(App.Model.PlayerData.CurrentSelectedSkinName);

            PrepareViewTexturesAndShow();
            View.LoadSubViewByName("loadoutButton"); // start Default to Loadout view
        }

        private void PrepareViewTexturesAndShow()
        {
            View.Show();

            TextureConfiguration configuration = ServiceLocator.Get<TextureManager>().Configuration;

            // Texturen vom Manager holen (aus Art/Textures oder persistentDataPath/CustomTextures)
            Texture2D mainMenuBackgroundTexture = ServiceLocator.Get<TextureManager>().GetTextureData(configuration.metagame.mainMenu.background).Texture;

            // An View übergeben
            View.SetBackgroundTexture(mainMenuBackgroundTexture);
        }
    }
}
