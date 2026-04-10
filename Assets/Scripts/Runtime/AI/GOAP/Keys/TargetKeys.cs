using CrashKonijn.Goap.Core;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>Position des naechsten sichtbaren Spielers (Sensor-basiert).</summary>
    public class PlayerTargetKey : ITargetKey { public string Name => nameof(PlayerTargetKey); }

    /// <summary>Position des naechsten uneroberten Checkpoints.</summary>
    public class CheckpointTargetKey : ITargetKey { public string Name => nameof(CheckpointTargetKey); }

    /// <summary>Fluchtposition weg vom naechsten Spieler.</summary>
    public class FleeTargetKey : ITargetKey { public string Name => nameof(FleeTargetKey); }

    /// <summary>Zufaellige Wander-Position in der Naehe des Bots.</summary>
    public class WanderTargetKey : ITargetKey { public string Name => nameof(WanderTargetKey); }
}
