# Copilot Instructions for RemakeSoF

## Project Architecture
- **Core Movement**: Faithful recreation of Quake III/SoF2 movement logic, with Unity-specific adaptations.
- **Asset Management**:
  - **PrefabManager**: Clean service (no MonoBehaviour) for loading prefabs via Addressables. Cache-first strategy, delegates to `PrefabRegistry`. Use via DI, not singleton.
  - **PrefabRegistry**: Internal cache + Addressables loading. Manages handles, releases on clear. Only accessed through `PrefabManager`.
  - **PrefabDataFactory**: Creates `PrefabData` instances from loaded prefabs. Used by `PrefabManager`.
  
## Patterns & Conventions
- **Dependency Injection**: Inject `PrefabManager` into systems that need prefab loading.
- **Cache-First Loading**: `PrefabManager` checks `PrefabRegistry` cache before loading from Addressables. Never bypass caches manually.
- **Manual Physics**: Prefer explicit physics/collision code over Unity built-ins for core movement.