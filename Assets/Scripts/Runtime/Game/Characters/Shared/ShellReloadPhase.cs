namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// Phasen fuer Shell-by-Shell Reload (M590, MM1).
    /// Start: Waffe oeffnen. Shell: Einzelne Shell laden (wiederholbar). End: Waffe schliessen.
    /// </summary>
    public enum ShellReloadPhase
    {
        /// <summary>Kein Shell-Reload aktiv (normaler Reload oder kein Reload).</summary>
        None,

        /// <summary>Reload-Start-Animation (Waffe oeffnen / vorbereiten).</summary>
        Start,

        /// <summary>Einzelne Shell laden (wird pro Shell wiederholt).</summary>
        Shell,

        /// <summary>Reload-End-Animation (Waffe schliessen / fertigstellen).</summary>
        End
    }
}
