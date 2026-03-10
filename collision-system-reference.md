# Collision System Reference — SoF2 vs Unity Implementation

## Übersicht

Dieses Dokument vergleicht das originale SoF2/Quake3-Kollisionssystem mit der aktuellen Unity-Implementierung und gibt eine Empfehlung für den besseren Ansatz.

---

## SoF2 Original (Quake3 Engine)

### Architektur

SoF2 nutzt ein **einheitliches Trace-System** für alle Kollisionen:

```
Server (g_active.c)
  └─ Pmove() mit pm.tracemask
       └─ trap_Trace(trace_t*, origin, mins, maxs, end, clientNum, mask)
            └─ Server Clip-Model-System (CM_*)
                 ├─ BSP World Geometry
                 ├─ Player Bodies (CONTENTS_BODY)
                 ├─ Items (CONTENTS_ITEM)
                 └─ Trigger Volumes (CONTENTS_TRIGGER)
```

### Kernkonzept: Content Flags + Trace Masks

Jede Entität im Spiel hat **Content Flags**, die beschreiben, was sie ist:

```c
#define CONTENTS_SOLID       0x00000001  // Welt-Geometrie
#define CONTENTS_PLAYERCLIP  0x00000010  // Clip-Brush nur für Spieler
#define CONTENTS_BODY        0x00000100  // Spieler-Körper
#define CONTENTS_CORPSE      0x00000200  // Leichen
#define CONTENTS_SHOTCLIP    0x00000080  // Schuss-Kollision
#define CONTENTS_TERRAIN     0x00001000  // Terrain
```

**Trace Masks** kombinieren diese Flags, um zu steuern, WAS eine Trace trifft:

```c
// Normale Spieler-Bewegung — trifft Welt UND andere Spieler
#define MASK_PLAYERSOLID  (CONTENTS_SOLID | CONTENTS_TERRAIN | CONTENTS_PLAYERCLIP | CONTENTS_BODY | CONTENTS_SHOTCLIP)

// Zuschauer — fliegt durch Spieler-Körper
MASK_PLAYERSOLID & ~CONTENTS_BODY

// Schuss-Trace — trifft Welt + Spieler
#define MASK_SHOT  (CONTENTS_SOLID | CONTENTS_TERRAIN | CONTENTS_BODY | CONTENTS_SHOTCLIP)
```

### Spieler-Bounding-Box

SoF2 nutzt eine **AABB** (Axis-Aligned Bounding Box), keine Capsule:

```c
#define DEFAULT_PLAYER_Z_MAX    43    // stehend: Kopf bei +43
#define CROUCH_PLAYER_Z_MAX     18    // geduckt: Kopf bei +18
#define MINS_Z                  -46   // Füße bei -46

// Ergibt eine Box von:
// mins = { -15, -15, -46 }
// maxs = { +15, +15, +43 }
// Dimension: 30 x 30 x 89 Units (breit x tief x hoch)
```

### Trace-Aufruf in der Bewegung

```c
// PM_SlideMove — 4-Bump Collision
pm->trace(&trace, pm->ps->origin, pm->mins, pm->maxs, end, 
          pm->ps->clientNum, pm->tracemask);
//        ^^^^^^^^^^^^^^^^^
//        Ignoriert eigenen Spieler automatisch per clientNum

// Ergebnis:
// trace.fraction    = 0.0-1.0 (wie weit gekommen)
// trace.endpos      = Endposition
// trace.plane.normal= Aufprall-Normal
// trace.entityNum   = Getroffene Entität
```

### Entscheidender Punkt: Einheitliches System

- **Eine Funktion** (`trap_Trace`) trifft ALLES (Welt, Spieler, Items)
- **Ein Mask-Parameter** steuert, was getroffen wird
- **clientNum** sorgt automatisch dafür, dass man sich nicht selbst trifft
- Server verwaltet alle Clip-Models zentral
- **Kein separates Collider-Setup pro Entität nötig** — der Server kennt die Bounding Boxes aller Entitäten

---

## Unity Implementierung (Aktuell)

### Architektur

Unity nutzt ein **physikbasiertes System** mit CapsuleCollider + CapsuleCast:

```
Server (ServerPlayerCharacter)
  ├─ CapsuleCollider (für andere Spieler sichtbar)
  ├─ PlayerPhysicsSimulation.Simulate()
  │    ├─ Disable eigenen Collider
  │    ├─ Physics.CapsuleCast(..., GroundMask)
  │    │    └─ Unity Physics Engine
  │    │         ├─ World Colliders (Static)
  │    │         ├─ Andere Spieler CapsuleColliders
  │    │         └─ LayerMask filtert
  │    └─ Enable eigenen Collider
  └─ Ergebnis → ServerMovementAck an Client

Client (ClientPlayerCharacter + ClientColliderSystem)
  ├─ CapsuleCollider (für Client-seitige Prediction)
  ├─ ClientColliderSystem.UpdatePhysicsCollider()
  └─ PlayerPhysicsSimulation.Simulate() (Prediction)
       └─ Gleicher CapsuleCast-Mechanismus
```

### Kernkonzept: LayerMask + CapsuleCollider

Statt Content Flags nutzt Unity **Layers**:

```
Layer 0:  Default
Layer 6:  Player         ← Alle Spieler-GameObjects
Layer 7:  PlayerLocal    ← (optional) eigener Spieler
Layer 8:  World          ← Statische Welt-Geometrie
...
```

**GroundMask** (LayerMask) steuert, was CapsuleCast trifft:

```csharp
// Muss Player-Layer inkludieren für Spieler-Spieler-Kollision
public LayerMask GroundMask = ~0;  // Aktuell: ALLES
```

### Spieler-Collider

```csharp
// ClientColliderSystem — berechnet Capsule aus Bones
CapsuleCollider m_PhysicsCollider;
m_PhysicsCollider.height = capsuleHeight;   // aus Cranium-Bone
m_PhysicsCollider.radius = capsuleRadius;   // aus Fuß-Abstand
m_PhysicsCollider.center = capsuleCenter;   // Mitte

// ServerPlayerCharacter — Default-Werte bis Client sendet
m_PhysicsCollider.height = 1.8f;
m_PhysicsCollider.radius = 0.25f;
m_PhysicsCollider.center = (0, 0.9, 0);
```

### Self-Collision-Vermeidung

```csharp
// SoF2: clientNum Parameter ignoriert eigenen Spieler automatisch
// Unity: Manuell Collider deaktivieren/aktivieren

m_PhysicsCollider.enabled = false;  // Vor CapsuleCast
// ... CapsuleCast ...
m_PhysicsCollider.enabled = true;   // Nach CapsuleCast
```

---

## Direktvergleich

| Aspekt | SoF2 (Quake3) | Unity (Aktuell) |
|--------|---------------|-----------------|
| **Trace-Funktion** | `trap_Trace(trace, origin, mins, maxs, end, clientNum, mask)` | `Physics.CapsuleCast(top, bottom, radius, dir, hit, dist, mask)` |
| **Kollisionsform** | AABB (mins/maxs Box) | Capsule (Höhe + Radius) |
| **Filterung** | Content Flags (`MASK_PLAYERSOLID`) | LayerMask |
| **Self-Collision** | `clientNum` Parameter — Engine ignoriert automatisch | Collider manuell disable/enable |
| **Spieler-Registrierung** | Server registriert Clip-Model automatisch | CapsuleCollider als Component hinzufügen |
| **Spieler-Spieler** | `CONTENTS_BODY` in `MASK_PLAYERSOLID` | Player-Layer in GroundMask |
| **Zuschauer** | `MASK_PLAYERSOLID & ~CONTENTS_BODY` | GroundMask ohne Player-Layer |
| **Slide-Move** | 4-Bump + PM_ClipVelocity | 4-Bump + PM_ClipVelocity (identisch) |
| **Step-Up** | `maxs[2]` reduzieren + Trace | CapsuleCast hoch/vorwärts/runter |
| **Ground-Check** | Trace nach unten + Normal prüfen | CapsuleCast nach unten + Normal + Grace Period |
| **Autorität** | Nur Server | Server autoritativ + Client Prediction |
| **Broadphase** | Custom BSP Tree | Unity PhysX (optimiert) |

---

## Welcher Ansatz ist besser?

### Empfehlung: **Unity-nativer Ansatz (CapsuleCollider + CapsuleCast)**

#### Warum:

1. **Konzeptuell identisch**
   - SoF2's `trap_Trace` = Unity's `Physics.CapsuleCast` — beides Sweep-Tests gegen registrierte Geometrie
   - SoF2's Content Flags = Unity's LayerMask — beides bitmaskenbasierte Filterung
   - SoF2's clientNum-basierte Self-Exclusion = Unity's Collider disable/enable
   - Das **Konzept** ist 1:1 das gleiche, nur die API ist anders

2. **PhysX-Engine ist überlegen**
   - Unity nutzt NVIDIA PhysX — hochoptimierte Broadphase (Sweep-and-Prune, BVH)
   - Quake3's BSP-Trace ist schnell für statische Welt, aber nicht für viele dynamische Entitäten
   - PhysX skaliert besser mit vielen Spielern

3. **Capsule vs AABB**
   - Capsule ist die **bessere Wahl** für Character-Collision in Unity
   - Gleitet besser an Wänden/Ecken (keine AABB-Kanten-Artefakte)
   - SoF2 nutzte AABB nur wegen Engine-Limitierung — die Engine konnte keine Capsule-Traces

4. **Integration**
   - CapsuleCollider integriert sich nahtlos mit Unity's gesamtem Physik-Stack
   - Raycasts, Trigger, Rigidbody-Interaktion — alles funktioniert automatisch
   - Ein eigenes Trace-System zu bauen wäre Over-Engineering

5. **Server-autoritativ funktioniert identisch**
   - Server hat CapsuleCollider für jeden Spieler → CapsuleCast trifft sie
   - Client Prediction nutzt den gleichen Code → gleiche Ergebnisse
   - Disable/Enable für Self-Collision ist minimalinvasiv

#### Was man NICHT nachbauen sollte:

- ❌ **Eigenes Trace-System** — PhysX ist schneller und zuverlässiger als alles Selbstgebaute
- ❌ **AABB statt Capsule** — Capsule ist in Unity die bessere Wahl
- ❌ **Content Flags System** — LayerMask ist das Unity-Äquivalent und funktioniert identisch
- ❌ **Zentrales Clip-Model-Management** — Unity's Physics Engine übernimmt das automatisch

#### Was man beibehalten/portieren SOLL:

- ✅ **4-Bump SlideMove** — exakt portiert, funktioniert identisch
- ✅ **PM_ClipVelocity** — 1:1 identisch
- ✅ **Step-Up Logik** — konzeptuell gleich, Unity-angepasst
- ✅ **MASK_PLAYERSOLID-Konzept** — als LayerMask: GroundMask muss World + Player-Layer enthalten
- ✅ **Disable/Enable für Self-Collision** — Unity-Äquivalent zu clientNum-Exclusion
- ✅ **Server-autoritativ** — gleiche Architektur wie SoF2

---

## Aktuelle Implementierungsdetails

### CapsuleCollider-Lifecycle

```
Spieler spawnt
  ├─ Server: ServerPlayerCharacter.Awake()
  │    └─ AddComponent<CapsuleCollider>() mit Default-Werten (1.8m, 0.25r)
  │
  └─ Client: ClientColliderSystem
       └─ CalculateAutoCapsuleSize() → UpdatePhysicsCollider()
            └─ AddComponent<CapsuleCollider>() mit Bone-berechneten Werten
            └─ SendCapsuleDimensions() → Server aktualisiert seine Werte
```

### Self-Collision-Flow (Server)

```
ProcessCommand(PlayerCommand cmd)
  1. m_PhysicsCollider.enabled = false
  2. m_Simulation.Simulate(ref position, cmd)
     └─ CapsuleCast trifft NUR andere Spieler + Welt
  3. m_PhysicsCollider.enabled = true
  4. return ServerMovementAck
```

### Self-Collision-Flow (Client Prediction)

```
RunPhysicsStep()
  1. m_ColliderSystem.PhysicsCollider.enabled = false  (falls vorhanden)
  2. m_Simulation.Simulate(ref position, cmd)
     └─ CapsuleCast trifft NUR andere Spieler + Welt
  3. m_ColliderSystem.PhysicsCollider.enabled = true
  4. Position speichern für Reconciliation
```

### LayerMask-Konfiguration

```
GroundMask sollte enthalten:
  ✅ Default (0)      — Welt-Geometrie
  ✅ World             — Statische Welt
  ✅ Player            — Andere Spieler (für Spieler-Spieler-Kollision)
  
GroundMask sollte NICHT enthalten:
  ❌ PlayerLocal       — Eigener Spieler (Self-Collision)
  ❌ Trigger           — Trigger Volumes
  ❌ Ignore Raycast    — UI/Debug-Objekte
```

---

## SoF2-Äquivalenz-Tabelle

| SoF2 Konzept | Unity Äquivalent | Status |
|---------------|-------------------|--------|
| `trap_Trace()` | `Physics.CapsuleCast()` | ✅ Portiert |
| `MASK_PLAYERSOLID` | `GroundMask` (LayerMask) | ✅ Portiert |
| `CONTENTS_BODY` | Player-Layer | ✅ Implementiert |
| `CONTENTS_SOLID` | Default/World-Layer | ✅ Implementiert |
| `clientNum` Self-Exclusion | Collider disable/enable | ✅ Implementiert |
| `pm->mins/maxs` (AABB) | CapsuleCollider (Capsule) | ✅ Besser als Original |
| `PM_SlideMove` 4-Bump | `PM_StepSlideMove` 4-Bump | ✅ Portiert |
| `PM_ClipVelocity` | `PM_ClipVelocity` | ✅ 1:1 Identisch |
| `STEPSIZE` (18) | `PmStepSize` (1.8f) | ✅ /10 skaliert |
| `OVERCLIP` (1.001f) | `OVERCLIP` (1.001f) | ✅ Identisch |
| `PM_AddTouchEnt()` | Nicht portiert | ⚠️ Für Trigger/Items nötig |
| `MASK_PLAYERSOLID & ~CONTENTS_BODY` (Spectator) | GroundMask ohne Player-Layer | ⚠️ Noch nicht implementiert |
| Water Physics | — | ❌ Nicht implementiert |
| Prone Collision | — | ❌ Nicht implementiert |

---

## Zusammenfassung

> **Der Unity-Ansatz ist der richtige.** SoF2's Kollisionssystem und Unity's Physics.CapsuleCast sind
> konzeptuell identisch — beide nutzen Sweep-Tests mit Masken-Filterung gegen registrierte Geometrie.
> Der Unterschied ist nur die API, nicht die Architektur.
>
> Unity's PhysX-Engine liefert eine bessere Broadphase, Capsule statt AABB ist smoother,
> und LayerMask ist das direkte Äquivalent zu Content Flags.
> Ein eigenes Trace-System nachzubauen wäre Over-Engineering ohne Mehrwert.
