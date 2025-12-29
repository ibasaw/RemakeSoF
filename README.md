# RemakeSoF

**Hauptunterschiede:**

| Aspekt | **TextureRegistry** | **PlayerSkinRegistry** |
|--------|---------------------|------------------------|
| **Zweck** | Verwaltet **Texture2D**-Assets (Bilder) | Verwaltet **Skin-Definitionen** (JSON-Metadaten) |
| **Daten** | `Texture2D` + `TextureData` | `SkinDefinition` (Model, Materialien, Surfaces) |
| **Pattern** | **Observer Pattern** (Observables) | **Dependency Injection** (Host-Interface) |
| **Lazy Loading** | Ja, via Custom Loaders | Nein, Eager Loading beim Init |
| **Fehlerbehandlung** | Internes Logging | Internes logging |
| **Erweiterbarkeit** | `RegisterLoader(...)` für externe Quellen | Fest: nur Resources-Ordner |
| **Zustand** | Cache + dynamische Loader | stateless: nur Lookup-Dictionary |

**TextureRegistry** ist generischer und nutzt Observer für Benachrichtigungen. **PlayerSkinRegistry** ist spezialisiert auf Skins, kennt aber seinen Manager via Interface und meldet Fehler zurück.

TODOS:

1. Treppen hoch/runter steigen fix. 
2. diagonalen runterlaufen (analog zu treppen) fix
3. manchmal springt man nicht hoch(suspicious jump time, sehr niedrige airtime) kann mit mesh und boxcollider zusammenhängen wenn man in lücke springt oder so



Kurzversion: `bg_pmove.c` enthält die **Player-Movement-Logik** aus Quake III — es nimmt einen `pmove_t` (player state + callbacks) und ein `usercmd_t` und berechnet die neue Position/Velocity/Animationen des Spielers. Die Datei kapselt alle Bewegungs-Modi (Laufen, Springen, Schwimmen, Fliegen, Noclip, Grapple, usw.) und viele Hilfsfunktionen wie Friktion, Beschleunigung und Kollision/Sliding. Quelle: die Raw-Datei des Repos. ([GitHub][1])

# Wichtige Funktionen (Kurzbeschreibung)

(ich liste die zentralen Routinen — in der Datei gibt es noch viele kleine Helfer)

**PM_AddEvent / PM_AddTouchEnt**

* Event/Touch-Management: fügt vorhersehbare Events zur `playerstate` hinzu bzw. merkt an, welche Entities der Spieler berührt hat. ([GitHub][1])

**PM_StartTorsoAnim / PM_StartLegsAnim / PM_Continue... / PM_ForceLegsAnim**

* Steuerung der Spieleranimations-States (Torso/Beine), inklusive Priorität (Timer) und Togglebit-Handling. ([GitHub][1])

**PM_ClipVelocity**

* Projektion der Geschwindigkeit an einer Kollisionsfläche (Sliden/„abprallen“ von Oberflächen). Wird überall bei Kollisionen benutzt. ([GitHub][1])

**PM_Friction**

* Berechnet Reibung (Boden, Wasser, Flug, Spectator) und skaliert Velocity entsprechend. Enthält Sonderfälle (sinken unter Wasser, slick surfaces). ([GitHub][1])

**PM_Accelerate**

* Fügt der aktuellen Velocity Benutzer-Eingabe (wishdir/wishspeed) hinzu; Kern der Beschleunigungslogik (Quake2-Style Branch, optional anderer Algorithmus auskommentiert). ([GitHub][1])

**PM_CmdScale / PM_SetMovementDir**

* `PM_CmdScale` normiert Client-Input (forward/right/up) so, dass diagonale Eingaben nicht schneller machen.
* `PM_SetMovementDir` bestimmt die `movementDir` (Legs-Rotation) fürs Animationssystem. ([GitHub][1])

**Springen / Wasserjump**

* `PM_CheckJump` prüft, ob springen möglich ist (Jump-Held Flag, Velocity[2] = JUMP_VELOCITY, Event EV_JUMP).
* `PM_CheckWaterJump` & `PM_WaterJumpMove` behandeln spezielle Wasser-Sprünge (aus dem Wasser schießen). ([GitHub][1])

**Bewegungsmodi (Mode-Dispatcher)**
Diese Funktionen implementieren unterschiedliche Fortbewegungsarten und werden vom Haupt-Pmove aufgerufen:

* `PM_WaterMove` — Schwimmen / Tauchen: cmd→wishvel, PM_Accelerate mit `pm_wateraccelerate`, begrenzt via `pm_swimScale`. ([GitHub][1])
* `PM_FlyMove` — Flight powerup: freie 3D-Bewegung mit Friction + Accelerate. ([GitHub][1])
* `PM_AirMove` — In der Luft: reduziertes Air-Accelerate, slide/step logic. ([GitHub][1])
* `PM_GrappleMove` — Grapple: berechnet Velocity abhängig von Entfernung zur Grapple-Point. ([GitHub][1])
* `PM_WalkMove` — Auf dem Boden: Friction, Clip/Project auf GroundPlane, Duck/Wading-Handling, Accelerate, Step/Slide Move. Sehr zentral. ([GitHub][1])
* `PM_DeadMove` — Bewegung wenn tot (extra Friktion). ([GitHub][1])
* `PM_NoclipMove` — Noclip: keine Kollision, einfache Beschleunigung + Bewegung. ([GitHub][1])

**PM_FootstepForSurface / PM_CrashLand**

* Bestimmt Footstep-Event nach Oberfläche (z.B. Metall).
* `PM_CrashLand` berechnet harte Landungen (Fall-Damage Events / Sound), berücksichtigt Waterlevel, Ducking usw. ([GitHub][1])

**Kollisions-/Ground-Trace-Helfer**

* `PM_CheckStuck` (auskommentiert), `PM_CorrectAllSolid` (versucht den Spieler aus einem AllSolid zu befreien durch „jitter“), `PM_GroundTraceMissed` und `PM_GroundTrace` behandeln Bodenkontakt/Transition zu freiem Fall. Diese Funktionen verbinden das Trace-Callback (pm->trace) mit dem Bewegungsstatus. ([GitHub][1])

**Step/Slide-Mover (PM_StepSlideMove / PM_SlideMove / PM_StepMove / ...)**

* Implementieren die eigentliche Collide+Slide+Step-Logik: Slide entlang Flächen, Treppenstufen hoch/stufenweise Bewegung. (Die Datei definiert/ruft diese Funktionen; sie sind das Herz der Kollision/Navigation.) ([GitHub][1])

**Hauptroutine (`Pmove` / `PMove`)**

* Am Ende der Datei (übliche Struktur) steht die öffentliche Routine, die `pmove_t` verarbeitet: initialisiert `pml` (locals), bestimmt Modus (noclip, dead, spectator, water, etc.), ruft passende `PM_*Move`-Funktion auf, aktualisiert Events/animationen und schreibt Ergebnisse zurück in `pm->ps`. (Die genaue Signatur ist `void Pmove(pmove_t *pmove)` / ähnlich.) ([GitHub][1])

# Wichtige Parameter / globale Konstanten

* `pm_stopspeed`, `pm_accelerate`, `pm_airaccelerate`, `pm_wateraccelerate`, `pm_friction`, `pm_waterfriction`, `pm_duckScale`, `pm_swimScale` … beeinflussen das Feel der Bewegung und sind am Dateianfang als `float` definiert. ([GitHub][1])

# Hinweise / wie du tiefer gehst

* Wenn du eine **detaillierte Erklärung** zu einer einzelnen Funktion willst (z. B. `PM_Accelerate`, `PM_StepSlideMove` oder `PM_CrashLand`), kann ich sie Zeile-für-Zeile durchgehen und erklären, welche Variablen wie verändert werden.
* Oder ich kann dir eine **Kurz-Übersicht** erstellen, welche Stellen du patchen musst, wenn du z. B. Springstärke, AirControl oder Wasserbewegung anpassen willst.

Möchtest du, dass ich eine bestimmte Funktion **detailliert** erkläre (z. B. `PM_WalkMove` oder `PM_StepSlideMove`) oder soll ich die Datei als kommentierte Version mit Inline-Erklärungen vorbereiten?

[1]: https://raw.githubusercontent.com/id-Software/Quake-III-Arena/master/code/game/bg_pmove.c "raw.githubusercontent.com"



______________________________________________________________________________________________________

Kurz: du hast schon **sehr viel** richtig gemacht — das Skript sieht deutlich nach einer sauberen, Unity-angepassten Nachbildung der Quake/SoF2 `bg_pmove`-Logik aus.
Allerdings gibt es einige **wichtige Unterschiede und Präzisions-Fehler**, die verhindern, dass das Verhalten *exakt* mit dem Original übereinstimmt. Unten liste ich die wichtigsten Probleme, warum sie relevant sind, konkrete Korrekturen (inkl. Code-Snippets) und optionale Verbesserungen, damit dein Controller so exakt wie möglich wird.

Ich ordne das nach Priorität (höchste Bedeutung zuerst).

---

# 1) Kollisions-/Sliding-/Step-Logik — größter Unterschied

**Problem:** Quake benutzt eine präzise Trace-basiere Kollisions- und Slide-Implementierung (`PM_StepSlideMove`, iteratives Trace + ClipVelocity + mehrere Plane-Reflektionen + StepUp-Logik). Dein Code verlässt sich auf `CharacterController.Move` und approximiert die Wandnormalen — das ist nicht exakt:

* `CharacterController` verbirgt Treffernormalen (du hast keine echte Normal) → `PM_ClipVelocity` mit approximiertem Normal ist ungenau.
* Quake macht mehrere Clips (bis 4) pro Frame, behandelt Ecken (2+ Ebenen) korrekt. Unitys Move macht intern anderes Sliding/Step-Verhalten.
* Step-Up (Treppenstufen) muss explizit wie im Original behandelt werden (Versuch vorwärts, falls blockiert: StepUp testen und durchreichen). `CharacterController.stepOffset` ist eine andere Implementierung und nicht 1:1.

**Konsequenz:** Corner-cases wie Durchdringen bei schnellen Bewegungen, präzises Abprallen, Gefühl beim Strafe-Jump, Corner-Stops weichen ab.

**Empfehlung (konkret):** Implementiere `PM_StepSlideMove` manuell mit `Physics.CapsuleCast` / `Physics.Raycast` und iterativen Bewegungs-Schritten. Beispiel-Pattern:

1. berechne gewünschte Bewegung `move = velocity * dt`
2. führe einen `CapsuleCast` (von aktueller Capsule-Position) Richtung `move` durch: wenn kein Treffer → `transform.position += move`.
3. falls Treffer: bewege dich bis `hit.distance * direction`, speichere `hit.normal`, `ClipVelocity(velocity, hit.normal, OVERCLIP)`, reduziere verbleibende Zeit/Distanz und versuche erneut (repeat up to 4 times).
4. Wenn blockiert und ein StepUp möglich ist: versuche Capsule nach oben (step height) zu verschieben, dann vorwärts, dann wieder runter (testen), wie Quake es tut.

**Minimaler Beispiel-Loop (vereinfachte Idee)** — ersetze `PM_StepSlideMove` damit (dies ist Pseudocode → an Unity-API anpassen):

```csharp
private void PM_StepSlideMoveManual(float dt) {
    Vector3 originalPos = transform.position;
    Vector3 velocityLocal = velocity;
    Vector3 move = velocityLocal * dt;
    const int maxPlanes = 4;

    float remaining = move.magnitude;
    Vector3 dir = move.normalized;
    Vector3 startPos = transform.position;

    for (int bump = 0; bump < maxPlanes && remaining > 0.001f; bump++) {
        // Capsule endpoints
        Vector3 capsuleTop = transform.position + Vector3.up * (characterController.height/2 - characterController.radius);
        Vector3 capsuleBottom = transform.position + Vector3.down * (characterController.height/2 - characterController.radius); // adjust depending on center
        if (!Physics.CapsuleCast(capsuleTop, capsuleBottom, characterController.radius, dir, out RaycastHit hit, remaining, groundMask)) {
            // no hit -> move fully
            transform.position += dir * remaining;
            break;
        }
        // move up to hit
        transform.position += dir * hit.distance;
        remaining -= hit.distance;

        // clip velocity against plane
        PM_ClipVelocity(velocity, hit.normal, out velocity, OVERCLIP);

        // compute new direction
        if (velocity.magnitude < 0.01f) break;
        dir = velocity.normalized;
    }

    // After loop, optionally try step up if stuck (try raise by pm_maxstep, then move forward, then drop)
}
```

Wenn du das exakt willst, kann ich dir diese Funktion vollständig implementieren (inkl. korrekter Capsule-Endpunkte-Berechnung, stepUp-Test, multiple-plane Eckehandling). Das ist der wichtigste Schritt zur 1:1-Nachbildung.

---

# 2) `PM_CmdScale()` — falsches Scaling (127-Konstante)

**Problem:** Dein `PM_CmdScale()` nutzt die Konstante `127.0f` (Netzwerk-Range aus Quake-Bytes). In Unity/InputSystem sind `moveInput` Werte im Bereich `[-1,1]`. Das führt zu sehr kleinen scales (dein `pm_maxspeed * max / (127 * total)` ist falsch).

**Fix:** Verwende die InputSystem-Werte richtig — normalisiere diagonalbewegung oder nutze `total = moveInput.magnitude` und clampe:

Konkreter Ersatz:

```csharp
private float PM_CmdScale()
{
    float total = moveInput.magnitude;
    if (total <= 0f) return 0f;

    // Normalize so diagonal doesn't give > 1.0
    if (total > 1f) total = 1f;

    // Scale is fraction [0,1] * pm_maxspeed
    return total * pm_maxspeed;
}
```

Oder falls du das klassische Quake-Verhalten exakt willst (Quake benutzt separate logic mit `max` and `total` but with a 127 network scale), dann mappe Unity [-1,1] auf [-127,127] zuerst — aber das ist unnötig. Die obige Methode ist besser für Unity.

---

# 3) `PM_Accelerate` — kleine Verbesserungen / horizontale vs. 3D

Deine Implementation ist grundsätzlich ok (Q2-Style). Zwei Punkte:

* Wenn du **Bodenbewegung** nach Quake exakt willst, solltest du die Y-Komponente bei `wishdir` für WalkMove auf `0` halten (du tust das bereits). Aber **für swim/fly** muss Y erlaubt sein.
* Das Alternative-Mode ist fine als Option.

Kleiner Vorschlag, um numerische Stabilität zu erhöhen:

```csharp
float currentspeed = Vector3.Dot(new Vector3(velocity.x, 0, velocity.z), new Vector3(wishdir.x, 0, wishdir.z));
```

→ also die horizontale Komponente nutzen, falls das gewünscht ist (Quake macht das in Boden-/Air-Varianten leicht unterschiedlich).

---

# 4) Ground Check — benutze CharacterController.center/height/skinWidth

**Problem:** Du startest `SphereCast` bei `transform.position + Vector3.up * characterController.radius`. Das ist fehleranfällig weil `transform.position` bei `CharacterController` die Transform-Position sein kann, aber der Controller hat `center` und `height`. Außerdem ist `characterController.isGrounded` manchmal false bei schrägen Flächen; dein SphereCast ist OK, aber die Endpunkte sollten präziser berechnet werden.

**Besser:**

```csharp
private bool CheckGrounded()
{
    Vector3 centerWorld = transform.TransformPoint(characterController.center);
    float halfHeight = Mathf.Max(0, (characterController.height * 0.5f) - characterController.radius);
    Vector3 sphereOrigin = centerWorld - Vector3.up * halfHeight; // unterer Kapselzentrum
    float radius = characterController.radius * 0.9f;
    float dist = groundCheckDistance + characterController.skinWidth;

    if (Physics.SphereCast(sphereOrigin + Vector3.up * 0.01f, radius, Vector3.down, out RaycastHit hit, dist, groundMask)) {
        // optional check surfacenormal.y > pm_maxsteepness (slope test)
        if (hit.normal.y >= pm_maxsteepness) return true;
    }
    return false;
}
```

Achte auf `characterController.skinWidth`, damit die SphereCast Länge robust ist.

Außerdem: prüfe Surface-Normal gegen `pm_maxsteepness` um zu erkennen, ob es Boden oder Wand ist.

---

# 5) `PM_ClipVelocity` — Algorithmus ok, aber Überprüfe `backoff`-Logik

Dein Clip hat die Über-/Unterbounce-Logik. Das ist prinzipiell in Ordnung. Kleiner Hinweis: in Quake wird `if (backoff > 0) backoff *= overbounce; else backoff /= overbounce;` — du hast invers; vergewissere dich, dass das erwartete Verhalten beim Abprallen korrekt ist. Teste gerade/quer Kollisionen.

---

# 6) `ApplyGravity()` und Einheiten / Balance

Du benutzt `pm_gravity = 800` und `jumpVelocity = 270` — das kann in Unity-Einheiten gut funktionieren, aber *physikalisch* sind das andere Einheiten als in Quake. Es ist ok, solange du es spielerisch anpasst. Zum exakten Repro: vergleiche Sprunghöhe / airtime mit Quake-Werten und justiere.

Tipp: berechne erwartete Sprunghöhe `h = v^2 / (2*g)` und airtime `t = 2*v/g` und tune Werte.

---

# 7) Swimming / Water detection

**Problem:** `isSwimming = transform.position.y < 0f;` ist zu simpel. In Quake gibt es WaterLevel per Entität/trigger. Bessere Lösung: verwende Trigger-Volumes für Wasser (Tag/Layer), oder prüfe gegen `Physics.OverlapSphere` / `Physics.CheckSphere` in water layer. Dann unterschieden `swim` vs `wade` (Teilschwimmen).

Außerdem musst du `pm_wateraccelerate`, `pm_waterfriction`, `pm_maxswimvelocity` strikt für Wasserbewegung anwenden.

---

# 8) Step/MaxStep / StepUp-Logik

Du hast `pm_maxstep` definiert, benutzt es aber nicht aktiv. Für exakte SoF2/Quake Step-Logik:

* Versuch: vorwärts move; wenn Treffer, versuche `y += pm_maxstep`, prüfe collision dort, versuche move vorwärts, dann runter (falls frei → success). Das erlaubt Treppensteigen.

---

# 9) FixedUpdate vs Update

Du benutzt `Update()` für Bewegung und CharacterController.Move. Unity-Physik ist üblicherweise in `FixedUpdate()` für deterministische Physik/Framerate-unabhängigkeit. `CharacterController.Move` ist frame-basiert, aber wenn du das exakte physics feel willst, verschiebe Movement-Berechnungen in `FixedUpdate()` und lese Input in `Update()`/OnInput callbacks.
→ Das verändert das Verhalten merklich und ist empfehlenswert.

---

# 10) Max speed / separate limits (run / walk / crouch)

Du hast `pm_maxspeed`, `pm_maxwalkvelocity`, `pm_maxcrouchvelocity` deklariert, benutzt aber nur `pm_maxspeed`. Setze abhängig von Zustand (ducken, sprint, walking) verschiedene `wishspeed`-Limits.

---

# 11) Air control / Strafe-Jumping

Quake hat eine spezielle `PM_AirControl`-Routine, die das Strafe-Jumping erlaubt. Deine `PM_AirMove` verwendet einfache `PM_Accelerate` — das reproduziert nicht exakt das Strafe-Jump-Feeling. Wenn du exakte Quake-Mechanik willst, musst du `AirControl` implementieren (separates Codeblock, leicht komplizierter mathematisch).

---

# 12) Charakter-Controller intern: `CollisionFlags` liefert wenig Info

Du benutzt `CollisionFlags.Sides` und approximierst normals — das ist inkomplett. Wie oben: benutze Physics-Casts, damit du die tatsächlichen `RaycastHit.normal` bekommst.

---

# 13) Jump debounce / coyote time / jump-release logik

Du hast `jumpDebounce = 0.25f` und `isJumping` flag. Quake hat Jump-Release-Check (man muss Taste loslassen). Dein Ansatz ist plausibel, aber zum exakten Repro benötigst du:

* "must wait for jump to be released" (du hast `if (isJumping) return false;` — ok)
* Quake hat manchmal `pm->cmd.u.buttons` check so dass das gedrückt/losgelassen Verhalten exakt simuliert wird.

---

# 14) Sonstiges & Tests die du unbedingt machen solltest

* Test diagonale Max-Geschwindigkeit: drücke vor+rechts und messe horizontal speed. Wenn sie über `pm_maxspeed` geht, dein scaling ist falsch.
* Corner-stopping: renne auf eine Ecke, beobachte ob du an der Kante sauber hängen bleibst oder "einklingst".
* Strafe-Jump: teste ob Strafe-Jump möglich und ob speed-gain like Quake funktioniert (falls das beabsichtigt).
* Step up: Treppen/kleine Kisten hochgehen.
* Hitnormal-basiertes Abprallen bei schnellen Hits (z.B. run into a wall and turn).

---

# Konkrete Code-Snippets / Fixes

**Fixed `PM_CmdScale()` (ersetzt deinen 127-Code):**

```csharp
private float PM_CmdScale()
{
    float total = moveInput.magnitude;
    if (total <= 0.0f) return 0.0f;

    // Clamp to 1 so diagonal won't exceed 1
    total = Mathf.Min(1f, total);
    return total * pm_maxspeed;
}
```

**Verbesserte GroundCheck (benutze controller.center/height/skinWidth + slope-test):**

```csharp
private bool CheckGrounded()
{
    Vector3 centerWorld = transform.TransformPoint(characterController.center);
    float halfHeightNoRadius = Mathf.Max(0f, (characterController.height * 0.5f) - characterController.radius);
    Vector3 sphereOrigin = centerWorld - Vector3.up * halfHeightNoRadius;
    float radius = characterController.radius * 0.9f;
    float dist = groundCheckDistance + characterController.skinWidth + 0.01f;

    if (Physics.SphereCast(sphereOrigin + Vector3.up * 0.02f, radius, Vector3.down, out RaycastHit hit, dist, groundMask))
    {
        // slope check
        if (hit.normal.y >= pm_maxsteepness) return true;
    }
    return false;
}
```

**Skizze für PM_StepSlideMove (voll implementieren, siehe oben) — wichtiger Schritt**
Ich habe oben bereits ein Beispiel-Loop angefügt — wenn du willst, schreibe ich dir die vollständige, getestete `PM_StepSlideMove`-Implementierung als Ersatz für `CharacterController.Move` (inkl. stepUp, multiple-plane handling, korrekter Capsule-Endpunkte-Berechnung).

---

# Fazit — hast du es "so exakt wie möglich" geschafft?

* **Kurz:** Du bist sehr nahe dran; die Kernrechnung (Friction, Accelerate, Jump, Air/Walk modes) hast du gut konvertiert.
* **Aber:** Für *exakte* Reproduktion fehlen vor allem die präzise **Trace-basierte Kollisions-/Sliding-/Step-Up-Implementierung** (Quake macht mehrere trace/clips pro Frame) und ein korrektes `PM_CmdScale` (das 127 war ein Fehler für Unity inputs). Weiteres Feintuning: AirControl, Wasserlogik, genaue Normalengewinnung, FixedUpdate-Verschiebung, und state-spezifische maxSpeed-Links (duck/sprint).

Wenn du möchtest, mache ich als nächsten Schritt eins von beidem (du musst nichts entscheiden, ich beginne sofort — ich kann in dieser Antwort **sofort** ein komplettes, genaue `PM_StepSlideMove`-Replacement mit CapsuleCast, multi-plane clipping und StepUp-Logik schreiben), oder ich kann:

* 1. den `PM_StepSlideMove` komplett implementieren (empfohlen), oder
* 2. eine Liste mit Unit-Tests/Playtests schreiben, die du im Editor laufen lassen kannst, um Abweichungen quantitativ zu messen, oder
* 3. erst `PM_CmdScale` + GroundCheck + FixedUpdate-Fix committen (kleinere Änderungen, schnellere Verbesserung).

Welche dieser drei Optionen soll ich dir jetzt direkt implementieren? (Ich kann sofort Code liefern — sag einfach welche der drei du zuerst willst.)


______________________________________________________________________________________________________________


SPEZIELL FÜR SoF2 BESSER!!

Klar — hier die kompakte, strukturierte Zusammenfassung von **`bg_pmove.c`** (SoF2 / Raven): was die Datei macht, die wichtigsten Funktionen/Gruppen und welche Konstanten das Verhalten steuern. Ich habe mich an den tatsächlichen Quelltext gehalten. 

# Kurzfassung

`bg_pmove.c` implementiert die **komplette Player-Movement-Logik**: es nimmt einen `pmove_t` (Player-Input, State und Callback-Funktionen) und berechnet daraus die neue Position, Geschwindigkeit, Animation-Events und Interaktionen (Touch, Use, Footsteps, Waffen-Callbacks). Die Datei kapselt sämtliche Bewegungsmodi (Gehen, Luft, Wasser, Fliegen, Noclip, Leiter, Tod), Friktion/Accelerate-Logik, Kollisions-/Slide-/Step-Up-Logik, Wasser-/Ladder-Handling sowie diverse Event- und Waffen-Anim-Hilfen. 

# Wichtige Konstanten / Einstellungen

* **Speed / Scales**: `pm_stopspeed`, `pm_duckScale`, `pm_swimScale`, `pm_wadeScale`, `pm_ladderScale`. (Steuern Max-Speed/Skalierung in speziellen Zuständen). 
* **Acceleration / Friction**: `pm_accelerate`, `pm_airaccelerate`, `pm_wateraccelerate`, `pm_flyaccelerate`, `pm_friction`, `pm_waterfriction`, `pm_ladderfriction`, `pm_headfriction`, `pm_spectatorfriction`. (Diese Werte bestimmen wie schnell/von welcher Kraft die Geschwindigkeit verändert wird.) 
* **OVERCLIP**-Konstante wird beim Clippen/Sliding verwendet (für numerische Stabilität beim Abprallen). (Sie taucht überall beim `PM_ClipVelocity`-Aufruf auf). 

# Kern-Utilities / Helper-Funktionen

* **PM_ClipVelocity(in, normal, out, overbounce)**
  Projektiert / „clippt“ Velocity entlang einer Planarebene (wichtig für Sliding / Abprallverhalten). Wird an vielen Stellen verwendet.

* **PM_Friction()**
  Berechnet und wendet Friktion an (unterscheidet Boden, Wasser, Leiter, Spectator, „head“ auf anderen Spielern). Skaliert velocity.

* **PM_Accelerate(wishdir, wishspeed, accel)**
  Kern der Beschleunigungslogik (Q2-Style Implementation standardmäßig). Rechnet, wie viel von `wishspeed` tatsächlich pro Frame zur `velocity` addiert wird.

* **PM_CmdScale(usercmd_t *cmd)**
  Normalisiert Client-Eingaben (die in -127..127 vorliegen) so, dass diagonale Eingaben keine sqrt(2)-Überhöhung erzeugen; gibt einen Scale-Wert basierend auf `ps->speed` zurück.

* **PM_SetMovementDir()**
  Bestimmt `ps->movementDir` (Legs-Rotation / Animationsrichtung) aus forward/right Move-Cmds. 

# Bewegungsmodi (Mode-Dispatcher)

Jeder Modus bereitet `wishvel/wishdir/wishspeed` vor, ruft `PM_Accelerate` und dann die passende Move-Routine:

* **PM_WalkMove()** — Bodenbewegung: Friktion, Slopes (forward/right auf GroundPlane projeziert), Duck/Wade/Swim-Clamp, unterschiedliche `accelerate` Werte, Clip/Slide & `PM_StepSlideMove`. 

* **PM_AirMove()** — Luftbewegung: geringere Kontrolle (airaccelerate), behält movementDir für Animationen, kann gegen sehr steile groundPlane clippend sliden, ruft `PM_StepSlideMove(qtrue)`.

* **PM_WaterMove() / PM_WaterJumpMove() / PM_CheckWaterJump()** — Wasser-Handling: spezielles `wishvel` (inkl. upmove), Swim-Speed-Clamp (`pm_swimScale`), Wasser-Jump-Erkennung (push out of water) und Sink-Default.

* **PM_FlyMove()** — freie 3D-Bewegung (z.B. Fly powerup / noclip-like control but with friction/accelerate).

* **PM_LadderMove()** — Leiter: benutzt Ladder-Forward, Leiter-Skalierung und spezielle Up/Down-Bewegungen; kann Ladder-Jump behandeln. 

* **PM_NoclipMove()** — Noclip: spezielle erhöhte Friktion, volle 3D-Beschleunigung und direkte Position-Integration (keine Kollisionen).

* **PM_DeadMove()** — Bewegungsreduktion bei Tod (extra friction / speed reduction). 

# Kollisions-, Slide- und Step-Logik

* **PM_StepSlideMove(qboolean gravity)** und **PM_SlideMove** sind das Herz der Kollisionsbearbeitung: iterative Trace/ClipLoops, mehrere Ebenen (Planes) berücksichtigen, StepUp (Treppen) testen, und Position/velocity entsprechend anpassen. Viele Bewegungsmodi rufen diese Routinen auf, um physikalisch sinnvolles Sliding und Step-Up Verhalten zu erreichen. (Die Move-Routinen rufen `PM_StepSlideMove` und `PM_SlideMove` an den passenden Stellen auf.) 

* **PM_CorrectAllSolid / PM_GroundTrace / PM_GroundTraceMissed**
  Ground-Trace prüft, ob der Spieler „auf dem Boden“ ist (slope-checks, incl. `MIN_WALK_NORMAL` und `MIN_WALK_NORMAL_TERRAIN`). `PM_CorrectAllSolid` versucht den Spieler aus einem AllSolid zu „jittern“. `PM_GroundTraceMissed` behandelt den Übergang zu freiem Fall. 

# Jumping / Landing / Schadens-Logik

* **PM_CheckJump()** — prüft Jump-Tasten, Duck-State, Ladder-Jump Specialcase, setzt `PMF_JUMPING` und `velocity[2] = JUMP_VELOCITY`. 

* **PM_CrashLand(impactMaterial, impactNormal)** — berechnet bei hartem Landen Fall-„Delta“, löst Events (Fallshort/Medium/Far), reduziert Geschwindigkeit bei harten Landungen, setzt Land-timers (z. B. keine weiteren Jumps) und spielt Sounds/Events. Die Berechnung benutzt eine quadratische Lösung zur Bestimmung der genauen Aufprallgeschwindigkeit. 

# Events, Footsteps, Water-Events, Use

* **PM_Footsteps()** — erzeugt footstep-Events bzw. swim/splash Events basierend auf `bobCycle`, `xyspeed`, `waterlevel` und `buttons` (walk/run). 

* **PM_WaterEvents()** — erzeugt EV_WATER_TOUCH, EV_WATER_LAND, EV_WATER_CLEAR u.ä. beim Übergang zwischen Wasserleveln. 

* **PM_Use()** — behandelt „Use“-Input, Debounce/UseTime Mechanik und löst EV_USE aus. 

# Waffen / Animation / Callbacks

* Diverse Routinen zur Wahl/Start/Finish von Waffen-Animationen: **PM_SetWeaponTime**, **PM_GetAnimFromName**, **BG_GetWeaponAnim**, **PM_HandleWeaponAction**, **PM_BeginZoomIn/Out**, **PM_BeginWeaponChange / PM_FinishWeaponChange**. Diese kombinieren Anims, Timer, Callback-Notetracks und feuern Events wie `EV_FIRE_WEAPON`, `EV_ALT_FIRE`, `EV_WEAPON_CALLBACK` etc. 

* **PM_CheckWeaponNotes()** — tickt durch Anim-NoteTracks und triggert Waffen-Events (inkl. Inaccuracy/KickAngle Anpassungen). 

# Sonstiges / nützliche Details

* `pm->ps->pm_flags` enthält viele Zustände (DUCKED, JUMPING, LADDER, TIME_KNOCKBACK, ZOOMED, …) die das PM-Verhalten beeinflussen. 
* `pml` (local pmove variables) enthält temporäre Infos pro Frame: forward/right vectors, frametime (`pml.frametime`), previous_velocity/origin, walking/groundPlane flags — diese werden überall benutzt, um vektorprojektionen, sliding und zeitabhängige Rechnungen korrekt zu machen. 

# Fazit / Empfehlung

* **Wichtigste Teile** zum Verständnis / Portieren: `PM_StepSlideMove`/`PM_SlideMove` (Kollisionshandling), `PM_Friction`, `PM_Accelerate`, `PM_CmdScale`, `PM_GroundTrace` und `PM_CheckJump` — sie definieren das „Feeling“.
* Wenn du das Movement **1:1** in Unity nachbauen willst, musst du vor allem die **Trace-basierte Slide/Step-Logik** und das `CmdScale`/Input-Mapping sorgfältig übernehmen — Unitys CharacterController/Physik verhalten sich nicht identisch. (Ich kann dir beim Portieren dieser Funktionen helfen und eine präzise Unity-Implementierung oder kommentierten Line-by-Line-Durchgang schreiben.)

Möchtest du, dass ich eine bestimmte Funktion **zeilenweise** erkläre (z. B. `PM_StepSlideMove` oder `PM_CrashLand`) oder direkt eine **Unity-Portierung** der kritischen Teile (Step/Slide/Trace) schreibe?


# CSGO Bunnyhop / Longjump Formeln

Dieses Dokument fasst alle relevanten Berechnungen für CSGO-ähnliches Bunnyhopping / Longjumps zusammen.

---

## 1️⃣ Grundgeschwindigkeit

```text
v_new = v_old + (wishDir * airAccel + mousePush) * deltaTime
```

* v\_old = aktuelle horizontale Velocity (X/Z)
* wishDir = normalized forward + minimal side (z.B. forward + 0.1 \* strafeDir)
* airAccel = Luftbeschleunigung (höher → mehr Speed pro Frame)
* mousePush = Maus-X-Delta \* mouseStrafeMultiplier \* mouseAccelFactor
* deltaTime = FixedDeltaTime
* Maus-Push wirkt nur, wenn Mausrichtung und A/D Input synchron sind

---

## 2️⃣ Side-Speed → Forward-Speed Konvertierung

```text
v_forward += abs(dot(v, strafeDir)) * k
v_side -= dot(v, strafeDir) * k
```

* k \~ 0.8 (80% Seitwärtsgeschwindigkeit → Vorwärts)
* strafeDir = A/D Richtung (normalized)
* Nur anwenden, wenn |dot(v, strafeDir)| > epsilon

---

## 3️⃣ Air-Speed Cap

```text
if (v_new.magnitude > airSpeedCap)
    v_new = v_new.normalized * airSpeedCap
```

* AirSpeedCap begrenzt horizontale Geschwindigkeit

---

## 4️⃣ Forward Boost beim Strafing

```text
strafeSpeed = dot(v, strafeDir)
v_forward += strafeSpeed * 0.8
```

* Effekt: mehr horizontaler Speed abhängig von Side-Strafe

---

## 5️⃣ Landing Bonus (optional)

```text
landingSpeedBonus = lastHorizontalVelocity.magnitude * 0.01
v_ground = v_horizontal + landingSpeedBonus
landingSpeedBonus *= landingBonusDecay
```

* landingSpeedBonus \~ 1-5% der Horizontalgeschwindigkeit
* Exponentiell nach Landung reduziert

---

## 6️⃣ Jump-Formel

```text
v_y = sqrt(2 * g * jumpHeight)
t_air = 2 * v_y / g
```

* jumpHeight = Sprunghöhe
* g = |Gravity|
* Delta x = v\_horizontal \* t\_air → Distance

---

## 7️⃣ Distance Gain / Longjump

```text
d_jump = v_horizontal * t_air
```

* horizontal = aktuelle Velocity nach AirAccel + MousePush + ForwardBoost
* Höhere AirAccel → höhere horizontal velocity → längere Distanz
* Höhere jumpHeight → längere AirTime → mehr Distance

---

## 8️⃣ Strafe-Efficiency (Debug / GUI)

```text
efficiency = 1 - Angle(v_horizontal, wishDir) / 90°
```

* 0 → völlig seitlich
* 1 → perfekt forward

---

## 9️⃣ Maus / Input-Sync Bedingung

```text
if (sign(A/D) == sign(MouseDeltaX))
    apply full AirAccel
else
    minimal AirAccel
```

* Nur SpeedGain, wenn A/D Input und Maus synchron

---

## 🔧 Praktische Parameter für CSGO-Style

| Parameter             | Typische Werte |
| --------------------- | -------------- |
| airAccel              | 50-100         |
| mouseStrafeMultiplier | 0.002          |
| mouseAccelFactor      | 30             |
| velAlignFactor        | 2              |
| airSpeedCap           | 300            |
| jumpHeight            | 1.5 - 1.8      |
| sideBoostFactor       | 0.8            |

---

Diese Formeln kannst du direkt in Unity implementieren, um Bhop/Longjump-Mechaniken ähnlich wie in CSGO zu simulieren.

```
#pragma semicolon 1

#include <sourcemod>
#include <sdktools>
#include <cstrike>
#include <clientprefs>

#pragma newdecls required

#define TRAINER_TICK_INTERVAL 10

EngineVersion g_Game;

float gF_LastAngle[MAXPLAYERS + 1][3];
int gI_ClientTickCount[MAXPLAYERS + 1];
float gF_ClientPercentages[MAXPLAYERS + 1][TRAINER_TICK_INTERVAL];

Handle gH_StrafeTrainerCookie;
bool gB_StrafeTrainer[MAXPLAYERS + 1] = {false, ...};

public Plugin myinfo = 
{
	name = "BHOP Strafe Trainer",
	author = "PaxPlay",
	description = "Bhop Strafe Trainer",
	version = "0.1",
	url = "https://github.com/PaxPlay/bhop-strafe-trainer"
};

public void OnPluginStart()
{	
	g_Game = GetEngineVersion();
	if(g_Game != Engine_CSGO && g_Game != Engine_CSS)
	{
		SetFailState("This plugin is for CSGO/CSS only.");	
	}
	
	RegConsoleCmd("sm_strafetrainer", Command_StrafeTrainer, "Toggles the Strafe trainer.");
	
	gH_StrafeTrainerCookie = RegClientCookie("strafetrainer_enabled", "strafetrainer_enabled", CookieAccess_Protected);
	
	// Late loading
	for(int i = 1; i <= MaxClients; i++)
	{
		if(AreClientCookiesCached(i))
		{
			OnClientCookiesCached(i);
		}
	}
}

public void OnClientDisconnect(int client)
{
	gB_StrafeTrainer[client] = false;
}

public void OnClientCookiesCached(int client)
{
	gB_StrafeTrainer[client] = GetClientCookieBool(client, gH_StrafeTrainerCookie);
}

public Action Command_StrafeTrainer(int client, int args)
{
	if (client != 0)
	{
		gB_StrafeTrainer[client] = !gB_StrafeTrainer[client];
		SetClientCookieBool(client, gH_StrafeTrainerCookie, gB_StrafeTrainer[client]);
		ReplyToCommand(client, "[SM] Strafe Trainer %s!", gB_StrafeTrainer[client] ? "enabled" : "disabled");
	}
	else
	{
		ReplyToCommand(client, "[SM] Invalid client!");
	}
	
	return Plugin_Handled;
}

float NormalizeAngle(float angle)
{
	float newAngle = angle;
	while (newAngle <= -180.0) newAngle += 360.0;
	while (newAngle > 180.0) newAngle -= 360.0;
	return newAngle;
}

float GetClientVelocity(int client)
{
	float vVel[3];
	
	vVel[0] = GetEntPropFloat(client, Prop_Send, "m_vecVelocity[0]");
	vVel[1] = GetEntPropFloat(client, Prop_Send, "m_vecVelocity[1]");
	
	
	return GetVectorLength(vVel);
}

float PerfStrafeAngle(float speed)
{
	return RadToDeg(ArcTangent(30 / speed));
}

void VisualisationString(char[] buffer, int maxlength, float percentage)
{
	
	if (0.5 <= percentage <= 1.5)
	{
		int Spaces = RoundFloat((percentage - 0.5) / 0.05);
		for (int i = 0; i <= Spaces + 1; i++)
		{
			FormatEx(buffer, maxlength, "%s ", buffer);
		}
		
		FormatEx(buffer, maxlength, "%s|", buffer);
		
		for (int i = 0; i <= (21 - Spaces); i++)
		{
			FormatEx(buffer, maxlength, "%s ", buffer);
		}
	}
	else
		Format(buffer, maxlength, "%s", percentage < 1.0 ? "|                   " : "                    |");
}

void GetPercentageColor(float percentage, int &r, int &g, int &b)
{
	float offset = FloatAbs(1 - percentage);
	
	if (offset < 0.05)
	{
		r = 0;
		g = 255;
		b = 0;
	}
	else if (0.05 <= offset < 0.1)
	{
		r = 128;
		g = 255;
		b = 0;
	}
	else if (0.1 <= offset < 0.25)
	{
		r = 255;
		g = 255;
		b = 0;
	}
	else if (0.25 <= offset < 0.5)
	{
		r = 255;
		g = 128;
		b = 0;
	}
	else
	{
		r = 255;
		g = 0;
		b = 0;
	}
}

public Action OnPlayerRunCmd(int client, int &buttons, int &impulse, float vel[3], float angles[3], int &weapon, int &subtype, int &cmdnum, int &tickcount, int &seed, int mouse[2])
{
	if (!gB_StrafeTrainer[client])
		return Plugin_Continue; // dont run when disabled
	if ((GetEntityFlags(client) & FL_ONGROUND) || (GetEntityMoveType(client) == MOVETYPE_NOCLIP) || (GetEntityMoveType(client) == MOVETYPE_LADDER))
		return Plugin_Continue; // dont run when disabled
	
	// calculate differences
	float AngDiff[3];
	AngDiff[0] = NormalizeAngle(gF_LastAngle[client][0] - angles[0]); //not really used
	AngDiff[1] = NormalizeAngle(gF_LastAngle[client][1] - angles[1]);
	AngDiff[2] = NormalizeAngle(gF_LastAngle[client][2] - angles[2]); //not really used
	
	// get the perfect angle
	float PerfAngle = PerfStrafeAngle(GetClientVelocity(client));
	
	// calculate the current percentage
	float Percentage = FloatAbs(AngDiff[1]) / PerfAngle;
	
	
	if (gI_ClientTickCount[client] >= TRAINER_TICK_INTERVAL) // only every 10th tick, not really usable otherwise
	{
		float AveragePercentage = 0.0;
		
		for (int i = 0; i < TRAINER_TICK_INTERVAL; i++) // calculate average from the last ticks
		{
			AveragePercentage += gF_ClientPercentages[client][i];
			gF_ClientPercentages[client][i] = 0.0;
		}
		AveragePercentage /= TRAINER_TICK_INTERVAL;
		
		char sVisualisation[32]; // get the visualisation string
		VisualisationString(sVisualisation, sizeof(sVisualisation), AveragePercentage);
		
		// format the message
		char sMessage[256];
		Format(sMessage, sizeof(sMessage), "%d\%", RoundFloat(AveragePercentage * 100));
		
		Format(sMessage, sizeof(sMessage), "%s\n══════^══════", sMessage);
		Format(sMessage, sizeof(sMessage), "%s\n %s ", sMessage, sVisualisation);
		Format(sMessage, sizeof(sMessage), "%s\n══════^══════", sMessage);
		
		
		// get the text color
		int r, g, b;
		GetPercentageColor(AveragePercentage, r, g, b);
		
		// print the text
		Handle hText = CreateHudSynchronizer();
		if(hText != INVALID_HANDLE)
		{
			SetHudTextParams(-1.0, 0.2, 0.1, r, g, b, 255, 0, 0.0, 0.0, 0.1);
			ShowSyncHudText(client, hText, sMessage);
			CloseHandle(hText);
		}
		
		gI_ClientTickCount[client] = 0;
	}
	else
	{
		// save the percentage to an array to calculate the average later
		gF_ClientPercentages[client][gI_ClientTickCount[client]] = Percentage;
		gI_ClientTickCount[client]++;
	}
	
	// save the angles to a variable used in the next tick
	gF_LastAngle[client] = angles;
	
	return Plugin_Continue;
}

stock bool GetClientCookieBool(int client, Handle cookie)
{
	char sValue[8];
	GetClientCookie(client, gH_StrafeTrainerCookie, sValue, sizeof(sValue));
	
	return (sValue[0] != '\0' && StringToInt(sValue));
}

stock void SetClientCookieBool(int client, Handle cookie, bool value)
{
	char sValue[8];
	IntToString(value, sValue, sizeof(sValue));
	
	SetClientCookie(client, cookie, sValue);
}
```
