using System;
using Tolik.RemakeSoF.Runtime.AuthenticationManagement;
using Tolik.RemakeSoF.Runtime.ConnectionManagement;
using Tolik.RemakeSoF.Runtime.PlayerSkinManagement;
using Tolik.RemakeSoF.Runtime.TextureManagement;
using Tolik.RemakeSoF.Runtime.PrefabManagement;
using Unity.Multiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using ConnectionEvent = Tolik.RemakeSoF.Runtime.ConnectionManagement.ConnectionEvent;
using Tolik.RemakeSoF.Runtime.DataManagement;
using Tolik.RemakeSoF.Runtime.ConsoleManagement;

namespace Tolik.RemakeSoF.Runtime.ApplicationLifecycle
{
    /// <summary>
    /// This is the application's entry point, where the configuration is read and the application is initialized
    /// accordingly. This also keeps references to systems that must persist throughout the application's lifecycle.
    /// </summary>
    [MultiplayerRoleRestricted]
    public class ApplicationEntryPoint : MonoBehaviour
    {
        const string k_DefaultServerListenAddress = "0.0.0.0";
        public static ApplicationEntryPoint Singleton { get; private set; }

#if UNITY_EDITOR
        public static bool s_AreTestsRunning = false;
        public bool AreTestsRunning => s_AreTestsRunning;
#endif
        // refernces used in controllers to access important systems that must persist across scenes.
        // These are set via the inspector to ensure they are present and to avoid issues with initialization order.
        [SerializeField]
        ConnectionManager m_ConnectionManager;
        public ConnectionManager ConnectionManager => m_ConnectionManager;

        [SerializeField]
        AuthenticationManager m_AuthenticationManager;
        public AuthenticationManager AuthenticationManager => m_AuthenticationManager;

        [SerializeField]
        PlayerSkinManager m_PlayerSkinManager;
        public PlayerSkinManager PlayerSkinManager => m_PlayerSkinManager;

        [SerializeField]
        ConsoleManager m_ConsoleManager;
        public ConsoleManager ConsoleManager => m_ConsoleManager;

        [SerializeField]
        internal int MinPlayers = 1;
        [SerializeField]
        internal int MaxPlayers = 2;
        ServerCommandListener m_ServerCommandListener;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Singleton = Singleton != null ? Singleton : this;

            m_ConnectionManager.EventManager.AddListener<ConnectionEvent>(OnConnectionEvent);
            m_AuthenticationManager.EventManager.AddListener<AuthenticationEvent>(OnAuthenticationEvent);
        }

        void OnDestroy()
        {
            m_ConnectionManager.EventManager.RemoveListener<ConnectionEvent>(OnConnectionEvent);
            m_AuthenticationManager.EventManager.RemoveListener<AuthenticationEvent>(OnAuthenticationEvent);
            ServiceLocator.ClearAll();
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnApplicationStarted()
        {
            if (!Singleton) //this happens during PlayMode tests
            {
                return;
            }
            Singleton.InitializeNetworkLogic(); //note: this is the entry point for all autoconnected instances (including standalone servers)
        }

        /// <summary>
        /// Initializes the application's network-related behaviour according to the configuration. Servers load the main
        /// game scene and automatically start. Clients load the metagame scene and, if autonnect is set to true, attempt
        /// to connect to a server automatically based on the IP and port passed through the configuration or the command
        /// line arguments.
        /// </summary>
        void InitializeNetworkLogic()
        {
            var commandLineArgumentsParser = new CommandLineArgumentsParser();
            ushort listeningPort = (ushort)commandLineArgumentsParser.Port;

            PrefabManager prefabManager = new();
            ServiceLocator.Register(prefabManager);

            switch (MultiplayerRolesManager.ActiveMultiplayerRoleMask)
            {
                case MultiplayerRoleFlags.Server:
                    //lock framerate on dedicated servers
                    Application.targetFrameRate = commandLineArgumentsParser.TargetFramerate;
                    QualitySettings.vSyncCount = 0;

                    // WeaponDataLoader fuer server-seitige Attack-Parameter
                    WeaponDataLoader serverWeaponLoader = new();
                    ServiceLocator.Register(serverWeaponLoader);

                    // Start CLI command listener for server
                    m_ServerCommandListener = new ServerCommandListener();
                    ServiceLocator.Register(m_ServerCommandListener);
                    m_ServerCommandListener.Start();

                    m_ConnectionManager.StartServerIP(k_DefaultServerListenAddress, listeningPort);
                    break;
                case MultiplayerRoleFlags.Client:
                    {
                        // 1. Pure Services registrieren
                        TextureManager textureManager = new();
                        ServiceLocator.Register(textureManager);

                        SkinDefinitionLoader skinLoader = new();
                        ServiceLocator.Register(skinLoader);

                        SurfaceDefinitionLoader surfaceLoader = new();
                        ServiceLocator.Register(surfaceLoader);

                        CharacterTemplateLoader templateLoader = new();
                        ServiceLocator.Register(templateLoader);

                        ItemDefinitionLoader itemLoader = new();
                        ServiceLocator.Register(itemLoader);

                        LegacyShaderLoader shaderLoader = new();
                        ServiceLocator.Register(shaderLoader);

                        GoreDataLoader goreDataLoader = new();
                        ServiceLocator.Register(goreDataLoader);

                        WeaponDataLoader weaponDataLoader = new();
                        ServiceLocator.Register(weaponDataLoader);

                        SceneManager.LoadScene("MetagameScene");
                        Debug.Log($"[ApplicationEntryPoint] InitializeNetworkLogic - Client instance started, loaded MetagameScene.");
                        break;
                    }
                case MultiplayerRoleFlags.ClientAndServer:
                    throw new ArgumentOutOfRangeException("MultiplayerRole", "ClientAndServer is an invalid multiplayer role in this sample. Please select the Client or Server role.");
            }
        }

        void OnAuthenticationEvent(AuthenticationEvent evt)
        {
            Debug.Log($"[ApplicationEntryPoint] OnAuthenticationEvent - Received authentication event with status: {evt.status}");
        }

        void OnConnectionEvent(ConnectionEvent evt)
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask == MultiplayerRoleFlags.Server)
            {
                switch (evt.status)
                {
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.ServerEndedSession:
                    case ConnectStatus.StartServerFailed:
                        // If server ends networked session or fails to start, quit the application
                        Quit();
                        break;
                    case ConnectStatus.Success:
                        // If server successfully starts, load game scene
                        NetworkManager.Singleton.SceneManager.LoadScene("GameScene01", LoadSceneMode.Single);
                        break;
                }
            }
            else
            {
                switch (evt.status)
                {
                    case ConnectStatus.GenericDisconnect:
                    case ConnectStatus.UserRequestedDisconnect:
                    case ConnectStatus.ServerEndedSession:
                        // If client is disconnected, return to metagame scene
                        Debug.Log($"[ApplicationEntryPoint] OnConnectionEvent - Client disconnected with status: {evt.status}, returning to metagame scene...");
                        SceneManager.LoadScene("MetagameScene");
                        break;
                }
            }
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
