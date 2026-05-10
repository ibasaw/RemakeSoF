using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Runtime;
using Tolik.RemakeSoF.Runtime.Game.Characters.Networked;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Hider-Hauptziel: Ueberleben / Sicher sein.
    /// Bedingung (IsSafe >= 1) wird nie durch Sensoren erfuellt,
    /// darum plant der GOAP-Planner immer eine Aktion:
    /// TakeCoverAction → FleeAction (wenn Spieler sichtbar) oder WanderAction (Fallback).
    ///
    /// Dynamische Kosten: Bei niedriger HP sinken die Kosten → das Ziel wird hoeher
    /// priorisiert, da im GOAP-Planner geringere Kosten = hoehere Prioritaet entsprechen.
    /// Erlaubt es den Bot bei kritischer HP aggressiver Cover/Flucht zu suchen
    /// statt z. B. ein konkurrierendes Hunt-Ziel zu verfolgen.
    /// </summary>
    public class SurviveGoal : GoalBase
    {
        /// <inheritdoc />
        public override float GetCost(IActionReceiver agent, IComponentReference references)
        {
            float baseCost = this.Config.BaseCost;

            NetworkedCharacterState state = references.GetCachedComponent<NetworkedCharacterState>();
            if (state == null)
            {
                return baseCost;
            }

            return SurviveGoalCost.Compute(baseCost, state.Health);
        }
    }

    /// <summary>
    /// Reine Cost-Berechnung fuer SurviveGoal — extrahiert aus dem Goal um
    /// ohne Unity-Runtime / NetworkBehaviour-Setup unit-testbar zu sein.
    /// </summary>
    public static class SurviveGoalCost
    {
        /// <summary>HP-Schwelle ab der das Ziel als kritisch eingestuft wird.</summary>
        public const int CriticalHealthThreshold = 30;

        /// <summary>HP-Schwelle ab der das Ziel als angeschlagen eingestuft wird.</summary>
        public const int WoundedHealthThreshold = 60;

        /// <summary>Kostenmultiplikator bei kritischer HP (geringer = hoehere Prioritaet).</summary>
        public const float CriticalCostMultiplier = 0.3f;

        /// <summary>Kostenmultiplikator bei angeschlagener HP.</summary>
        public const float WoundedCostMultiplier = 0.6f;

        /// <summary>Berechnet die endgueltigen Kosten basierend auf BaseCost und aktueller HP.</summary>
        public static float Compute(float baseCost, int health)
        {
            if (health <= CriticalHealthThreshold)
            {
                return baseCost * CriticalCostMultiplier;
            }

            if (health <= WoundedHealthThreshold)
            {
                return baseCost * WoundedCostMultiplier;
            }

            return baseCost;
        }
    }
}


