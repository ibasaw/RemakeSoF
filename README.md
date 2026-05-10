# RemakeSoF

A faithful recreation of **Soldier of Fortune II: Double Helix** in Unity 6, featuring server-authoritative Quake III/SoF2-style movement physics, Netcode for GameObjects multiplayer, a fully data-driven weapon/effect/sound pipeline, and a GOAP-based AI bot system.

Assets not included in the repository — contact me if you want them.

First Asset Release:
https://github.com/anatoli308/RemakeSoF/releases/tag/release1

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Engine | Unity 6000.3.8f1 |
| Networking | Netcode for GameObjects 2.8.0 |
| Server | Unity Dedicated Server 2.0.1 |
| Rendering | Universal Render Pipeline 17.3.0 |
| Assets | Addressables 2.8.1 |
| Input | Input System 1.18.0 |
| Camera | Cinemachine 3.1.4 |
| Auth | Custom FastAPI Backend (JWT) |

---

## Architecture

### MVC + State Machines + Service Locator

```
ApplicationEntryPoint (Singleton, DontDestroyOnLoad)
 ├── ServiceLocator
 │   ├── PrefabManager          (Addressables cache)
 │   ├── TextureManager         (Material/texture cache)
 │   ├── WeaponDataLoader       (JSON weapon definitions)
 │   ├── EffectDataLoader       (JSON effect definitions)
 │   ├── SoundManager           (Audio clip registry)
 │   ├── SurfaceImpactDataLoader(Surface → effect mapping)
 │   ├── EffectFactory          (Spawns particles/decals/lights)
 │   └── MasterServerService    (REST: auth, server browser)
 │
 ├── ConnectionManager          (NGO lifecycle state machine)
 ├── AuthenticationManager      (Auth flow state machine)
 ├── PlayerSkinManager          (Skin load/apply state machine)
 └── ConsoleManager             (Console toggle state machine)
```

- **MVC Pattern** — `BaseApplication<M,V,C>` discovers Model/View/Controller via DFS. Used per scene (`MetagameApplication`, `GameApplication`).
- **State Machines** — `StateMachine<TState, TSelf>` with CRTP for type-safe state references. States handle `Enter()`/`Exit()`, managers orchestrate and own events.
- **Service Locator** — Pure services (no MonoBehaviour) registered in `ApplicationEntryPoint.Awake()`, accessed via `ServiceLocator.Get<T>()`.
- **Service Decomposition** — Complex managers delegate to internal Loaders/Appliers (e.g. `PlayerSkinManager` → 4 Loaders + `PlayerSkinApplier`).
- **EventManager** — Type-safe event broadcasting for decoupled MVC communication.

### Scenes

| Scene | Purpose |
|-------|---------|
| **Startup** | Bootstrap, service registration, network init |
| **Metagame** | Login, skin/loadout selection, server browser |
| **Game** | Gameplay with Netcode, round flow, HUD |

---

## Movement Physics

SoF2/Quake III movement ported from `bg_pmove.c` / `bg_slidemove.c` to C# (`PlayerPhysicsSimulation.cs`):

- **Server-authoritative** with client-side prediction and reconciliation
- **4-bump SlideMove** + step-up collision via `CapsuleCast`
- Manual physics — no Rigidbody, no Unity CharacterController
- Client sends `PlayerCommand` structs (MoveInput, YawAngle, PitchAngle, Buttons, DeltaTime, SequenceNumber), server simulates and acknowledges via `ServerMovementAck`

### Physics Parameters (SoF2 ÷ 10)

| Parameter | Value | SoF2 Original |
|-----------|-------|---------------|
| Gravity | 80 | 800 |
| Max Speed | 28 | 280 |
| Jump Velocity | 27 | 270 |
| Accelerate | 6 | 6 |
| Air Accelerate | 1 | 1 |
| Friction | 6 | 6 |
| Max Steepness | 0.7 | 0.7 |

See [referenzen/server-authoritative-movement.md](referenzen/server-authoritative-movement.md) and [referenzen/sof2-physics-reference.md](referenzen/sof2-physics-reference.md) for details.

---

## Player Character Architecture

```
Player Prefab (NetworkObject)
 ├── NetworkedPlayerCharacter    — ProcessCommand: movement + attack + fire gating
 ├── NetworkedCharacterState     — NetworkVariables (skin, weapon, health, ammo, kills)
 ├── ClientPlayerCharacter       — Prediction, FP weapon, visual bones, camera effects
 ├── ServerPlayerCharacter       — Authoritative physics, GetEyePosition()
 └── ClientCharacterSkinHandler  — Skin instantiation via Addressables
```

- **Server** processes movement commands, owns physics collider, runs hitscan raycasts
- **Client (Owner)** predicts locally, reconciles on server ack, drives visual bones (pelvis/lumbar lean & twist)
- **Client (Remote)** receives interpolated state, no prediction

---

## Weapon System

### Third-Person & First-Person

SoF2's Ghoul2 composite weapon model faithfully recreated:

| Component | Description |
|-----------|-------------|
| `WeaponLoader` | Loads TP world model, attaches to `rhang_tag_bone` |
| `FpWeaponLoader` | Loads FP view model, attaches to `FP_WeaponHolder` |
| `FirstPersonHandsLoader` | Loads buffer + lhand/rhand composite model |
| `FirstPersonCameraEffects` | Per-weapon viewOffset, bob, landing, duck |
| Weapon Overlay Camera | Separate FOV for FP models (depth hack equivalent) |

### Data-Driven Weapon Definitions (`SoF2_Weapons_New.json`)

All weapon parameters loaded from JSON via `WeaponDataLoader` (Pure Service):

```
WeaponDefinition
 ├── WeaponAttackDefinition   (damage, range, inaccuracy, pellets, spread, kickAngles)
 │   └── WeaponProjectileDefinition (rockets, grenades)
 ├── WeaponAmmoDefinition     (clip size, reserve, alt-ammo e.g. M203)
 └── WeaponAnimationEntry[]   (per-stance animation clips)
```

### Server-Authoritative Fire Gating (`pm_debounce`)

Prevents rapid-fire cheating — ported from `bg_pmove.c` `PM_GetAttackButtons()`:
- Semi-auto: button must be released between shots
- Burst: server controls burst count decrement
- Auto: continuous fire while button held
- Per-command cooldown, inaccuracy buildup at sustained fire

See [referenzen/server-authoritative-weapon-fire.md](referenzen/server-authoritative-weapon-fire.md) and [referenzen/fp-weapon-system.md](referenzen/fp-weapon-system.md).

---

## Hitscan Damage System

Server-authoritative, no separate attack RPC — attack is read from `PlayerCommand.Buttons`:

```
PlayerCommand (Attack bit set)
        ↓
NetworkedPlayerCharacter.ProcessAttack()
  ├─ TryConsumeAmmo()              (server-side)
  ├─ Eye position + aim direction  (from PitchAngle/YawAngle)
  ├─ Inaccuracy cone buildup       (base → max at sustained fire)
  ├─ Multi-pellet loop             (shotguns: N pellets per shot)
  │   ├─ Physics.Raycast()         (HitboxCollider layer)
  │   ├─ HitboxCollider → DamageMultiplier → SetHealth()
  │   └─ DebugTracerClientRpc()    (visual tracer per pellet)
  ├─ KickAngles → ApplyKickAnglesClientRpc()
  └─ Re-enable physics collider
```

### Hitbox Regions (17 regions from `SoF2_DATA.json`)

| Region | Damage Multiplier |
|--------|-------------------|
| Head / Neck | 1.75× |
| Chest / Gut / Groin | 1.0× |
| Shoulders / Upper Arms | 0.7× |
| Thighs / Lower Legs | 0.7× |
| Hands | 0.3× |
| Feet | 0.4× |

`HitboxCollider` components auto-computed from bone-to-bone distances at runtime.

See [referenzen/hitscan-damage-system.md](referenzen/hitscan-damage-system.md) and [referenzen/hit-detection-gore-system.md](referenzen/hit-detection-gore-system.md).

---

## Skin & Model System

SoF2's character skin system faithfully recreated:

- **SkinDefinitionLoader** — Loads skin definitions from Resources
- **SurfaceDefinitionLoader** — Loads `NPC_definition.json` for mesh segments
- **CharacterTemplateLoader** — Loads `SoF2_NPCs.json` for NPC templates
- **LegacyShaderLoader** — Parses `.g2shader` files for material setup (includes `hitLocation` texture paths for future gore)
- **PlayerSkinApplier** — Applies materials, toggles mesh surfaces, sets animator controllers

### Core Data Files

| File | Content |
|------|---------|
| `SoF2_NPCs.json` | NPC templates, per-stance bounding boxes, mesh segments |
| `SoF2_DATA.json` | 17 hit regions, weapon data, game constants |
| `SoF2_Weapons_New.json` | All weapon definitions (attack, ammo, animations, FP model offsets) |
| `NPC_definition.json` | Surface definitions per model |
| `average_sleeves.skl.json` | Skeleton animation actions, PCJ rotation limits |

---

## Visual Effect System

Fully data-driven — no hardcoded particle prefabs. All effects defined in JSON (ported from SoF2's `.efx` format):

```
JSON Effect Definitions
        ↓
EffectDataLoader (Pure Service)
        ↓
EffectFactory (MonoBehaviour, ServiceLocator)
  ├─ SpawnImpactEffect()    → particles + decal + sound
  ├─ SpawnMuzzleEffect()    → muzzle flash + smoke
  ├─ SpawnShellCasing()     → 3D model + physics + bounce sound
  ├─ SpawnDebris()          → particles + 3D chunks
  └─ SpawnExplosion()       → explosion + light + camera shake + debris
```

Effect segments: `Particle`, `Trail`, `Decal`, `Light`, `CameraShake`, `Emitter` (physics 3D objects), `Sound`.

Surface-dependent impact effects resolved via `SurfaceImpactDataLoader` (ammo type × surface material → effect ID).

See [referenzen/effect-system.md](referenzen/effect-system.md).

---

## Sound System

Data-driven — no hardcoded `AudioClip` references. SoF2-style numbered sound variants:

```
SoundManager (Pure Service)
  ├─ ScanSoundDirectory()   → builds SoundRegistry from disk
  ├─ GetClip(key)           → AudioClip by SoF2-style path
  └─ GetNumberedClip(base)  → random numbered variant (e.g. body_impact0..4)

Consumers:
  ├─ ClientFootstepHandler        → footstep + landing
  ├─ NetworkedPlayerCharacter     → fire sound, shell ejection, impact sound (via RPC)
  └─ EffectFactory                → effect-embedded sounds (type: "sound")
```

See [referenzen/sound-system.md](referenzen/sound-system.md).

---

## Map Loading System

SoF2 maps imported as FBX prefabs with all idTech3 shader properties stored in `Ghoul2Meta` components (via `FBXGhoul2PropsImporter` AssetPostprocessor):

```
MapLoader.LoadMapAsync(mapName)
  ├─ Addressable load + instantiate
  ├─ MapColliderApplier     → MeshCollider per renderer        (server + client)
  ├─ MapTextureApplier      → texture, cull, transparency      (client only)
  └─ MapSkyboxApplier       → skyParms + directional sun light (client only)
```

`Ghoul2Meta` properties: `mapped_texture_N`, `cull_N`, `is_transparent_N`, `sky_types_json_N`, `sun_N` (r g b intensity degrees elevation).

See [referenzen/map-loading-system.md](referenzen/map-loading-system.md).

---

## Round Flow & Gametype System

### Round Flow State Machine

```
LoadingState → WaitingForReadyState → WarmupState → StartingRoundState
                                                            ↓
                                     WaitingForReady ← RunningState → SwitchingMapState
```

- `LoadingState` — waits for map load + all clients ready
- `WarmupState` — configurable warmup time (`g_warmup`)
- `RunningState` — match countdown, game loop, gametype delegation
- `SwitchingMapState` — 5s countdown, then map rotation

### Gametype System

Extensible via `IGametype` interface + `BaseGametype`. Active gametype implemented: **Hide and Seek**.

| CVAR | Default | Description |
|------|---------|-------------|
| `hideandseek_hidetime` | 30s | Time Hiders have to hide |
| `hideandseek_seektime` | 120s | Time Seekers have to find all Hiders |
| `hideandseek_seekercount` | 1 | Number of Seekers |
| `hideandseek_roundlimit` | 5 | Rounds per map |
| `hideandseek_seekerweapons` | true | Seekers have weapons |
| `hideandseek_hiderweapons` | false | Hiders have weapons |

See [referenzen/round-flow-system.md](referenzen/round-flow-system.md) and [referenzen/hide-and-seek-gametype.md](referenzen/hide-and-seek-gametype.md).

---

## AI Bot System (Feature Complete — April 2026)

GOAP-based Seeker Bot with NavMesh pathfinding and SoF2/Quake3 physics:

### Detection

- **360° OverlapSphere** (30m radius) + LOS raycast
- **35-Sensor Grid** (7h × 5v, 120° × 40° FOV, 25m range) for directional awareness
- **4s Player Memory** — pursues last known position after losing sight
- **Stealth Awareness** (60m radius, no LOS) — biases patrol toward detected players

### Patrol

- **Least-Visited Checkpoint System** — avoids repetitive routes
- **Player Breadcrumbs** (circular buffer, 64 entries) — 30% chance to investigate recent player positions
- **Unreachable checkpoint skip** — reset after full cycle

### GOAP Actions

| Action | Condition | Effect |
|--------|-----------|--------|
| `Patrol` | default | explores map |
| `Chase` | player in memory | moves to last known position |
| `Shoot` | player visible + in range | fires weapon |
| `MoveToShootingPosition` | player visible but not in range | closes distance |

See [referenzen/seeker-bot-complete.md](referenzen/seeker-bot-complete.md) and [referenzen/goap-system-reference.md](referenzen/goap-system-reference.md).

---

## Metagame Systems

### Loadout Screen

3D character preview with live skin selection:
- `RenderTexture` + dedicated camera + light on Layer 30
- Skin thumbnail list (horizontal scroll, clickable)
- QuakeColorLabel player name display (live `^1color^7 codes`)
- Prev/Next navigation + Equip button

### Server Browser

- `MasterServerService` — REST calls to FastAPI backend (`/api/servers`, `/api/login`, `/api/register`)
- Paginated server list with `JoinServerController`
- DirectIP connection via separate `DirectIPView`
- Connecting screen with timeout handling

### HUD

UIToolkit-based HUD driven by `NetworkVariable.OnValueChanged`:
- Weapon / ammo display, health bar, stun indicator
- Hit confirmation feedback, gametype messages
- Round countdown (`3-2-1 GO`)
- Tab Scoreboard (`ScoreboardView`) — team scores, kills/deaths
- Match Recap screen (`MatchRecapView`) — game over + map switch countdown

See [referenzen/hud-system.md](referenzen/hud-system.md), [referenzen/loadout-system.md](referenzen/loadout-system.md), [referenzen/server-browser-system.md](referenzen/server-browser-system.md).

---

## Game Distribution Pipeline

```
Unity Build (IL2CPP, Release)
        ↓
Post-Build Python Script:
  1. Code signing (optional, signtool)
  2. SHA256 manifest generation per file
  3. Archive creation (.tar.zst)
  4. Archive hash + upload to FastAPI backend
        ↓
FastAPI Backend (authserver):
  GET /api/game/version   → version info + manifest
  GET /api/game/download  → streaming archive
        ↓
Launcher (Tauri/Rust):
  1. Version check
  2. Download archive
  3. SHA256 verify
  4. Extract to install directory
```

The `Art/` folder (textures, sounds, models) is not part of the Unity build — it is copied manually post-build. This keeps Unity build times short; assets are loaded at runtime via `TextureManager` and `SoundManager`.

See [referenzen/game-distribution-pipeline.md](referenzen/game-distribution-pipeline.md).

---

## Dedicated Server

Multiplayer roles via `Unity.DedicatedServer.MultiplayerRoles`:

- **Server** — Headless, authoritative, spawns player characters, processes commands, registers with master server
- **Client** — Renders, predicts, sends input
- **Host mode explicitly unsupported** — `ClientAndServer` role throws an exception

### Command Line Arguments

| Argument | Default | Description |
|----------|---------|-------------|
| `--port` | 7777 | Server listen port |
| `--target-framerate` | 30 | Server tick rate |

Server registers automatically with the FastAPI master server on startup and sends 30s heartbeats. See [referenzen/dedicatedgameserver.md](referenzen/dedicatedgameserver.md) and [referenzen/server-configuration.md](referenzen/server-configuration.md).

---

## Coding Conventions

- **No `var`** — always explicit types
- **`new(TypeName)`** syntax for instantiation
- **XML doc comments** on all public members
- **KISS, DRY, YAGNI, SRP** — no speculative features
- **Composition over Inheritance** — Service Decomposition pattern
- **No polling, no coroutines** — use events, callbacks, async/await
- **Fail Fast** — guard clauses, early validation
- **Cache-First** — always through PrefabManager/TextureManager, never bypass registries

---

## Related Repositories

| Repository | Description |
|------------|-------------|
| [authserver](https://github.com/anatoli308/authserver) | FastAPI backend — JWT auth, game file distribution, master server |
| [gamelauncher](https://github.com/anatoli308/gamelauncher) | Tauri launcher — version check, download, install, skin selection |

---

## Documentation (`referenzen/`)

| Document | Description |
|----------|-------------|
| [server-authoritative-movement.md](referenzen/server-authoritative-movement.md) | Movement physics, prediction, reconciliation |
| [sof2-physics-reference.md](referenzen/sof2-physics-reference.md) | SoF2 original physics constants |
| [hitscan-damage-system.md](referenzen/hitscan-damage-system.md) | Raycast damage, hitboxes, inaccuracy |
| [server-authoritative-weapon-fire.md](referenzen/server-authoritative-weapon-fire.md) | pm_debounce, fire gating, fire modes |
| [fp-weapon-system.md](referenzen/fp-weapon-system.md) | First-person weapon composite model |
| [weapon-data-system.md](referenzen/weapon-data-system.md) | JSON weapon definitions, WeaponDataLoader |
| [weapon-swap-reload-system.md](referenzen/weapon-swap-reload-system.md) | Weapon swap & reload flow |
| [effect-system.md](referenzen/effect-system.md) | Data-driven visual effects (particles, decals, lights) |
| [sound-system.md](referenzen/sound-system.md) | Data-driven audio system |
| [map-loading-system.md](referenzen/map-loading-system.md) | FBX map import, Ghoul2Meta, texture/skybox application |
| [texture-system.md](referenzen/texture-system.md) | TextureManager, material caching |
| [shader-overview.md](referenzen/shader-overview.md) | URP shader reference |
| [hit-detection-gore-system.md](referenzen/hit-detection-gore-system.md) | SoF2 hitLocation textures, gore system reference |
| [round-flow-system.md](referenzen/round-flow-system.md) | Round lifecycle state machine |
| [hide-and-seek-gametype.md](referenzen/hide-and-seek-gametype.md) | Hide & Seek gametype implementation |
| [seeker-bot-complete.md](referenzen/seeker-bot-complete.md) | GOAP bot — full feature reference |
| [goap-system-reference.md](referenzen/goap-system-reference.md) | GOAP planner architecture |
| [ai-sensor-movement-system.md](referenzen/ai-sensor-movement-system.md) | Bot sensor grid & movement |
| [loadout-system.md](referenzen/loadout-system.md) | Skin selection UI, 3D preview |
| [server-browser-system.md](referenzen/server-browser-system.md) | Server browser, DirectIP, connecting screen |
| [master-server-service.md](referenzen/master-server-service.md) | REST master server integration |
| [hud-system.md](referenzen/hud-system.md) | UIToolkit HUD, scoreboard, match recap |
| [metagame-scene.md](referenzen/metagame-scene.md) | Metagame scene overview |
| [authentication-system.md](referenzen/authentication-system.md) | Auth state machine, JWT |
| [console-system.md](referenzen/console-system.md) | In-game developer console |
| [custom-skins-architecture.md](referenzen/custom-skins-architecture.md) | Custom skin pipeline |
| [game-distribution-pipeline.md](referenzen/game-distribution-pipeline.md) | Build → archive → distribute pipeline |
| [dedicated-server-build.md](referenzen/dedicated-server-build.md) | Dedicated server build instructions |
| [dedicatedgameserver.md](referenzen/dedicatedgameserver.md) | Server setup & multiplayer testing |
| [server-configuration.md](referenzen/server-configuration.md) | Server CVARs reference |
| [projectile-system.md](referenzen/projectile-system.md) | Rocket/grenade projectile system |
| [scoreboard-system.md](referenzen/scoreboard-system.md) | Scoreboard data & display |
| [match-recap-system.md](referenzen/match-recap-system.md) | End-of-match screen |
| [collision-system-reference.md](referenzen/collision-system-reference.md) | Physics layers, collision matrix |
| [client-visual-bone-system.md](referenzen/client-visual-bone-system.md) | Visual bone lean/twist system |
| [urp-graphics-settings.md](referenzen/urp-graphics-settings.md) | URP renderer settings |
| [art-assets-inventory.md](referenzen/art-assets-inventory.md) | Art asset inventory |
| [quake-color-label.md](referenzen/quake-color-label.md) | QuakeColorLabel (^color codes) |
| [runtime-bugfix-log.md](referenzen/runtime-bugfix-log.md) | Runtime bug fixes & lessons learned |
| [AGENTS.md](referenzen/AGENTS.md) | AI architecture planning document |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Full architecture spec & coding rules |