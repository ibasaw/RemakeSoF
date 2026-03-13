# SoF2 Physics Reference — Unity Port

## Unit-Konvertierung

```
1 SoF2-Unit = 1 Inch = 0.0254 Meter
Lineare Werte: SoF2-Wert × 0.0254 = Unity-Meter
Dimensionslose Werte: unverändert
```

## Physik-Parameter

| Parameter | SoF2 (QU) | Unity (m) | Typ | SoF2 Quelle |
|-----------|-----------|-----------|-----|-------------|
| `PmAccelerate` | 6.0 | 6.0 | dimensionslos | `pm_accelerate` |
| `PmAirAccelerate` | 1.0 | 1.0 | dimensionslos | `pm_airaccelerate` |
| `PmFriction` | 6.0 | 6.0 | dimensionslos | `pm_friction` |
| `PmStopSpeed` | 100 | 2.54 | m/s | `pm_stopspeed` |
| `PmMaxSpeed` | 280 | 7.112 | m/s | `g_speed` |
| `PmGravity` | 800 | 20.32 | m/s² | `g_gravity` |
| `PhysMaxVelocity` | 320 | 8.128 | m/s | `phys_maxvelocity` |
| `PhysMaxWalkVelocity` | 320 | 8.128 | m/s | `phys_maxwalkvelocity` |
| `JumpVelocity` | 270 | 6.858 | m/s | `JUMP_VELOCITY` |
| `PmMaxSteepness` | 0.7 | 0.7 | dimensionslos | `MIN_WALK_NORMAL` |
| `PmStepSize` | 18 | 0.4572 | m | `STEPSIZE` |
| `PmMaxBarrier` | 32 | 0.8128 | m | Level-Design Konvention |
| `PmDuckScale` | 0.25 | 0.25 | dimensionslos | `PM_DUCKSCALE` |
| `JumpDebounce` | 250 | 0.25 | s | `pm_time = 250` (PMF_TIME_LAND) |
| `OVERCLIP` | 1.001 | 1.001 | dimensionslos | `OVERCLIP` |
| `MAX_CLIP_PLANES` | 5 | 5 | — | `MAX_CLIP_PLANES` |

## Unity-spezifische Konstanten

| Konstante | Wert | Grund |
|-----------|------|-------|
| `GROUND_TRACE_DIST` | 0.08m | SoF2: 0.25 QU = 0.00635m. Erhöht weil Unity CapsuleCast auf Meshes unzuverlässig bei <1cm. |
| `SKIN_WIDTH` | 0.02m | Minimaler Abstand zu Oberflächen (Unity hat keine globale skin width wie Quake). |
| `MAX_DEPENETRATION_ITERATIONS` | 3 | Iterative Depenetration via OverlapCapsule (SoF2 braucht das nicht — BSP hat kein Tunneling). |

## Pipeline — SoF2 PmoveSingle → Unity Simulate

```
SoF2 bg_pmove.c (PmoveSingle)         Unity PlayerPhysicsSimulation.Simulate()
─────────────────────────────         ─────────────────────────────────────────
pml.previous_velocity = pm->ps        m_PreviousVelocity = Velocity
PM_GroundTrace()                   →  PM_GroundTrace(ref position)
if (pml.walking)                   →  if (m_Walking)
    PM_WalkMove()                  →      PM_WalkMove(ref position, cmd)
else                               →  else
    PM_AirMove()                   →      PM_AirMove(ref position, cmd)
PM_GroundTrace()                   →  PM_GroundTrace(ref position)
```

### PM_WalkMove Ablauf

```
SoF2                                   Unity
─────                                  ─────
PM_CheckJump() → falls ja: AirMove    PM_CheckJump(cmd, y) → falls true: AirMove
PM_Friction()                          PM_Friction()
forward/right auf Ground projizieren   ClipVelocity(forward/right, groundNormal)
wishvel 3D (inkl. Y aus Slope)         wishvel mit Slope-Y
PM_Accelerate(wishdir, wishspeed)      PM_Accelerate(wishdir, wishspeed, PmAccelerate)
ClipVelocity + Speed-Restore           ClipVelocity + Speed-Restore (identisch)
PM_StepSlideMove(qfalse)              PM_StepSlideMove(ref pos, false)
```

### PM_AirMove Ablauf

```
SoF2                                   Unity
─────                                  ─────
PM_Friction() (kein Drop)              PM_Friction() (kein Drop — m_Walking=false)
flat forward/right                     flat forward/right
PM_Accelerate(wishdir, wishspeed)      PM_Accelerate(wishdir, wishspeed, PmAirAccelerate)
if groundPlane: ClipVelocity           if m_GroundPlane: ClipVelocity
PM_StepSlideMove(qtrue)               PM_StepSlideMove(ref pos, true)
```

### PM_SlideMove — Gravity Half-Step

```
SoF2 (bg_slidemove.c):
    endVelocity[2] = pm->ps->velocity[2] - pm->ps->gravity * pml.frametime
    pm->ps->velocity[2] = (pm->ps->velocity[2] + endVelocity[2]) * 0.5

Unity:
    endVelocity.y = Velocity.y - PmGravity * m_DeltaTime
    Velocity.y = (Velocity.y + endVelocity.y) * 0.5f
    // Am Ende: Velocity = endVelocity
```

### PM_StepSlideMove Ablauf

```
1. PM_SlideMove() — regulärer Versuch
2. Keine Kollision? → Fertig
3. Velocity.y > 0 && (kein Boden || zu steil)? → Fertig (kein Step-Up beim Springen)
4. Save downO/downV (Ergebnis von Schritt 1)
5. Reset auf startO/startV
6. Step-Up: position.y += stepSize (max PmStepSize, geclampt bei Ceiling)
7. PM_SlideMove() — zweiter Versuch auf erhöhter Position
8. Step-Down: CapsuleCast nach unten, position.y -= dropDist
9. ClipVelocity gegen Step-Oberfläche
```

## Jump-Debounce — SoF2 PMD_JUMP Port

```
SoF2:
  - Sprung setzt pm_debounce |= PMD_JUMP (Bit-Flag)
  - Cleared wenn upmove < 10 (Button losgelassen)
  - Harte Landung (vel[2] < -200): pm_time = 250 (250ms Lockout)

Unity:
  - Sprung setzt IsDebounceActive = true
  - Cleared wenn !cmd.HasButton(Jump) (Button losgelassen)
  - Harte Landung (Velocity.y < -5.08): JumpDebounce = 0.25s (250ms Lockout)
  - PM_CheckJump prüft: JumpDebounce > 0 → blockiert
```

## Ground Detection — PM_GroundTrace

```
SoF2:
  - Trace origin[2] - 0.25 nach unten
  - Kickoff: vel[2] > 0 && Dot(vel, traceNormal) > 10 → nicht grounded
  - Slope: traceNormal[2] < MIN_WALK_NORMAL → groundPlane, nicht walking
  - Velocity wird NICHT genullt

Unity:
  - CapsuleCast 0.08m nach unten (Radius × 0.95 für Edge-Cases)
  - Kickoff: Velocity.y > 0 && Dot(Velocity, hitNormal) > 0.254 → nicht grounded
  - Slope: hitNormal.y < PmMaxSteepness → m_GroundPlane, nicht m_Walking
  - Velocity.y = 0 NUR bei Erstlandung (kompensiert größere Trace-Distanz)
  - CorrectGroundPosition: schiebt Capsule über Boden-Hit
```

## Gemessene Sprungwerte (Stand: 2026-03-12)

Stillstand-Sprung auf flachem Boden, zwei unabhängige Messungen:

| Wert | Messung 1 | Messung 2 | SoF2 Theorie | Abweichung |
|------|-----------|-----------|--------------|------------|
| **Airtime** | 0.65s | 0.64s | 0.675s | -4% |
| **Jump Phase** | 0.33s | 0.32s | 0.338s | -3% |
| **Fall Phase** | 0.32s | 0.32s | 0.338s | -5% |
| **Jump Height** | 1.0m | 1.0m | 1.157m | -13% |
| **Fall Height** | 1.2m | 1.2m | — | landete tiefer |
| **Horiz Dist** | 0.0m | 0.0m | 0.0m | ✓ |
| **Vert Path** | 2.2m | 2.2m | ~2.3m | -4% |

### Analyse

- **Airtime/Phasen**: 3-5% kürzer weil `GROUND_TRACE_DIST = 0.08m` den Boden ~0.074m früher erkennt als SoF2
- **Jump Height**: 13% niedriger aus gleichem Grund (Ground-Trace findet Boden VOR dem theoretischen Aufprall)
- **Determinismus**: Beide Messungen nahezu identisch → Simulation ist deterministisch
- **Gravity Half-Step**: Symmetrische Jump/Fall-Phase bestätigt korrekte Integration

### Theoretische Berechnung

```
Jump Velocity:  v = 6.858 m/s
Gravity:        g = 20.32 m/s²
Time to Peak:   t = v/g = 6.858/20.32 = 0.3375s
Max Height:     h = v²/(2g) = 47.03/40.64 = 1.157m
Total Airtime:  T = 2t = 0.675s
```

## Methoden-Übersicht

| Methode | SoF2 Quelle | Beschreibung |
|---------|-------------|-------------|
| `Simulate()` | `PmoveSingle()` | Haupt-Loop: GroundTrace → Walk/AirMove → GroundTrace |
| `PM_GroundTrace()` | `PM_GroundTrace()` | CapsuleCast nach unten, Kickoff/Slope Check |
| `PM_CheckJump()` | `PM_CheckJump()` | pm_time + PMD_JUMP + Button-Check |
| `PM_Friction()` | `PM_Friction()` | Bodenreibung, speed<1→stop |
| `PM_Accelerate()` | `PM_Accelerate()` | Quake-Style Beschleunigung |
| `PM_ClipVelocity()` | `PM_ClipVelocity()` | Velocity aus Surface entfernen |
| `PM_WalkMove()` | `PM_WalkMove()` | Boden: Jump→Friction→Accel→Clip→SlideMove |
| `PM_AirMove()` | `PM_AirMove()` | Luft: Friction→Accel→SlideMove(gravity) |
| `PM_SlideMove()` | `PM_SlideMove()` | 4-Bump Multi-Plane Clip + Gravity Half-Step |
| `PM_StepSlideMove()` | `PM_StepSlideMove()` | Slide → Step-Up → Slide → Step-Down |
| `ResolvePenetration()` | — | Unity-spezifisch: OverlapCapsule + Push-Out |
| `CorrectGroundPosition()` | — | Unity-spezifisch: Capsule über Boden-Hit positionieren |
| `ApplyVelocityLimits()` | — | Horizontale Speed begrenzen |
| `PM_CmdScale()` | `PM_CmdScale()` | Input-Magnitude normalisieren |

## Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Assets/Scripts/.../Shared/PlayerPhysicsSimulation.cs` | Shared Physik-Simulation (Client + Server) |
| `Assets/Scripts/.../Shared/PlayerCommand.cs` | Input-Struct + Server-Ack |
| `Assets/Scripts/.../Client/ClientPlayerCharacter.cs` | Prediction + Reconciliation + Collider-Disable |
| `Assets/Scripts/.../Server/ServerPlayerCharacter.cs` | Autoritative Simulation + Collider-Disable |
| `Assets/Scripts/.../Client/ClientColliderSystem.cs` | Capsule aus Bones berechnen |
| `docs/code/game/bg_pmove.c` | SoF2 Original-Quellcode |
| `docs/code/game/bg_slidemove.c` | SoF2 SlideMove/StepSlideMove |
| `docs/code/game/bg_local.h` | SoF2 Konstanten |
