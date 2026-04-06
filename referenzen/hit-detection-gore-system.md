# SoF2 Hit Detection & Gore System Reference

## Übersicht

SoF2's GHOUL2-System hatte die detaillierteste Hit-Zonen-Erkennung seiner Zeit.
Dieses Dokument beschreibt das **originale SoF2-System**, unseren **Unity-Ansatz** und die **empfohlene Strategie** für Gore/Dismemberment.

---

## 1. Originales SoF2 Hit Detection (GHOUL2)

SoF2 verwendete **3 Systeme** zusammen:

### 1.1 NPC Bounds (Grob-Test)
Einfache AABB pro NPC-State für den ersten `trap_Trace`-Test:

| State | Min | Max | Größe (Units) |
|-------|-----|-----|----------------|
| Stand | -17 -17 -45 | 17 17 45 | 34×34×90 |
| Crouch | -17 -17 -45 | 17 17 45 | 34×34×90 |
| Dive | -17 -17 -45 | 17 17 15 | 34×34×60 |
| Prone | -17 -17 -45 | 17 17 -35 | 34×34×10 |
| Pain | -30 -30 -45 | 30 30 45 | 60×60×90 |
| Dead | -10 -10 -45 | 10 10 -35 | 20×20×10 |
| Vault | -17 -17 5 | 17 17 45 | 34×34×40 |
| Fall | -10 -10 15 | 10 10 45 | 20×20×30 |

**Quelle**: `SoF2_NPCs.json` → `NPC_Base_Human` → `Bounds`

### 1.2 hitLocation-Texturen (Fein-Test — das eigentliche System)
- Jede Mesh-Surface hatte eine **Damage Map Texture** (`hitLocation` in `.g2shader`-Dateien)
- Nach Trace-Hit → UV-Koordinate am Trefferpunkt auslesen → hitLocation-Textur samplen → **Pixelfarbe = Körperzone**
- Ermöglichte **36+ verschiedene Zonen** pro Charakter (bis zu einzelnen Ohren, Augen, Fingern)
- Basis des Gore-Systems (Extremitäten abtrennen, Kopfteile wegsprengen)

**Shader-Daten vorhanden in**: `Assets/Resources/Data/shaders/*.g2shader`
```
hitLocation  models/characters/average_face/m_avg_w1_hit
hitLocation  models/characters/chem_suit/hood_chem_suit_hit
hitMaterial  models/characters/average_sleeves/b_col_rebel_h4_hit
```

**Code**: `ShaderEntry.HitLocation` + `ShaderDataReader.cs` parst diese Einträge

### 1.3 HitRegion-Index (Animations-Trigger)
17 Regionen für Pain/Death-Animationsauswahl — **nicht für Hitbox-Geometrie**:

| Index | Region | Bone-Zuordnung |
|-------|--------|----------------|
| 0 | Head | cranium → cervical |
| 1 | Right Leg | rtibia → rtarsal |
| 4 | Neck | cervical → thoracic |
| 8 | Chest | thoracic → upper_lumbar |
| 12 | Right Foot | rtarsal |
| 16 | Left Shoulder | lclavical → lhumerus |
| 20 | Left Arm | lhumerus → lradius |
| 24 | Left Hand | lradius → lhand |
| 28 | Right Shoulder | rclavical → rhumerus |
| 32 | Right Arm | rhumerus → rradius |
| 36 | Right Hand | rradius → rhand |
| 40 | Gut | upper_lumbar → lower_lumbar |
| 44 | Groin | lower_lumbar → pelvis |
| 48 | Left Thigh | lfemurYZ → ltibia |
| 52 | Left Leg | ltibia → ltarsal |
| 56 | Left Foot | ltarsal |
| 60 | Right Thigh | rfemurYZ → rtibia |

**Quelle**: `SoF2_DATA.json` → `HitRegions`, `HitRegion.cs`

---

## 2. Unser Unity-Ansatz: Per-Bone BoxCollider

### 2.1 ClientHitboxSystem.cs — 17 BoxCollider an Bones
Industriestandard (CS:GO, Valorant, Overwatch). Jeder BoxCollider:
- Ist ein Trigger (kein physisches Pushing)
- Bewegt sich mit dem Bone
- Hat `HitboxCollider`-Komponente mit `HitRegion` + `DamageMultiplier`

### 2.2 Hitbox-Definitionen (aktuell)

| Hitbox | Start-Bone → End-Bone | DmgMult | WidthFactor |
|--------|----------------------|---------|-------------|
| Head | cranium → cervical | 1.75× | 0.55 |
| Neck | cervical → thoracic | 1.75× | 0.35 |
| Chest | thoracic → upper_lumbar | 1.0× | 0.80 |
| Gut | upper_lumbar → lower_lumbar | 1.0× | 0.70 |
| Groin | lower_lumbar → pelvis | 1.0× | 0.60 |
| L/R Shoulder | clavical → humerus | 0.7× | 0.35 |
| L/R Arm | humerus → radius | 0.7× | 0.30 |
| L/R Hand | radius → hand | 0.3× | 0.25 |
| L/R Thigh | femurYZ → tibia | 0.7× | 0.40 |
| L/R Leg | tibia → tarsal | 0.7× | 0.30 |
| L/R Foot | tarsal (EndBone) | 0.4× | 0.25 |

### 2.3 Vergleich: SoF2 Original vs. Unity-System

| Aspekt | SoF2 Original | Unity-System |
|--------|---------------|-------------|
| Grob-Test | AABB pro State | CapsuleCollider (ClientColliderSystem) |
| Fein-Test | hitLocation-Texture UV-Sampling | Per-Bone BoxCollider |
| Zonen | 36+ (Textur-basiert) | 17 (Bone-basiert) |
| Geometrie | Mesh-Surface Tracing | BoxCollider an Bones |
| Server-tauglich | Nein (braucht Mesh-Daten) | Ja (nur Bone-Transforms) |
| Performance | Teuer (MeshCollider+UV-Readback) | Günstig (BoxCollider Trigger) |

---

## 3. Gore/Dismemberment System

### 3.1 SoF2 Gore Areas (hierarchisch)
Das Gore-System arbeitet **hierarchisch** — wird ein Arm abgetrennt, fliegt die Hand automatisch mit.

**Quelle**: `SoF2_DATA.json` → `Gore` → `gore_areas`

#### Extremitäten (Limbs)
```
hand
 ├─ Surfaces_Off: fingers_<PS>, hand_<PS>
 ├─ Surfaces_On:  cap_arm_lwr_<PS>_hand_off (Gore-Cap)
 ├─ Chunk:        hand_<PS> (Force: 40-60N)
 └─ FX:           gore_mist_small, blood_spurt_arterial_small

arm_lower → Children: [hand]
 ├─ Flags: NoChildSurfacesOn, NoChildChunks, NoChildFX
 ├─ Surfaces_Off: arm_lwr_<PS>, armpad_<PS>
 ├─ Surfaces_On:  cap_arm_uppr_<PS>_lwr_off
 ├─ Chunk:        arm_lwr_<PS> (Force: 40-60N)
 ├─ BoltOn:       bone_small @ *bicep_<PS>g
 └─ FX:           gore_mist_small, blood_spurt_arterial_small

arm_upper → Children: [arm_lower]
 ├─ Flags: NoChildSurfacesOn, NoChildChunks, NoChildFX
 ├─ Surfaces_Off: arm_uppr_<PS>, sleeve_<PS>, armpad_<PS>
 ├─ Surfaces_On:  cap_torso_<PS>_arm_off
 ├─ Chunk:        arm_uppr_<PS> (Force: 60-100N)
 ├─ BoltOn:       shoulder_bone @ *shldr_<PS>g
 └─ FX:           gore_mist_small, blood_spurt_arterial

foot
 ├─ Surfaces_Off: foot_<PS>
 ├─ Surfaces_On:  cap_leg_lwr_<PS>_foot_off
 ├─ Chunk:        foot_<PS> (Force: 20-50N)
 └─ FX:           gore_mist_small, blood_spurt_arterial_small

leg_lower → Children: [foot]
 ├─ Surfaces_Off: leg_lwr_<PS>, kneepad_<PS>
 ├─ Surfaces_On:  cap_leg_uppr_<PS>_lwr_off
 ├─ Chunk:        leg_lwr_<PS> (Force: 20-50N)
 └─ FX:           gore_mist_small, blood_spurt_arterial_small

leg_upper → Children: [leg_lower]
 ├─ Surfaces_Off: leg_uppr_<PS>, kneepad_<PS>
 ├─ Surfaces_On:  cap_hip_<PS>_off
 ├─ Chunk:        leg_uppr_<PS> (Force: 20-50N)
 ├─ BoltOn:       bone_long @ *hip_<PS>g
 └─ FX:           gore_mist_small, blood_spurt_arterial

hip → Children: [] (+ leg_upper als Children_Off im Chunk)
 ├─ Surfaces_Off: hip_<PS>
 ├─ Surfaces_On:  cap_torso_<PS>_off, cap_leg_uppr_<PS>_hip_off, cap_hip_<OS>_off
 ├─ Chunk:        hip_<PS> (Force: 20-50N)
 └─ FX:           gore_mist_small, blood_spurt_arterial

torso
 └─ FX:           blood_mist_small @ *uchest_<PS>
```

#### Kopf (hierarchisch, detailliert)
```
head → Children: [head_right, head_left]
 ├─ Flags: NoChildSurfacesOn, NoChildFX, NoChildBoltOns
 ├─ Surfaces_On:  cap_torso_<PS>_off, cap_torso_<OS>_off
 └─ FX:           blood_spurt_arterial, gore_mist_small, flesh_chunks @ *neckg

head_<PL> → Children: [head_back_lower, head_back_upper, head_front_lower,
                        head_front_mid, head_front_upper, head_side]
 ├─ Flags: NoChildSurfacesOn, NoChildBoltOns
 ├─ Surfaces_On:  cap_torso_<OS/PS>_off, cap_head_*_off (diverse)
 └─ BoltOn:       brain @ *headg

head_front_mid (Gesicht-Mitte)
 ├─ Surfaces_Off: head_frnt_mid_<PS>, eyelash_<PS>, ear_r, ...
 └─ Surfaces_On:  cap_head_frnt_lwr/uppr/bck/side_off (8 Caps!)

head_side (Seite)
 ├─ Surfaces_Off: head_side_<PS>, ear_<PS>, Haare...
 └─ Surfaces_On:  6 verschiedene Caps

head_back_lower / head_back_upper / head_front_lower / head_front_upper
 └─ Jeweils eigene Surface-Listen + Cap-Zuordnung
```

### 3.2 Gore Pieces (fliegende Teile)
```
brain          → models/characters/gore/brain/g2brain.glm      @ *headg
bone_long      → models/characters/gore/bone_long/bone_long_mp.glm
shoulder_bone  → models/characters/gore/shoulder_bone/shoulder_bone.glm
bone_small     → models/characters/gore/bone_small/bone_small.glm
```

### 3.3 Gore Effects
```
flesh_chunks              → flesh_chunks_mp.efx
gore_mist_small           → gore_mist_small.efx
blood_spurt_arterial      → blood_spurt_arterial_mp.efx
blood_spurt_arterial_small → blood_spurt_arterial_small_mp.efx
```

### 3.4 Gore Cap Surfaces (NPC_definition.json)
Wann eine Extremität entfernt wird, werden **Gore-Cap-Surfaces** aktiviert, die den "Stumpf" zeigen:

```
cap_arm_lwr_{l/r}_hand_off      — Unterarm-Cap nach Hand-Abtrennung
cap_arm_lwr_{l/r}_uppr_off      — Unterarm-Cap nach Oberarm-Abtrennung
cap_arm_uppr_{l/r}_lwr_off      — Oberarm-Cap nach Unterarm-Abtrennung
cap_arm_uppr_{l/r}_torso_off    — Oberarm-Cap bei Schulter-Abtrennung
cap_foot_{l/r}_off               — Fuß-Cap
cap_hand_{l/r}_off               — Hand-Cap
cap_head_bck_lwr_{l/r}_off      — Kopf hinten unten
cap_head_bck_uppr_{l/r}_off     — Kopf hinten oben
cap_head_frnt_lwr_{l/r}_off     — Kopf vorne unten
cap_head_frnt_mid_{l/r}_off     — Kopf vorne mitte (Gesicht)
cap_head_frnt_uppr_{l/r}_off    — Kopf vorne oben
cap_head_side_{l/r}_off         — Kopf Seite
cap_hip_{l/r}_off                — Hüfte
cap_leg_lwr_{l/r}_foot_off      — Unterschenkel nach Fuß-Abtrennung
cap_leg_lwr_{l/r}_uppr_off      — Unterschenkel nach Oberschenkel-Abtrennung
cap_leg_uppr_{l/r}_hip_off      — Oberschenkel nach Hüfte
cap_leg_uppr_{l/r}_lwr_off      — Oberschenkel nach Unterschenkel
cap_torso_{l/r}_arm_off         — Torso nach Arm-Abtrennung
cap_torso_{l/r}_off             — Torso nach Hals/Kopf-Abtrennung
```

### 3.5 Template-Platzhalter
```
<PS> = Primary Short (z.B. "r" für rechts)
<PL> = Primary Long (z.B. "right")
<OS> = Opposite Short (z.B. "l" für links)
<OL> = Opposite Long (z.B. "left")
```

---

## 4. Empfohlene Strategie: Hitbox → Surface Gore

### 4.1 Zwei-Stufen-Ansatz

```
Waffen-Raycast
     │
     ▼
┌─────────────────────────────────┐
│ Stufe 1: BoxCollider (17 Zonen) │
│ → Bestimmt HitRegion            │
│ → Damage-Berechnung             │
│ → Pain/Death-Animation          │
└──────────────┬──────────────────┘
               │
               ▼
┌─────────────────────────────────┐
│ Stufe 2: Surface-System (Gore)  │
│ → HitRegion → Gore Area Mapping │
│ → Surfaces_Off / Surfaces_On    │
│ → Chunk spawnen + Force          │
│ → BoltOn (Knochen, Gehirn)      │
│ → FX (Blut, Fleisch)            │
└─────────────────────────────────┘
```

### 4.2 HitRegion → Gore Area Mapping

| HitRegion (BoxCollider) | Gore Area(s) |
|------------------------|--------------|
| Head | head → head_right/left → 6 Unter-Zonen |
| Neck | head (ganzer Kopf fliegt ab) |
| Chest | torso |
| Gut | torso |
| Groin | hip |
| LeftShoulder / RightShoulder | arm_upper |
| LeftArm / RightArm | arm_lower |
| LeftHand / RightHand | hand |
| LeftThigh / RightThigh | leg_upper |
| LeftLeg / RightLeg | leg_lower |
| LeftFoot / RightFoot | foot |

### 4.3 Ablauf bei Treffer

1. **Raycast** trifft BoxCollider → `HitboxCollider.HitRegion` + `DamageMultiplier` auslesen
2. **Damage berechnen**: `Waffenschaden × DamageMultiplier`
3. **Pain/Death-Animation**: HitRegion-Index für Animation-Lookup verwenden
4. **Gore-Check**: Wenn DamageLevel hoch genug (abhängig von Waffe + Schaden):
   - HitRegion → Gore Area mappen
   - `Surfaces_Off` deaktivieren (Körperteil verschwindet)
   - `Surfaces_On` aktivieren (Gore-Caps zeigen "Stumpf")
   - `Chunk` spawnen (abgetrenntes Teil fliegt mit Force weg)
   - `BoltOn` spawnen (Knochen/Gehirn am Stumpf)
   - `FX` abspielen (Blut, Fleisch-Chunks)
5. **Hierarchie beachten**: Wenn `arm_upper` abgetrennt → `arm_lower` + `hand` automatisch mit (Children)

### 4.4 Für den Detail-Kopf (optional)

Für den Kopf kann man den **Trefferpunkt auf dem BoxCollider** analysieren:
- BoxCollider-Hit-Position in lokale Bone-Koordinaten umrechnen
- Anhand der lokalen Position bestimmen: front/back/left/right/upper/lower
- Entsprechende Kopf-Unter-Zone auswählen (`head_front_mid`, `head_side`, etc.)
- Nur die entsprechenden Surfaces abschalten → halber Kopf weg, statt ganzer Kopf

Das ist **kein UV-Sampling nötig** — die Box-Position reicht aus für ~6 Kopf-Zonen.

---

## 5. Surface-Hierarchie aller Körperteile

### 5.1 Reguläre Model-Surfaces (NPC_definition.json)

**Arms** (Shader `a_xxx`):
```
arm_lwr_l, arm_lwr_r, arm_uppr_l, arm_uppr_r,
fingers_l, fingers_r, hand_l, hand_r
```

**Body** (Shader `b_xxx`):
```
foot_l, foot_r, hip_l, hip_r,
leg_lwr_l, leg_lwr_r, leg_uppr_l, leg_uppr_r,
torso_l, torso_r
```

**Face** (Shader `f_xxx`):
```
ear_l, ear_r, eyeball_l, eyeball_r,
head_frnt_lwr_l, head_frnt_lwr_r,
head_frnt_mid_l, head_frnt_mid_r,
head_frnt_uppr_l, head_frnt_uppr_r,
mouth_l, mouth_r
```

**Head** (Shader `h_xxx`):
```
head_bck_lwr_l, head_bck_lwr_r,
head_bck_uppr_l, head_bck_uppr_r,
head_side_l, head_side_r
```

### 5.2 Vorhandene Code-Klassen

| Klasse | Pfad | Zweck |
|--------|------|-------|
| `HitRegion` | Shared/HitRegion.cs | Enum der 17 Regionen mit Original-Indices |
| `HitboxCollider` | Shared/HitboxCollider.cs | Komponente auf BoxCollider mit Region + DmgMult |
| `ClientHitboxSystem` | Client/ClientHitboxSystem.cs | Erstellt 17 BoxCollider an Bones |
| `GoreArea` | DTOs/GoreManagement/GoreArea.cs | DTO für Gore-Zonen-Definition |
| `GorePiece` | DTOs/GoreManagement/GorePiece.cs | DTO für Gore-Model-Stücke |
| `GoreDataLoader` | DataManagement/GoreDataLoader.cs | Lädt Gore-Daten aus SoF2_DATA.json |
| `ShaderEntry` | DTOs/SkinManagement/ShaderEntry.cs | HitLocation + HitMaterial pro Surface |
| `ShaderDataReader` | Shared/ShaderDataReader.cs | Parst .g2shader Dateien inkl. hitLocation |

---

## 6. Damage Levels & Waffen-Integration

### 6.1 DamageLevels (SoF2_DATA.json)
```
0 = Low
1 = Medium
2 = High
3 = Low Death
4 = Medium Death
5 = High Death
```

### 6.2 Welcher DamageLevel triggert welches Gore?
- **Low/Medium**: Blut-Effekt, Pain-Animation → kein Dismemberment
- **High**: Stärkerer Blut-Effekt, eventuell kleine Chunks
- **Low Death**: Einfacher Tod, minimales Gore
- **Medium Death**: Tod + Dismemberment der getroffenen Zone
- **High Death**: Tod + massives Gore + Kinderzonen fliegen mit

### 6.3 Waffen-Gore-Flag
Aus `SoF2_Weapons_new.json`: `mp_gore: false/true` — kontrolliert ob die Waffe Gore auslöst.

---

## 7. Zusammenfassung der Architektur

```
Server-Raycast → BoxCollider Hit
                    │
                    ├── HitRegion → Damage Calculation
                    │                    │
                    │                    ├── Health > 0 → Pain Animation (HitRegion-Index)
                    │                    │
                    │                    └── Health ≤ 0 → Death
                    │                                      │
                    │                                      ├── DamageLevel Check
                    │                                      │
                    │                                      └── Gore System
                    │                                           │
                    │                                           ├── Map HitRegion → GoreArea
                    │                                           ├── Surfaces_Off (Body)
                    │                                           ├── Surfaces_On (Gore Caps)
                    │                                           ├── Spawn Chunks (Force)
                    │                                           ├── Spawn BoltOns (Bones)
                    │                                           └── Play FX (Blood)
                    │
                    └── Optional: Hit-Position → Kopf-Subzone
                                   für detailliertes Head-Gore
```

---

## 8. Gore-Decal-System (PGoreWeaponDispatch + PGoreDecalApplier)

### 8.1 Überblick

Zusätzlich zum Dismemberment-System (Extremitäten abtrennen) gibt es ein **Decal-basiertes Gore-System**,
das Blut-/Wund-Texturen direkt auf den Charakter projiziert. Dieses System arbeitet **unabhängig** vom
Dismemberment und wird bei **jedem Treffer** ausgelöst (nicht nur bei Tod/Abtrennung).

```
Server: Waffen-Treffer erkannt
  └─ PGoreWeaponDispatch.CreateGoreEntries(weaponId, isAlt, hitLocation, hitDirection)
       └─ List<PGoreData> (1-5 Einträge je nach Waffe + GoreDetailLevel)
            └─ Per ClientRpc an alle Clients gesendet
                 └─ PGoreDecalApplier.ApplyGoreDecals(entries, characterRoot)
                      └─ Für jeden Eintrag: Quad-Decal am nächsten Bone
```

### 8.2 PGoreWeaponDispatch

**Datei**: `Assets/Scripts/Runtime/Management/GoreManagement/PGoreWeaponDispatch.cs`

Erzeugt waffen-spezifische Gore-Decal-Listen basierend auf Waffentyp und Trefferpunkt.

**Konstanten**:

| Konstante | Wert | Beschreibung |
|-----------|------|-------------|
| `SOF2_UNIT_SCALE` | 0.0254 | SoF2 Units → Unity Meter |
| `DEFAULT_GROW_DURATION` | 15000 ms | Blutlachen-Wachstumsdauer |
| `DEFAULT_GROW_START_FRACTION` | 0.1 (10%) | Startgröße beim Wachsen |

**GoreDetailLevel** (statische Property):

| Level | Beschreibung |
|-------|-------------|
| 0 | Nur Hauptwunde (minimales Gore) |
| 1 | + wachsende Blutlache (Standard) |
| 2 | + Pellet-Markierungen (Schrotflinten-Detail) |

**Haupt-Methode**:
```csharp
public static List<PGoreData> CreateGoreEntries(
    string weaponId,        // Waffen-ID (z.B. "m4", "knife", "m590")
    bool isAltAttack,       // Alt-Fire Modus
    Vector3 hitLocation,    // Trefferpunkt (World-Space)
    Vector3 hitDirection    // Schussrichtung
)
```

**Unterstützte Waffen** (20+):

| Kategorie | Waffen | Gore-Typ |
|-----------|--------|----------|
| Messer | knife | Punktur + 3 Slashes + Soak |
| Pistolen | m1911a1, silvertalon, ussocom | Kleines Einschussloch + Soak |
| SMGs | microuzi | Kleines Einschussloch + Soak |
| Gewehre | m3a1, mp5, sig551, m4, ak74 | Mittleres Einschussloch + Soak |
| Schrotflinten | m590, usas12 | Mehrere Pellet-Markierungen + großer Soak |
| Großkaliber | msg90a1, m60 | Großes Einschussloch + großer Soak |
| Explosiv | mm1, rpg7, smohg92, f1, m67, l2a2 | Großer Blast-Gore |
| Stun/Blend | m84, m15 | Minimaler Gore / kein Gore |
| Brand | anm14 | Brand-Gore |
| Sonstige | mdn11, default | Mittlerer Standard-Gore |

**Interne Helfer-Methoden**:

| Methode | Beschreibung |
|---------|-------------|
| `AddBulletGore()` | Einschussloch + wachsende Blutlache (bei Level ≥ 1) |
| `AddShotgunGore()` | Mehrere Pellet-Markierungen (bei Level ≥ 2) |
| `AddGore()` | Einfacher Gore-Eintrag (Typ + Größe + Position + Richtung) |
| `AddGrowGore()` | Wachsender Gore-Eintrag (Blutlache, animierte Größe) |
| `AddSlashGore()` | Schnittwunde mit Winkel + S/T-Größe (Messer) |
| `AddSlashGrowGore()` | Wachsende Schnittwunde |
| `AddTimedGore()` | Zeitlich begrenzter Gore-Eintrag (verschwindet nach Lifetime) |

### 8.3 PGoreDecalApplier

**Datei**: `Assets/Scripts/Runtime/Management/GoreManagement/PGoreDecalApplier.cs`

Wendet die von `PGoreWeaponDispatch` erzeugten `PGoreData`-Einträge als visuelle Decals
auf dem Charakter-Modell an.

**Konstanten**:

| Konstante | Wert | Beschreibung |
|-----------|------|-------------|
| `DECAL_SURFACE_OFFSET` | 0.002 m | Z-Fighting-Prävention |
| `DEFAULT_LIFETIME_SECONDS` | 120 s | Standard-Decal-Lebensdauer |
| `SOF2_DECAL_SHADER` | `"SoF2/Decal"` | Primärer Custom-Shader |
| `FALLBACK_SHADER` | `"SoF2/EffectParticle"` | Fallback-Shader #1 |
| `URP_FALLBACK_SHADER` | `"Universal Render Pipeline/Particles/Unlit"` | URP-Fallback |

**Methoden**:

| Methode | Beschreibung |
|---------|-------------|
| `ApplyGoreDecals(List<PGoreData>, GameObject)` | Wendet Gore-Decal-Liste auf Character an |
| `SpawnGoreDecal(PGoreData, GameObject, Transform[])` | Einzelnes Decal: nächsten Bone finden, Quad erstellen, Material konfigurieren, Growth-Behaviour + Lifetime setzen |
| `FindNearestBone(Vector3, Transform[])` | Findet nächsten Bone per Squared-Magnitude |
| `GetOrCreateMaterial(string)` | Material-Cache mit Shader-Fallback-Kette |
| `ClearCache()` | Zerstört gecachte Materialien |

**Decal-Mechanik**:
- Jedes Gore-Decal ist ein **Quad** (wie Footstep-Decals, aber an Bones statt am Boden)
- Quad wird am **nächsten Bone** des Charakters positioniert (via `FindNearestBone`)
- Wachsende Decals (Blutlachen) skalieren über `growDuration` von `startFraction` auf volle Größe
- Material-Cache verhindert redundante Material-Erstellung
- Default Lifetime: 120s (quasi-permanent für die Dauer eines Spiels)

### 8.4 PGoreType Enum

23+ Gore-Typen mit je eigener Textur:

| Typ | Beschreibung | Waffen-Bezug |
|-----|-------------|-------------|
| Puncture | Stichstelle | Knife |
| SlashHorizontal | Horizontaler Schnitt | Knife |
| SlashVertical | Vertikaler Schnitt | Knife |
| SlashDiagonal | Diagonaler Schnitt | Knife |
| BulletSmall | Kleines Einschussloch | Pistolen, SMGs |
| BulletMedium | Mittleres Einschussloch | Gewehre |
| BulletLarge | Großes Einschussloch | Scharfschützen, MGs |
| BloodSoak | Blutlache (wachsend) | Alle Kugelwaffen |
| ShotgunPellet | Schrot-Pellet-Markierung | Schrotflinten |
| BlastBurn | Explosions-Brandmarkierung | Explosivwaffen |
| ... | (weitere typenspezifische Texturen) | Diverse |

### 8.5 3-Layer Decal-Architektur (Gesamtübersicht)

Das Spiel verwendet **drei unabhängige Decal-Systeme**:

| Layer | Datei | Ziel | Größe | Lifetime | Beschreibung |
|-------|-------|------|-------|----------|-------------|
| **Footstep** | EffectFactory.cs | Boden (World) | 0.22m | 15s | Fußabdrücke auf Oberflächen |
| **Gore** | PGoreDecalApplier.cs | Charakter (Bones) | variabel | 120s | Blut/Wunden auf Spielern |
| **Impact** | EffectFactory.cs | Boden/Wand (World) | aus JSON | aus JSON | Einschusslöcher auf Geometry |
