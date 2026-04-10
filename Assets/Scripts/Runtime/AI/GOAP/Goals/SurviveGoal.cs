using CrashKonijn.Goap.Runtime;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Hider-Hauptziel: Ueberleben / Sicher sein.
    /// Bedingung (IsSafe >= 1) wird nie durch Sensoren erfuellt,
    /// darum plant der GOAP-Planner immer eine Aktion:
    /// FleeAction (wenn Spieler sichtbar) oder WanderAction (Fallback).
    /// </summary>
    public class SurviveGoal : GoalBase
    {
    }
}
