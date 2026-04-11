# AI Sensor & Movement System — Current State (GOAP)

## Sensor3D (`Assets/Scripts/Runtime/AI/Sensors/Sensor3D.cs`)
- SphereCast or Raycast per sensor
- Properties: Output (0-1 normalized), RawDistance (meters), HitLayer, HitCollider, WorldDirection
- Configure(maxDist, layerMask, sphereRadius, useSphereCast)
- Max range: 25m, SphereCast radius: 0.15m

## SensorArrayGenerator (`Assets/Scripts/Runtime/AI/Sensors/SensorArrayGenerator.cs`)
- 7 horizontal × 5 vertical = 35 sensors
- FOV: 120° horizontal (±60°), 40° vertical (±20°)
- Layers: Default(0) | Ground(6) | Player(7) | BrushCollision(9)
- Index = v * 7 + h (v=0-4 vertical, h=0-6 horizontal)
- Center ray: h=3, v=2 → index 17
- Placed at head height: k_SensorHeightOffset = 1.83m

## AIBotController Tick Sequence
1. Reset per-frame flags (`m_SensorsTickedThisFrame`, `m_RadiusScanDoneThisFrame`)
2. GOAP Actions call intent setters (SetMoveTarget, SetLookTarget, SetShouldAttack, etc.)
3. `EnsureSensorsTicked()` — lazy tick all 35 sensors
4. `EnsureRadiusScan()` — lazy 360° OverlapSphere + LOS
5. `CheckStuck()` — 4-phase stuck detection
6. `ReactToSensorObstacles()` — sensor-based obstacle avoidance
7. `UpdateStealthAwareness()` — 60m proximity scan
8. `BuildCommand()` — build PlayerCommand from intents + wall repulsion
9. Reset intents (m_MoveTarget=null, m_LookTarget=null, m_ShouldAttack/Jump/Crouch=false)

## Movement: BuildCommand() Flow
1. Calculate targetYaw from LookTarget (priority) or MoveTarget
2. Calculate pitch: Atan2 to target or decay toward 0°
3. Calculate moveInput: relative angle from targetYaw to movement direction
4. Apply wall repulsion: `CalculateWallRepulsion()` → moveInput.x offset
5. Apply stuck override (Phase 3: reverse movement)
6. Build PlayerCommand struct

## NavMesh Pathfinding
- `UpdateNavPath()` — recalc when target moved >threshold
- `InsetPathCornersFromWalls()` — push corners away from edges
- `SmoothPath()` — string-pulling via NavMesh.Raycast
- `GetSteeringTarget()` — return next unvisited waypoint >1.5m away

## Player Detection Priority
1. 360° OverlapSphere (30m) + Raycast LOS → m_NearestPlayerTransform
2. Player Memory (4s grace period) → m_LastKnownPlayerPosition
3. Damage Reaction (5s window) → m_LastAttackerPosition
4. Stealth Awareness (60m, no LOS) → m_StealthAwarenessPosition (checkpoint bias only)

## Bot Physics (same as player)
- Quake3/SoF2 movement: 7.112 m/s max, 6.858 m/s jump velocity
- Crouch: 25% speed penalty
- Strafe-jump speed boost (why chase-jumping helps)
