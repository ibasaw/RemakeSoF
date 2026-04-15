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
            // Team-spezifische Waffen aus Server-Config lesen
            string[] configured = team == GametypeTeam.Red
                ? ServerConfig?.g_redTeamStartWeapons
                : ServerConfig?.g_blueTeamStartWeapons;

            // Leer oder null = nur Knife
            if (configured == null || configured.Length == 0)
            {
                return new[] { "knife" };
            }

            // Knife ist immer dabei, deduplizieren falls bereits enthalten
            bool hasKnife = System.Array.Exists(configured, w => w == "knife");
            if (hasKnife)
            {
                return configured;
            }

            string[] withKnife = new string[configured.Length + 1];
            withKnife[0] = "knife";
            System.Array.Copy(configured, 0, withKnife, 1, configured.Length);
            return withKnife;
        }

        /// <inheritdoc />
        public virtual (int clip, int reserve, int altClip, int altReserve)? GetStartAmmo(string weaponName)
        {
            // Null = Standard-Ammo aus Waffendaten verwenden
            return null;
        }

        /// <inheritdoc />
        public virtual int GetCurrentPhase()
        {
            return 0;
        }

        /// <inheritdoc />
        public virtual float GetPhaseTimeRemaining()
        {
            return 0f;
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

        /// <inheritdoc />
        public virtual int GetCurrentRound()
        {
            return CurrentRound;
        }

        /// <inheritdoc />
        public virtual int GetRoundLimit()
        {
            return ServerConfig.roundlimit;
        }

        /// <inheritdoc />
        public virtual bool IsRoundLimitReached()
        {
            int roundLimit = GetRoundLimit();
            return roundLimit > 0 && CurrentRound >= roundLimit;
        }

        /// <inheritdoc />
        public virtual int GetTimelimit()
        {
            return ServerConfig.timelimit;
        }

        /// <inheritdoc />
        public virtual bool ShouldBotUseCombatAI(GametypeTeam team)
        {
            // Default: Alle Bots verwenden Kampf-KI (Patrol/Chase/Shoot).
            return true;
        }

        /// <inheritdoc />
        public virtual GametypeDamageResult OnDamage(ulong attackerClientId, ulong victimClientId, GametypeTeam attackerTeam, GametypeTeam victimTeam, int damage, string weaponName, bool isAltAttack = false)
        {
            // Default: Schaden unveraendert durchlassen.
            return GametypeDamageResult.Default(damage);
        }

        /// <inheritdoc />
        public virtual void OnProjectileDetonated(string weaponName, bool isAltAttack, Vector3 position, Vector3 normal)
        {
            // Default: keine Post-Detonation-Aktion.
        }
    }
}
