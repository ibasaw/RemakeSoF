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
- Collision: RaycastAll along movement vector each frame (with owner filtering)
- Bounce: `Vector3.Reflect(velocity, normal) × bounceCoeff`
- Impact projectiles detonate on any world or hitbox collision (excluding owner)
- Timer projectiles bounce off world, countdown to detonation
- MAX_LIFETIME = 15s safety cleanup

### MISSILE_PRESTEP (RPG7 Self-Hit Prevention)
RPG7-Projektile spawnen mit Forward-Offset um Selbsttreffer zu vermeiden:
```csharp
const float MISSILE_PRESTEP = 32f * 0.0254f;  // 32 QU = 0.8128m
```
- Spawn an `eyePos + aimDirection * MISSILE_PRESTEP` statt `eyePos`
- Wall-Check: Raycast von `eyePos` → `spawnPos` auf worldLayerMask
  - Wenn Wand dazwischen: Spawn trotzdem an `eyePos` (SoF2-Verhalten)
- Quelle: SoF2 `g_weapon.c` MISSILE_PRESTEP

### Owner-Filtering (Eigene Collider ignorieren)
ServerProjectile speichert `m_OwnerRoot` (Transform des Besitzers):
```csharp
// In Initialize():
m_OwnerRoot = ownerPlayerObject.transform;

// In Move():
RaycastHit[] worldHits = Physics.RaycastAll(..., m_WorldLayerMask);
foreach (hit in worldHits)
    if (!hit.transform.IsChildOf(m_OwnerRoot))  // Owner filtern
        → Welt-Treffer

RaycastHit[] hitboxHits = Physics.RaycastAll(..., m_HitboxLayerMask);
foreach (hit in hitboxHits)
    if (!hit.transform.IsChildOf(m_OwnerRoot))  // Owner filtern
        → Hitbox-Treffer (nächster)
```
- `RaycastAll` statt `Raycast` um alle Treffer zu sammeln und Owner auszufiltern
- `IsChildOf(m_OwnerRoot)` erkennt sowohl Hitbox-Trigger als auch Physics-Collider
- Explosion (`Detonate()` via `OverlapSphere`) trifft Owner absichtlich (Self-Damage, Rocket-Jumping)

### Layer-Masken
```csharp
m_HitboxLayerMask = LayerMask.GetMask("Hitbox");
m_ExplosionLayerMask = LayerMask.GetMask("Player");
m_WorldLayerMask = ~(m_HitboxLayerMask | LayerMask.GetMask("BrushCollision", "Player"));
```
- Player-Layer in worldLayerMask excludiert (Movement-BoxCollider soll nicht als Wand zählen)

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

## Client Visual
`ProjectileSpawnClientRpc(spawnPosition, direction, speed, gravity, bounce, detonation, timer, projectileId, effectId, explosionEffectId, modelKey)` is sent to all clients.
Creates `ClientProjectileVisual` with:
1. 3D model via PrefabManager (if `modelKey` is set — knife, f1), Material: `SoF2/MapSurface` (_LightBlend=0.5)
2. Data-driven trail/particle effects via EffectFactory (if `effectId` is set)
3. Fallback colored sphere + simple trail (if neither available, uses `Sprites/Default`)
4. Knife-specific end-over-end rotation on `knifeworldbase` bone

### Explosion mit Debris
Bei Detonation ruft `ClientProjectileVisual.Detonate()` → `EffectFactory.SpawnExplosion(explosionEffectId)` auf.
Sofern die Explosion Emitter-Segmente enthält, werden physikalische 3D-Debris-Chunks gespawnt.
→ Vollständige Dokumentation: siehe **effect-system.md**

## EjectBone Mapping
- RPG7: `ejection_rpg7`
- MM1: `ejection_mm1`
- F1: `gun` (both primary and alt attack)

## Client Visual — Projectile Models
`ProjectileSpawnClientRpc` sends `modelKey` (from JSON `"model"` field) to all clients.
`ClientProjectileVisual` loads the 3D model via PrefabManager (Addressables, cache-first).

### Model Mapping
| Weapon | `model` Key | Prefab | Visual |
|--------|------------|--------|--------|
| Knife (throw) | `"knife"` | `Weapons/knife.prefab` | 3D knife mesh, end-over-end rotation |
| F1 (both attacks) | `"f1"` | `Weapons/f1.prefab` | 3D grenade mesh, follows flight direction |
| RPG7 | — | — | Trail effect only (`effects/rpg7_trail`) |
| MM1 | — | — | Trail effect only (`effects/m203_trail`) |
| M4 M203 | — | — | Trail effect only (`effects/m203_trail`) |

### Knife Rotation
The knife FBX contains a `knifeworldbase` bone with a pre-defined rotation animation.
`ClientProjectileVisual` searches for this bone and applies continuous X-axis rotation (720°/s)
to simulate end-over-end tumble during flight. Falls back to model root if bone not found.

### Collision
Projectile models have **no colliders** — all collision detection is point-based (Raycast along
flight path in `ServerProjectile`). Colliders are stripped from instantiated models on spawn.

### Fallback
If the model prefab is not found via PrefabManager, a colored primitive sphere (0.1m) is used
as fallback (same as before model loading was added).
