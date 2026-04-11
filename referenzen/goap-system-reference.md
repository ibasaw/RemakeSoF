# GOAP System Reference

## Architecture
```
GOAP Planner → Actions (Patrol/Chase/Shoot) → AIBotController (Intent Fields) → PlayerCommand → PlayerPhysicsSimulation
```

## Package
- CrashKonijn GOAP v3.1.2 (`com.crashkonijn.goap@229986caf265`)

## File Structure

### Setup
- `Assets/Scripts/Runtime/AI/GOAP/AIGoapSetup.cs` — Builds Seeker + Hider AgentTypes, registers with GoapBehaviour
- `Assets/Scripts/Runtime/AI/GOAP/AIActionData.cs` — Shared action data class

### Goals
- `HuntPlayerGoal.cs` — Seeker: EnemyDown >= 1
- `SurviveGoal.cs` — Hider: IsSafe >= 1

### World Keys (Assets/Scripts/Runtime/AI/GOAP/Keys/)
- `WorldKeys.cs`: IsPlayerVisible, IsPlayerInRange, HasAmmo, EnemyDown, IsSafe
- `TargetKeys.cs`: PlayerTargetKey, CheckpointTargetKey, FleeTargetKey, WanderTargetKey

### Actions (Assets/Scripts/Runtime/AI/GOAP/Actions/)
| Action | Target | BaseCost | Preconditions | Effect | MoveMode |
|--------|--------|----------|---------------|--------|----------|
| PatrolAction | CheckpointTargetKey | 5 | none | IsPlayerVisible↑ | PerformWhileMoving |
| ChasePlayerAction | PlayerTargetKey | 3 | IsPlayerVisible>=1 | IsPlayerInRange↑ | PerformWhileMoving |
| ShootPlayerAction | PlayerTargetKey | 1 | IsPlayerVisible>=1, IsPlayerInRange>=1, HasAmmo>=1 | EnemyDown↑ | PerformWhileMoving |
| FleeAction | FleeTargetKey | 1 | IsPlayerVisible>=1 | IsSafe↑ | PerformWhileMoving |
| WanderAction | WanderTargetKey | 5 | none | IsSafe↑ | PerformWhileMoving |

### Sensors (Assets/Scripts/Runtime/AI/GOAP/Sensors/)
| Sensor | Type | Key | Timer | Description |
|--------|------|-----|-------|-------------|
| PlayerVisibilitySensor | WorldSensor | IsPlayerVisible | Always | OverlapSphere + LOS + DamageReaction |
| PlayerRangeSensor | WorldSensor | IsPlayerInRange | Always | XZ distance vs weapon range |
| AmmoSensor | WorldSensor | HasAmmo | Always | Weapon ammo check |
| EnemyDownSensor | WorldSensor | EnemyDown | Always | Kill detection |
| SafetySensor | WorldSensor | IsSafe | — | Hider safety check |
| NearestPlayerTargetSensor | TargetSensor | PlayerTargetKey | Always | Sensor3D + DamageReaction + Memory |
| CheckpointTargetSensor | TargetSensor | CheckpointTargetKey | 1s interval | GetNearestCheckpoint() |
| FleeTargetSensor | TargetSensor | FleeTargetKey | — | Hider flee position |
| WanderTargetSensor | TargetSensor | WanderTargetKey | — | Hider wander position |

## Seeker Plan Chain (automatic)
```
HuntPlayerGoal (EnemyDown >= 1)
  └─ Planner:
     PatrolAction (→IsPlayerVisible↑)
     → ChasePlayerAction (→IsPlayerInRange↑)
     → ShootPlayerAction (→EnemyDown↑)
```

## Integration Points
- `NetworkedGameState` → creates GoapBehaviour, calls `AIGoapSetup.Initialize()`
- `ServerAICharacter` → auto-adds AIBotController + SensorArrayGenerator
- `AIBotController` → bridge between GOAP intent fields and PlayerCommand
- Actions call `controller.SetMoveTarget()`, `SetLookTarget()`, `SetShouldAttack()`, etc.
- Intents reset to null/false after each `BuildCommand()` tick

## Legacy (EANN — replaced)
- Files still exist: AIEvolutionManager.cs, AIFitnessEvaluator.cs, AIPopulationSerializer.cs, EANN/ folder
- No longer active — replaced by GOAP system
