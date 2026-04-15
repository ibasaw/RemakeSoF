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
using Tolik.RemakeSoF.Runtime.ChatManagement;
using Tolik.RemakeSoF.Runtime.ConsoleManagement;
using Tolik.RemakeSoF.Runtime.Game.Effects;
using Tolik.RemakeSoF.Runtime.GoreManagement;
using Tolik.RemakeSoF.Runtime.GametypeManagement;
using Tolik.RemakeSoF.Runtime.SoundManagement;
using UnityEngine.Audio;

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
        ChatManager m_ChatManager;
        public ChatManager ChatManager => m_ChatManager;

        /// <summary>MasterMixer fuer Audio-Routing (SFX/Music-Gruppen werden automatisch aufgeloest).</summary>
        [SerializeField]
        UnityEngine.Audio.AudioMixer m_MasterMixer;
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
            DeregisterFromMasterServer();
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

                    // Server-Konfiguration laden (SoF2-CVARs aus JSON)
                    ServerConfigurationLoader serverConfigLoader = new();
                    ServiceLocator.Register(serverConfigLoader);

                    // Gametype-Definitionen laden
                    GametypeDefinitionLoader serverGametypeLoader = new();
                    ServiceLocator.Register(serverGametypeLoader);

                    // Port aus Config ueberschreiben falls nicht per CLI gesetzt
                    if (commandLineArgumentsParser.Port == 7777 && serverConfigLoader.Configuration.sv_port != 7777)
                    {
                        listeningPort = (ushort)serverConfigLoader.Configuration.sv_port;
                    }

                    // SkinDefinitionLoader fuer server-seitige Hitbox-Skeleton-Aufloesung (skinName → modelName)
                    SkinDefinitionLoader serverSkinLoader = new();
                    ServiceLocator.Register(serverSkinLoader);

                    // WeaponDataLoader fuer server-seitige Attack-Parameter
                    WeaponDataLoader serverWeaponLoader = new();
                    ServiceLocator.Register(serverWeaponLoader);

                    // EffectDataLoader fuer server-seitige Effekt-Referenzen
                    EffectDataLoader serverEffectLoader = new();
                    ServiceLocator.Register(serverEffectLoader);

                    // SurfaceImpactDataLoader fuer server-seitige Impact-Effekt-Aufloesung
                    SurfaceImpactDataLoader serverSurfaceImpactLoader = new();
                    ServiceLocator.Register(serverSurfaceImpactLoader);

                    // MapDataLoader fuer Map-Definitionen und Spawn-Points
                    MapDataLoader serverMapDataLoader = new();
                    ServiceLocator.Register(serverMapDataLoader);

                    // GametypeManager erstellen (liest g_gametype aus ServerConfiguration)
                    GametypeManager gametypeManager = new();
                    ServiceLocator.Register(gametypeManager);

                    // Start CLI command listener for server
                    m_ServerCommandListener = new ServerCommandListener();
                    ServiceLocator.Register(m_ServerCommandListener);
                    m_ServerCommandListener.Start();

                    // MasterServerService fuer Server-Registrierung beim Master-Server
                    MasterServerService serverMasterService = new(serverConfigLoader.Configuration.sv_master);
                    ServiceLocator.Register(serverMasterService);

                    // Server-IP aus Config (CLI hat Vorrang)
                    string listenAddress = serverConfigLoader.Configuration.sv_ip ?? k_DefaultServerListenAddress;
                    m_ConnectionManager.StartServerIP(listenAddress, listeningPort);
                    break;
                case MultiplayerRoleFlags.Client:
                    {
                        // 1. Pure Services registrieren

                        // Server-Konfiguration laden fuer sv_master (Client braucht Master-Server-URL)
                        ServerConfigurationLoader clientConfigLoader = new();
                        ServiceLocator.Register(clientConfigLoader);

                        // MasterServerService fuer Server-Browser (Server-Liste abrufen)
                        MasterServerService clientMasterService = new(clientConfigLoader.Configuration.sv_master);
                        ServiceLocator.Register(clientMasterService);

                        // Gametype-Definitionen fuer Client-UI (Gametype-Auswahl etc.)
                        GametypeDefinitionLoader clientGametypeLoader = new();
                        ServiceLocator.Register(clientGametypeLoader);

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

                        GoreManager goreManager = new();
                        ServiceLocator.Register(goreManager);

                        WeaponDataLoader weaponDataLoader = new();
                        ServiceLocator.Register(weaponDataLoader);

                        EffectDataLoader effectDataLoader = new();
                        ServiceLocator.Register(effectDataLoader);

                        SoundManager soundManager = new();
                        soundManager.SetMixer(m_MasterMixer);
                        ServiceLocator.Register(soundManager);

                        EffectFactory effectFactory = new();
                        ServiceLocator.Register(effectFactory);

                        SurfaceImpactDataLoader surfaceImpactLoader = new();
                        ServiceLocator.Register(surfaceImpactLoader);

                        MapDataLoader clientMapDataLoader = new();
                        ServiceLocator.Register(clientMapDataLoader);

                        CrosshairDataLoader crosshairDataLoader = new();
                        ServiceLocator.Register(crosshairDataLoader);

                        SceneManager.LoadScene("MetagameScene");
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
                        // Deregister from master server before quitting
                        DeregisterFromMasterServer();
                        Quit();
                        break;
                    case ConnectStatus.Success:
                        // If server successfully starts, register at master server and load game scene
                        RegisterAtMasterServer();
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

        /// <summary>
        /// Registriert diesen Server beim Master-Server nach erfolgreichem Start.
        /// </summary>
        async void RegisterAtMasterServer()
        {
            MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
            ServerConfigurationLoader configLoader = ServiceLocator.Get<ServerConfigurationLoader>();
            if (masterService == null || configLoader == null)
            {
                return;
            }

            int currentPlayers = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
            await masterService.RegisterServerAsync(configLoader.Configuration, currentPlayers);
        }

        /// <summary>
        /// Deregistriert diesen Server beim Master-Server vor dem Beenden.
        /// </summary>
        async void DeregisterFromMasterServer()
        {
            MasterServerService masterService = ServiceLocator.Get<MasterServerService>();
            if (masterService == null || !masterService.IsRegistered)
            {
                return;
            }

            await masterService.DeregisterServerAsync();
        }
    }
}
