using CrashKonijn.Agent.Core;
using CrashKonijn.Goap.Core;
using CrashKonijn.Goap.Runtime;
using UnityEngine;

namespace Tolik.RemakeSoF.Runtime.AI.GOAP
{
    /// <summary>
    /// Baut die GOAP-AgentTypes fuer Seeker und Hider per Code
    /// und registriert sie beim GoapBehaviour.
    /// Wird von NetworkedGameState nach dem Erstellen des GoapBehaviour aufgerufen.
    /// </summary>
    public class AIGoapSetup
    {
        /// <summary>Name des Seeker-AgentTypes.</summary>
        public const string SeekerAgentTypeName = "SeekerBot";

        /// <summary>Name des Hider-AgentTypes.</summary>
        public const string HiderAgentTypeName = "HiderBot";

        /// <summary>Seeker-AgentType (nach Initialize verfuegbar).</summary>
        public IAgentType SeekerAgentType { get; private set; }

        /// <summary>Hider-AgentType (nach Initialize verfuegbar).</summary>
        public IAgentType HiderAgentType { get; private set; }

        /// <summary>
        /// Erstellt die Seeker- und Hider-AgentTypes und registriert sie beim GoapBehaviour.
        /// </summary>
        /// <param name="goapBehaviour">Das aktive GoapBehaviour in der Szene.</param>
        public void Initialize(GoapBehaviour goapBehaviour)
        {
            BuildSeekerAgentType(goapBehaviour);
            BuildHiderAgentType(goapBehaviour);

            Debug.Log("[AI·GOAP] AgentTypes erstellt und registriert: Seeker + Hider.");
        }

        /// <summary>
        /// Baut den Seeker-AgentType:
        /// Goal: HuntPlayerGoal (EnemyDown >= 1)
        /// Aktionskette: PatrolAction → ChasePlayerAction → ShootPlayerAction.
        /// </summary>
        private void BuildSeekerAgentType(GoapBehaviour goapBehaviour)
        {
            AgentTypeBuilder builder = new(goapBehaviour.Config.GoapInjector, SeekerAgentTypeName);

            CapabilityBuilder cap = builder.CreateCapability("SeekerCombat");

            // --- Goal ---
            cap.AddGoal<HuntPlayerGoal>()
                .SetBaseCost(1f)
                .AddCondition<EnemyDown>(Comparison.GreaterThanOrEqual, 1);

            // --- Actions ---
            cap.AddAction<PatrolAction>()
                .SetTarget<CheckpointTargetKey>()
                .SetBaseCost(5f)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving)
                .AddEffect<IsPlayerVisible>(EffectType.Increase);

            cap.AddAction<ChasePlayerAction>()
                .SetTarget<PlayerTargetKey>()
                .SetBaseCost(3f)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving)
                .AddCondition<IsPlayerVisible>(Comparison.GreaterThanOrEqual, 1)
                .AddEffect<IsPlayerInRange>(EffectType.Increase);

            cap.AddAction<ShootPlayerAction>()
                .SetTarget<PlayerTargetKey>()
                .SetBaseCost(1f)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving)
                .AddCondition<IsPlayerVisible>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<IsPlayerInRange>(Comparison.GreaterThanOrEqual, 1)
                .AddCondition<HasAmmo>(Comparison.GreaterThanOrEqual, 1)
                .AddEffect<EnemyDown>(EffectType.Increase);

            // --- World Sensors ---
            cap.AddWorldSensor<PlayerVisibilitySensor>()
                .SetKey<IsPlayerVisible>();

            cap.AddWorldSensor<PlayerRangeSensor>()
                .SetKey<IsPlayerInRange>();

            cap.AddWorldSensor<AmmoSensor>()
                .SetKey<HasAmmo>();

            cap.AddWorldSensor<EnemyDownSensor>()
                .SetKey<EnemyDown>();

            // --- Target Sensors ---
            cap.AddTargetSensor<NearestPlayerTargetSensor>()
                .SetTarget<PlayerTargetKey>();

            cap.AddTargetSensor<CheckpointTargetSensor>()
                .SetTarget<CheckpointTargetKey>();

            IAgentTypeConfig config = builder.Build();
            goapBehaviour.Register(config);

            SeekerAgentType = goapBehaviour.GetAgentType(SeekerAgentTypeName);
        }

        /// <summary>
        /// Baut den Hider-AgentType:
        /// Goal: SurviveGoal (IsSafe >= 1)
        /// Aktionen: FleeAction (wenn Spieler sichtbar) oder WanderAction (Fallback).
        /// </summary>
        private void BuildHiderAgentType(GoapBehaviour goapBehaviour)
        {
            AgentTypeBuilder builder = new(goapBehaviour.Config.GoapInjector, HiderAgentTypeName);

            CapabilityBuilder cap = builder.CreateCapability("HiderSurvival");

            // --- Goal ---
            cap.AddGoal<SurviveGoal>()
                .SetBaseCost(1f)
                .AddCondition<IsSafe>(Comparison.GreaterThanOrEqual, 1);

            // --- Actions ---
            cap.AddAction<FleeAction>()
                .SetTarget<FleeTargetKey>()
                .SetBaseCost(1f)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving)
                .AddCondition<IsPlayerVisible>(Comparison.GreaterThanOrEqual, 1)
                .AddEffect<IsSafe>(EffectType.Increase);

            cap.AddAction<TakeCoverAction>()
                .SetTarget<CoverTargetKey>()
                .SetBaseCost(0.8f)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving)
                .AddCondition<IsPlayerVisible>(Comparison.GreaterThanOrEqual, 1)
                .AddEffect<IsSafe>(EffectType.Increase);

            cap.AddAction<WanderAction>()
                .SetTarget<WanderTargetKey>()
                .SetBaseCost(5f)
                .SetMoveMode(ActionMoveMode.PerformWhileMoving)
                .AddEffect<IsSafe>(EffectType.Increase);

            // --- World Sensors ---
            cap.AddWorldSensor<PlayerVisibilitySensor>()
                .SetKey<IsPlayerVisible>();

            cap.AddWorldSensor<SafetySensor>()
                .SetKey<IsSafe>();

            // --- Target Sensors ---
            cap.AddTargetSensor<FleeTargetSensor>()
                .SetTarget<FleeTargetKey>();

            cap.AddTargetSensor<CoverTargetSensor>()
                .SetTarget<CoverTargetKey>();

            cap.AddTargetSensor<WanderTargetSensor>()
                .SetTarget<WanderTargetKey>();

            IAgentTypeConfig config = builder.Build();
            goapBehaviour.Register(config);

            HiderAgentType = goapBehaviour.GetAgentType(HiderAgentTypeName);
        }
    }
}
