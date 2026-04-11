using UnityEngine;
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
        /// <summary>Team Rot (Hider).</summary>
        Red = 1,
        /// <summary>Team Blau (Seeker).</summary>
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
        /// Weist einem neuen Spieler ein Team zu basierend auf aktueller Teambalance.
        /// </summary>
        /// <param name="currentRedCount">Aktuelle Anzahl Spieler in Team Rot.</param>
        /// <param name="currentBlueCount">Aktuelle Anzahl Spieler in Team Blau.</param>
        /// <returns>Das zugewiesene Team.</returns>
        GametypeTeam AssignTeam(int currentRedCount, int currentBlueCount);

        /// <summary>
        /// Gibt die Start-Waffen fuer ein Team zurueck.
        /// Null = Default-Waffenset verwenden (alle Waffen).
        /// </summary>
        /// <param name="team">Das Team des Spielers.</param>
        /// <returns>Array von Waffennamen oder null fuer Default.</returns>
        string[] GetStartWeapons(GametypeTeam team);

        /// <summary>
        /// Gibt gametype-spezifische Ammo-Overrides fuer eine Waffe zurueck.
        /// Null = Standard-Ammo aus den Waffendaten verwenden.
        /// Ermoeglicht Gametypes die Start-Munition pro Waffe zu limitieren.
        /// </summary>
        /// <param name="weaponName">ID der Waffe (z.B. "knife", "m4").</param>
        /// <returns>Tuple (clip, reserve, altClip, altReserve) oder null fuer Default.</returns>
        (int clip, int reserve, int altClip, int altReserve)? GetStartAmmo(string weaponName);

        /// <summary>
        /// Gibt die aktuelle gametype-spezifische Phase als int zurueck.
        /// Wird als NetworkVariable an Clients synchronisiert.
        /// 0 = keine Phase / Standard.
        /// </summary>
        int GetCurrentPhase();

        /// <summary>
        /// Gibt die verbleibende Zeit der aktuellen Phase in Sekunden zurueck.
        /// Fuer HideAndSeek: Versteckzeit waehrend Hiding, Suchzeit waehrend Seeking.
        /// Default: 0 (keine Phase-Timer).
        /// </summary>
        float GetPhaseTimeRemaining();

        /// <summary>
        /// Initialisiert den Runden-State mit aktuellen Teamgroessen.
        /// Wird vor OnRoundStart aufgerufen (z.B. AliveHiderCount setzen).
        /// </summary>
        /// <param name="redCount">Anzahl Spieler in Team Rot.</param>
        /// <param name="blueCount">Anzahl Spieler in Team Blau.</param>
        void InitializeRoundState(int redCount, int blueCount);

        /// <summary>
        /// Wird beim Rundenende aufgerufen fuer Cleanup.
        /// </summary>
        void OnRoundEnd();

        /// <summary>
        /// Prueft ob die Team-Anforderungen fuer den Rundenstart erfuellt sind.
        /// Fuer team-basierte Gametypes (z.B. HideAndSeek): Beide Teams muessen besetzt sein.
        /// Fuer FFA-Gametypes: Default true.
        /// </summary>
        /// <param name="currentRedCount">Aktuelle Anzahl Spieler in Team Rot.</param>
        /// <param name="currentBlueCount">Aktuelle Anzahl Spieler in Team Blau.</param>
        /// <returns>True wenn genug Spieler in den benoetigten Teams sind.</returns>
        bool AreTeamsReady(int currentRedCount, int currentBlueCount);

        /// <summary>
        /// Gibt die aktuelle Rundennummer zurueck (1-basiert).
        /// </summary>
        int GetCurrentRound();

        /// <summary>
        /// Gibt das Rundenlimit zurueck.
        /// 0 = unendlich viele Runden.
        /// </summary>
        int GetRoundLimit();

        /// <summary>
        /// Prueft ob das Rundenlimit erreicht ist.
        /// Bei roundlimit == 0 wird false zurueckgegeben (unendlich viele Runden).
        /// </summary>
        /// <returns>True wenn die maximale Rundenzahl erreicht oder ueberschritten ist.</returns>
        bool IsRoundLimitReached();

        /// <summary>
        /// Gibt das Match-Timelimit in Sekunden zurueck (fuer Map-Wechsel bei roundlimit == 0).
        /// 0 = kein Timelimit (unendlich).
        /// </summary>
        int GetTimelimit();

        /// <summary>
        /// Wird aufgerufen bevor Schaden angewendet wird.
        /// Ermoeglicht dem Gametype den Schaden zu modifizieren, zu blockieren
        /// oder Zusatzeffekte (Stun, Nachrichten) auszuloesen.
        /// </summary>
        /// <param name="attackerClientId">Client-ID des Angreifers.</param>
        /// <param name="victimClientId">Client-ID des Opfers.</param>
        /// <param name="attackerTeam">Team des Angreifers.</param>
        /// <param name="victimTeam">Team des Opfers.</param>
        /// <param name="damage">Berechneter Schaden.</param>
        /// <param name="weaponName">Name der verwendeten Waffe.</param>
        /// <param name="isAltAttack">Ob der Schaden von einem Alt-Angriff stammt (z.B. Messer-Wurf).</param>
        /// <returns>GametypeDamageResult mit modifiziertem Schaden und optionalen Effekten.</returns>
        GametypeDamageResult OnDamage(ulong attackerClientId, ulong victimClientId, GametypeTeam attackerTeam, GametypeTeam victimTeam, int damage, string weaponName, bool isAltAttack = false);

        /// <summary>
        /// Wird nach Projektil-Detonation aufgerufen.
        /// Ermoeglicht dem Gametype, Post-Detonation-Aktionen auszufuehren
        /// (z.B. Barrier-Spawn statt Explosion in HideAndSeek).
        /// </summary>
        /// <param name="weaponName">Name der Waffe die das Projektil abgefeuert hat.</param>
        /// <param name="isAltAttack">Ob das Projektil von einem Alt-Angriff stammt.</param>
        /// <param name="position">Detonationspunkt in Weltkoordinaten.</param>
        /// <param name="normal">Oberflaechennormale am Auftreffpunkt.</param>
        void OnProjectileDetonated(string weaponName, bool isAltAttack, Vector3 position, Vector3 normal);
    }
}
