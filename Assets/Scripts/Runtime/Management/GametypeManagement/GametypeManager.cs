using Tolik.RemakeSoF.Runtime.ApplicationLifecycle;
using Tolik.RemakeSoF.Runtime.DataManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Zentraler Manager fuer Gametype-Logik. Pure Service, registriert im ServiceLocator.
    /// Haelt die aktive IGametype-Implementierung und delegiert Events von der
    /// RoundFlowStateMachine an den konkreten Gametype.
    ///
    /// Verantwortung:
    /// - Gametype-Instanz erstellen und initialisieren basierend auf g_gametype aus ServerConfiguration.
    /// - Runden-Lifecycle delegieren (Start, Frame, Ende).
    /// - Gametype-Events verarbeiten (Tod, Team-Elimination, Zeitablauf).
    /// - Single Source of Truth fuer den aktiven Gametype.
    /// </summary>
    public class GametypeManager
    {
        /// <summary>Die aktive Gametype-Implementierung.</summary>
        IGametype m_ActiveGametype;

        /// <summary>Die Server-Konfiguration.</summary>
        ServerConfiguration m_ServerConfig;

        /// <summary>Der Gametype-Definition-Loader.</summary>
        GametypeDefinitionLoader m_DefinitionLoader;

        /// <summary>Zugriff auf den aktiven Gametype (readonly).</summary>
        public IGametype ActiveGametype => m_ActiveGametype;

        /// <summary>ID des aktiven Gametypes.</summary>
        public string ActiveGametypeId => m_ActiveGametype?.GametypeId ?? "none";

        /// <summary>
        /// Konstruktor: Laedt Gametype basierend auf Server-Konfiguration.
        /// </summary>
        public GametypeManager()
        {
            ServerConfigurationLoader configLoader = ServiceLocator.Get<ServerConfigurationLoader>();
            m_DefinitionLoader = ServiceLocator.Get<GametypeDefinitionLoader>();

            if (configLoader == null || m_DefinitionLoader == null)
            {
                Debug.LogError("[GametypeManager] ServerConfigurationLoader oder GametypeDefinitionLoader nicht im ServiceLocator registriert!");
                return;
            }

            m_ServerConfig = configLoader.Configuration;
            InitializeGametype(m_ServerConfig.g_gametype);
        }

        /// <summary>
        /// Erstellt und initialisiert den Gametype fuer den angegebenen Identifier.
        /// </summary>
        /// <param name="gametypeId">Gametype-Identifier (z.B. "hideandseek").</param>
        void InitializeGametype(string gametypeId)
        {
            GametypeDefinition definition = m_DefinitionLoader.GetByGametypeId(gametypeId);
            if (definition == null)
            {
                Debug.LogError($"[GametypeManager] GametypeDefinition nicht gefunden: '{gametypeId}'");
                return;
            }

            m_ActiveGametype = GametypeFactory.Create(gametypeId);
            if (m_ActiveGametype == null)
            {
                Debug.LogError($"[GametypeManager] Gametype konnte nicht erstellt werden: '{gametypeId}'");
                return;
            }

            m_ActiveGametype.Initialize(definition, m_ServerConfig);
            Debug.Log($"[GametypeManager] Aktiver Gametype: {definition.gametypeName} ({gametypeId})");
        }

        /// <summary>
        /// Wechselt den aktiven Gametype zur Laufzeit (z.B. per Console-Kommando).
        /// </summary>
        /// <param name="gametypeId">Neuer Gametype-Identifier.</param>
        /// <returns>True wenn erfolgreich gewechselt.</returns>
        public bool ChangeGametype(string gametypeId)
        {
            GametypeDefinition definition = m_DefinitionLoader.GetByGametypeId(gametypeId);
            if (definition == null)
            {
                Debug.LogWarning($"[GametypeManager] Unbekannter Gametype: '{gametypeId}'");
                return false;
            }

            m_ActiveGametype?.OnRoundEnd();
            InitializeGametype(gametypeId);
            return true;
        }

        // ── Delegation an aktiven Gametype ──────────────────────────────

        /// <summary>Delegiert Rundenstart an den aktiven Gametype.</summary>
        public void OnRoundStart()
        {
            m_ActiveGametype?.OnRoundStart();
        }

        /// <summary>Delegiert Frame-Update an den aktiven Gametype.</summary>
        /// <param name="deltaTime">Vergangene Zeit seit letztem Frame.</param>
        public void OnRunFrame(float deltaTime)
        {
            m_ActiveGametype?.OnRunFrame(deltaTime);
        }

        /// <summary>Delegiert Client-Tod an den aktiven Gametype.</summary>
        public GametypeEventResult OnClientDeath(ulong victimClientId, ulong killerClientId, GametypeTeam victimTeam, GametypeTeam killerTeam)
        {
            if (m_ActiveGametype == null)
            {
                return GametypeEventResult.None;
            }

            return m_ActiveGametype.OnClientDeath(victimClientId, killerClientId, victimTeam, killerTeam);
        }

        /// <summary>Delegiert Team-Elimination an den aktiven Gametype.</summary>
        public GametypeEventResult OnTeamEliminated(GametypeTeam eliminatedTeam)
        {
            if (m_ActiveGametype == null)
            {
                return GametypeEventResult.None;
            }

            return m_ActiveGametype.OnTeamEliminated(eliminatedTeam);
        }

        /// <summary>Delegiert Zeitablauf an den aktiven Gametype.</summary>
        public GametypeEventResult OnTimeExpired()
        {
            if (m_ActiveGametype == null)
            {
                return GametypeEventResult.None;
            }

            return m_ActiveGametype.OnTimeExpired();
        }

        /// <summary>Gibt die Countdown-Dauer fuer die aktuelle Runde zurueck.</summary>
        public uint GetRoundTimeLimit()
        {
            return m_ActiveGametype?.GetRoundTimeLimit() ?? 300;
        }

        /// <summary>Gibt zurueck ob Respawning aktuell erlaubt ist.</summary>
        public bool AllowRespawn()
        {
            return m_ActiveGametype?.AllowRespawn() ?? true;
        }

        /// <summary>Delegiert Rundenende an den aktiven Gametype.</summary>
        public void OnRoundEnd()
        {
            m_ActiveGametype?.OnRoundEnd();
        }
    }
}
