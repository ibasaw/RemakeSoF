using CrashKonijn.Goap.Core;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>Spieler ist per Sensor sichtbar (0 = nicht sichtbar, 1 = sichtbar).</summary>
    public class IsPlayerVisible : IWorldKey { public string Name => nameof(IsPlayerVisible); }

    /// <summary>Spieler ist in Waffenreichweite (0 = ausser Reichweite, 1 = in Reichweite).</summary>
    public class IsPlayerInRange : IWorldKey { public string Name => nameof(IsPlayerInRange); }

    /// <summary>Bot hat Munition (0 = leer, 1 = geladen).</summary>
    public class HasAmmo : IWorldKey { public string Name => nameof(HasAmmo); }

    /// <summary>Feind eliminiert — wird nie von Sensoren erfuellt (immer 0). Dient als perpetuelles Seeker-Ziel.</summary>
    public class EnemyDown : IWorldKey { public string Name => nameof(EnemyDown); }

    /// <summary>Bot ist sicher — wird nie von Sensoren erfuellt (immer 0). Dient als perpetuelles Hider-Ziel.</summary>
    public class IsSafe : IWorldKey { public string Name => nameof(IsSafe); }
}
