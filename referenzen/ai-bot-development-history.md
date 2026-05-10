# AI Bot Development History — Session Summary (April 2026)

## What Was Done (Chronological)

### Phase 1: EANN → GOAP Migration
- Replaced neural network (EANN) AI with Goal-Oriented Action Planning (GOAP)
- Created 5 Actions, 2 Goals, 4 WorldKeys, 4 TargetKeys, 8 Sensors
- Plan chain: PatrolAction → ChasePlayerAction → ShootPlayerAction

### Phase 2: Detection & Combat Fixes
- Fixed shoot↔patrol oscillation (dead player detection)
- Added 360° OverlapSphere (30m) player detection
- Added head-height aiming (k_TargetHeadHeight = 1.7f)
- Fixed Chase→Shoot XZ distance check
- Added PlayerDirectlyVisible guard (no blindfire)

### Phase 3: Movement Quality
- NavMesh pathfinding + runtime NavMesh bake
- Sensor-based obstacle avoidance (jump/crouch/strafe)
- Wall collision 180° turn fallback
- Client-side path visualization (LineRenderer, SyncDebugPathClientRpc)

### Phase 4: Player Memory & Pursuit
- 4s player memory after LOS loss (k_PlayerMemoryDuration)
- Walk to last known position after memory expires
- Damage reaction (5s window, attacker position)

### Phase 5: Patrol Intelligence
- Least-visited checkpoint system (m_CheckpointVisitCounts[])
- Stealth awareness (60m OverlapSphere no-LOS, checkpoint bias)
- Player breadcrumb system (64-entry circular buffer, 30% chance selection)

### Phase 6: Path Optimization
- String-pulling path smoothing (NavMesh.Raycast)
- Wall repulsion (sensor-based corridor centering)
- NavMesh corner insetting (push corners 0.8m from edges)
- Dynamic path recalculation (1m chase / 3m patrol)
- Direct LOS steering (NavMesh bypass when clear path)

### Phase 7: Movement Speed & Agility
- Chase-jumping (1.5-3.5s interval, Quake bunny-hop speed boost)
- Removed crouch mirroring (was slowing bot down)
- Player action mirroring (jump + attack only)

### Phase 8: Predictive Aiming
- Velocity tracking from position delta
- 0.15s aim lead time, 3m max offset
- Cached per-frame, reused by all aiming code

### Phase 9: Stuck Detection
- 4-phase escalation: skip waypoint → forward jump → 180° reverse → abandon checkpoint
- Stuck-mirror-replay (cached player actions replayed during chase stuck)

## Design Decisions Made
- **GOAP over BT/Octrees**: Discussed alternatives, GOAP is stronger for expandability
- **NavMesh over Octrees**: Ground-based agents, NavMesh is purpose-built
- **No persistent heatmap yet**: Breadcrumbs are V1, heatmap planned as V2
- **No crouch mirroring**: Slows bot down in chase, counterproductive
- **XZ distance for range checks**: 3D distance inflated by eye-height difference

## Status: Feature-Complete Seeker Bot
See `seeker-bot-complete.md` for full feature reference.

---

# Session Mai 2026 — TDM-Bot Combat & Movement Realism

### Phase 10: Combat-Realism (FireMode + Pattern-Gate)
- `BotPersonality.fireMode` (single / burst / full) — pro Personality konfigurierbar.
- `UpdateFirePatternGate()` simuliert Triggerdiscipline: Cooldowns zwischen Bursts, Pause nach Volley.
- Verhindert Dauerfeuer-Lock auf weit entfernte Ziele und gibt jedem Personality-Profil ein eigenes Schussbild.

### Phase 11a: Chase-Crouch Bug
- Stuck-Replay spiegelte gecachtes Spieler-Crouch auch während Chase → 75% Speed-Drop.
- Phase-3 Reverse-Recovery zwang 1 s Crouch beim Rückwärts-Drehen — auch wenn Spieler in Sicht.
- Beide Pfade entfernt / hinter `!PlayerSensorDetected` gegated.
- Siehe Runtime-Bugfix-Log Bug #17.

### Phase 11b: SoF2-Style Bhop (Step-Up-Jump + Land-Chain)
- **Step-Up-Jump**: NavMesh-Corner mit Y-Delta > `PmStepSize` (0.4572 m) bei XZ-Anlauf < 2 m → proaktiver Sprung statt CharacterController-Step → kein Momentum-Verlust an Stufen/Geländer.
- **Land-Chain**: `m_PhysicsSimulation.JustLanded && !IsDebounceActive` triggert sofortigen Re-Jump beim Bodenkontakt → echter Q3/SoF2-Bhop. Debounce-Gate verhindert Double-Jumps und respektiert PMD_JUMP.
- Implementiert in `AIBotController.BuildCommand` (~10 Zeilen), kein neuer State, koexistiert mit periodischem Chase-Sprung aus `ChasePlayerAction`.

### Phase 11c: Physics-Audit (PlayerPhysicsSimulation)
- Vollständiger Review von `PlayerPhysicsSimulation.cs` gegen `bg_pmove.c` / `bg_slidemove.c`.
- Pipeline-Reihenfolge, PM_Accelerate (Q3-Projection-Form, Strafe-Jump-fähig), PM_Friction, PM_ClipVelocity, PM_StepSlideMove, PMD_JUMP-Debounce, Slope-Speed-Restore, Same-Frame-Land+Jump-Detection — alles 1:1 portiert.
- Einzige bewusste Unity-Konzession: `GROUND_TRACE_DIST = 0.08 m` statt SoF2-Original `0.00635 m` (Unity-BoxCast-Slope-Issue, dokumentiert).
- Custom Erweiterungen (`KnockbackTime`, `StunTime`, Bhop-Telemetrie) sauber gekapselt und kommentiert.

### Phase 12: Weapon-Selection (Anti-Ping-Pong)
- `EvaluateWeaponState` prüfte Ammo erst nach dem Swap-Commit → Knife↔Ranged-Loop wenn alle Ranged-Waffen leer.
- Neuer Ansatz: Inventar einmal scannen, beste Ranged-mit-Ammo + Melee-Fallback ermitteln, dann gezielt mit `ServerSelectWeapon(name)` wechseln.
- Neue Helper auf `NetworkedCharacterState`: `TryGetAmmoFor(weapon, …)` und `ServerSelectWeapon(name)`.
- Siehe Runtime-Bugfix-Log Bug #18.

## Design-Erkenntnisse Mai 2026
- **Ducken niemals während Chase**: PmDuckScale 0.25 macht jeden taktischen Vorteil zunichte.
- **Bhop ist 10 Zeilen, nicht 100**: Saubere Physics-Sim macht Land-Chain trivial — keine eigene State-Machine nötig.
- **Blindes Cyclen ist gefährlich**: Wenn die Sim Konsequenzen erst nach dem nächsten Tick sichtbar macht, muss die Entscheidungslogik schon vorher informiert sein.
