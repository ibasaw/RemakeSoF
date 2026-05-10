# Seeker Bot — Complete Feature Reference

## Overview
GOAP-basierter Seeker-Bot für SoF2 Hide&Seek. Nutzt NavMesh-Pathfinding, 35-Sensor-Grid, Quake3/SoF2-Physik.
Status: **Feature-Complete** (Stand: April 2026)

## Core File
`Assets/Scripts/Runtime/AI/AIBotController.cs` — Bridge zwischen GOAP-Planner und ServerAICharacter

---

## 1. Patrol & Suche

### Least-Visited Checkpoint System
- `m_CheckpointVisitCounts[]` — Besuchszähler pro Checkpoint
- `PickLeastVisitedCheckpoint()` — wählt aus Checkpoints mit niedrigstem Visit-Count, gewichtet zufällig
- `m_UnreachableCheckpoints` (HashSet) — temporär unerreichbare überspringen, Reset nach vollem Zyklus
- Arrival: `k_CheckpointArrivalDist = 3f`

### Player Breadcrumbs (dynamische Patrol-Ziele)
- `s_Breadcrumbs[64]` — statischer Circular Buffer für Spieler-Positionen
- Aufnahme alle `k_BreadcrumbInterval = 3s`, min `k_BreadcrumbMinDistance = 5m` Abstand
- `k_BreadcrumbChance = 0.3f` — 30% Chance ein Breadcrumb statt statischem Checkpoint zu wählen
- `m_BreadcrumbTarget` (Vector3?) — aktuelles Breadcrumb-Ziel pro Bot
- `RecordPlayerBreadcrumbs()` — OverlapSphere findet Spieler, speichert Positionen
- `PickRandomBreadcrumb()` — wählt Breadcrumb >8m entfernt, NavMesh-validiert
- `ClearBreadcrumbs()` — Reset bei Map-Wechsel

### Stealth Awareness
- `k_StealthAwarenessRadius = 60f` — OverlapSphere ohne LOS-Check
- `k_StealthScanInterval = 2s` — nicht jeden Frame
- Biased Checkpoint-Auswahl: Inverse-Distance-Weighting Richtung erkanntem Spieler
- Nur aktiv wenn Bot den Spieler nicht direkt sieht

---

## 2. Erkennung & Verfolgung

### 360° OverlapSphere
- `k_PlayerDetectionRadius = 30f`
- `m_RadiusHits[16]` — wiederverwendbares Array
- Raycast LOS-Check nach OverlapSphere-Treffer
- Lazy-Tick pro Frame (`m_RadiusScanDoneThisFrame`)

### 35-Sensor-Grid (7h × 5v)
- FOV: 120° horizontal (±60°), 40° vertikal (±20°)
- `k_SensorHeightOffset = 1.83f` (SoF2 Augenhöhe)
- SphereCast 0.15m Radius, 25m Range
- Layer: Default(0) | Ground(6) | Player(7) | BrushCollision(9)

### 4s Player Memory
- `k_PlayerMemoryDuration = 4.0f`
- `m_LastKnownPlayerPosition`, `m_LastPlayerSeenTime`
- `PlayerMemoryActive` — grace period nach Sichtverlust
- `PlayerDirectlyVisible` — nur echte LOS (kein Memory)

### Walk to Last Known
- Chase/Shoot laufen zu `LastKnownPlayerPosition` nach Memory-Ablauf
- Arrival: `k_LastKnownArrivalDist = 2.5f`
- Dann zurück zu Patrol

### Damage Reaction
- `m_LastAttackerPosition`, `m_LastDamageTime`
- `k_DamageReactionWindow = 5s`
- GOAP-Sensoren nutzen Angreifer-Position als Fallback

---

## 3. Bewegung & Navigation

### NavMesh Pathfinding
- `NavMeshPath m_NavPath` — gecachter Pfad
- `m_PathCorners[]`, `m_PathIndex` — Wegpunkt-Tracking
- `k_WaypointArrivalDist = 1.5f`

### Path Smoothing (String Pulling)
- `SmoothPath()` — NavMesh.Raycast eliminiert redundante Corners
- Reduziert Zick-Zack auf geraden Strecken

### Direct LOS Steering
- `m_IsDirectLOSSteering` — NavMesh-Bypass bei freier Sicht zum Spieler
- NavMesh.Raycast Check: wenn kein NavMesh-Hindernis, direkte Bewegung
- Nur aktiv wenn `PlayerDirectlyVisible`

### Dynamic Path Recalculation
- `k_PathRecalcThreshold = 3f` (Patrol)
- `k_PathRecalcThresholdChase = 1.0f` (Chase/Shoot)
- Reaktiveres Umpathing während Verfolgung

### Wall Repulsion + Corner Insetting
- `k_WallRepulsionDistance = 5f`, `k_WallRepulsionStrength = 0.5f`
- `CalculateWallRepulsion()` — liest linke/rechte Seitensensoren
- `InsetPathCornersFromWalls()` — NavMesh.FindClosestEdge, `k_PathCornerInsetDistance = 0.8f`

### Chase-Jumping (Bunny-Hop)
- `k_ChaseJumpIntervalMin = 1.5f`, `k_ChaseJumpIntervalMax = 3.5f`
- Periodischer Sprung in ChasePlayerAction für Quake/SoF2 Speed-Boost

### SoF2-Style Bhop (BuildCommand, Mai 2026)
Zusätzlich zum periodischen Chase-Sprung läuft ein physik-getriebener Bhop direkt im
`BuildCommand`-Pfad. Voraussetzung: aktives Move-Target, Move-Input > 0,
`PlayerPhysicsSimulation` referenziert.

- **Step-Up-Jump**: nächster NavMesh-Corner mit Y-Delta > `PmStepSize` (0.4572 m) bei
  XZ-Anlauf < 2 m → proaktiver Sprung. Verhindert Momentum-Verlust an Stufen/Geländern,
  weil der Step-Bonus aus PM_StepSlideMove sonst die Geschwindigkeit kappt.
- **Land-Chain**: `m_PhysicsSimulation.JustLanded && !IsDebounceActive` → sofortiger
  Re-Jump beim Bodenkontakt. Echtes Q3/SoF2-Bhop-Verhalten mit Speed-Erhalt.
- Debounce-Gate respektiert PMD_JUMP — kein Double-Jump-Spam.
- Koexistiert mit `ChasePlayerAction`-Periodensprung; `IsDebounceActive` filtert
  doppelte Eingaben automatisch.

### Sensor-basierte Hindernis-Reaktion
- `ReactToSensorObstacles()` — 7×5 Grid auswerten
- Unten nah + oben frei → Springen
- Oben nah + unten frei → Ducken
- Front blockiert → seitliches Ausweichen
- `k_ObstacleNearThreshold = 2.5f`, `k_ObstacleCloseThreshold = 1.2f`

### 4-Phasen Stuck Detection
- Phase 1 (2s): Wegpunkt überspringen + Sprung
- Phase 2 (3s): Vorwärts-Sprung Richtung Wegpunkt
- Phase 3 (5s): 180° drehen + rückwärts + ducken (1s)
- Phase 4 (7s): Checkpoint aufgeben, als besucht markieren, neuen wählen

---

## 4. Kampf

### Predictive Aiming
- `m_TrackedPlayerVelocity` — aus Position-Delta berechnet
- `k_AimLeadTimeSec = 0.15f` — simulierte Reaktionszeit
- `k_AimLeadMaxOffset = 3f` — Cap für Vorhalt

### PlayerDirectlyVisible Guard
- ShootPlayerAction feuert nur wenn Spieler direkt sichtbar (kein Memory/Damage-Fallback)
- Verhindert Blindfire in Wände

### Player Action Mirroring
- `MirrorNearestPlayerActions()` — spiegelt Jump + Attack
- Kein Crouch-Mirroring (verlangsamt Bot — `PmDuckScale = 0.25` bricht Speed/Bhop)
- Gecachte letzte Mirror-Aktion für Stuck-Replay
- **Stuck-Replay-Crouch entfernt** (Mai 2026): früher wurde gecachtes Spieler-Crouch
  beim Stuck-Recovery gespiegelt, das machte den Bot beim Verfolgen 75% langsamer.
- **Phase-3-Reverse-Crouch nur out-of-combat**: 4-Phasen-Stuck-Recovery zwingt 1 s
  Crouch beim Rückwärts-Drehen, aber nur wenn `!PlayerSensorDetected`.

### FireMode (Personality-getriebene Triggerdiscipline)
- `BotPersonality.fireMode` — `single` / `burst` / `full`.
- `UpdateFirePatternGate()` ersetzt Dauerfeuer durch realistische Bursts mit
  Cooldowns und Pausen. Verhindert dass weit entfernte Ziele in Sekunden leergefeuert werden.
- Pro-Personality-Profile (Aggressive=full, Sniper=single, Veteran=burst, …).

### Waffen-Range-Erkennung
- `m_WeaponRangeMeters` — gecacht aus SoF2 Waffendefinitionen
- `m_CachedWeaponName` — Waffenwechsel-Erkennung
- XZ-Distanz für Reichweiten-Vergleich (nicht 3D)

### Waffen-Auswahl (`EvaluateWeaponState`, Mai 2026)
Prüft alle `k_WeaponEvalInterval = 1.5 s` informiert das Inventar — kein blindes
Cyclen mehr, kein Knife↔Ranged Ping-Pong.
- Skip während laufendem Swap (`HasPendingWeaponSwap`) und während Pre-Round-Countdown.
- Inventar einmal scannen → `bestRanged` (erste Ranged-mit-Ammo) + `fallbackMelee`.
- Aktuelle Waffe trocken (clip+reserve = 0, nicht infinite) → bestes Alternativ-Target.
- Aktuelle Waffe Melee + Spieler in Sicht + Ranged-mit-Ammo verfügbar → Ranged.
- Sonst: bleiben.
- Wechsel via `NetworkedCharacterState.ServerSelectWeapon(name)` — direkt zur Zielwaffe,
  validiert Inventar, setzt `m_PendingSwapTarget`.
- Ammo-Abfrage für andere Waffen via `NetworkedCharacterState.TryGetAmmoFor(name, …)`
  (Live-NV für aktive Waffe, AmmoCache für Rest).

---

## 5. Konstanten-Übersicht

| Konstante | Wert | Beschreibung |
|-----------|------|--------------|
| k_SensorHeightOffset | 1.83f | Augenhöhe (72/89 × 2.26m) |
| k_PlayerDetectionRadius | 30f | OverlapSphere Radius |
| k_PlayerMemoryDuration | 4.0f | Memory nach Sichtverlust |
| k_DamageReactionWindow | 5.0f | Damage-Reaktionsfenster |
| k_PathRecalcThreshold | 3.0f | Patrol Pfad-Neuberechnung |
| k_PathRecalcThresholdChase | 1.0f | Chase Pfad-Neuberechnung |
| k_WallRepulsionDistance | 5.0f | Sensor-Distanz für Abstossung |
| k_WallRepulsionStrength | 0.5f | Max lateraler Input |
| k_PathCornerInsetDistance | 0.8f | Corner-Einrückung von Wänden |
| k_ObstacleNearThreshold | 2.5f | Hindernis-Reaktionsdistanz |
| k_ObstacleCloseThreshold | 1.2f | Sofort-Ausweich-Distanz |
| k_CheckpointArrivalDist | 3.0f | Checkpoint erreicht |
| k_WaypointArrivalDist | 1.5f | NavMesh-Wegpunkt erreicht |
| k_LastKnownArrivalDist | 2.5f | Last-Known-Position erreicht |
| k_AimLeadTimeSec | 0.15f | Vorhalt-Faktor |
| k_AimLeadMaxOffset | 3.0f | Max Vorhalt-Offset |
| k_StealthAwarenessRadius | 60.0f | Stealth-Scan Radius |
| k_StealthScanInterval | 2.0f | Stealth-Scan Intervall |
| k_MaxBreadcrumbs | 64 | Breadcrumb Buffer-Größe |
| k_BreadcrumbInterval | 3.0f | Breadcrumb-Aufnahme Intervall |
| k_BreadcrumbMinDistance | 5.0f | Min Abstand neue Breadcrumb |
| k_BreadcrumbChance | 0.3f | Breadcrumb-Wahlwahrscheinlichkeit |
| k_ChaseJumpIntervalMin | 1.5f | Min Chase-Sprung Intervall |
| k_ChaseJumpIntervalMax | 3.5f | Max Chase-Sprung Intervall |
| k_TargetHeadHeight | 1.7f | Ziel-Kopfhöhe für Aiming |
| k_PitchDecayDegPerSec | 15.0f | Pitch-Rückführung ohne Ziel |
| k_WeaponEvalInterval | 1.5f | Waffen-Eval Intervall |

---

## 6. Mögliche V2-Erweiterungen (nicht implementiert)
- **Persistente Heatmap**: Spieler-Hotspots pro Map speichern (JSON), als gewichtete virtuelle Checkpoints nutzen
- **Sound-basierte Erkennung**: Schüsse/Schritte als Checkpoint-Bias
- **Difficulty Scaling**: Aim-Delay, Reaktionszeit, Sensor-Radius variabel
- **Utility-based Action Cost**: Dynamische GOAP-ActionCost basierend auf Situation
- **Neue GOAP-Actions**: TakeCoverAction, FlankAction, RetreatAction
