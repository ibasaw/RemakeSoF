# Projectile System Reference

## Overview
Server-authoritative projectile system for RPG7, MM1, and F1 Grenade.
No NetworkObject — server simulates physics+damage, clients get visual RPCs.

## Architecture

```
ProcessAttack (NetworkedPlayerCharacter)
  └─ if attackDef.Projectile != null
       └─ ProcessProjectileAttack()
            ├─ Impact (RPG7, MM1): SpawnProjectile() immediately
            └─ Timer (F1): Start cook phase → TickServerGrenadeCook() → SpawnProjectile() on throw

ServerProjectile (MonoBehaviour, server-only)
  ├─ Update(): velocity + gravity → Raycast collision detection
  ├─ Impact detonation: explode on world/hitbox collision
  ├─ Timer detonation: countdown → explode, with bounce off surfaces
  └─ Detonate(): OverlapSphere → distance-based damage falloff → SetHealth()
```

## Files
- `Assets/Scripts/Runtime/Game/Projectiles/ServerProjectile.cs` — Server-side projectile simulation
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` — ProcessProjectileAttack, grenade cook/throw, ProjectileSpawnClientRpc

## Weapon Data (from SoF2_Weapons_New.json)

| Weapon | Damage | Radius (QU) | Radius (m) | Speed (QU/s) | Speed (m/s) | Gravity | Bounce | Detonation | Timer |
|--------|--------|-------------|------------|--------------|-------------|---------|--------|------------|-------|
| RPG7   | 200    | 300         | 7.62       | 1500         | 38.1        | 0       | 0      | impact     | —     |
| MM1    | 200    | 150         | 3.81       | 1400         | 35.56       | 0.5     | 0      | impact     | —     |
| F1     | 200    | 300         | 7.62       | 1000         | 25.4        | 1.0     | 0.45   | timer      | 3.0s  |
| F1 alt | 200    | 300         | 7.62       | 700          | 17.78       | 1.0     | 0.25   | timer      | 3.0s  |

## Grenade Cook/Throw Two-Phase System
1. Player presses Attack → `ProcessProjectileAttack()` starts cook phase
2. `m_ServerIsGrenadeCooking = true`, cook frames start counting (mp_attack = GRENADE_START, 23f@20fps = 1.15s)
3. Player can aim during cook (PitchAngle/YawAngle updated each tick)
4. Cook frames expire → `TickServerGrenadeCook()` spawns projectile with reduced timer (3.0 - 1.15 = 1.85s remaining)
5. Throw follow-through phase starts (mp_attackEnd = GRENADE_END, 18f@20fps = 0.9s)
6. Player is action-blocked during entire cook+throw sequence

## Explosion Damage
- `OverlapSphere` on Hitbox layer within radius
- Linear distance falloff: full damage at center, 0 at edge
- Per-player deduplication: highest damage per target wins
- Self-damage not explicitly prevented (SoF2 allows rocket-jumping)

## ServerProjectile Physics
- Velocity in Unity m/s = speed_QU × SOF2_UNIT_SCALE (0.0254)
- Gravity: `Physics.gravity × gravityScale × dt` added to velocity each frame
- Collision: Raycast along movement vector each frame
- Bounce: `Vector3.Reflect(velocity, normal) × bounceCoeff`
- Impact projectiles detonate on any world or hitbox collision
- Timer projectiles bounce off world, countdown to detonation
- MAX_LIFETIME = 15s safety cleanup

## Action Gating
Grenade cook/throw blocks all other actions:
```csharp
bool noActionRunning = m_ServerAttackFramesRemaining <= 0
    && m_ServerReloadFramesRemaining <= 0
    && m_ServerAltAttackFramesRemaining <= 0
    && !m_ServerIsSwapping
    && !m_ServerIsGrenadeCooking
    && m_ServerGrenadeThrowFramesRemaining <= 0;
```

## Client Visual (TODO)
`ProjectileSpawnClientRpc(spawnPosition, direction, speed, gravity)` is sent to all clients.
Currently logs only. Future: spawn visual trail/model on clients.

## EjectBone Mapping
- RPG7: `ejection_rpg7`
- MM1: `ejection_mm1`
- F1: `gun` (both primary and alt attack)
