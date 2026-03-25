# Runtime Bugfix Log

Chronologisches Protokoll aller Runtime-Bugfixes mit Ursachen, Lösungen und betroffenen Dateien.

---

## Bug #1: Self-Hit (Eigene Hitboxes getroffen)

**Symptom**: Hitscan-Raycast trifft eigene Hitbox-Trigger → Spieler verletzt sich selbst.

**Ursache**: Vor dem Raycast wurde nur der Movement-BoxCollider deaktiviert, nicht die 17 Hitbox-Trigger.

**Fix**:
- `ClientHitboxSystem.SetHitboxesEnabled(bool)` hinzugefügt: Toggled `m_HitboxObjects[i].SetActive(enabled)`
- `NetworkedPlayerCharacter.m_OwnHitboxSystem` in `OnServerSpawn()` via `GetComponent<ClientHitboxSystem>()`
- ProcessAttack + ProcessAltAttack: `m_OwnHitboxSystem?.SetHitboxesEnabled(false/true)` um Raycasts

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Client/ClientHitboxSystem.cs`
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs`

---

## Bug #2: Ammo Alt-Attack (Reserve statt Clip)

**Symptom**: Alt-Attack (Projektile ohne separaten Ammo-Typ) zieht von Reserve statt Clip ab → Ammo springt zufällig.

**Ursache**: `TryConsumeAltAmmo()` verwendete `m_ReserveAmmo.Value--` für Projektile-ohne-separaten-Ammo.

**Fix**: `m_ReserveAmmo.Value--` → `m_CurrentClipAmmo.Value--` in der projectile-without-separate-ammo Logik.

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs`

---

## Bug #3: Schwarze Blasen statt Blut-Effekte (Color Parsing)

**Symptom**: Effekte zeigen schwarze/graue Blasen statt farbiger Partikel (Blut, Rauch, Feuer).

**Ursache**: Alter .efx-Parser hatte nur 1 von 3 RGB-Kanälen gespeichert → `[0.52]` statt `[0.52, 0.0, 0.05]`.
`ColorFromArray([0.52])` → Hellgrau statt Dunkelrot.

**Fix (KORREKT)**:
- `Tools/fix_colors_only.py`: Liest korrekte 3-Kanal-Werte aus Original-.efx-Dateien
- Patcht NUR Single-Channel-Color-Arrays in JSONs → 157 Farben in 18 Dateien gefixt
- Alle anderen hand-crafted Daten (Trail, Decal, Tracer, Segment-Namen) bleiben erhalten

**WARNUNG**: NIEMALS volle Rekonvertierung (`reconvert_all_effects.py`) ausführen!
Hand-crafted JSON-Daten (startWidth, endWidth, decal rotation/lifetime, segment names) 
gehen dabei verloren und sind NICHT aus .efx-Dateien rekonstruierbar.

**Dateien**:
- 18× `Assets/Resources/Data/Effects/SoF2_Effects_*.json`
- `Tools/fix_colors_only.py` (Fixscript, für zukünftige Nutzung behalten)

---

## Bug #4: RPG7 Selbst-Detonation beim Abschuss

**Symptom**: RPG7-Schuss detoniert sofort mit Knockback auf den Schützen.

**Ursache**: Projektil spawnt an `eyePos` ohne Forward-Offset → trifft eigene Collider im ersten Frame.

**Fix (2-teilig)**:
1. **MISSILE_PRESTEP**: 32 QU (0.8128m) Forward-Offset beim Spawn mit Wall-Check-Raycast
2. **Owner-Filtering**: `Physics.RaycastAll` + `IsChildOf(m_OwnerRoot)` Filter für World + Hitbox Raycasts

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` (ProcessProjectileAttack)
- `Assets/Scripts/Runtime/Game/Projectiles/ServerProjectile.cs` (Initialize, Move)

---

## Bug #5: Movement-Collider blockiert Hitboxes

**Symptom**: Schüsse auf Spieler treffen den Movement-BoxCollider statt die Hitbox-Trigger → kein Schaden.
Nur Treffer "außerhalb" des Movement-Colliders (Arme, Beine) registrieren.

**Ursache**: `worldLayerMask = ~(Hitbox | BrushCollision)` schloss den Player-Layer (7) NICHT aus.
Der World-Raycast traf den Movement-BoxCollider (non-Trigger, Layer "Player"), der oft näher lag
als die Hitbox-Trigger. Im Distanz-Vergleich "gewann" der World-Hit → kein Damage.

**Fix**: `"Player"` zur Exclusion-Maske hinzugefügt:
```csharp
// VORHER:
int worldLayerMask = ~(hitboxLayerMask | LayerMask.GetMask("BrushCollision"));
// NACHHER:
int worldLayerMask = ~(hitboxLayerMask | LayerMask.GetMask("BrushCollision", "Player"));
```

**Dateien (4 Stellen)**:
- `NetworkedPlayerCharacter.cs` → ProcessAttack (Zeile ~814)
- `NetworkedPlayerCharacter.cs` → ProcessAltAttack (Zeile ~1983)
- `ServerProjectile.cs` → Initialize (Zeile ~142)
- `ClientProjectileVisual.cs` → Initialize (Zeile ~160)

---

## Offene Punkte (Nice-to-Have)

| Feature | Status | Beschreibung |
|---------|--------|-------------|
| Alpha Curve | Nicht implementiert | `EffectAlphaDefinition.Curve` existiert im DTO, `BuildGradient()` ignoriert es (immer linear) |
| Decal Fade-Out | Nicht implementiert | Decals verschwinden nach 30s abrupt (`Object.Destroy`), kein gradueller Fade |
| parmMax Randomization | Nicht implementiert | Alpha + Size ignorieren `parmMax` → kein per-Partikel Timing-Randomisierung |
| fireDelay | Nicht implementiert | MM1 (350ms), USAS12 (75ms), MSG90A1 (100ms) Pre-Fire-Delay |
