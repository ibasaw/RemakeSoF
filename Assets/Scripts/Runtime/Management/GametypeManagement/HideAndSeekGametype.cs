using Tolik.RemakeSoF.Runtime.DataManagement;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Phase des Hide-and-Seek-Rundenablaufs.
    /// </summary>
    public enum HideAndSeekPhase
    {
        /// <summary>Versteckphase: Hider verstecken sich, Seeker sind geblindet/eingefroren.</summary>
        Hiding,
        /// <summary>Suchphase: Seeker suchen, Hider muessen ueberleben.</summary>
        Seeking,
        /// <summary>Runde vorbei, Ergebnis wird angezeigt.</summary>
        RoundOver
    }

    /// <summary>
    /// Hide-and-Seek Gametype-Implementierung.
    ///
    /// Ablauf:
    /// 1. Spieler werden in Hider (Rot) und Seeker (Blau) aufgeteilt.
    /// 2. Versteckphase: Hider haben X Sekunden zum Verstecken, Seeker sind eingefroren.
    /// 3. Suchphase: Seeker suchen und eliminieren Hider.
    /// 4. Suchzeit laeuft ab → Hider gewinnen, oder alle Hider gefunden → Seeker gewinnen.
    /// 5. Naechste Runde: Teams werden getauscht.
    ///
    /// CVARs aus ServerConfiguration:
    /// - hideandseek_hidetime: Versteckzeit in Sekunden (Default 30).
    /// - hideandseek_seektime: Suchzeit in Sekunden (Default 120).
    /// - hideandseek_seekercount: Anzahl Seeker (Default 1).
    /// - hideandseek_hiderweapons: Hider haben Waffen (Default false).
    /// - hideandseek_seekerweapons: Seeker haben Waffen (Default true).
    /// - hideandseek_roundlimit: Max. Runden (Default 5).
    /// </summary>
    public class HideAndSeekGametype : BaseGametype
    {
        /// <inheritdoc />
        public override string GametypeId => "hideandseek";

        /// <summary>Aktuelle Phase der Runde.</summary>
        public HideAndSeekPhase CurrentPhase { get; private set; }

        /// <summary>Verbleibende Zeit in der aktuellen Phase (Sekunden).</summary>
        public float PhaseTimeRemaining { get; private set; }

        /// <summary>Anzahl lebender Hider in der aktuellen Runde.</summary>
        public int AliveHiderCount { get; internal set; }

        /// <summary>Konfigurierte Seeker-Anzahl.</summary>
        int SeekerCount => ServerConfig.hideandseek_seekercount > 0 ? ServerConfig.hideandseek_seekercount : 1;

        /// <summary>Konfigurierte Versteckzeit.</summary>
        int HideTime => ServerConfig.hideandseek_hidetime > 0 ? ServerConfig.hideandseek_hidetime : 30;

        /// <summary>Konfigurierte Suchzeit.</summary>
        int SeekTime => ServerConfig.hideandseek_seektime > 0 ? ServerConfig.hideandseek_seektime : 120;

        /// <summary>Konfiguriertes Rundenlimit.</summary>
        int RoundLimit => ServerConfig.hideandseek_roundlimit > 0 ? ServerConfig.hideandseek_roundlimit : 5;

        /// <inheritdoc />
        public override void Initialize(GametypeDefinition definition, ServerConfiguration serverConfig)
        {
            base.Initialize(definition, serverConfig);
            CurrentPhase = HideAndSeekPhase.RoundOver;
            PhaseTimeRemaining = 0f;
        }

        /// <inheritdoc />
        public override void OnRoundStart()
        {
            base.OnRoundStart();

            // Starte mit Versteckphase
            CurrentPhase = HideAndSeekPhase.Hiding;
            PhaseTimeRemaining = HideTime;

            Debug.Log($"[HideAndSeek] Runde {CurrentRound}/{RoundLimit} — Versteckphase: {HideTime}s");
        }

        /// <inheritdoc />
        public override void OnRunFrame(float deltaTime)
        {
            if (CurrentPhase == HideAndSeekPhase.RoundOver)
            {
                return;
            }

            PhaseTimeRemaining -= deltaTime;

            if (PhaseTimeRemaining <= 0f)
            {
                OnPhaseTimeExpired();
            }
        }

        /// <summary>
        /// Wird aufgerufen wenn die aktuelle Phasenzeit abgelaufen ist.
        /// </summary>
        void OnPhaseTimeExpired()
        {
            switch (CurrentPhase)
            {
                case HideAndSeekPhase.Hiding:
                    // Versteckzeit vorbei → Suchphase beginnt
                    CurrentPhase = HideAndSeekPhase.Seeking;
                    PhaseTimeRemaining = SeekTime;
                    Debug.Log($"[HideAndSeek] Suchphase beginnt! Seeker duerfen suchen. Zeit: {SeekTime}s");
                    break;

                case HideAndSeekPhase.Seeking:
                    // Suchzeit vorbei → Hider gewinnen
                    Debug.Log("[HideAndSeek] Suchzeit abgelaufen! Hider haben ueberlebt!");
                    CurrentPhase = HideAndSeekPhase.RoundOver;
                    break;
            }
        }

        /// <inheritdoc />
        public override GametypeEventResult OnClientDeath(ulong victimClientId, ulong killerClientId, GametypeTeam victimTeam, GametypeTeam killerTeam)
        {
            if (CurrentPhase != HideAndSeekPhase.Seeking)
            {
                return GametypeEventResult.None;
            }

            // Nur Hider (Red) Tode zaehlen
            if (victimTeam != GametypeTeam.Red)
            {
                return GametypeEventResult.None;
            }

            AliveHiderCount--;

            // Seeker bekommt Score fuer Fund
            GametypeEventResult result = new()
            {
                ClientScoreDelta = 1,
                ClientScoreTargetId = killerClientId
            };

            Debug.Log($"[HideAndSeek] Hider {victimClientId} gefunden! Verbleibende Hider: {AliveHiderCount}");

            // Alle Hider gefunden → Seeker gewinnen
            if (AliveHiderCount <= 0)
            {
                result.BlueTeamScoreDelta = 1;
                result.RestartRound = true;
                result.RestartDelaySeconds = 5f;
                result.BroadcastMessage = "Alle Hider gefunden! Seeker gewinnen die Runde!";
                CurrentPhase = HideAndSeekPhase.RoundOver;
                Debug.Log("[HideAndSeek] Alle Hider eliminiert! Seeker gewinnen.");
            }

            return result;
        }

        /// <inheritdoc />
        public override GametypeEventResult OnTeamEliminated(GametypeTeam eliminatedTeam)
        {
            if (CurrentPhase == HideAndSeekPhase.RoundOver)
            {
                return GametypeEventResult.None;
            }

            // Hider-Team komplett eliminiert → Seeker gewinnen
            if (eliminatedTeam == GametypeTeam.Red)
            {
                CurrentPhase = HideAndSeekPhase.RoundOver;
                return new GametypeEventResult
                {
                    BlueTeamScoreDelta = 1,
                    RestartRound = true,
                    RestartDelaySeconds = 5f,
                    BroadcastMessage = "Alle Hider eliminiert! Seeker gewinnen!"
                };
            }

            return GametypeEventResult.None;
        }

        /// <inheritdoc />
        public override GametypeEventResult OnTimeExpired()
        {
            if (CurrentPhase == HideAndSeekPhase.RoundOver)
            {
                return GametypeEventResult.None;
            }

            // Zeit abgelaufen → Hider gewinnen, ueberlebende Hider bekommen +1 Kill
            CurrentPhase = HideAndSeekPhase.RoundOver;
            return new GametypeEventResult
            {
                RedTeamScoreDelta = 1,
                RestartRound = true,
                RestartDelaySeconds = 5f,
                BroadcastMessage = "Zeit abgelaufen! Hider gewinnen die Runde!",
                AwardSurvivalKillsToTeam = GametypeTeam.Red
            };
        }

        /// <inheritdoc />
        public override uint GetRoundTimeLimit()
        {
            // Gesamte Rundenzeit = Versteckzeit + Suchzeit
            return (uint)(HideTime + SeekTime);
        }

        /// <inheritdoc />
        public override bool AllowRespawn()
        {
            // Kein Respawning in Hide and Seek
            return false;
        }

        /// <inheritdoc />
        public override float GetPhaseTimeRemaining()
        {
            return PhaseTimeRemaining;
        }

        /// <inheritdoc />
        public override void OnRoundEnd()
        {
            base.OnRoundEnd();
            CurrentPhase = HideAndSeekPhase.RoundOver;
        }

        /// <inheritdoc />
        public override bool IsRoundLimitReached()
        {
            return RoundLimit > 0 && CurrentRound >= RoundLimit;
        }

        /// <summary>
        /// Gibt zurueck ob Seeker in der aktuellen Phase eingefroren sein sollen.
        /// True waehrend der Versteckphase.
        /// </summary>
        public bool AreSeekersFrozen()
        {
            return CurrentPhase == HideAndSeekPhase.Hiding;
        }

        /// <summary>
        /// Gibt zurueck ob Hider Waffen haben duerfen (aus Server-Config).
        /// </summary>
        public bool HidersHaveWeapons()
        {
            return ServerConfig.hideandseek_hiderweapons;
        }

        /// <summary>
        /// Gibt zurueck ob Seeker Waffen haben duerfen (aus Server-Config).
        /// </summary>
        public bool SeekersHaveWeapons()
        {
            return ServerConfig.hideandseek_seekerweapons;
        }

        /// <inheritdoc />
        public override GametypeTeam AssignTeam(int currentRedCount, int currentBlueCount)
        {
            // Seeker (Blau) zuerst bis SeekerCount erreicht, dann Hider (Rot)
            if (currentBlueCount < SeekerCount)
            {
                return GametypeTeam.Blue;
            }

            return GametypeTeam.Red;
        }

        /// <inheritdoc />
        public override string[] GetStartWeapons(GametypeTeam team)
        {
            // Alle Spieler starten nur mit Knife
            return new[] { "knife" };
        }

        /// <inheritdoc />
        public override int GetCurrentPhase()
        {
            return (int)CurrentPhase;
        }

        /// <inheritdoc />
        public override void InitializeRoundState(int redCount, int blueCount)
        {
            AliveHiderCount = redCount;
            Debug.Log($"[HideAndSeek] InitializeRoundState: {blueCount} Seeker, {redCount} Hider (AliveHiderCount={AliveHiderCount})");
        }

        /// <inheritdoc />
        public override bool AreTeamsReady(int currentRedCount, int currentBlueCount)
        {
            // HideAndSeek benoetigt mindestens 1 Seeker (Blau) UND 1 Hider (Rot)
            return currentBlueCount >= 1 && currentRedCount >= 1;
        }

        /// <inheritdoc />
        public override int GetRoundLimit()
        {
            return RoundLimit;
        }

        /// <summary>Stun-Dauer in Sekunden wenn ein Hider einen Seeker mit dem Messer trifft.</summary>
        const float STUN_DURATION = 2f;

        /// <inheritdoc />
        public override GametypeDamageResult OnDamage(ulong attackerClientId, ulong victimClientId, GametypeTeam attackerTeam, GametypeTeam victimTeam, int damage, string weaponName)
        {
            // Nur waehrend der Suchphase relevant
            if (CurrentPhase != HideAndSeekPhase.Seeking)
            {
                return GametypeDamageResult.Default(0);
            }

            // Seeker (Blue) trifft Hider (Red) → Instant Kill (volle HP als Schaden)
            if (attackerTeam == GametypeTeam.Blue && victimTeam == GametypeTeam.Red)
            {
                return new GametypeDamageResult
                {
                    ModifiedDamage = 1000,
                    ApplyStun = false,
                    StunDuration = 0f,
                    AttackerMessage = "You killed {victimName}!",
                    VictimMessage = "You got killed by {attackerName}"
                };
            }

            // Hider (Red) trifft Seeker (Blue) → Kein Schaden, Stun stattdessen
            if (attackerTeam == GametypeTeam.Red && victimTeam == GametypeTeam.Blue)
            {
                return new GametypeDamageResult
                {
                    ModifiedDamage = 0,
                    ApplyStun = true,
                    StunDuration = STUN_DURATION,
                    AttackerMessage = "You stunned {victimName}",
                    VictimMessage = "You got stunned by {attackerName}"
                };
            }

            return GametypeDamageResult.Default(damage);
        }
    }
}
