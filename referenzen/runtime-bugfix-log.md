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

## Bug #6: Granaten-Doppelwurf (Grenade Double-Throw)

**Symptom**: Nach dem Wurf einer Granate wird sofort eine zweite Granate gecockt/geworfen, obwohl der Spieler den Feuerknopf nicht erneut gedrückt hat.

**Ursache**: Server hatte kein Debounce-System für Feuerknöpfe. Nach Ablauf des `weaponTime` vom Wurf wurde der noch gehaltene Feuerknopf sofort als neuer Angriff interpretiert — es fehlte die Rising-Edge-Erkennung.

**Fix**: Komplettes SoF2 `pm_debounce`-Bitfeld-System implementiert (siehe `server-authoritative-weapon-fire.md`):
- `m_ServerDebounce` (int) mit Bitflags: `PMD_ATTACK = 0x0002`, `PMD_ALTATTACK = 0x0010`, `PMD_FIREMODE = 0x0004`
- `ProcessServerCommandLogic()`: Debounce-Check vor jedem Fire-Code — Button muss losgelassen und erneut gedrückt werden
- `ProcessAttack()` / `ProcessAltAttack()`: Setzen jeweiliges PMD-Bit nach erfolgreichem Schuss
- Burst-Mask blockiert AltAttack + Reload + Zoom + FireMode während Burst

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs`
- `Assets/Scripts/Runtime/Game/Characters/Shared/PlayerCommand.cs` (CommandButtons.FireMode)
- `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs` (m_FireModeSwitchRequested)

---

## Bug #7: Knife Alt-Attack Ammo (Messerwurf verbraucht falsche Ammo-Quelle)

**Symptom**: Nach dem ersten Messerwurf kann kein zweiter Wurf mehr ausgeführt werden, obwohl Reserve-Ammo vorhanden ist.

**Ursache**: `TryConsumeAltAmmo()` in `NetworkedCharacterState.cs` konsumierte für den Messerwurf aus `m_CurrentClipAmmo`. Das Messer hat `Ammo.Infinite = true` (unendliche Stabs), aber `ClipSize = 1` — der Wurf zog von Clip ab, was die "unendliche" Stab-Ammo auf 0 setzte.

**Fix**: Drei-Pattern-Logik in `TryConsumeAltAmmo()`:
1. **Melee** (kein Ammo-Verbrauch): `AltAttack.AmmoPerShot == 0` → return true
2. **Separater Alt-Ammo**: `weapon.HasSeparateAltAmmo` → `m_AltClipAmmo.Value--`
3. **Projektil ohne separaten Ammo**:
   - `weapon.Ammo.Infinite == true` → `m_ReserveAmmo.Value--` (Messerwurf)
   - `weapon.Ammo.Infinite == false` → `m_CurrentClipAmmo.Value--` (Granaten)

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs`

---

## Bug #8: Flashbang zu kurz (M84 Stun Grenade)

**Symptom**: M84 Flashbang blendet nur ~2.8s, was für eine Stun Grenade unrealistisch kurz ist. Kein taktischer Vorteil durch Flashen.

**Ursache**: Fixe Werte (Hold 0.3s + Fade 2.5s) ohne Intensitätsskalierung. SoF2 nutzt bis zu 11s Fade-Zeit.

**Fix**: Komplettes Rework basierend auf SoF2 `CG_FlashBang()` Werten:
- Affect-Radius: 20.32m → 44.45m (SoF2 `MAX_FLASHBANG_AFFECT_DISTANCE = 1750 QU`)
- Max-Radius: 20.32m → 76.2m (SoF2 `MAX_FLASHBANG_DISTANCE = 3000 QU`)
- Fade: 2.5s fix → 0–11s skaliert mit Intensität (SoF2 `MAX_FLASHBANG_TIME = 11000ms`)
- Hold: 0.3s fix → 0–1.5s skaliert (Hold erst ab 50% Intensität)
- Blickrichtung: Dot-Product-Lerp → Distanz-Strafe-System
- Gesamtdauer max: 2.8s → 12.5s

**Dateien**:
- `Assets/Scripts/Runtime/Game/Effects/FlashbangScreenEffect.cs`

---

## Offene Punkte (Nice-to-Have)

| Feature | Status | Beschreibung |
|---------|--------|-------------|
| Alpha Curve | Nicht implementiert | `EffectAlphaDefinition.Curve` existiert im DTO, `BuildGradient()` ignoriert es (immer linear) |
| Decal Fade-Out | Nicht implementiert | Decals verschwinden nach 30s abrupt (`Object.Destroy`), kein gradueller Fade |
| parmMax Randomization | Nicht implementiert | Alpha + Size ignorieren `parmMax` → kein per-Partikel Timing-Randomisierung |
| fireDelay | Nicht implementiert | MM1 (350ms), USAS12 (75ms), MSG90A1 (100ms) Pre-Fire-Delay |

---

## Bug #9: Dedicated Server — Spieler fällt durch die Map (MeshCollider Read/Write)

**Symptom**: Spieler fällt beim Spawn durch die Map. Server-Log zeigt:
`CollisionMeshData couldn't be created because the mesh has been marked as non-accessible.`
`This Mesh Collider [...] doesn't have Read/Write enabled.`

**Ursache**: `MapColliderApplier` setzte `cookingOptions` explizit auf
`CookForFasterSimulation | EnableMeshCleaning | WeldColocatedVertices`. Diese Werte sind
identisch mit Unity-Defaults, aber das explizite Setzen erzwingt `Read/Write Enabled` auf den Meshes.
Ohne Read/Write kann Unity die CollisionMeshData nicht bauen → keine Collider → Durchfallen.

**Fix**: `cookingOptions`-Zuweisung an allen 3 Stellen (Brush, Clip, Surface) entfernt.
Unity nutzt intern dieselben Defaults, aber ohne die Read/Write-Anforderung.

**Dateien**:
- `Assets/Scripts/Runtime/Game/MapLoader/MapColliderApplier.cs`

---

## Bug #10: Dedicated Server — Shader-Crash bei Waffen/Effekt-Loading

**Symptom**: `ArgumentNullException: Value cannot be null. Parameter name: shader` beim Schießen.
`Trying to access a shader but no shaders were included in the build because Dedicated Server Optimizations is enabled.`

**Ursache**: Mehrere `[Rpc(SendTo.Everyone)]` RPCs in `NetworkedPlayerCharacter` erzeugten
visuelle Effekte (Tracer, Projektile, Muzzle-Flash) die auf dem Dedicated Server ausgeführt
wurden. `Shader.Find()` returned null (Shader gestripped) → `new Material(null)` → Crash.

Zusätzlich: `ClientPlayerCharacter.OnNetworkSpawn()` hatte keinen Server-Guard und lud
Waffen-Modelle (WeaponLoader → Shader.Find → Material) auf dem Dedicated Server.

**Fix (4 Stellen)**:
1. `TracerClientRpc` — Server-Guard: `if (IsServer && !IsHost) return;`
2. `ProjectileSpawnClientRpc` — Server-Guard: `if (IsServer && !IsHost) return;`
3. `MuzzleEffectsClientRpc` — Server-Guard: `if (IsServer && !IsHost) return;`
4. `ClientPlayerCharacter.OnNetworkSpawn()` — Early-Return für Dedicated Server

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedPlayerCharacter.cs`
- `Assets/Scripts/Runtime/Game/Characters/Client/ClientPlayerCharacter.cs`

---

## Bug #11: Bot zählt nicht als Spieler (Runde startet nicht)

**Symptom**: Mit `sv_minclients=1` und 1 Bot startet die Runde nicht, weil `MinPlayersReached` false bleibt.

**Ursache**: `playersConnected.Value` und `MinPlayersReached` zählten nur `ConnectedClientsIds` (menschliche Spieler). Bots sind server-owned NetworkObjects ohne ClientId.

**Fix**: 
- `GetTotalPlayerCount()` zählt `ConnectedClientsIds.Count + AIBotSpawner.SpawnedBotCount`
- `UpdatePlayerCounts()` setzt `playersConnected.Value` und `MinPlayersReached` mit Bot-Counts
- Alle Event-Handler (`OnClientConnected`, `OnClientDisconnected`, `OnBotsSpawned`) nutzen `UpdatePlayerCounts()`

**Dateien**:
- `Assets/Scripts/Runtime/Game/Networked/RoundFlowStateMachine.cs`

---

## Bug #12: Bot im falschen Team (gleiche Team wie Spieler)

**Symptom**: Spieler und Bot werden ins gleiche Team eingeteilt. HideAndSeek braucht mindestens 1 pro Team.

**Ursache**: `CountTeams()` in `ServerListeningState.cs` iterierte nur `ConnectedClientsIds`, nicht `AIBotSpawner.SpawnedBots`.

**Fix**: `CountTeams()` erweitert um `AIBotSpawner.SpawnedBots` Iteration via `NetworkedGameState.Singleton?.AIBotSpawner`.

**Dateien**:
- `Assets/Scripts/Runtime/Management/ConnectionManagement/ConnectionStates/ServerListeningState.cs`

---

## Bug #13: Bot schwebt in der Luft (keine Physik)

**Symptom**: Bot spawnt und schwebt an der Spawn-Position, fällt nicht auf den Boden.

**Ursache**: `ServerAICharacter` hatte keine Physik-Simulation. Nur ein statischer Transform ohne Gravity.

**Fix**: `PlayerPhysicsSimulation` (dieselbe wie für Spieler) zu `ServerAICharacter` hinzugefügt. Simuliert jeden Frame mit leerem `PlayerCommand` (MoveInput=zero → Bot steht still, fällt aber mit Gravity).

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Server/ServerAICharacter.cs`

---

## Bug #14: Server-Player-Collider hat falsche Dimensionen

**Symptom**: Server-seitige Capsule-Dimensionen (1.8m/0.25m) weichen von Client-seitigen SoF2-Werten (2.2606m/0.381m) ab. Inkonsistente Kollision zwischen Server und Client.

**Ursache**: `ServerPlayerCharacter` hatte eigene Defaults (`k_DefaultCapsuleHeight = 1.8f`, `k_DefaultCapsuleRadius = 0.25f`) die nicht mit `ClientColliderSystem` SoF2-Werten übereinstimmten.

**Fix (2-stufig)**:
1. Konstanten in `ClientColliderSystem` von `private` auf `internal` geändert
2. `ServerPlayerCharacter` Defaults entfernt, referenziert jetzt direkt `ClientColliderSystem.k_SoF2StandingHeight` und `ClientColliderSystem.k_SoF2Radius` → **Single Source of Truth**

**Dateien**:
- `Assets/Scripts/Runtime/Game/Characters/Client/ClientColliderSystem.cs`
- `Assets/Scripts/Runtime/Game/Characters/Server/ServerPlayerCharacter.cs`

---

## Bug #15: Map-Wechsel statt nächste Runde (Race Condition)

**Symptom**: Nach Rundenende wechselt die Map sofort statt die nächste Runde zu starten, obwohl `hideandseek_roundlimit=5` und erst 1 Runde gespielt.

**Ursache**: Race Condition zwischen zwei Timern in `RoundFlowRunningState`:
1. `OnRunFrame` → `OnPhaseTimeExpired()` setzt `CurrentPhase = RoundOver` (Frame-genau)
2. `RunCountdown` → `OnTimeExpired()` sieht `RoundOver` → gab `GametypeEventResult.None` zurück
3. `result.RestartRound == false` → Fallthrough zu `SwitchingMapState`

**Fix**: `OnTimeExpired()` gibt jetzt immer `RestartRound = true` zurück, auch wenn Phase bereits `RoundOver`.

**Dateien**:
- `Assets/Scripts/Runtime/Management/GametypeManagement/HideAndSeekGametype.cs`

---

## Bug #16: Hider-Überlebens-Score nicht vergeben (Race Condition)

**Symptom**: Wenn Hider die Runde überleben, bekommen sie keinen +1 Kill und das Hider-Team keinen +1 TeamScore.

**Ursache**: Gleiche Race Condition wie Bug #15. `OnPhaseTimeExpired()` (Seeking) setzte nur `CurrentPhase = RoundOver` ohne Scoring. `OnTimeExpired()` sah `RoundOver` → kein Score.

**Fix**: `m_HidersSurvived` Flag:
- `OnPhaseTimeExpired()` (Seeking) setzt `m_HidersSurvived = true`
- `OnTimeExpired()` prüft Flag:
  - `m_HidersSurvived = true` → Red +1 TeamScore + AwardSurvivalKills
  - `m_HidersSurvived = false` (Eliminierung) → nur Restart (Score bereits bei OnClientDeath)

**Dateien**:
- `Assets/Scripts/Runtime/Management/GametypeManagement/HideAndSeekGametype.cs`

---

## Bug #17: Bot duckt sich beim Verfolgen (Chase-Crouch)

**Symptom**: Beim Verfolgen eines Spielers spiegelte der Bot dessen Crouch-State und wurde dadurch ~75% langsamer (`PmDuckScale = 0.25`). Bhop-Beschleunigung brach ab.

**Ursache**: Zwei Code-Pfade in `AIBotController` setzten `m_ShouldCrouch = true` während eines aktiven Chase:
1. **Stuck-Replay**: `if (m_LastMirrorCrouch) m_ShouldCrouch = true;` spiegelte das gecachte Spieler-Crouch beim Stuck-Recovery — auch während Chase.
2. **Phase-3 Reverse-Crouch**: Der 4-Phasen-Stuck-Recovery zwingt 1 s Crouch beim Rückwärts-Drehen — wurde aber auch während Chase ausgelöst.

Beides widersprach dem expliziten Kommentar in `MirrorNearestPlayerActions()` ("Ducken wird absichtlich NICHT gespiegelt").

**Fix**:
- Stuck-Replay-Crouch komplett entfernt (nur Jump wird gespiegelt).
- Phase-3 Reverse-Crouch in `if (!PlayerSensorDetected)` gewrappt — kein Slowdown wenn Bot den Spieler aktiv sieht.

**Dateien**:
- `Assets/Scripts/Runtime/AI/AIBotController.cs`

---

## Bug #18: Seeker-Bot Waffen-Ping-Pong (Knife ↔ Ranged Loop)

**Symptom**: Bot wechselt im Kampf korrekt von leerer Ranged-Waffe (clip=0, reserve=0) auf Knife — aber sofort wieder zurück auf Ranged → wieder auf Knife → endlos. Sieht aus wie Stottern.

**Ursache**: `EvaluateWeaponState` prüfte den Ammo-Zustand **erst nach dem Swap-Commit**, nicht vor der Wechsel-Entscheidung:
1. Bot hält Knife, sieht Spieler → "Melee im Kampf, cycle zu Ranged"
2. `ServerCycleWeapon(1)` swappt blind zur nächsten Waffe (M4 mit 0/0).
3. Nach Commit: nächster Eval sieht "M4 trocken" → cycle weiter → wrap-around zurück zu Knife.
4. Loop, weil die Wechsel-Entscheidung nie wusste, dass alle Ranged-Waffen leer sind.

**Fix**: Informierte Waffenwahl statt blindes Cyclen.
- Neue Helper auf `NetworkedCharacterState`:
  - `TryGetAmmoFor(weaponName, out clip, out reserve)` — liefert Live-Ammo für aktive Waffe + Cache-Werte für andere Inventar-Waffen.
  - `ServerSelectWeapon(weaponName)` — direkter Wechsel zu einer Ziel-Waffe (mit Inventar-Validation), statt rotierendem Cycle.
- `EvaluateWeaponState` neu:
  1. Inventar einmal scannen → erste Ranged-mit-Ammo + erste Melee-Fallback ermitteln.
  2. Aktuelle Waffe trocken? → bestes Alternativ-Target wählen (Ranged bevorzugt).
  3. Aktuelle Waffe Melee + Kampf + Ranged-mit-Ammo verfügbar? → Ranged.
  4. Sonst: bleiben.
- Wenn keine Ranged-Waffe Munition hat, bleibt der Bot auf dem Knife — kein Ping-Pong mehr.

**Dateien**:
- `Assets/Scripts/Runtime/AI/AIBotController.cs` (`EvaluateWeaponState`)
- `Assets/Scripts/Runtime/Game/Characters/Networked/NetworkedCharacterState.cs` (`TryGetAmmoFor`, `ServerSelectWeapon`)
