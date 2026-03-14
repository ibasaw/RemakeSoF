namespace Tolik.RemakeSoF.Runtime.Game.Characters.Shared
{
    /// <summary>
    /// Phasen fuer den Waffen-Wechsel (Drop alte Waffe, Raise neue Waffe).
    /// Drop: Alte Waffe wird weggesteckt (mp_drop). Raise: Neue Waffe wird gezogen (mp_raise).
    /// </summary>
    public enum WeaponSwapPhase
    {
        /// <summary>Kein Waffenwechsel aktiv.</summary>
        None,

        /// <summary>Alte Waffe wird weggesteckt (mp_drop Animation).</summary>
        Drop,

        /// <summary>Neue Waffe wird gezogen (mp_raise Animation).</summary>
        Raise
    }
}
