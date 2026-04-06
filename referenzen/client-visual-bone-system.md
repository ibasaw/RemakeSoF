# Client-Side Visual Bone System

## Übersicht

Rein kosmetische Bone-Korrekturen auf dem Owner-Client — beeinflussen **keine Physik, Collision oder Position**.
Laufen in `LateUpdate()` nach dem Animator-Pass.

## Warum nur Client-seitig?

| Aspekt | Server | Client (Owner) |
|--------|--------|-----------------|
| Position / Velocity | ✅ `PlayerPhysicsSimulation` | ✅ Prediction via `PlayerPhysicsSimulation` |
| Ground-State / Jump | ✅ autoritativ | ✅ lokal + Reconciliation |
| Pelvis / Legs Rotation | ❌ nicht nötig | ✅ `UpdatePelvisRotation()` |
| Lumbar Lean / Twist | ❌ nicht nötig | ✅ `UpdateLumbarRotation()` |
| Movement Direction (0-7) | ❌ nicht nötig | ✅ `ComputeMovementDir()` |
| Animation-State | ❌ | ✅ Owner schreibt `NetworkAnimationState` |

**Server braucht nur:** Position, Velocity, GroundState, IsJumping — geliefert durch `PlayerPhysicsSimulation`.

**Remote-Clients sehen Animationen** über `NetworkAnimationState` (Speed, Horizontal, Vertical, IsMoving, IsGrounded, IsWalking, IsCrouching).

## SoF2/Q3 Vergleich

| SoF2/Q3 | Unity |
|---------|-------|
| Server: `playerState_t` (pos, vel, angles, groundstate) | Server: `ServerMovementAck` (Position, Velocity, IsGrounded, IsJumping) |
| Client: `cg_players.c` (Torso-Twist, Legs-Facing, Lean) | Client: `UpdatePelvisRotation()`, `UpdateLumbarRotation()` |

## Systeme im Detail

### 1. Pelvis / Legs Rotation (`UpdatePelvisRotation()`)

- **SmoothedLegsForward**: Slerp-basiert, dreht Legs in Bewegungsrichtung
- **Movement**: `m_LegsYawOffsetDegrees` (90°) Skeleton-Korrektur für SoF2-Modelle
- **Idle**: `m_IdleYawByDir[8]` — pro Richtungsindex ein Yaw-Offset
- **Torso-Follow**: Legs drehen sich langsam Richtung Kamera wenn kein Input

### 2. Lumbar Rotation (`UpdateLumbarRotation()`)

- **Lower Lumbar + Upper Lumbar** schauen zum LookAt-Punkt (`PitchTarget.forward * 100m`)
- **Lean**: Roll bei Seitwärtsbewegung (`m_RollLeanDegrees`), Pitch bei Vor/Rückwärts (`m_PitchLeanDegrees`)
- **Strafe-Yaw-Twist**: `m_StrafeYawDegrees` auf Lower, 60% auf Upper
- **Lumbar-Offsets**: Yaw + Pitch Offsets pro Lumbar-Bone (gesmoothed)
- **Movement-Idle-Offset**: Zusätzlicher Yaw-Offset auf Upper Lumbar basierend auf `m_MovementOffsets[dir]`

### 3. Movement Direction (`ComputeMovementDir()`)

- SoF2 `PM_SetMovementDir` Logik
- Index 0-7: Forward, Forward-Right, Right, Back-Right, Back, Back-Left, Left, Forward-Left
- Bestimmt Idle-Pose-Offset für Legs und Upper Lumbar

## Serialisierte Parameter (Inspector)

```
[Header("Pelvis / Legs Facing")]
m_LegsYawOffsetDegrees    = 90       — Skeleton-Korrektur (SoF2-Modelle)
m_IdleYawByDir[8]         = {112, 45, 68, 68, 112, 180, 180, 90}
m_BaseLegsRotationSmooth  = 8
m_TorsoFollowYawInfluence = 65

[Header("Lumbar Yaw Offsets")]
m_UpperLumbarYawOffset    = -5
m_LowerLumbarYawOffset    = 0
m_LumbarYawSmooth         = 8

[Header("Lumbar Pitch Offsets")]
m_UpperLumbarPitchOffset  = 0
m_LowerLumbarPitchOffset  = 0
m_LumbarPitchSmooth       = 8

[Header("Lean / Strafe")]
m_RollLeanDegrees         = 25
m_PitchLeanDegrees        = 20
m_LeanSmooth              = 8
m_StrafeYawDegrees        = 12

[Header("Movement Direction Idle Offsets")]
m_MovementOffsets[8]      = {0, 0, 0, 0, 0, 0, 0, 0}
m_MovementOffsetSmooth    = 6
```

## Datei

Alle Bone-Visuals liegen in `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs`:
- Methoden: `UpdatePelvisRotation()`, `UpdateLumbarRotation()`, `ComputeMovementDir()`
- Aufgerufen in: `LateUpdate()` (Owner-only)
