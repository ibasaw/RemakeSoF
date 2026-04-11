# AI Bot Development History — Session Summary (April 2026)

## What Was Done (Chronological)

### Phase 1: EANN → GOAP Migration
- Replaced neural network (EANN) AI with Goal-Oriented Action Planning (GOAP)
- Created 5 Actions, 2 Goals, 4 WorldKeys, 4 TargetKeys, 8 Sensors
- Plan chain: PatrolAction → ChasePlayerAction → ShootPlayerAction

### Phase 2: Detection & Combat Fixes
- Fixed shoot↔patrol oscillation (dead player detection)
- Added 360° OverlapSphere (30m) player detection
- Added head-height aiming (k_TargetHeadHeight = 1.7f)
- Fixed Chase→Shoot XZ distance check
- Added PlayerDirectlyVisible guard (no blindfire)

### Phase 3: Movement Quality
- NavMesh pathfinding + runtime NavMesh bake
- Sensor-based obstacle avoidance (jump/crouch/strafe)
- Wall collision 180° turn fallback
- Client-side path visualization (LineRenderer, SyncDebugPathClientRpc)

### Phase 4: Player Memory & Pursuit
- 4s player memory after LOS loss (k_PlayerMemoryDuration)
- Walk to last known position after memory expires
- Damage reaction (5s window, attacker position)

### Phase 5: Patrol Intelligence
- Least-visited checkpoint system (m_CheckpointVisitCounts[])
- Stealth awareness (60m OverlapSphere no-LOS, checkpoint bias)
- Player breadcrumb system (64-entry circular buffer, 30% chance selection)

### Phase 6: Path Optimization
- String-pulling path smoothing (NavMesh.Raycast)
- Wall repulsion (sensor-based corridor centering)
- NavMesh corner insetting (push corners 0.8m from edges)
- Dynamic path recalculation (1m chase / 3m patrol)
- Direct LOS steering (NavMesh bypass when clear path)

### Phase 7: Movement Speed & Agility
- Chase-jumping (1.5-3.5s interval, Quake bunny-hop speed boost)
- Removed crouch mirroring (was slowing bot down)
- Player action mirroring (jump + attack only)

### Phase 8: Predictive Aiming
- Velocity tracking from position delta
- 0.15s aim lead time, 3m max offset
- Cached per-frame, reused by all aiming code

### Phase 9: Stuck Detection
- 4-phase escalation: skip waypoint → forward jump → 180° reverse → abandon checkpoint
- Stuck-mirror-replay (cached player actions replayed during chase stuck)

## Design Decisions Made
- **GOAP over BT/Octrees**: Discussed alternatives, GOAP is stronger for expandability
- **NavMesh over Octrees**: Ground-based agents, NavMesh is purpose-built
- **No persistent heatmap yet**: Breadcrumbs are V1, heatmap planned as V2
- **No crouch mirroring**: Slows bot down in chase, counterproductive
- **XZ distance for range checks**: 3D distance inflated by eye-height difference

## Status: Feature-Complete Seeker Bot
See `seeker-bot-complete.md` for full feature reference.
