namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Ergebnis eines Gametype-Events (Tod, Team-Elimination, Zeitablauf).
    /// Enthaelt Scoring-Aktionen und Runden-Steuerung.
    /// </summary>
    public struct GametypeEventResult
    {
        /// <summary>Team-Score-Aenderung fuer Team Rot.</summary>
        public int RedTeamScoreDelta;

        /// <summary>Team-Score-Aenderung fuer Team Blau.</summary>
        public int BlueTeamScoreDelta;

        /// <summary>Spieler-Score-Aenderung fuer einen bestimmten Client.</summary>
        public int ClientScoreDelta;

        /// <summary>Client-ID auf den sich ClientScoreDelta bezieht.</summary>
        public ulong ClientScoreTargetId;

        /// <summary>Ob die Runde nach diesem Event neu gestartet werden soll.</summary>
        public bool RestartRound;

        /// <summary>Delay in Sekunden vor dem Runden-Restart.</summary>
        public float RestartDelaySeconds;

        /// <summary>Broadcast-Nachricht fuer alle Clients (leer = keine Nachricht).</summary>
        public string BroadcastMessage;

        /// <summary>
        /// Team dessen ueberlebende (alive) Spieler jeweils +1 Kill erhalten.
        /// GametypeTeam.None = keine Survival-Kills vergeben.
        /// </summary>
        public GametypeTeam AwardSurvivalKillsToTeam;

        /// <summary>Erstellt ein leeres Ergebnis ohne Aktionen.</summary>
        public static GametypeEventResult None => new();

        /// <summary>Erstellt ein Ergebnis das einen Runden-Restart ausloest.</summary>
        /// <param name="message">Broadcast-Nachricht.</param>
        /// <param name="delaySeconds">Delay vor Restart.</param>
        public static GametypeEventResult WithRestart(string message, float delaySeconds = 5f)
        {
            return new GametypeEventResult
            {
                RestartRound = true,
                RestartDelaySeconds = delaySeconds,
                BroadcastMessage = message
            };
        }
    }
}
