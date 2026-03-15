# Hitscan Damage System

Server-autoritatives Hitscan-Schadenssystem nach SoF2-Vorbild (`g_weapon.c` FireWeapon).
Kein separater RPC — Attack wird aus dem `PlayerCommand` (usercmd_t) gelesen,
Position + Blickrichtung sind exakt synchron mit der Server-Physik.

---

## Architektur-Überblick

```
Client sendet PlayerCommand (mit PitchAngle, YawAngle, Attack-Button)
        ↓
Server: NetworkedPlayerCharacter.ProcessCommand()
        ↓
Server gated: noActionRunning && cmd.HasButton(Attack)
        ↓
ProcessAttack(cmd)
  ├─ TryConsumeAmmo() (Server-autoritativ)
  ├─ UpdateServerAttackParameters() (Frame-Cooldown)
  ├─ Eye-Position + Aim-Direction berechnen
  ├─ Inaccuracy-Buildup (Basis → MaxInaccuracy bei Dauerfeuer)
  ├─ Multi-Pellet Loop (Schrotflinten: N Pellets pro Schuss)
  │   ├─ ApplyInaccuracy() (Kegel-Streuung pro Pellet)
  │   ├─ Physics.Raycast() auf Hitbox-Layer
  │   ├─ HitboxCollider → DamageMultiplier → SetHealth()
  │   └─ DebugTracerClientRpc() (pro Pellet)
  ├─ KickAngles → ApplyKickAnglesClientRpc() (View-Recoil an Owner)
  └─ Collider wieder aktivieren
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **NetworkedPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs` | Server: ProcessAttack, Raycast, Damage, Tracer-RPC, KickAngles-RPC |
| **ServerPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Server/ServerPlayerCharacter.cs` | GetEyePosition(), SetPhysicsColliderEnabled() |
| **ClientPlayerCharacter** | `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` | PitchAngle im Command, ApplyKickAngles() |
| **PlayerCommand** | `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerCommand.cs` | Struct mit PitchAngle, YawAngle, Buttons |
| **AimCameraController** | `Assets/Scripts/Runtime/Game/Camera/AimCameraController.cs` | AddViewPunch() für KickAngles |
| **NetworkedCharacterState** | `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs` | TryConsumeAmmo(), SetHealth(), Health |
| **HitboxCollider** | `Assets/Scripts/Runtime/Game/Characters/Shared/HitboxCollider.cs` | DamageMultiplier, HitRegion |
| **WeaponAttackDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponAttackDefinition.cs` | Damage, Range, Inaccuracy, MaxInaccuracy, Pellets, Spread, KickAngles |

---

## PlayerCommand (usercmd_t)

```csharp
public struct PlayerCommand : INetworkSerializable
{
    public Vector2 MoveInput;      // WASD / Stick
    public float YawAngle;         // Horizontale Blickrichtung
    public float PitchAngle;       // Vertikale Blickrichtung (Maus Y)
    public int Buttons;            // Bitfield: Attack, AltAttack, Jump, Crouch, Reload, etc.
    public float DeltaTime;        // Frame-Zeit
    public uint SequenceNumber;    // Prediction-Sequenz
}
```

`PitchAngle` wird vom Client aus `m_PitchTarget.eulerAngles.x` gelesen.
Der Server nutzt `Quaternion.Euler(cmd.PitchAngle, cmd.YawAngle, 0f)` für die Schussrichtung.

---

## Eye-Position (SoF2 Viewheight)

```
SoF2: DEFAULT_VIEWHEIGHT = 26 QU
      playerMins.z = -46 QU (Füße)
      → Augenhöhe = 26 + 46 = 72 QU von den Füßen
      Gesamthöhe = 89 QU (CROUCH_VIEWHEIGHT = 12 → 58 QU)

Unity: EYE_HEIGHT_RATIO = 72f / 89f ≈ 0.809
       eyePos = transform.position + Vector3(0, CapsuleHeight × 0.809, 0)
```

Definiert in `ServerPlayerCharacter.GetEyePosition()`.

---

## Inaccuracy-System

### Basis-Inaccuracy
Jeder Schuss bekommt eine zufällige Streuung innerhalb eines Kegels (`ApplyInaccuracy()`).
Rotation per `Quaternion.AngleAxis` — gleichmäßig verteilter Kegel.

### MaxInaccuracy-Buildup (Dauerfeuer)
- Pro Schuss steigt `m_ServerAccumulatedInaccuracy` um `INACCURACY_BUILDUP_STEP (0.3°)`
- Maximum: `attackDef.MaxInaccuracy`
- Decay: Nach `INACCURACY_DECAY_TIME (0.5s)` ohne Schuss → Reset auf `attackDef.Inaccuracy`
- Tracking: `m_ServerAccumulatedInaccuracy`, `m_ServerLastShotTime` (Server-seitig)

### Waffen-Werte

| Waffe | Inaccuracy (Basis) | MaxInaccuracy | Buildup Effekt |
|-------|--------------------|---------------|----------------|
| M4 | 0.12° | 2.15° | Stark spürbar: 0.12 → 2.15 bei Dauerfeuer |
| AK74 | 0.18° | 2.5° | Noch stärker |
| M590 | 3.0° | 3.0° | Kein Buildup (Basis = Max) |
| USAS12 | 3.5° | 3.5° | Kein Buildup |
| MSG90A1 | 1.0° | 2.0° | Moderat |
| M60 | 0.475° | 3.0° | Stark: LMG wird ungenauer |
| M1911A1 | 0.5° | 2.5° | Moderat |

---

## Multi-Pellet (Schrotflinten)

Waffen mit `Pellets > 0` feuern N separate Raycasts pro Schuss:

```
pelletCount = attackDef.Pellets > 0 ? attackDef.Pellets : 1;
totalSpread = accumulatedInaccuracy + attackDef.Spread;  // pro Pellet
```

Jedes Pellet:
- Eigene zufällige Streuung (unabhängig)
- Eigener Physics.Raycast
- Eigene Damage-Berechnung (Damage × DamageMultiplier pro Pellet)
- Eigener Debug-Tracer

| Waffe | Pellets | Spread | Damage/Pellet | Max Damage (alle treffen, Body) |
|-------|---------|--------|---------------|-------------------------------|
| M590 | 8 | 0.05° | 22 | 176 |
| USAS12 | 8 | 0.05° | 15 | 120 |

---

## KickAngles (View-Recoil)

SoF2-Format: `[minPitch, maxPitch, minYaw, maxYaw]`

Server berechnet zufälligen Kick innerhalb der Grenzen, sendet per `Rpc(SendTo.Owner)`:
```
pitchKick = Random.Range(kickAngles[0], kickAngles[1]);  // nach oben
yawKick   = Random.Range(kickAngles[2], kickAngles[3]);  // seitlich
```

Client empfängt → `AimCameraController.AddViewPunch(pitch, yaw)`:
- **Permanent**: Modifiziert `m_Pitch` und `m_Yaw` direkt
- Spieler muss aktiv mit der Maus gegenlenken (SoF2-Stil, kein Auto-Recovery)

### Waffen-Werte

| Waffe | Pitch (min-max) | Yaw (min-max) | Gefühl |
|-------|-----------------|---------------|--------|
| M4 | 1-3° | -2 bis 1° | Leicht kontrollierbar |
| AK74 | 1-3° | -2 bis 1° | Wie M4 |
| M590 | 1-8° | -2 bis 1° | Starker Rückstoß |
| USAS12 | 1-9° | -2 bis 1° | Sehr stark |
| MSG90A1 | 1-15° | -2 bis 1° | Extrem (Sniper) |
| M60 | 1-3° | -2 bis 1° | LMG, wie Rifle |
| M1911A1 | 1-5° | -2 bis 1° | Pistole, moderat |
| MM1 | 1-12° | -2 bis 1° | Granatwerfer, heftig |

---

## Reichweite (Range)

SoF2-Units → Unity-Meter: `rangeMeters = attackDef.Range × 0.0254`

| Waffe | Range (QU) | Range (Meter) | Typ |
|-------|-----------|---------------|-----|
| Knife | 60 | ~1.5m | Melee |
| M590 | 1100 | ~28m | Schrotflinte |
| USAS12 | 1000 | ~25m | Auto-Schrotflinte |
| M4 | 8192 | ~208m | Sturmgewehr |
| AK74 | 8192 | ~208m | Sturmgewehr |
| M60 | 8192 | ~208m | LMG |
| M1911A1 | 8192 | ~208m | Pistole |
| MSG90A1 | 16384 | ~416m | Sniper |

---

## Attack Gating (Server-seitig)

Attacks werden nur zugelassen wenn **keine** andere Action läuft:
```csharp
bool noActionRunning = m_ServerAttackFramesRemaining <= 0
                    && m_ServerReloadFramesRemaining <= 0
                    && m_ServerAltAttackFramesRemaining <= 0
                    && !m_ServerIsSwapping;
```

Attack-Cooldown basiert auf `mp_attack` Animation:
- `m_ServerAttackFrames` = `attackAnim.Duration` (Frames)
- `m_ServerAttackFps` = `attackAnim.Fps`
- Frame-diskretes Timing (wie SoF2 weaponTime)

---

## Self-Hit-Prevention

Vor dem Raycast wird der eigene Collider deaktiviert:
```csharp
m_ServerPlayerCharacter.SetPhysicsColliderEnabled(false);
// ... Raycast-Loop ...
m_ServerPlayerCharacter.SetPhysicsColliderEnabled(true);
```

---

## Debug Tracer

`[SerializeField] bool m_ShowDebugTracers` — Inspector-Toggle pro Character.

Wenn aktiv:
- Server sendet `DebugTracerClientRpc(eyePos, hitPoint)` per `Rpc(SendTo.Everyone)`
- Client findet `ejectBone` der aktuellen Waffe als visuellen Startpunkt
- `Debug.DrawLine` (Scene-View, 2s)
- `CreateTracerLine` → temporärer LineRenderer (rot→gelb, 0.02m breit, 2s Dauer)

Tracer-Startpunkt: EjectBone der Waffe (z.B. `ejection_m4`), gefunden via `FindDeepChild()`.

---

## fireDelay (noch nicht implementiert)

3 Waffen haben `fireDelay` — ein Pre-Fire-Delay bevor der Schuss auslöst:
- **MM1**: 350ms (Granatwerfer Wind-Up)
- **USAS12**: 75ms
- **MSG90A1**: 100ms

Aktuell wird dies **nicht** separat behandelt. Das Attack-Frame-System regelt nur den
Cooldown zwischen Schüssen, nicht einen Delay vor dem ersten Schuss.

---

## HitRegion-Multiplikatoren

Aus `HitboxCollider.DamageMultiplier`:

| Region | Multiplikator |
|--------|--------------|
| Head, Neck | 1.75× |
| Chest, Waist | 1.0× |
| UpperArmL/R, ForearmL/R, HandL/R | 0.7× |
| ThighL/R, CalfL/R, FootL/R | 0.7× |

Formel: `finalDamage = Round(attackDef.Damage × hitbox.DamageMultiplier)`
