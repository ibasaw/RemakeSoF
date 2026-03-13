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
│  ● PM_GroundTrace (BoxCast Ground-Detection VOR + NACH Bewegung)               │
│  ● PM_WalkMove / PM_AirMove (Friction + Accelerate + StepSlideMove)            │
│  ● PM_SlideMove (Multi-Plane Clipping, Gravity Half-Step)                      │
│  ● PM_StepSlideMove (SlideMove → Step-Up → SlideMove → Step-Down)              │
│  ● PM_Friction / PM_Accelerate / PM_ClipVelocity / PM_CheckJump               │
│  ● ResolvePenetration (OverlapBox + Depenetration)                             │
└────────────────────────────────────────────────────────────────────────────────┘
```

## Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerPhysicsSimulation.cs` | `[Serializable]` SoF2-Physik-Simulation. Wird inline auf Client und Server im Inspector serialisiert. Keine Duplikation der Parameter. |
| `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerCommand.cs` | `PlayerCommand` (Input-Struct) + `ServerMovementAck` (Bestätigung). Beide `INetworkSerializable`. |
| `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | Owner-Client: Input → Prediction → Ringbuffer → RPC → Reconciliation. Hält `[SerializeField] PlayerPhysicsSimulation m_Simulation`. BoxCollider disable/enable für Self-Collision. |
| `Assets/Scripts/Runtime/Game/Characters/Server/ServerPlayerCharacter.cs` | Server: Empfängt Commands → `ProcessCommand()` → autoritative Position. Hält eigene `[SerializeField] PlayerPhysicsSimulation m_Simulation`. BoxCollider disable/enable für Self-Collision. |
| `Assets/Scripts/Runtime/Game/Characters/Client/ClientColliderSystem.cs` | Box-Dimensionen aus Bones / SoF2-Fixwerte berechnen, Visual-Debug-Mesh. |
| `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` | RPC-Bridge: `SubmitCommandServerRpc`, `MovementAckClientRpc`, `SubmitCapsuleDimensionsServerRpc`. |

## Physik-Pipeline (pro Frame) — SoF2 PmoveSingle Exact Port

```
Simulate(ref position, cmd):
  1. m_PreviousVelocity = Velocity  (für CrashLand)
  2. PMD_JUMP release: Button losgelassen → IsDebounceActive = false
  3. pm_time countdown (Landing-Lockout)
  4. PM_GroundTrace(ref position)   ← Boden VOR Bewegung prüfen
  5. m_Walking?
     ├─ Ja:  PM_WalkMove(ref position, cmd)
     │         ├─ PM_CheckJump → falls ja: PM_AirMove + return
     │         ├─ PM_Friction
     │         ├─ Forward/Right via ClipVelocity auf Ground projizieren
     │         ├─ PM_Accelerate(wishdir, wishspeed, PmAccelerate)
     │         ├─ ClipVelocity + Speed-Restore (Slope-Speed erhalten)
     │         └─ PM_StepSlideMove(ref position, gravity=false)
     └─ Nein: PM_AirMove(ref position, cmd)
               ├─ PM_Friction (kein Drop in Luft)
               ├─ PM_Accelerate(wishdir, wishspeed, PmAirAccelerate)
               ├─ Clip gegen steile GroundPlane (falls vorhanden)
               └─ PM_StepSlideMove(ref position, gravity=true)
                    └─ PM_SlideMove: Gravity Half-Step Integration
                       endVel.y = vel.y - g*dt
                       vel.y = (vel.y + endVel.y) * 0.5
  6. PM_GroundTrace(ref position)   ← Boden NACH Bewegung prüfen
  7. Landing-Detection + Airtime-Tracking
```

## Physik-Parameter (SoF2 Defaults, ×0.0254 Inches→Meter)

| Parameter | SoF2 Original (QU) | Unity-Wert (m) | Erklärung |
|-----------|---------------------|----------------|-----------|
| `PmAccelerate` | 10.0 | 10.0 | Boden-Beschleunigung (dimensionslos) |
| `PmAirAccelerate` | 1.0 | 1.0 | Luft-Beschleunigung (dimensionslos) |
| `PmFriction` | 6.0 | 6.0 | Boden-Reibung (dimensionslos) |
| `PmStopSpeed` | 100 | 2.54 | Stop-Speed Schwelle (100 × 0.0254) |
| `PmMaxSpeed` | 280 (g_speed) | 7.112 | Wish-Speed (280 × 0.0254) |
| `PmGravity` | 800 | 20.32 | Gravitation (800 × 0.0254) |
| `JumpVelocity` | 270 | 6.858 | Sprung-Y-Velocity (270 × 0.0254) |
| `PmMaxSteepness` | 0.7 | 0.7 | MIN_WALK_NORMAL (dimensionslos) |
| `PmStepSize` | 18 | 0.4572 | STEPSIZE (18 × 0.0254) |
| `PmMaxBarrier` | 32 | 0.8128 | Max Barriere (32 × 0.0254) |
| `PmDuckScale` | 0.25 | 0.25 | Duck Speed Scale (dimensionslos) |

### Interne Konstanten

| Konstante | Wert | SoF2 Original | Erklärung |
|-----------|------|---------------|-----------|
| `OVERCLIP` | 1.001 | 1.001 | Float-Precision Guard in ClipVelocity |
| `SKIN_WIDTH` | 0.02m | — | Minimaler Abstand zu Oberflächen (Unity-spezifisch) |
| `MAX_CLIP_PLANES` | 5 | 5 | Max Planes in PM_SlideMove |
| `GROUND_TRACE_DIST` | 0.08m | 0.00635m (0.25 QU) | Ground-Trace Distanz (erhöht für Unity BoxCast) |
| `MAX_DEPENETRATION_ITERATIONS` | 3 | — | Depenetration Loops (Unity-spezifisch) |

### Sprung-Physik — Theoretisch vs Gemessen

| Wert | SoF2 Theorie | Unity Gemessen | Abweichung |
|------|--------------|----------------|------------|
| Airtime (Stillstand) | 0.675s | 0.64–0.65s | -4% (GROUND_TRACE_DIST) |
| Jump Phase | 0.338s | 0.32–0.33s | -3% |
| Fall Phase | 0.338s | 0.32s | -5% |
| Jump Height | 1.157m | 1.0m | -13% (Ground early detect) |
| Vert Path (Stillstand) | ~2.3m | 2.2m | -4% |

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
| `SubmitCapsuleDimensionsServerRpc(h, r, cx, cy, cz)` | Client → Server | Box-Dimensionen vom Bone-System |

## Vorher → Nachher

| Aspekt | Vorher | Nachher |
|--------|--------|---------|
| Physik-Ausführung | Nur Client | Client (Prediction) + Server (Authority) |
| Netzwerk-Daten | Position + Rotation (RPC) | Nur Input (PlayerCommand RPC) |
| Server-Validierung | Distanz-Check | Volle Physik-Simulation + Anti-Cheat |
| Physik-Parameter | Dupliziert (Client-Fields + Simulation) | Einmal in `[Serializable] PlayerPhysicsSimulation` |
| Client-Korrektur | Keine | Reconciliation mit Command-Replay |
| Cheat-Schutz | Minimal | Server-autoritativ, Input-Clamping |
| Kollisionsform | CapsuleCast (nicht authentisch) | BoxCast AABB (SoF2-authentisch) |
| Velocity-Capping | `ApplyVelocityLimits()` | Entfernt — SoF2 hat keinen Hard-Cap |

## SoF2-Authentizitäts-Vergleich

| Aspekt | SoF2 Original | Unity Port | Status |
|--------|---------------|-----------|--------|
| Shared Simulation Code | `bg_pmove.c` auf Client + Server | `PlayerPhysicsSimulation` auf Client + Server | ✅ Identisch |
| Server Authority | Server führt `Pmove()` aus | Server führt `Simulate()` aus | ✅ Identisch |
| Client Prediction | `cg_predict.c` → `Pmove()` | `ClientPlayerCharacter` → `Simulate()` | ✅ Identisch |
| Input-Only Networking | Client sendet usercmd_t | Client sendet PlayerCommand | ✅ Identisch |
| Server Acknowledges | Server sendet playerState_t | Server sendet ServerMovementAck | ✅ Identisch |
| Reconciliation | Client replays ab letztem Ack | Client replays ab letztem Ack | ✅ Identisch |
| Kollision: BoxCast AABB | `trap_Trace(mins, maxs)` AABB | `Physics.BoxCast(halfExtents, identity)` | ✅ Identisch |
| Self-Collision | `clientNum` Skip | BoxCollider disable/enable | ✅ Konzeptuell identisch |
| Gravity Half-Step | `(vel + endVel) * 0.5` | `(Velocity.y + endVelocity.y) * 0.5f` | ✅ Identisch |
| 4-Bump SlideMove | `PM_SlideMove` | `PM_SlideMove` | ✅ 1:1 Port |
| StepSlideMove + Distanzvergleich | Step-Up → SlideMove → Step-Down → Compare | Identisch | ✅ 1:1 Port |
| Jump Debounce | `PMD_JUMP` + `pm_time` | `IsDebounceActive` + `JumpDebounce` | ✅ Identisch |
| Quake-Accelerate (ermöglicht Strafe-Jumping) | `PM_Accelerate` | `PM_Accelerate` | ✅ 1:1 Port |
| Velocity-Capping | Keiner (nur wishspeed Clamp) | Keiner (nur wishspeed Clamp) | ✅ Identisch |

## Inspector-Hinweise

- **ClientPlayerCharacter**: `m_Simulation` Foldout enthält alle Physik-Parameter. `m_Simulation` wird direkt serialisiert (kein Copy-Block mehr).
- **ServerPlayerCharacter**: Eigenes `m_Simulation` Foldout mit gleichen Defaults. `m_ServerPlayerCharacter` Reference auf NetworkedPlayerCharacter zuweisen.
- **NetworkedPlayerCharacter**: `m_ServerPlayerCharacter` Reference zuweisen.

## SoF2-Vergleich

| SoF2 (C) | Unity (C#) |
|-----------|------------|
| `bg_pmove.c` / `bg_slidemove.c` | `PlayerPhysicsSimulation.cs` |
| `PmoveSingle()` Pipeline | `Simulate()` — exakte Pipeline: GroundTrace → Walk/AirMove → GroundTrace |
| `PM_GroundTrace()` Trace 0.25 QU down | `PM_GroundTrace()` CapsuleCast 0.08m down |
| `PM_WalkMove()` CheckJump→Friction→Accel→Clip | `PM_WalkMove()` exakt gleiche Reihenfolge |
| `PM_AirMove()` Friction→Accel→Clip steep | `PM_AirMove()` exakt gleiche Reihenfolge |
| `PM_SlideMove()` 4-plane multi-clip + gravity half-step | `PM_SlideMove()` exakter Port: 5 clip-planes, crease-sliding |
| `PM_StepSlideMove()` slide→stepup→slide→stepdown | `PM_StepSlideMove()` exakter Port |
| `PM_ClipVelocity()` OVERCLIP=1.001 | `PM_ClipVelocity()` identisch |
| `PM_CheckJump()` PMD_JUMP debounce | `PM_CheckJump()` exakter Port |
| `usercmd_t` | `PlayerCommand` (INetworkSerializable) |
| Server berechnet Physik, sendet `playerState_t` | Server berechnet Physik, sendet `ServerMovementAck` |
| Client prediction + correction in `cg_predict.c` | Prediction in `ClientPlayerCharacter`, Reconciliation via Ringbuffer |
| Trace-basierte Kollision (BSP) | `Physics.CapsuleCast` (Unity Meshes) |
| Alle Werte in Quake Units (Inches) | Alle Werte ×0.0254 für Meter |
