# Collision System Reference — SoF2 vs Unity Implementation

## Übersicht

Dieses Dokument vergleicht das originale SoF2/Quake3-Kollisionssystem mit der aktuellen Unity-Implementierung.
Die Unity-Implementierung nutzt **BoxCast (AABB)** für maximale SoF2-Authentizität.

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

## Unity Implementierung (Aktuell — BoxCast AABB)

### Architektur

Unity nutzt ein **physikbasiertes System** mit BoxCollider + BoxCast (AABB, SoF2-authentisch):

```
Server (ServerPlayerCharacter)
  ├─ BoxCollider (für andere Spieler sichtbar)
  ├─ PlayerPhysicsSimulation.Simulate()
  │    ├─ Disable eigenen Collider
  │    ├─ Physics.BoxCast(..., Quaternion.identity, GroundMask)
  │    │    └─ Unity Physics Engine (AABB Sweep)
  │    │         ├─ World Colliders (Static)
  │    │         ├─ Andere Spieler BoxColliders
  │    │         └─ LayerMask filtert
  │    └─ Enable eigenen Collider
  └─ Ergebnis → ServerMovementAck an Client

Client (ClientPlayerCharacter + ClientColliderSystem)
  ├─ BoxCollider (für Client-seitige Prediction)
  ├─ ClientColliderSystem.UpdatePhysicsCollider()
  └─ PlayerPhysicsSimulation.Simulate() (Prediction)
       └─ Gleicher BoxCast-Mechanismus
```

### Kernkonzept: LayerMask + BoxCollider

Statt Content Flags nutzt Unity **Layers**:

```
Layer 0:  Default
Layer 6:  Player         ← Alle Spieler-GameObjects
Layer 7:  PlayerLocal    ← (optional) eigener Spieler
Layer 8:  World          ← Statische Welt-Geometrie
...
```

**GroundMask** (LayerMask) steuert, was BoxCast trifft:

```csharp
// Muss Player-Layer inkludieren für Spieler-Spieler-Kollision
public LayerMask GroundMask = ~0;  // Aktuell: ALLES
```

### Spieler-Collider (BoxCollider — SoF2 AABB)

```csharp
// ClientColliderSystem — berechnet Box aus Bones oder SoF2-Fixwerte
BoxCollider m_PhysicsCollider;
m_PhysicsCollider.size = new Vector3(capsuleRadius * 2f, capsuleHeight, capsuleRadius * 2f);
m_PhysicsCollider.center = capsuleCenter;

// SoF2-Fixwerte:
// Standing:  height=2.2606m, radius=0.381m → size=(0.762, 2.2606, 0.762)
// Crouching: height=1.6256m, radius=0.381m → size=(0.762, 1.6256, 0.762)

// ServerPlayerCharacter — Default bis Client sendet
m_PhysicsCollider.size = new Vector3(0.5f, 1.8f, 0.5f);
m_PhysicsCollider.center = new Vector3(0, 0.9f, 0);
```

### BoxCast-Aufruf (AABB-Sweep)

```csharp
// BoxHalfExtents = (CapsuleRadius, CapsuleHeight * 0.5f, CapsuleRadius)
Vector3 halfExtents = BoxHalfExtents;

// Sweep: identisch mit SoF2 trap_Trace(mins, maxs, end)
Physics.BoxCast(center, halfExtents, direction, out RaycastHit hit,
    Quaternion.identity,  // ← AABB, keine Rotation
    distance, GroundMask);

// Ground Detection: leicht geschrumpft XZ
Vector3 shrunkHalf = new Vector3(halfExtents.x * 0.95f, halfExtents.y, halfExtents.z * 0.95f);
Physics.BoxCast(center, shrunkHalf, Vector3.down, out hit,
    Quaternion.identity, GROUND_TRACE_DIST, GroundMask);

// Depenetration: OverlapBox
Collider[] overlaps = Physics.OverlapBox(center, halfExtents, Quaternion.identity, GroundMask);
```

### Self-Collision-Vermeidung

```csharp
// SoF2: clientNum Parameter ignoriert eigenen Spieler automatisch
// Unity: Manuell Collider deaktivieren/aktivieren

m_PhysicsCollider.enabled = false;  // Vor BoxCast
// ... BoxCast ...
m_PhysicsCollider.enabled = true;   // Nach BoxCast
```

---

## Direktvergleich

| Aspekt | SoF2 (Quake3) | Unity (Aktuell) |
|--------|---------------|-----------------|
| **Trace-Funktion** | `trap_Trace(trace, origin, mins, maxs, end, clientNum, mask)` | `Physics.BoxCast(center, halfExtents, dir, hit, Quaternion.identity, dist, mask)` |
| **Kollisionsform** | AABB (mins/maxs Box) | AABB (BoxCast mit `Quaternion.identity`) |
| **Filterung** | Content Flags (`MASK_PLAYERSOLID`) | LayerMask |
| **Self-Collision** | `clientNum` Parameter — Engine ignoriert automatisch | Collider manuell disable/enable |
| **Spieler-Registrierung** | Server registriert Clip-Model automatisch | BoxCollider als Component hinzufügen |
| **Spieler-Spieler** | `CONTENTS_BODY` in `MASK_PLAYERSOLID` | Player-Layer in GroundMask |
| **Zuschauer** | `MASK_PLAYERSOLID & ~CONTENTS_BODY` | GroundMask ohne Player-Layer |
| **Slide-Move** | 4-Bump + PM_ClipVelocity | 4-Bump + PM_ClipVelocity (identisch) |
| **Step-Up** | Erhöhe origin + Trace + Step-Down + Distanzvergleich | BoxCast hoch/vorwärts/runter + Distanzvergleich |
| **Ground-Check** | AABB-Trace nach unten + Normal prüfen | BoxCast nach unten + Normal prüfen |
| **Autorität** | Nur Server | Server autoritativ + Client Prediction |
| **Broadphase** | Custom BSP Tree | Unity PhysX (optimiert) |
| **Depenetration** | BSP hat kein Tunneling | OverlapBox + iteratives Push-Out |

---

## Warum BoxCast (AABB) statt CapsuleCast

### Entscheidung: **BoxCast = SoF2-authentisch**

SoF2 nutzte AABB-Traces (`trap_Trace` mit `mins`/`maxs`). Für maximale Authentizität verwendet der Unity-Port ebenfalls AABB via `Physics.BoxCast` mit `Quaternion.identity`.

#### Vorteile von BoxCast (AABB):

1. **SoF2-Authentizität**
   - Identische Kollisionsgeometrie wie das Original
   - Strafe-Jumping, Bhop, Step-Up verhalten sich wie in SoF2
   - Ecken-/Kantenverhalten ist identisch — keine Capsule-Rundung die Gameplay verändert

2. **Performance**
   - BoxCast mit `Quaternion.identity` ist der schnellste Sweep-Test in PhysX (einfache SAT-Tests)
   - Schneller als CapsuleCast (GJK/EPA) und schneller als rotierte BoxCasts

3. **Einfachheit**
   - `BoxHalfExtents = (radius, height*0.5, radius)` — triviale Berechnung
   - Keine Top/Bottom-Sphere-Berechnung wie bei CapsuleCast nötig

#### Was CapsuleCast besser macht (aber hier nicht relevant):

- Gleitet smoother an Ecken (Rundung) — aber SoF2 hat diese Rundung NICHT
- Besser für Third-Person-Spiele mit modernem Character-Feel — nicht unser Ziel

---

## Aktuelle Implementierungsdetails

### BoxCollider-Lifecycle

```
Spieler spawnt
  ├─ Server: ServerPlayerCharacter.InitializeServer()
  │    └─ AddComponent<BoxCollider>() mit Default-Werten
  │
  └─ Client: ClientColliderSystem
       └─ CalculateAutoCapsuleSize() → UpdatePhysicsCollider()
            └─ AddComponent<BoxCollider>() mit Bone-berechneten Werten
            └─ SendCapsuleDimensions(h, r, center) → Server aktualisiert
```

### Self-Collision-Flow (Server)

```
ProcessCommand(PlayerCommand cmd)
  1. m_PhysicsCollider.enabled = false
  2. m_Simulation.Simulate(ref position, cmd)
     └─ BoxCast trifft NUR andere Spieler + Welt
  3. m_PhysicsCollider.enabled = true
  4. return ServerMovementAck
```

### Self-Collision-Flow (Client Prediction)

```
RunPhysicsStep()
  1. m_ColliderSystem.PhysicsCollider.enabled = false  (falls vorhanden)
  2. m_Simulation.Simulate(ref position, cmd)
     └─ BoxCast trifft NUR andere Spieler + Welt
  3. m_ColliderSystem.PhysicsCollider.enabled = true
  4. Position speichern für Reconciliation
```

### LayerMask-Konfiguration

#### Unity Layer Zuordnung

```
Layer 0:  Default          — Visuelle Map-Surfaces (MeshCollider + SurfaceTypeMarker)
Layer 6:  Ground           — (optional)
Layer 7:  Player           — Spieler-BoxCollider
Layer 8:  Hitbox           — Per-Bone Hitbox-Collider (Trigger)
Layer 9:  BrushCollision   — BSP Brush/Clip-Volumes (nur Spielerbewegung)
```

#### Spielerbewegung (BoxCast)

```
GroundMask sollte enthalten:
  ✅ Default (0)         — Visuelle Welt-Surfaces
  ✅ BrushCollision (9)  — Brush/Clip-Volumes (Collision Geometry)
  ✅ Player              — Andere Spieler (für Spieler-Spieler-Kollision)
  
GroundMask sollte NICHT enthalten:
  ❌ Hitbox              — Hitbox-Trigger (nur für Waffen-Raycasts)
  ❌ Ignore Raycast      — UI/Debug-Objekte
```

#### Hitscan / Projektil-Raycasts

```
WorldLayerMask:
  ✅ Default (0)         — Visuelle Surfaces MIT SurfaceTypeMarker
  
  ❌ Hitbox              — Separater Raycast auf Hitbox-Layer
  ❌ BrushCollision      — Brush-Volumes haben keinen SurfaceTypeMarker
  ❌ Player              — Über Hitbox-System abgedeckt
```

```csharp
// Hitscan-Raycast (alle Stellen):
int worldLayerMask = ~(hitboxLayerMask | LayerMask.GetMask("BrushCollision"));
```

#### Footstep-Raycasts

```
GroundLayerMask:
  ✅ Default (0)         — Visuelle Surfaces MIT SurfaceTypeMarker
  
  ❌ Hitbox              — Keine Fußschritte auf Hitboxen
  ❌ BrushCollision      — Brush-Volumes haben keinen SurfaceTypeMarker
```

---

## Map-Collider-Architektur (BrushCollision Layer)

### Problem & Lösung

BSP-Maps haben zwei Arten von Geometrie:
1. **Brush Volumes** (`COL_*0..N`, `COL_*N_clip`): Unsichtbare Kollisions-Geometry ohne Textur-Info
2. **Visuelle Surfaces**: Sichtbare Faces mit `q3map_material` (Surface-Typ für Impact-Effekte)

In SoF2 treffen `MASK_SHOT`-Traces die visuellen Surfaces und lesen deren Surface Flags.
Clip Brushes werden von Schuss-Traces ignoriert.

**Unity-Lösung:** Brush/Clip-Volumes auf `BrushCollision`-Layer (9), visuelle Surfaces auf Default (0).
Hitscan-Raycasts excluden `BrushCollision` → treffen nur Surfaces mit `SurfaceTypeMarker`.
Spielerbewegung (`Physics.BoxCast`) nutzt alle Layer → Brushes blockieren weiterhin.

### Collider-Typen

| Geometry | Collider | Layer | SurfaceTypeMarker | Hitscan | Bewegung |
|---|---|---|---|---|---|
| Brush Volumes (`COL_*N`) | MeshCollider | BrushCollision | Nein | Ignoriert | Blockiert |
| Clip Volumes (`COL_*N_clip`) | MeshCollider | BrushCollision | Nein | Ignoriert | Blockiert |
| Sky Surfaces | BoxCollider (min 0.1m) | Default | Ja | Getroffen | Blockiert |
| Visuelle Surfaces | MeshCollider | Default | Ja | Getroffen | Blockiert |

### SoF2-Äquivalenz

| SoF2 (id Tech 3) | Unity |
|---|---|
| `CONTENTS_PLAYERCLIP` | BrushCollision Layer |
| `MASK_SHOT` ignoriert Clip Brushes | `~BrushCollision` LayerMask |
| `MASK_PLAYERSOLID` inkludiert alles | BoxCast ohne Layer-Ausschluss |
| Surface Flags (`SURF_METAL`) | `SurfaceTypeMarker.SurfaceType` |
| Trace → Surface Flag → Impact | Raycast → SurfaceTypeMarker → SurfaceImpactDataLoader |

---

## SoF2-Äquivalenz-Tabelle (Gesamt)

| SoF2 Konzept | Unity Äquivalent | Status |
|---------------|-------------------|--------|
| `trap_Trace()` | `Physics.BoxCast()` | ✅ AABB-authentisch |
| `MASK_PLAYERSOLID` | `GroundMask` (LayerMask) | ✅ Portiert |
| `MASK_SHOT` (ohne Clip Brushes) | `~(Hitbox \| BrushCollision)` | ✅ Implementiert |
| `CONTENTS_BODY` | Player-Layer | ✅ Implementiert |
| `CONTENTS_SOLID` | Default-Layer (Surfaces) + BrushCollision (Brushes) | ✅ Implementiert |
| `CONTENTS_PLAYERCLIP` | BrushCollision-Layer | ✅ Implementiert |
| `SURF_METAL` / `SURF_CONCRETE` | `SurfaceTypeMarker` (aus `q3map_material`) | ✅ Implementiert |
| `clientNum` Self-Exclusion | Collider disable/enable | ✅ Implementiert |
| `pm->mins/maxs` (AABB) | BoxCollider + BoxCast (AABB) | ✅ Identisch mit Original |
| `PM_SlideMove` 4-Bump | `PM_SlideMove` 4-Bump | ✅ 1:1 Port |
| `PM_ClipVelocity` | `PM_ClipVelocity` | ✅ 1:1 Identisch |
| `STEPSIZE` (18 QU) | `PmStepSize` (0.4572m) | ✅ Korrekt konvertiert |
| `OVERCLIP` (1.001f) | `OVERCLIP` (1.001f) | ✅ Identisch |
| `PM_AddTouchEnt()` | Nicht portiert | ⚠️ Für Trigger/Items nötig |
| `MASK_PLAYERSOLID & ~CONTENTS_BODY` (Spectator) | GroundMask ohne Player-Layer | ⚠️ Noch nicht implementiert |
| Water Physics | — | ❌ Nicht implementiert |
| Prone Collision | — | ❌ Nicht implementiert |

---

## Zusammenfassung

> **BoxCast (AABB) ist die richtige Wahl für SoF2-Authentizität.** SoF2 nutzte AABB-Traces — der Unity-Port
> bildet das 1:1 mit `Physics.BoxCast(center, halfExtents, dir, hit, Quaternion.identity, dist, mask)` ab.
> LayerMask ist das Äquivalent zu Content Flags, Collider-Disable das Äquivalent zu clientNum.
>
> Die Kernphysik (SlideMove, StepSlideMove, Accelerate, Friction, ClipVelocity) ist identischer C-zu-C#-Port
> aus bg_pmove.c/bg_slidemove.c. Nur Unity-spezifische Anpassungen (OverlapBox-Depenetration,
> CorrectGroundPosition, erhöhte GROUND_TRACE_DIST) wurden hinzugefügt.

Was du hast vs. was du brauchst
System	Dein Setup (CapsuleCollider)	Headshot-Detection
Bewegungs-Kollision	✅ CapsuleCast gegen Welt + Spieler	Bleibt so
Waffen-Hit-Detection	❌ Gleicher CapsuleCollider	Braucht Per-Bone Hitbox Colliders
Wie SoF2 das gelöst hat
SoF2 hatte das detaillierteste Hit-Zonen-System seiner Zeit (GHOUL2):

Jede Mesh-Surface hatte eine hitLocation-Textur (steht in deinen Shader-Dateien!):
hitLocation  models/characters/female_face/f_female_hit
hitLocation  models/characters/chem_suit/hood_chem_suit_hit
Wenn ein trap_Trace einen Spieler traf, wurde die UV-Koordinate am Trefferpunkt ausgelesen
Die hitLocation-Textur war eine Damage Map — Pixelfarbe = Körperzone (Kopf, Torso, Arm, Bein, etc.)
So konnte SoF2 36+ verschiedene Hit-Zonen pro Charakter unterscheiden (Gore-System)
Du hast diese Daten bereits im Projekt:

ShaderEntry.HitLocation in ShaderEntry.cs
hitLocation-Einträge in den Shader-Dateien
Surface-Namen wie head_side_r, head_bck_uppr_l — anatomisch benannte Mesh-Segmente
3 mögliche Ansätze
1. Per-Bone Hitbox Colliders (Empfehlung — Standard für Multiplayer-Shooter)
Character GameObject
  ├─ CapsuleCollider (Layer: Player)     ← Bewegungs-Kollision (bleibt)
  │
  └─ Bone Hierarchy
       ├─ cranium
       │    └─ CapsuleCollider (Layer: Hitbox, Tag: "Head")
       ├─ upper_lumbar
       │    └─ BoxCollider (Layer: Hitbox, Tag: "Torso")
       ├─ lower_lumbar
       │    └─ BoxCollider (Layer: Hitbox, Tag: "Torso")
       ├─ l_leg / r_leg
       │    └─ CapsuleCollider (Layer: Hitbox, Tag: "Legs")
       └─ l_arm / r_arm
            └─ CapsuleCollider (Layer: Hitbox, Tag: "Arms")
Bewegung: Physics.CapsuleCast gegen GroundMask (World + Player Layer) — wie bisher
Waffen: Physics.Raycast gegen HitboxMask (nur Hitbox Layer)
Hitbox-Collider sind Trigger (kein physisches Pushing), bewegen sich mit den Bones
Treffer → collider.tag oder Component abfragen → Körperzone → Damage-Multiplikator
Vorteile: Exakt, debugbar, Industrie-Standard (CS:GO, Valorant, Overwatch)
Nachteile: Mehr Collider pro Spieler (~6-10), muss bei Skin-Wechsel angepasst werden

2. Raycast + nächster Bone (einfacher, weniger genau)
Waffen-Raycast trifft erst den CapsuleCollider (Player)
Dann: Hit-Point gegen alle Bone-Positionen vergleichen → nächster Bone = Körperzone
Kein extra Collider nötig
Vorteile: Einfach, keine zusätzlichen Collider
Nachteile: Ungenau bei Animationen, keine echte Hitbox-Form

3. SoF2's GHOUL2 UV-Lookup (originalgetreu, aber komplex)
Waffen-Raycast trifft Mesh-Surface → UV am Trefferpunkt auslesen
hitLocation-Textur samplen → Farbe = Zone
Du hast die Daten bereits (ShaderEntry.HitLocation)
Vorteile: Originalgetreu, extrem präzise (36+ Zonen)
Nachteile: Komplex, MeshCollider nötig (teuer), UV-Lookup zur Runtime

Empfehlung
Ansatz 1 (Per-Bone Hitboxes) — weil:

Deine Bones existieren bereits (cranium, pelvis, lumbar, legs)
Sauber getrennt von Bewegungs-Kollision (eigener Layer)
Server-autoritativ: Server hat die gleichen Bones → Hitboxes auch serverseitig validierbar
Einfach erweiterbar: Damage-Multiplikatoren pro Zone (Head: 4x, Torso: 1x, Legs: 0.75x)
Industriestandard — jeder moderne Shooter macht es so
Das CapsuleCollider-System für Bewegung bleibt unverändert. Die Hitboxes sind ein separates System auf einem eigenen Layer, nur für Waffen-Raycasts.

