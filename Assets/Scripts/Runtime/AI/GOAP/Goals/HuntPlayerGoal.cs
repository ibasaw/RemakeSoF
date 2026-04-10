using CrashKonijn.Goap.Runtime;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Seeker-Hauptziel: Spieler eliminieren.
    /// Bedingung (EnemyDown >= 1) wird nie durch Sensoren erfuellt,
    /// darum plant der GOAP-Planner immer eine Aktionskette:
    /// PatrolAction → ChasePlayerAction → ShootPlayerAction.
    /// </summary>
    public class HuntPlayerGoal : GoalBase
    {
    }
}
