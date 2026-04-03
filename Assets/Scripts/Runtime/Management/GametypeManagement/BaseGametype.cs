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
        public virtual void OnRoundEnd()
        {
            Debug.Log($"[{GametypeId}] Runde {CurrentRound} beendet. Score: Rot={RedTeamScore} Blau={BlueTeamScore}");
        }
    }
}
