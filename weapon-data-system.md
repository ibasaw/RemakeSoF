# Weapon Data System

Daten-getriebenes Waffensystem: Alle Waffen-Parameter werden aus einer zentralen JSON-Datei geladen
und über DTOs (Data Transfer Objects) im gesamten Projekt verwendet.

---

## Architektur-Überblick

```
SoF2_Weapons_New.json (Resources/Data/)
        ↓
WeaponDataLoader (Pure Service, ServiceLocator)
  ├─ LoadFromResources() → Newtonsoft.Json Deserialisierung
  ├─ Cache: m_WeaponsById (Dictionary<string, WeaponDefinition>)
  └─ Cache: m_WeaponsByAnimatorIndex (Dictionary<int, WeaponDefinition>)
        ↓
WeaponDefinition (Root-DTO)
  ├─ WeaponAttackDefinition (Primär- + Alt-Angriff)
  │   ├─ WeaponProjectileDefinition (Raketen, Granaten)
  │   └─ WeaponAmmoDefinition (Alt-Ammo, z.B. M203)
  ├─ WeaponAmmoDefinition (Haupt-Munition)
  └─ Dictionary<string, WeaponAnimationEntry> (Animationen)
```

---

## Beteiligte Dateien

| Datei | Pfad | Rolle |
|-------|------|-------|
| **SoF2_Weapons_New.json** | `Assets/Resources/Data/SoF2_Weapons_New.json` | Zentrale Waffen-Datenbank |
| **WeaponDataLoader** | `Assets/Scripts/Runtime/Management/DataManagement/WeaponDataLoader.cs` | Pure Service: JSON laden + cachen |
| **WeaponDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponDefinition.cs` | Root-DTO |
| **WeaponAttackDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponAttackDefinition.cs` | Angriffs-Parameter |
| **WeaponAmmoDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponAmmoDefinition.cs` | Munitions-Parameter |
| **WeaponAnimationEntry** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponAnimationEntry.cs` | Animations-Daten |
| **WeaponProjectileDefinition** | `Assets/Scripts/Runtime/Shared/DTOs/WeaponManagement/WeaponProjectileDefinition.cs` | Projektil-Parameter |

---

## WeaponDataLoader

Pure Service (kein MonoBehaviour), registriert im `ServiceLocator` via `ApplicationEntryPoint`.

```csharp
// Zugriff überall im Projekt:
WeaponDataLoader loader = ServiceLocator.Get<WeaponDataLoader>();
WeaponDefinition weapon = loader.GetById("m4");
WeaponDefinition weapon = loader.GetByAnimatorIndex(2);
bool exists = loader.HasWeapon("knife");
```

### Methoden

| Methode | Zweck |
|---------|-------|
| `LoadFromResources()` | Lädt JSON aus `Resources/Data/SoF2_Weapons_New`, deserialisiert, füllt beide Caches |
| `GetById(string weaponId)` | Liefert `WeaponDefinition` per ID (z.B. "m4", "knife") |
| `GetByAnimatorIndex(int index)` | Liefert `WeaponDefinition` per Animator-Parameter-Index |
| `HasWeapon(string weaponId)` | Prüft ob Waffe existiert |

---

## DTO-Struktur

### WeaponDefinition (Root)

| Feld | Typ | JSON | Beschreibung |
|------|-----|------|-------------|
| Id | `string` | `"id"` | Unique Key: "knife", "m4", "ak74", "m590", etc. |
| DisplayName | `string` | `"displayName"` | UI-Anzeigename: "Knife", "M4 Carbine", etc. |
| Category | `int` | `"category"` | 1=Melee, 2=Pistol, 5=Rifle, 7=Heavy |
| AnimatorIndex | `int` | `"animatorIndex"` | Index für Animator-Parameter "CurrentWeapon" |
| MenuImage | `string` | `"menuImage"` | HUD-Icon Pfad |
| WorldModel | `string` | `"worldModel"` | 3rd-Person Mesh (Addressable-Key) |
| ViewModel | `string` | `"viewModel"` | 1st-Person Mesh |
| IsMelee | `bool` | `"isMelee"` | Nahkampfwaffe? |
| Ammo | `WeaponAmmoDefinition` | `"ammo"` | Haupt-Munition |
| Attack | `WeaponAttackDefinition` | `"attack"` | Primärangriff |
| AltAttack | `WeaponAttackDefinition` | `"altAttack"` | Alternativangriff (optional) |
| Sounds | `Dictionary<string, object>` | `"sounds"` | Sound-Definitionen |
| Animations | `Dictionary<string, WeaponAnimationEntry>` | `"animations"` | Animationen (mp_idle, mp_attack, mp_raise, mp_drop, mp_reload, etc.) |

### WeaponAttackDefinition

| Feld | Typ | JSON | Beschreibung |
|------|-----|------|-------------|
| Damage | `int` | `"damage"` | Schaden pro Treffer (MP-Wert) |
| Range | `int` | `"range"` | Reichweite in SoF2-Units (× 0.0254 = Meter) |
| Radius | `int` | `"radius"` | Explosionsradius (Projektilwaffen) |
| FireMode | `string` | `"fireMode"` | "single", "burst", "auto" |
| FireDelay | `int` | `"fireDelay"` | Pre-Fire-Delay in ms |
| Gore | `bool` | `"gore"` | Gore-Effekte? |
| Volume | `float` | `"volume"` | Lautstärke (0.0-1.0) |
| KickAngles | `List<float>` | `"kickAngles"` | Rückstoß [minPitch, maxPitch, minYaw, maxYaw] |
| Inaccuracy | `float` | `"inaccuracy"` | Basis-Streuung (Grad) |
| MaxInaccuracy | `float` | `"maxInaccuracy"` | Max-Streuung bei Dauerfeuer |
| Pellets | `int` | `"pellets"` | Projektile pro Schuss (Schrotflinten) |
| Spread | `float` | `"spread"` | Zusätzliche Streuung pro Pellet |
| MuzzleFlash | `string` | `"muzzleFlash"` | Muzzle-Flash Effekt (→ effect-system.md) |
| MuzzleSmoke | `string` | `"muzzleSmoke"` | Muzzle-Smoke Effekt (→ effect-system.md) |
| ShellCasingEject | `string` | `"shellCasingEject"` | Shell-Casing Effekt (→ effect-system.md) |
| EjectBone | `string` | `"ejectBone"` | Bone für Shell-Ejektion (z.B. "ejection_m4") |
| TracerEffect | `string` | `"tracerEffect"` | Tracer-Effekt (→ effect-system.md) |
| FireModes | `List<string>` | `"fireModes"` | Verfügbare Feuer-Modi |
| Melee | `string` | `"melee"` | Nahkampf-Typ für Alt-Attack ("bayonet") |
| Projectile | `WeaponProjectileDefinition` | `"projectile"` | Projektil-Definition |
| Ammo | `WeaponAmmoDefinition` | `"ammo"` | Separate Alt-Munition (z.B. M203) |

### WeaponAmmoDefinition

| Feld | Typ | JSON | Beschreibung |
|------|-----|------|-------------|
| Type | `string` | `"type"` | Munitionstyp: "5.56mm", "7.62mm", "12gauge", etc. |
| MaxClip | `int` | `"maxClip"` | Magazinkapazität |
| ExtraClips | `int` | `"extraClips"` | Anzahl Extra-Magazine |
| StartClip | `int` | `"startClip"` | Initiales Magazin bei Spawn |
| StartReserve | `int` | `"startReserve"` | Initiale Reserve bei Spawn |
| Infinite | `bool` | `"infinite"` | Unendliche Munition (Knife) |

### WeaponAnimationEntry

| Feld | Typ | JSON | Beschreibung |
|------|-----|------|-------------|
| Name | `string` | `"name"` | Skeleton-Enum (z.B. "TORSO_IDLE_KNIFE") |
| Source | `string` | `"source"` | XSI-Quelldatei |
| StartFrame | `int` | `"startFrame"` | Startframe in Skeleton-Animation |
| Duration | `int` | `"duration"` | Dauer in Frames |
| Fps | `int` | `"fps"` | Frames pro Sekunde |
| Loop | `bool` | `"loop"` | Loop-Flag |
| DurationInSeconds | `float` | (berechnet) | `Duration / (float)Fps` |

### WeaponProjectileDefinition

| Feld | Typ | JSON | Beschreibung |
|------|-----|------|-------------|
| Gravity | `float` | `"gravity"` | Gravitations-Faktor (0=keine) |
| Speed | `float` | `"speed"` | Projektil-Geschwindigkeit (Units/Sek) |
| Detonation | `string` | `"detonation"` | "impact", "sticky", "timed" |
| Effect | `string` | `"effect"` | Trail-Effekt |
| ExplosionEffect | `string` | `"explosionEffect"` | Explosions-Effekt |
| UnderwaterEffect | `string` | `"underwaterEffect"` | Unterwasser-Explosion |
| WaterExplosionEffect | `string` | `"waterExplosionEffect"` | Wasseroberflächen-Explosion |
| LoopSound | `string` | `"loopSound"` | Flug-Sound |
| Model | `string` | `"model"` | Projektil-Mesh |
| ObjectType | `string` | `"objectType"` | Objekttyp (z.B. "knife") |

---

## Waffen-Datenbank (Übersicht)

| ID | DisplayName | Category | AnimatorIndex | Melee | Range (QU) |
|----|-------------|----------|---------------|-------|-----------|
| knife | Knife | 1 | 0 | true | 60 |
| rpg7 | RPG-7 | 7 | 1 | false | 75* |
| m4 | M4 Carbine | 5 | 2 | false | 8192 |
| ak74 | AK-74 | 5 | 3 | false | 8192 |
| mm1 | MM-1 | 7 | 4 | false | — |
| m590 | M590 | 5 | 5 | false | 1100 |
| usas12 | USAS-12 | 7 | 6 | false | 1000 |
| msg90a1 | MSG90A1 | 5 | 7 | false | 16384 |
| m60 | M60 | 7 | 8 | false | 8192 |
| m1911a1 | M1911A1 | 2 | 9 | false | 8192 |
| f1 | F1 Grenade | 7 | 10 | false | — |

*RPG7 Range 75 = Nahkampf-Melee-Range für Alt-Attack, Primärangriff ist Projektil.

---

## Animations-Keys

Jede Waffe hat folgende Animationen (nicht alle zwingend):

| Key | Beschreibung | Verwendung |
|-----|-------------|------------|
| `mp_idle` | Idle-Animation | Default-State |
| `mp_attack` | Primärangriff | Attack-Cooldown berechnen |
| `mp_altattack` | Alternativangriff | AltAttack-Cooldown |
| `mp_reload` | Reload (Magazin) | Standard-Reload |
| `mp_reload_start` | Shell-Reload Start | Schrotflinten: Öffnen |
| `mp_reload_shell` | Shell-Reload Loop | Pro Patrone |
| `mp_reload_end` | Shell-Reload Ende | Schrotflinten: Schließen |
| `mp_raise` | Waffe ziehen | Weapon-Swap Raise-Phase |
| `mp_drop` | Waffe weglegen | Weapon-Swap Drop-Phase |

---

## WeaponLoader (Visual Loading)

`WeaponLoader` — Interner Service (kein MonoBehaviour), instanziiert Waffen-Prefabs:

```
WeaponLoader.LoadAndAttachWeapon("m4")
  → PrefabManager.GetPrefabAsync("m4")    (Addressables, cache-first)
  → Instantiate als Child von rhang_tag_bone
  → Scale-Kompensation: localScale = prefabScale / boneLossyScale
  → Z-Rotation: -90° (SoF2 Achsen-Korrektur)
```

| Member | Beschreibung |
|--------|-------------|
| `CurrentWeaponInstance` | Aktuell instanziiertes Waffen-GameObject |
| `CurrentWeaponName` | Name der aktuellen Waffe |
| `LoadAndAttachWeapon(key)` | Lädt + attached an Hand-Bone |
| `ClearCurrentWeapon()` | Zerstört aktuelle Waffe |
| `SetAttachmentBone(Transform)` | Setzt Hand-Bone-Referenz |

Die Waffen-Hierarchie im Prefab enthält Bones wie `ejection_m4` (für Shell-Ejektion/Tracer).
