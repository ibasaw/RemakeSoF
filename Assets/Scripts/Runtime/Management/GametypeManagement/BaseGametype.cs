using Tolik.RemakeSoF.Runtime.DataManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Abstrakte Basis fuer alle Gametype-Implementierungen.
    /// Stellt gemeinsame Felder und Default-Verhalten bereit.
    /// Konkrete Gametypes ueberschreiben nur was sie brauchen.
    /// </summary>
    public abstract class BaseGametype : IGametype
    {
        /// <inheritdoc />
        public abstract string GametypeId { get; }

        /// <inheritdoc />
        public GametypeDefinition Definition { get; private set; }

        /// <inheritdoc />
        public ServerConfiguration ServerConfig { get; private set; }

        /// <summary>Aktuelle Rundennummer (1-basiert).</summary>
        protected int CurrentRound { get; set; }

        /// <summary>Team-Score Rot.</summary>
        protected int RedTeamScore { get; set; }

        /// <summary>Team-Score Blau.</summary>
        protected int BlueTeamScore { get; set; }

        /// <inheritdoc />
        public virtual void Initialize(GametypeDefinition definition, ServerConfiguration serverConfig)
        {
            Definition = definition;
            ServerConfig = serverConfig;
            CurrentRound = 0;
            RedTeamScore = 0;
            BlueTeamScore = 0;
            Debug.Log($"[{GametypeId}] Gametype initialisiert: {definition.gametypeName}");
        }

        /// <inheritdoc />
        public virtual void OnRoundStart()
        {
            CurrentRound++;
            Debug.Log($"[{GametypeId}] Runde {CurrentRound} gestartet.");
        }

        /// <inheritdoc />
        public virtual void OnRunFrame(float deltaTime) { }

        /// <inheritdoc />
        public virtual GametypeEventResult OnClientDeath(ulong victimClientId, ulong killerClientId, GametypeTeam victimTeam, GametypeTeam killerTeam)
        {
            return GametypeEventResult.None;
        }

        /// <inheritdoc />
        public virtual GametypeEventResult OnTeamEliminated(GametypeTeam eliminatedTeam)
        {
            return GametypeEventResult.None;
        }

        /// <inheritdoc />
        public virtual GametypeEventResult OnTimeExpired()
        {
            return GametypeEventResult.None;
        }

        /// <inheritdoc />
        public virtual uint GetRoundTimeLimit()
        {
            RespawnType respawnType = Definition.GetRespawnType();
            if (respawnType == RespawnType.None)
            {
                return ServerConfig.roundtimelimit > 0 ? (uint)ServerConfig.roundtimelimit : 180;
            }

            return ServerConfig.timelimit > 0 ? (uint)ServerConfig.timelimit : 300;
        }

        /// <inheritdoc />
        public virtual bool AllowRespawn()
        {
            return Definition.GetRespawnType() != RespawnType.None;
        }

        /// <inheritdoc />
        public virtual GametypeTeam AssignTeam(int currentRedCount, int currentBlueCount)
        {
            // Default: Balance-Zuweisung (kleineres Team bevorzugen)
            return currentRedCount <= currentBlueCount ? GametypeTeam.Red : GametypeTeam.Blue;
        }

        /// <inheritdoc />
        public virtual string[] GetStartWeapons(GametypeTeam team)
        {
            // Null = Default-Waffenset (alle Waffen) in ServerListeningState verwenden
            return null;
        }

        /// <inheritdoc />
        public virtual int GetCurrentPhase()
        {
            return 0;
        }

        /// <inheritdoc />
        public virtual void InitializeRoundState(int redCount, int blueCount)
        {
            // Default: keine Aktion
        }

        /// <inheritdoc />
        public virtual void OnRoundEnd()
        {
            Debug.Log($"[{GametypeId}] Runde {CurrentRound} beendet. Score: Rot={RedTeamScore} Blau={BlueTeamScore}");
        }

        /// <inheritdoc />
        public virtual bool AreTeamsReady(int currentRedCount, int currentBlueCount)
        {
            // Default: Keine Team-Anforderungen, jeder Spieler reicht.
            return true;
        }
    }
}
