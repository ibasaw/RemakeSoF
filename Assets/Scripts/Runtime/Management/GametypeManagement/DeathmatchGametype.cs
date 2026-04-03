using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Deathmatch Gametype-Implementierung (Free-for-all).
    /// Simpelster Modus: Kills zaehlen, hoechster Score gewinnt.
    /// </summary>
    public class DeathmatchGametype : BaseGametype
    {
        /// <inheritdoc />
        public override string GametypeId => "dm";

        /// <inheritdoc />
        public override GametypeEventResult OnClientDeath(ulong victimClientId, ulong killerClientId, GametypeTeam victimTeam, GametypeTeam killerTeam)
        {
            if (killerClientId == victimClientId)
            {
                return GametypeEventResult.None;
            }

            return new GametypeEventResult
            {
                ClientScoreDelta = 1,
                ClientScoreTargetId = killerClientId
            };
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
