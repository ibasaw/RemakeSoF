# Server-Authoritative Movement – SoF2/Quake3 Style

## Architektur-Übersicht

```
┌─────────────────────┐       PlayerCommand (RPC)      ┌──────────────────────┐
│  ClientPlayerChar    │ ───────────────────────────►   │  ServerPlayerChar     │
│  (Owner-Client)      │                                │  (Server Authority)   │
│                      │   ServerMovementAck (RPC)      │                       │
│  ● Input sammeln     │ ◄─────────────────────────── │  ● ProcessCommand()   │
│  ● Prediction lokal  │                                │  ● Physik autoritativ │
│  ● Reconciliation    │                                │  ● Anti-Cheat Clamp   │
└──────────┬───────────┘                                └──────────┬────────────┘
           │                                                       │
           │  m_Simulation.Simulate()                              │  m_Simulation.Simulate()
           ▼                                                       ▼
┌────────────────────────────────────────────────────────────────────────────────┐
│                      PlayerPhysicsSimulation   [Serializable]                  │
│  Shared Pure C# Klasse — identischer Code auf Client + Server                 │
│  ● PM_StepSlideMove (4-Bump CapsuleCast + Step-Up)                             │
│  ● PM_WalkMove / PM_AirMove                                                    │
│  ● PM_Friction / PM_Accelerate / PM_ClipVelocity                               │
│  ● ApplyGravity / ProcessJump                                                   │
│  ● CheckGroundedState / TrySlopeGroundCheck                                     │
└────────────────────────────────────────────────────────────────────────────────┘
```

## Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerPhysicsSimulation.cs` | `[Serializable]` SoF2-Physik-Simulation. Wird inline auf Client und Server im Inspector serialisiert. Keine Duplikation der Parameter. |
| `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerCommand.cs` | `PlayerCommand` (Input-Struct) + `ServerMovementAck` (Bestätigung). Beide `INetworkSerializable`. |
| `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Owner-Client: Input → Prediction → Ringbuffer → RPC → Reconciliation. Hält `[SerializeField] PlayerPhysicsSimulation m_Simulation`. |
| `Assets/Scripts/Runtime/Game/Characters/Server/ServerPlayerCharacter.cs` | Server: Empfängt Commands → `ProcessCommand()` → autoritative Position. Hält eigene `[SerializeField] PlayerPhysicsSimulation m_Simulation`. |
| `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` | RPC-Bridge: `SubmitCommandServerRpc`, `MovementAckClientRpc`, `SubmitCapsuleDimensionsServerRpc`. |

## Physik-Pipeline (pro Frame)

```
1. Input sammeln (MoveInput, YawAngle, Jump, Walk, Crouch)
2. PM_CmdScale → Wish-Speed berechnen
3. CheckGroundedState → Ground/Air unterscheiden
4. Grounded?
   ├─ Ja: PM_Friction → PM_WalkMove → PM_Accelerate → PM_ClipVelocity (Ground-Plane)
   └─ Nein: ApplyGravity → PM_AirMove → PM_Accelerate
5. ProcessJump (falls Jump + Grounded + kein Debounce)
6. PM_StepSlideMove (4-Bump + Step-Up)
7. ApplyVelocityLimits
8. HandleLandingEvents
```

## Physik-Parameter (SoF2 Defaults, ÷10 skaliert)

| Parameter | SoF2 Original | Unity-Wert | Erklärung |
|-----------|---------------|-----------|-----------|
| `PmAccelerate` | 10 | 6.0 | Boden-Beschleunigung |
| `PmAirAccelerate` | 1 | 1.0 | Luft-Beschleunigung |
| `PmFriction` | 6 | 6.0 | Boden-Reibung |
| `PmStopSpeed` | 100 | 10.0 | Stop-Speed Schwelle |
| `PmMaxSpeed` | 280 | 28.0 | g_speed / Wish-Speed |
| `PmGravity` | 800 | 80.0 | Gravitation |
| `PhysMaxVelocity` | 320 | 32.0 | Max Luft-Velocity |
| `PhysMaxWalkVelocity` | 320 | 32.0 | Max Boden-Velocity |
| `JumpVelocity` | 270 | 27.0 | Sprung-Y-Velocity |
| `PmMaxSteepness` | 0.7 | 0.7 | MIN_WALK_NORMAL (dimensionslos) |
| `PmMaxStep` | 18 | 1.8 | Step-Höhe |
| `PmStepSize` | 18 | 1.8 | STEPSIZE |
| `PmMaxBarrier` | 32 | 3.2 | Max Barriere |
| `PmDuckScale` | 0.25 | 0.25 | Duck Speed Scale (dimensionslos) |

## Client-Side Prediction & Reconciliation

```
Client:
  1. PlayerCommand erstellen (SequenceNumber++)
  2. m_Simulation.Simulate() — lokale Vorhersage
  3. Position + Command im Ringbuffer speichern (128 Slots)
  4. Command an Server senden (SendPlayerCommand → SubmitCommandServerRpc)

Server:
  1. Command empfangen → Input validieren (DeltaTime, MoveInput clampen)
  2. m_Simulation.Simulate() — autoritative Physik
  3. ServerMovementAck zurücksenden (Position, Velocity, IsGrounded, IsJumping)

Client (bei Empfang):
  1. Server-Position mit vorhergesagter Position vergleichen
  2. Fehler > 5cm? → Korrektur:
     a. Position + State auf Server-Werte zurücksetzen
     b. Alle unbestätigten Commands (LastAcked+1 bis Current) erneut simulieren
  3. Fehler ≤ 5cm? → Vorhersage war korrekt, keine Aktion
```

## RPC-Übersicht

| RPC | Richtung | Beschreibung |
|-----|----------|-------------|
| `SubmitCommandServerRpc(PlayerCommand)` | Client → Server | Input-Daten für einen Frame |
| `MovementAckClientRpc(ServerMovementAck)` | Server → Client | Autoritative Position + State |
| `CorrectionClientRpc(Vector3, Quaternion)` | Server → Client | Hard-Correction (Respawn/Teleport) |
| `SubmitCapsuleDimensionsServerRpc(h, r, cx, cy, cz, gcd)` | Client → Server | Capsule-Daten vom Bone-System |

## Vorher → Nachher

| Aspekt | Vorher | Nachher |
|--------|--------|---------|
| Physik-Ausführung | Nur Client | Client (Prediction) + Server (Authority) |
| Netzwerk-Daten | Position + Rotation (RPC) | Nur Input (PlayerCommand RPC) |
| Server-Validierung | Distanz-Check | Volle Physik-Simulation + Anti-Cheat |
| Physik-Parameter | Dupliziert (Client-Fields + Simulation) | Einmal in `[Serializable] PlayerPhysicsSimulation` |
| Client-Korrektur | Keine | Reconciliation mit Command-Replay |
| Cheat-Schutz | Minimal | Server-autoritativ, Input-Clamping |

## Inspector-Hinweise

- **ClientPlayerCharacter**: `m_Simulation` Foldout enthält alle Physik-Parameter. `m_Simulation` wird direkt serialisiert (kein Copy-Block mehr).
- **ServerPlayerCharacter**: Eigenes `m_Simulation` Foldout mit gleichen Defaults. `m_ServerPlayerCharacter` Reference auf NetworkedPlayerCharacter zuweisen.
- **NetworkedPlayerCharacter**: `m_ServerPlayerCharacter` Reference zuweisen.

## SoF2-Vergleich

| SoF2 (C) | Unity (C#) |
|-----------|------------|
| `bg_pmove.c` / `bg_slidemove.c` | `PlayerPhysicsSimulation.cs` |
| `usercmd_t` | `PlayerCommand` (INetworkSerializable) |
| Server berechnet Physik, sendet `playerState_t` | Server berechnet Physik, sendet `ServerMovementAck` |
| Client prediction + correction in `cg_predict.c` | Prediction in `ClientPlayerCharacter`, Reconciliation via Ringbuffer |
| `PM_SlideMove` (4-plane clip) | `PM_StepSlideMove` (4-bump CapsuleCast) |
| Trace-basierte Kollision | `Physics.CapsuleCast` |
| Alle Werte in Quake Units | Alle Werte ÷10 für Unity |
