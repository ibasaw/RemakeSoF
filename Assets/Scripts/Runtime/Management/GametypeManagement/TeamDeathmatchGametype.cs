using Tolik.RemakeSoF.Runtime.Game.Networked;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.GametypeManagement
{
    /// <summary>
    /// Team Deathmatch Gametype-Implementierung.
    /// Zwei Teams kaempfen gegeneinander. Das Team das zuerst das Fraglimit erreicht gewinnt.
    /// Falls Fraglimit 0 ist, gewinnt das Team mit den meisten Kills bei Zeitablauf.
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

            // Fraglimit-Check: Aktuellen Score aus NetworkedGameState + Delta pruefen
            int fraglimit = ServerConfig.fraglimit;
            if (fraglimit > 0 && NetworkedGameState.Singleton != null)
            {
                int newRedScore = NetworkedGameState.Singleton.redTeamScore.Value + result.RedTeamScoreDelta;
                int newBlueScore = NetworkedGameState.Singleton.blueTeamScore.Value + result.BlueTeamScoreDelta;

                if (newRedScore >= fraglimit)
                {
                    result.RestartRound = true;
                    result.RestartDelaySeconds = 5f;
                    result.BroadcastMessage = "Team Red gewinnt!";
                    Debug.Log($"[TDM] Fraglimit erreicht! Team Red gewinnt mit {newRedScore} Kills.");
                }
                else if (newBlueScore >= fraglimit)
                {
                    result.RestartRound = true;
                    result.RestartDelaySeconds = 5f;
                    result.BroadcastMessage = "Team Blue gewinnt!";
                    Debug.Log($"[TDM] Fraglimit erreicht! Team Blue gewinnt mit {newBlueScore} Kills.");
                }
            }

            return result;
        }

        /// <inheritdoc />
        public override GametypeEventResult OnTimeExpired()
        {
            // Gewinner-Bestimmung anhand der aktuellen Team-Scores
            if (NetworkedGameState.Singleton != null)
            {
                int redScore = NetworkedGameState.Singleton.redTeamScore.Value;
                int blueScore = NetworkedGameState.Singleton.blueTeamScore.Value;

                if (redScore > blueScore)
                {
                    Debug.Log($"[TDM] Zeitlimit abgelaufen. Team Red gewinnt {redScore}:{blueScore}.");
                    return GametypeEventResult.WithRestart($"Zeitlimit erreicht! Team Red gewinnt {redScore}:{blueScore}!");
                }

                if (blueScore > redScore)
                {
                    Debug.Log($"[TDM] Zeitlimit abgelaufen. Team Blue gewinnt {blueScore}:{redScore}.");
                    return GametypeEventResult.WithRestart($"Zeitlimit erreicht! Team Blue gewinnt {blueScore}:{redScore}!");
                }

                Debug.Log($"[TDM] Zeitlimit abgelaufen. Unentschieden {redScore}:{blueScore}.");
                return GametypeEventResult.WithRestart($"Zeitlimit erreicht! Unentschieden {redScore}:{blueScore}!");
            }

            return GametypeEventResult.WithRestart("Zeitlimit erreicht!");
        }

        /// <inheritdoc />
        public override bool AllowRespawn()
        {
            return true;
        }
    }
}
