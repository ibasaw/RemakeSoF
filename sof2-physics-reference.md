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
| `JumpVelocity` | 270 | 6.858 | m/s | `JUMP_VELOCITY` |
| `PmMaxSteepness` | 0.7 | 0.7 | dimensionslos | `MIN_WALK_NORMAL` |
| `PmStepSize` | 18 | 0.4572 | m | `STEPSIZE` |
| `PmMaxBarrier` | 32 | 0.8128 | m | Level-Design Konvention |
| `PmDuckScale` | 0.25 | 0.25 | dimensionslos | `PM_DUCKSCALE` |
| `JumpDebounce` | 250 | 0.25 | s | `pm_time = 250` (PMF_TIME_LAND) |
| `OVERCLIP` | 1.001 | 1.001 | dimensionslos | `OVERCLIP` |
| `MAX_CLIP_PLANES` | 5 | 5 | — | `MAX_CLIP_PLANES` |

### Entfernte Nicht-SoF2-Parameter

| Parameter | Wert | Grund der Entfernung |
|-----------|------|---------------------|
| ~~`PhysMaxVelocity`~~ | ~~8.128~~ | SoF2 hat keinen Hard-Velocity-Cap. Nur wishspeed-Clamping existiert. |
| ~~`PhysMaxWalkVelocity`~~ | ~~8.128~~ | Wie oben — `ApplyVelocityLimits()` existierte nicht in bg_pmove.c. |
| ~~`GroundCheckDistance`~~ | ~~0.254~~ | Legacy-Feld, intern nie genutzt. Ground-Detection nutzt `GROUND_TRACE_DIST` Konstante. |

## Unity-spezifische Konstanten

| Konstante | Wert | Grund |
|-----------|------|-------|
| `GROUND_TRACE_DIST` | 0.08m | SoF2: 0.25 QU = 0.00635m. Erhöht weil Unity BoxCast auf Meshes unzuverlässig bei <1cm. |
| `SKIN_WIDTH` | 0.02m | Minimaler Abstand zu Oberflächen (Unity hat keine globale skin width wie Quake). |
| `MAX_DEPENETRATION_ITERATIONS` | 3 | Iterative Depenetration via OverlapBox (SoF2 braucht das nicht — BSP hat kein Tunneling). |

## Kollisionsgeometrie — SoF2 AABB → Unity BoxCast

SoF2 nutzt eine **AABB** (Axis-Aligned Bounding Box) für Spieler-Kollision:

```c
// bg_public.h
playerMins = { -15, -15, -46 }  // Quake-Units
playerMaxs = { +15, +15, +43 }
// → 30 × 30 × 89 QU = 0.762 × 0.762 × 2.2606 m
```

Unity-Port: `Physics.BoxCast` mit `Quaternion.identity` (AABB) statt CapsuleCast:

```
BoxHalfExtents = (CapsuleRadius, CapsuleHeight * 0.5, CapsuleRadius)
                = (0.381, 1.1303, 0.381) m  // Standing
```

### Warum BoxCast statt CapsuleCast

- SoF2 nutzte AABB — für authentisches SoF2-Feeling ist BoxCast (AABB) korrekt
- BoxCast ist auch schneller als CapsuleCast in Unity (einfachere SAT-Tests)
- Capsule-Rundung fängt Kanten/Obstacles anders als SoF2 — verändert Bhop/Step-Up-Verhalten

### Collider-Typen

| Kontext | SoF2 | Unity |
|---------|------|-------|
| Bewegungs-Traces | `trap_Trace(mins, maxs)` AABB | `Physics.BoxCast(halfExtents, Quaternion.identity)` |
| Spieler-Kollision | Clip-Model im Server | `BoxCollider` auf Client/Server-GameObject |
| Self-Collision-Vermeidung | `clientNum` Parameter | Collider disable/enable vor/nach Cast |
| Ground-Detection | AABB-Trace 0.25 QU down | BoxCast 0.08m down (0.95× shrink XZ) |
| Depenetration | BSP hat kein Tunneling | `Physics.OverlapBox` + iteratives Push-Out |

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
Duck: accelerate *= 2                  Duck: accelerate = PmAccelerate * 2
PM_Accelerate(wishdir, wishspeed)      PM_Accelerate(wishdir, wishspeed, accelerate)
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
8. Step-Down: BoxCast nach unten, position.y -= dropDist
9. Horizontal-Distanz-Vergleich: downDist > upDist? → downO/downV beibehalten (SoF2-authentisch)
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
  - Harte Landung (m_PreviousVelocity.y < -6.86): JumpDebounce = 0.25s (250ms Lockout)
    Threshold erhöht weil GROUND_TRACE_DIST (0.08m) Boden früher erkennt als SoF2 (0.006m).
    Zusätzliche Gravity über die extra Distanz: v² = v0² + 2*g*d → ~1.8 m/s extra.
  - PM_CheckJump prüft: JumpDebounce > 0 → blockiert
```

## Ground Detection — PM_GroundTrace

```
SoF2:
  - Trace origin[2] - 0.25 nach unten (AABB)
  - Kickoff: vel[2] > 0 && Dot(vel, traceNormal) > 10 → nicht grounded
  - Slope: traceNormal[2] < MIN_WALK_NORMAL → groundPlane, nicht walking
  - Velocity wird NICHT genullt

Unity:
  - BoxCast 0.08m nach unten (XZ × 0.95 für Edge-Cases)
  - Kickoff: Velocity.y > 0 && Dot(Velocity, hitNormal) > 0.254 → nicht grounded
  - Slope: hitNormal.y < PmMaxSteepness → m_GroundPlane, nicht m_Walking
  - Velocity.y = 0 NUR bei Erstlandung (kompensiert größere Trace-Distanz)
  - CorrectGroundPosition: schiebt Box über Boden-Hit (box bottom + SKIN_WIDTH)
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
| `PM_GroundTrace()` | `PM_GroundTrace()` | BoxCast nach unten, Kickoff/Slope Check |
| `PM_CheckJump()` | `PM_CheckJump()` | pm_time + PMD_JUMP + Button-Check |
| `PM_Friction()` | `PM_Friction()` | Bodenreibung, speed<1→stop |
| `PM_Accelerate()` | `PM_Accelerate()` | Quake-Style Beschleunigung (ermöglicht Strafe-Jumping) |
| `PM_ClipVelocity()` | `PM_ClipVelocity()` | Velocity aus Surface entfernen |
| `PM_WalkMove()` | `PM_WalkMove()` | Boden: Jump→Friction→Accel→Clip→SlideMove |
| `PM_AirMove()` | `PM_AirMove()` | Luft: Friction→Accel→SlideMove(gravity) |
| `PM_SlideMove()` | `PM_SlideMove()` | 4-Bump Multi-Plane Clip + Gravity Half-Step + Crease-Slide |
| `PM_StepSlideMove()` | `PM_StepSlideMove()` | Slide → Step-Up → Slide → Step-Down → Distanz-Vergleich |
| `PM_CmdScale()` | `PM_CmdScale()` | Input-Magnitude normalisieren |
| `ResolvePenetration()` | — | Unity-spezifisch: OverlapBox + Push-Out (BSP braucht das nicht) |
| `CorrectGroundPosition()` | — | Unity-spezifisch: Box über Boden-Hit positionieren |
| `UpdateBhopChainTracking()` | — | Nicht-SoF2: CS:GO-Style Bhop-HUD-Tracking |

## Bhop Chain Tracking (CS:GO-Style HUD)

Kein SoF2-Feature, sondern ein Debug/HUD-Feature:

- Chain startet bei erstem Jump, zählt aufeinanderfolgende Sprünge
- Chain bricht ab wenn Spieler > 0.3s am Boden bleibt (`BHOP_CHAIN_GROUND_TIMEOUT`)
- Trackt: Chain-Count, Peak-Speed, Distanz seit Chain-Start
- Letzte abgeschlossene Chain wird für HUD-Anzeige gespeichert

## Bekannte Abweichungen von SoF2

| Abweichung | Grund | Auswirkung |
|------------|-------|------------|
| `GROUND_TRACE_DIST` 0.08m statt 0.006m | Unity Mesh-Kollision braucht größere Distanz | Boden wird ~0.074m früher erkannt, Sprünge 4% kürzer |
| `Velocity.y = 0` bei Erstlandung | Kompensiert frühere Boden-Erkennung | Verhindert Aufwärts-Bounce durch WalkMove Speed-Restore |
| CrashLand-Threshold -6.86 statt -5.08 | Kompensiert frühere Boden-Erkennung (mehr Fall-Velocity gemessen) | Gleicher Effekt wie SoF2 -200 QU/s |
| `SKIN_WIDTH` 0.02m | Unity hat keine globale Skin-Width | Verhindert Oberflächen-Clipping |
| `ResolvePenetration()` | BSP hat kein Tunneling, Unity Meshes schon | Iteratives Push-Out als Safety-Net |
| `CorrectGroundPosition()` | SoF2 BSP tracet exakt, Unity Meshes können leicht einsinken | Hält Box über Bodenoberfläche |

## Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Assets/Scripts/.../Shared/PlayerPhysicsSimulation.cs` | Shared Physik-Simulation (Client + Server), alle PM_* Methoden |
| `Assets/Scripts/.../Shared/PlayerCommand.cs` | Input-Struct + Server-Ack |
| `Assets/Scripts/.../Client/ClientPlayerCharacter.cs` | Prediction + Reconciliation + Collider-Disable |
| `Assets/Scripts/.../Server/ServerPlayerCharacter.cs` | Autoritative Simulation + Collider-Disable |
| `Assets/Scripts/.../Client/ClientColliderSystem.cs` | Box aus Bones / SoF2-Fixwerte berechnen, Visual-Debug |
| `Assets/Scripts/.../Networked/NetworkedPlayerCharacter.cs` | RPC-Bridge für Commands + Capsule-Dimensionen |
| `docs/code/game/bg_pmove.c` | SoF2 Original-Quellcode |
| `docs/code/game/bg_slidemove.c` | SoF2 SlideMove/StepSlideMove |
| `docs/code/game/bg_local.h` | SoF2 Konstanten |
