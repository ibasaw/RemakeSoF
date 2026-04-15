namespace Tolik.RemakeSoF.Runtime
{
    internal class ResumeButtonClickedEvent : AppEvent { }

    internal class QuitButtonClickedEvent : AppEvent { }

    internal class MatchEndAcknowledgedEvent : AppEvent { }

    internal class StartMatchEvent : AppEvent { }

    internal class MenuToggleEvent : AppEvent { }

    internal class EndMatchEvent : AppEvent { }

    /// <summary>
    /// Wird gesendet wenn der Spieler die Scoreboard-Taste drueckt (started).
    /// </summary>
    internal class ScoreboardShowEvent : AppEvent { }

    /// <summary>
    /// Wird gesendet wenn der Spieler die Scoreboard-Taste loslaesst (canceled).
    /// </summary>
    internal class ScoreboardHideEvent : AppEvent { }

    /// <summary>
    /// Broadcasted by the server when a player is killed. Displayed in the chat HUD as a kill feed line.
    /// </summary>
    internal class KillFeedReceivedEvent : AppEvent
    {
        public string killerName;
        public string victimName;
        public string weaponName;
        public string hitRegion;
    }
}
