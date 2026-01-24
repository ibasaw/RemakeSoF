# Copilot Instructions for RemakeSoF

## Project Architecture
- **Core Movement**: Faithful recreation of Quake III/SoF2 movement logic, with Unity-specific adaptations. See [README.md](../README.md) for deep-dive on `bg_pmove` logic, including step/slide/collision and animation state handling.
- **Asset Management**:
  - **PrefabManager**: Clean service (no MonoBehaviour) for loading prefabs via Addressables. Cache-first strategy, delegates to `PrefabRegistry`. Use via DI, not singleton.
  - **PrefabRegistry**: Internal cache + Addressables loading. Manages handles, releases on clear. Only accessed through `PrefabManager`.

WIP:
  - **TextureRegistry**: Observer pattern, dynamic asset loading for textures.
  - **PlayerSkinDataRegistry**: Static lookup for skin definitions and surface mappings.
  - **AI/Agents**: Multi-layered agent architecture:
  - **EANN**: Neural net "brain" (inputs: world, neighbors, goals; outputs: movement/decisions)
  - **FSM/BT**: Interprets EANN output, sets high-level goals
  - **Boids/Steering**: Handles physical movement, separation/cohesion/alignment
  - **Agent/Swarm**: Emergent behavior, environment response
  - **Evolution/Training**: (PSO/GA/NEAT) for offline/online learning
  - **Persistence**: EANN weights, global memory, and agent state can be saved/loaded (see [AGENTS.md](../AGENTS.md))

## Developer Workflows
- **Movement Logic**: For 1:1 Quake-like movement, avoid Unity's `CharacterController.Move` for collision/slide/step. Instead, use manual `CapsuleCast`-based step/slide logic (see [README.md](../README.md), section on `PM_StepSlideMoveManual`).
- **Input Scaling**: Normalize input vectors for movement; do not use Quake's 127 constant with Unity's InputSystem (see [README.md](../README.md), `PM_CmdScale` fix).
- **Asset Loading**: 
  - Use `PrefabManager.LoadPrefab<T>(key)` or `LoadPrefabAsync<T>(key)` for Addressables-based loading.
  - Manager handles caching automatically (cache-first).
  - Inject `PrefabManager` via DI, avoid singleton access.
  - For maps, characters, weapons: Use chunked loading patterns.
- **AI Save/Load**: Persist EANN weights, global memory, and optionally agent state for consistent emergent behavior across sessions ([AGENTS.md](../AGENTS.md)).

## Patterns & Conventions
- **Dependency Injection**: Used in `PlayerSkinDataRegistry` for error handling and host communication. Inject `PrefabManager` into systems that need prefab loading.
- **Cache-First Loading**: `PrefabManager` checks `PrefabRegistry` cache before loading from Addressables. Never bypass cache manually.

- **Observer Pattern**: Used in `TextureRegistry` for asset change notifications.
- **Manual Physics**: Prefer explicit physics/collision code over Unity built-ins for core movement.
- **Emergent AI**: Agent behavior emerges from EANN+FSM+Boids stack; see [AGENTS.md](../AGENTS.md) for diagrams and save/load points.

## Key Files & References
- [README.md](../README.md): Movement, registry, and architecture details
- [AGENTS.md](../AGENTS.md): AI/agent architecture, persistence, and diagrams
- [Assets/README.md](../../Assets/README.md): Asset import, chunking, and TODOs

## Example: Manual Step/Slide Move
See [README.md](../README.md) for a C# pseudocode loop for Quake-style movement using `CapsuleCast` and velocity clipping.

---

**When in doubt, prefer explicit, Quake-style logic over Unity shortcuts.**

If you need to extend agent behavior, follow the EANN→FSM→Boids→Agent stack and persist relevant state for reproducibility.
