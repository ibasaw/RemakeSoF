# AI Bot System — Infrastructure & Spawning

## Key Files
- **AIBotSpawner**: `Assets/Scripts/Runtime/Game/Networked/AIBotSpawner.cs` — spawn/despawn/respawn, reads sv_botcount/names/skins from ServerConfig
- **NetworkedAICharacter**: `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedAICharacter.cs` — networked, position sync, visual load, hitbox/collider init, client path sync via `SyncDebugPathClientRpc`
- **ServerAICharacter**: `Assets/Scripts/Runtime/Game/Characters/Server/ServerAICharacter.cs` — PlayerPhysicsSimulation (identical to player), calls `AIBotController.Tick()` every frame
- **AIBotController**: `Assets/Scripts/Runtime/AI/AIBotController.cs` — Bridge zwischen GOAP-Planner und ServerAICharacter

## Config CVARs (SoF2_Server_Configuration.json)
- sv_botcount (int) — number of bots
- sv_botnames (string[]) — bot names, round-robin
- sv_botskins (string[]) — bot skins, round-robin

## Architecture
- AIBotSpawner is MonoBehaviour (not NetworkBehaviour)
- Bots use PrefabManager.LoadPrefab("AICharacter") — sync, cache-first
- Collider from ClientColliderSystem (SoF2 AABB, shared server+client)
- Physics from shared PlayerPhysicsSimulation (same Quake3/SoF2 physics as player)
- Hitboxes from ClientHitboxSystem.BuildHitboxes() after visual load
- Bots counted in GetTotalPlayerCount(), CountTeams(), AwardSurvivalKills()

## Prefab Components
NetworkObject, NetworkedAICharacter, ServerAICharacter, NetworkedCharacterState, ServerCharacterController, ClientHitboxSystem, ClientColliderSystem, AICharacterSkinHandler

## SoF2 Dimensions (Single Source of Truth: ClientColliderSystem)
- k_SoF2StandingHeight = 2.2606f (89 QU)
- k_SoF2CrouchingHeight = 1.6256f (64 QU)
- k_SoF2Radius = 0.381f (15 QU)

## AI Decision Architecture: GOAP (crashkonijn v3.1.2)
- Replaced EANN (neural network) with Goal-Oriented Action Planning
- Package: `com.crashkonijn.goap@229986caf265`
- Setup: `AIGoapSetup.cs` builds AgentTypes per code, registers with GoapBehaviour
- See `seeker-bot-complete.md` for full Seeker-Bot feature reference
- See `goap-system-reference.md` for GOAP file structure and plan chains

## Spawn Flow
```
WaitingForReadyState.Enter()
  └─ AIBotSpawner.SpawnInitialBots()
       ├─ LoadBotConfiguration() ← sv_botcount/names/skins
       ├─ for (i < botCount): SpawnBot()
       │    ├─ Instantiate + NetworkObject.Spawn()
       │    ├─ InitializeBot(name, skin, teamId)
       │    └─ m_SpawnedBots.Add(networkObject)
       └─ OnInitialBotsSpawned?.Invoke()
```

## Data Flow (per Frame)
```
GOAP Planner → Actions (Patrol/Chase/Shoot)
  └─ AIBotController (Intent Fields: MoveTarget, LookTarget, Attack, Jump, Crouch)
       └─ Tick() → BuildCommand() → PlayerCommand
            └─ ServerAICharacter → PlayerPhysicsSimulation.Simulate()
```

## Debug Visualization
- 3 LineRenderers on NetworkedAICharacter (path, steer, target)
- Client-side path sync via `SyncDebugPathClientRpc`
- Shader: "Unlit/Color" (cached as `s_CachedLineShader`)
