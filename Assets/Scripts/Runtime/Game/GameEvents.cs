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
}
