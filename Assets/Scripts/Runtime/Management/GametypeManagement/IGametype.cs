using Tolik.RemakeSoF.Runtime.DataManagement;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Team-Zuordnung fuer rundenbasierte/team-basierte Gametypes.
    /// </summary>
    public enum GametypeTeam
    {
        /// <summary>Kein Team / Spectator.</summary>
        None = 0,
        /// <summary>Team Rot (Verteidiger / Seeker).</summary>
        Red = 1,
        /// <summary>Team Blau (Angreifer / Hider).</summary>
        Blue = 2
    }

    /// <summary>
    /// Schnittstelle fuer konkrete Gametype-Implementierungen.
    /// Jeder Gametype implementiert seine eigene Spiellogik:
    /// Runden-Start/Ende, Scoring, Todesmeldungen, Zeitablauf.
    /// Der GametypeManager orchestriert und delegiert an die aktive Implementierung.
    /// </summary>
    public interface IGametype
    {
        /// <summary>Gametype-Identifier (z.B. "tdm", "hideandseek").</summary>
        string GametypeId { get; }

        /// <summary>Die zugehoerige GametypeDefinition aus der JSON-Konfiguration.</summary>
        GametypeDefinition Definition { get; }

        /// <summary>Die aktive Server-Konfiguration fuer gametype-spezifische CVARs.</summary>
        ServerConfiguration ServerConfig { get; }

        /// <summary>
        /// Initialisiert den Gametype mit Konfiguration und Definition.
        /// Wird einmalig beim Gametype-Wechsel aufgerufen.
        /// </summary>
        /// <param name="definition">Die Gametype-Definition.</param>
        /// <param name="serverConfig">Die Server-Konfiguration.</param>
        void Initialize(GametypeDefinition definition, ServerConfiguration serverConfig);

        /// <summary>
        /// Wird beim Rundenstart aufgerufen (nach Countdown, Spieler gespawnt).
        /// Setzt rundenspezifischen State zurueck.
        /// </summary>
        void OnRoundStart();

        /// <summary>
        /// Wird jeden Server-Frame aufgerufen (entspricht GAMETYPE_RUN_FRAME).
        /// </summary>
        /// <param name="deltaTime">Vergangene Zeit seit letztem Frame in Sekunden.</param>
        void OnRunFrame(float deltaTime);

        /// <summary>
        /// Wird aufgerufen wenn ein Spieler stirbt (entspricht GTEV_CLIENT_DEATH).
        /// </summary>
        /// <param name="victimClientId">Client-ID des Opfers.</param>
        /// <param name="killerClientId">Client-ID des Toetenden (kann == victim sein bei Selbstmord).</param>
        /// <param name="victimTeam">Team des Opfers.</param>
        /// <param name="killerTeam">Team des Toetenden.</param>
        /// <returns>GametypeEventResult mit Scoring- und Runden-Aktionen.</returns>
        GametypeEventResult OnClientDeath(ulong victimClientId, ulong killerClientId, GametypeTeam victimTeam, GametypeTeam killerTeam);

        /// <summary>
        /// Wird aufgerufen wenn ein komplettes Team eliminiert wurde (entspricht GTEV_TEAM_ELIMINATED).
        /// Nur relevant fuer roundBased-Gametypes.
        /// </summary>
        /// <param name="eliminatedTeam">Das eliminierte Team.</param>
        /// <returns>GametypeEventResult mit Scoring- und Runden-Aktionen.</returns>
        GametypeEventResult OnTeamEliminated(GametypeTeam eliminatedTeam);

        /// <summary>
        /// Wird aufgerufen wenn die Rundenzeit abgelaufen ist (entspricht GTEV_TIME_EXPIRED).
        /// </summary>
        /// <returns>GametypeEventResult mit Scoring- und Runden-Aktionen.</returns>
        GametypeEventResult OnTimeExpired();

        /// <summary>
        /// Gibt die Countdown-Dauer fuer diese Runde zurueck (in Sekunden).
        /// Kann je nach Gametype-Phase variieren (z.B. HideTime vs SeekTime).
        /// </summary>
        uint GetRoundTimeLimit();

        /// <summary>
        /// Gibt zurueck ob der Gametype aktuell Respawning erlaubt.
        /// </summary>
        bool AllowRespawn();

        /// <summary>
        /// Wird beim Rundenende aufgerufen fuer Cleanup.
        /// </summary>
        void OnRoundEnd();
    }
}
