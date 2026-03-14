# RemakeSoF

A faithful recreation of **Soldier of Fortune II: Double Helix** in Unity 6, featuring server-authoritative Quake III/SoF2-style movement physics, Netcode for GameObjects multiplayer, and an advanced AI system built on EANN + GOAP + Boids.

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
| Auth | Unity Authentication 3.6.0 |

---

## Architecture

### MVC + State Machines + Service Locator

```
ApplicationEntryPoint (Singleton, DontDestroyOnLoad)
 ├── ServiceLocator
 │   ├── PrefabManager      (Addressables cache)
 │   └── TextureManager     (Material/texture cache)
 │
 ├── ConnectionManager       (NGO lifecycle state machine)
 ├── AuthenticationManager   (Auth flow state machine)
 ├── PlayerSkinManager       (Skin load/apply state machine)
 └── ConsoleManager          (Console toggle state machine)
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
| **Metagame** | Main menu, login, skin selection, matchmaking |
| **Game** | Gameplay with Netcode integration |

---

## Movement Physics

SoF2/Quake III movement ported from `bg_pmove.c` / `bg_slidemove.c` to C# (`PlayerPhysicsSimulation.cs`):

- **Server-authoritative** with client-side prediction and reconciliation
- **4-bump SlideMove** + step-up collision via `CapsuleCast`
- Manual physics — no Rigidbody, no Unity CharacterController
- Client sends `PlayerCommand` structs, server simulates and acknowledges via `ServerMovementAck`

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

---

## Player Character Architecture

```
Player Prefab (NetworkObject)
 ├── NetworkedPlayerCharacter    — Spawns Client/Server components
 ├── NetworkedCharacterState     — NetworkVariables (skin, weapon, health)
 ├── ClientPlayerCharacter       — Prediction, visual bone system, collider, weapon
 ├── ServerPlayerCharacter       — Authoritative simulation, CapsuleCollider
 └── ClientCharacterSkinHandler  — Skin instantiation via Addressables
```

- **Server** processes movement commands, owns physics collider
- **Client (Owner)** predicts locally, reconciles on server ack, drives visual bones (pelvis/lumbar lean & twist)
- **Client (Remote)** receives interpolated state, no prediction

### Weapon System

`WeaponLoader` loads weapons via Addressables and attaches to the `rhang_tag_bone` on the character skeleton. Weapon state synced via `NetworkedCharacterState.m_CurrentWeaponName`.

---

## Skin & Model System

SoF2's character skin system faithfully recreated:

- **SkinDefinitionLoader** — Loads skin definitions from Resources
- **SurfaceDefinitionLoader** — Loads `NPC_definition.json` for mesh segments
- **CharacterTemplateLoader** — Loads `SoF2_NPCs.json` for NPC templates
- **LegacyShaderLoader** — Parses `.g2shader` files for material setup
- **PlayerSkinApplier** — Applies materials, toggles mesh surfaces, sets animator controllers

### Data Files

| File | Content |
|------|---------|
| `SoF2_NPCs.json` | NPC templates, per-stance bounding boxes, mesh segments |
| `SoF2_DATA.json` | 17 hit regions, weapon data, game constants |
| `NPC_definition.json` | Surface definitions per model |
| `average_sleeves.skl.json` | Skeleton animation actions, PCJ rotation limits |

---

## Hitbox System (Planned)

Per-bone BoxColliders mapped to SoF2's 17 hit regions:

| Region | Index | Bone Pair | Damage Multiplier |
|--------|-------|-----------|-------------------|
| Head | 0 | cranium → cervical | 1.75× |
| Right Leg | 1 | rtibia → rtarsal | 0.7× |
| Neck | 4 | cervical → thoracic | 1.75× |
| Chest | 8 | thoracic → upper_lumbar | 1.0× |
| Right Foot | 12 | rtarsal | 0.4× |
| Left Shoulder | 16 | lclavical → lhumerus | 0.7× |
| Left Arm | 20 | lhumerus → lradius | 0.7× |
| Left Hand | 24 | lradius → lhand | 0.3× |
| Right Shoulder | 28 | rclavical → rhumerus | 0.7× |
| Right Arm | 32 | rhumerus → rradius | 0.7× |
| Right Hand | 36 | rradius → rhand | 0.3× |
| Gut | 40 | upper_lumbar → lower_lumbar | 1.0× |
| Groin | 44 | lower_lumbar → pelvis | 1.0× |
| Left Thigh | 48 | lfemurYZ → ltibia | 0.7× |
| Left Leg | 52 | ltibia → ltarsal | 0.7× |
| Left Foot | 56 | ltarsal | 0.4× |
| Right Thigh | 60 | rfemurYZ → rtibia | 0.7× |

Collider sizes auto-computed from bone-to-bone distances at runtime using the skeleton hierarchy from `TorsoMask.mask` / `LegsMask.mask`.

---

## AI System (Planned)

Adaptive NPC intelligence stack as described in [AGENTS.md](AGENTS.md):

```
Evolution / Training (PSO / GA / NEAT)
        ↓
EANN (Neural Network) ←──── Global Memory (Heatmaps, Decay)
        ↓
GOAP (Goal-Oriented Action Planning)
        ↓
Boids / Steering (Separation, Cohesion, Alignment)
        ↓
Agent / Population (Emergent Behavior)
```

- **EANN** — Modular subnets for movement, aggression, team coordination
- **GOAP** — Dynamic action planning with preconditions/effects (replaces FSM/BT)
- **Boids** — Flocking and pursuit with parameters driven by EANN output
- **Global Memory** — Persistent heatmaps, danger zones, player hotspots with decay
- **Save/Load** — EANN weights + Global Memory + agent status persistable across sessions

---

## Dedicated Server

Multiplayer roles via `Unity.DedicatedServer.MultiplayerRoles`:

- **Server** — Headless, authoritative, spawns player characters, processes commands
- **Client** — Renders, predicts, sends input
- **Host mode explicitly unsupported** — `ClientAndServer` role throws an exception

### Command Line Arguments

| Argument | Default | Description |
|----------|---------|-------------|
| `--port` | 7777 | Server listen port |
| `--target-framerate` | 30 | Server tick rate |

See [dedicatedgameserver.md](dedicatedgameserver.md) for detailed setup and testing instructions.

---

## Coding Conventions

- **No `var`** — always explicit types
- **`new(TypeName)`** syntax for instantiation
- **XML doc comments** on all public members
- **KISS, DRY, YAGNI, SRP** — no speculative features
- **Composition over Inheritance** — Service Decomposition pattern
- **Fail Fast** — guard clauses, early validation
- **Cache-First** — always through PrefabManager/TextureManager, never bypass registries

---

## Documentation

| Document | Description |
|----------|-------------|
| [dedicatedgameserver.md](dedicatedgameserver.md) | Dedicated server setup & multiplayer testing |
| [AGENTS.md](AGENTS.md) | AI architecture (EANN + GOAP + Boids) |
| [docs/](docs/) | SoF2 source code reference & PDF documentation |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Full architecture spec & coding rules |