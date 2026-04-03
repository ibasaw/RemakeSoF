using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Team Deathmatch Gametype-Implementierung.
    /// Zwei Teams, Kills fuer das Gegner-Team zaehlen als Team-Score.
    /// </summary>
    public class TeamDeathmatchGametype : BaseGametype
    {
        /// <inheritdoc />
        public override string GametypeId => "tdm";

        /// <inheritdoc />
        public override GametypeEventResult OnClientDeath(ulong victimClientId, ulong killerClientId, GametypeTeam victimTeam, GametypeTeam killerTeam)
        {
            // Selbstmord oder Teamkill zaehlen nicht
            if (killerClientId == victimClientId || killerTeam == victimTeam)
            {
                return GametypeEventResult.None;
            }

            GametypeEventResult result = new()
            {
                ClientScoreDelta = 1,
                ClientScoreTargetId = killerClientId
            };

            // Team-Score fuer das Team des Killers
            if (killerTeam == GametypeTeam.Red)
            {
                result.RedTeamScoreDelta = 1;
            }
            else if (killerTeam == GametypeTeam.Blue)
            {
                result.BlueTeamScoreDelta = 1;
            }

            return result;
        }

        /// <inheritdoc />
        public override GametypeEventResult OnTimeExpired()
        {
            return GametypeEventResult.WithRestart("Zeitlimit erreicht!");
        }

        /// <inheritdoc />
        public override bool AllowRespawn()
        {
            return true;
        }
    }
}
