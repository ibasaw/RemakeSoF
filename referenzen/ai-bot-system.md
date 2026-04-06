# AI Bot System

Server-seitige AI-Bot-Verwaltung mit identischer Physik, Hitboxes und Collidern wie menschliche Spieler.
Bots werden über die Server-Konfiguration (JSON) konfiguriert und beim Match-Start automatisch gespawnt.

---

## Architektur-Überblick

```
Server-Start
  └─ ApplicationEntryPoint.Awake()
       └─ ServiceLocator.Register(ServerConfigurationLoader)
            └─ SoF2_Server_Configuration.json geladen
                 └─ sv_botcount, sv_botnames, sv_botskins

Map geladen
  └─ RoundFlowStateMachine → WaitingForReadyState.Enter()
       └─ AIBotSpawner.SpawnInitialBots()
            ├─ LoadBotConfiguration() ← liest sv_botcount/names/skins aus ServerConfig
            ├─ PrefabManager.LoadPrefab("AICharacter") ← sync, cache-first
            └─ SpawnBot() × N
                 ├─ Instantiate(prefab) + NetworkObject.Spawn() (server-owned)
                 ├─ GametypeManager.AssignTeam() → TeamId
                 └─ NetworkedAICharacter.InitializeBot(name, skin, teamId)
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **AIBotSpawner** | `Assets/Scripts/Runtime/Game/Networked/AIBotSpawner.cs` | Server-seitiges Spawn/Despawn/Respawn, liest Config |
| **NetworkedAICharacter** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedAICharacter.cs` | Networked AI, Position-Sync, Visual-Load, Hitbox/Collider Init |
| **ServerAICharacter** | `Assets/Scripts/Runtime/Game/Characters/Server/ServerAICharacter.cs` | Server-Physik (PlayerPhysicsSimulation), GOAP/EANN Platzhalter |
| **ServerCharacterController** | `Assets/Scripts/Runtime/Game/Characters/Server/ServerCharacterController.cs` | Damage/Death/Respawn (shared mit Spieler) |
| **ClientHitboxSystem** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientHitboxSystem.cs` | 29 Bone-basierte BoxCollider-Trigger |
| **ClientColliderSystem** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientColliderSystem.cs` | SoF2 AABB BoxCollider (Single Source of Truth) |
| **PlayerPhysicsSimulation** | `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerPhysicsSimulation.cs` | SoF2/Quake3 Physik-Engine |

---

## Server-Konfiguration (SoF2_Server_Configuration.json)

```json
{
    "sv_botcount": 1,
    "sv_botnames": ["Hawk", "Viper", "Ghost", "Snake", "Jackal", ...],
    "sv_botskins": ["mullins_jungle"]
}
```

| CVAR | Typ | Default | Beschreibung |
|------|-----|---------|-------------|
| `sv_botcount` | `int` | `0` | Anzahl Bots beim Server-Start (0 = keine) |
| `sv_botnames` | `string[]` | 15 SoF2-Namen | Bot-Namen, Round-Robin-Zuweisung |
| `sv_botskins` | `string[]` | `["mullins_jungle"]` | Bot-Skins, Round-Robin-Zuweisung |

Fallback auf Default-Arrays wenn Config leer/null.

---

## Prefab-Struktur (AICharacter)

Das AICharacter-Prefab benötigt folgende Komponenten:

| Komponente | Serialisierte Referenz |
|------------|----------------------|
| `NetworkObject` | — |
| `NetworkedAICharacter` | → m_ServerAICharacter, m_SkinHandler, m_CharacterState, m_ServerCharacterController, m_HitboxSystem, m_ColliderSystem |
| `ServerAICharacter` | → m_NetworkedAICharacter |
| `NetworkedCharacterState` | — |
| `ServerCharacterController` | → m_CharacterState |
| `ClientHitboxSystem` | — |
| `ClientColliderSystem` | — |
| `AICharacterSkinHandler` | — |

---

## Physik-System (identisch mit Spieler)

`ServerAICharacter` nutzt dieselbe `PlayerPhysicsSimulation` wie `ServerPlayerCharacter`:

- **SoF2/Quake3 Physik**: Gravity, Ground-Trace, Friction, Air Control, Knockback
- **Capsule-Dimensionen**: Kommen von `ClientColliderSystem` via `SetColliderSystem()`
- **GroundMask**: `~0` (alle Layer, Self-Collision via Collider-Toggle verhindert)
- **PhysicsCollider**: `ClientColliderSystem.PhysicsCollider` (shared BoxCollider)
- **Aktuell**: Leerer `PlayerCommand` (MoveInput = zero) → Bot steht still

```
ClientColliderSystem (SoF2 Authentic)
  ├─ k_SoF2StandingHeight = 2.2606f  (89 QU)
  ├─ k_SoF2CrouchingHeight = 1.6256f (64 QU)
  └─ k_SoF2Radius = 0.381f           (15 QU)
```

Diese Konstanten sind `internal` und werden auch von `ServerPlayerCharacter` referenziert → **Single Source of Truth**.

---

## Hitbox-System

`NetworkedAICharacter.OnVisualInstantiated()` wird nach dem Skin-Load aufgerufen:

1. **BuildHitboxes**: `ClientHitboxSystem.BuildHitboxes(visualRoot)` → 29 Bone-basierte BoxCollider
2. **InitCollider**: `ClientColliderSystem.CalculateAutoCapsuleSize(bones, isCrouching=false)` → SoF2 AABB
3. **Server-Sync**: `ServerAICharacter.SetColliderSystem(colliderSystem)` → Physik übernimmt Dimensionen

Dies passiert auf **allen Seiten** (Server + Client), damit Hitboxes und Collider überall konsistent sind.

---

## Bot als "Spieler" für Runden-Logik

Bots werden in der Runden-Logik als vollwertige Spieler gezählt:

| System | Wie Bots gezählt werden |
|--------|------------------------|
| `RoundFlowStateMachine.GetTotalPlayerCount()` | Humans + `AIBotSpawner.SpawnedBotCount` |
| `RoundFlowStateMachine.UpdatePlayerCounts()` | Setzt `playersConnected` und `MinPlayersReached` inkl. Bots |
| `ServerListeningState.CountTeams()` | Iteriert `AIBotSpawner.SpawnedBots` für Team-Zählung |
| `NetworkedGameState.AwardSurvivalKills()` | Iteriert humans + `AIBotSpawner.SpawnedBots` |
| `GametypeManager.AssignTeam()` | `CountAllTeamMembers()` zählt humans + bots |

---

## Spawn-Flow

```
WaitingForReadyState.Enter()
  └─ AIBotSpawner.SpawnInitialBots()
       ├─ LoadBotConfiguration() ← sv_botcount/names/skins
       ├─ for (i < botCount): SpawnBot()
       │    ├─ Instantiate + NetworkObject.Spawn()
       │    ├─ InitializeBot(name, skin, teamId)
       │    └─ m_SpawnedBots.Add(networkObject)
       └─ OnInitialBotsSpawned?.Invoke()
            └─ WaitingForReadyState.OnBotsSpawned()
                 ├─ UpdatePlayerCounts()
                 └─ TryStartMatch()
```

---

## Respawn-Flow

```
RoundFlowStateMachine (Warmup-Ende)
  └─ AIBotSpawner.RespawnAllBots()
       └─ foreach bot: NetworkedAICharacter.RespawnAtNextSpawnPoint()
```

---

## Cleanup

```
Map-Wechsel / OnDestroy:
  └─ AIBotSpawner.DespawnAllBots()
       └─ foreach bot: NetworkObject.Despawn()
       └─ m_SpawnedBots.Clear()
```

---

## Zukunft: AI-Verhalten (GOAP/EANN)

`ServerAICharacter.Update()` hat TODO für AI-Logik (aktuell idle).
Geplante Architektur (siehe AGENTS.md):

```
EANN (Neuronales Netz) → Entscheidet Ziele + Parameter
  └─ GOAP (Goal-Oriented Action Planning) → Dynamische Aktionsplanung
       └─ Boids / Steering → Physische Bewegung
            └─ PlayerCommand.MoveInput + Buttons → PlayerPhysicsSimulation
```

Wenn AI aktiv: `PlayerCommand` wird mit echten Werten gefüllt statt leerer Input.
