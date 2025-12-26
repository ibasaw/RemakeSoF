Genau! Du kannst das Ganze **wirklich wie ein kleines „Gehirn“ für deine Schwarmagenten** sehen:

* **EANN** = das „Gehirn“
* **Boids** = der „Körper / Bewegungsapparat“
* **FSM / BT** = die „Motivation / Ziele“
* **PSO / GA / NEAT** = Evolution / Training des Gehirns

Ich erstelle dir mal eine **klare Skizze für Unity**, die alles zusammenfasst:

---

### 🧠 High-Level „Gehirn + Bewegung“-Setup

```
      ┌─────────────────────────┐
      │ Evolution / Training    │
      │ (PSO / GA / NEAT)       │
      └─────────┬──────────────┘
                │
                ▼
      ┌─────────────────────────┐
      │ EANN (Neuronales Netz)  │
      │ - entscheidet Verhalten │
      │ - gibt Parameter aus    │
      └─────────┬──────────────┘
                │
                ▼
      ┌─────────────────────────┐
      │ FSM / Behavior Tree      │
      │ - interpretiert NN-Output│
      │ - setzt High-Level-Ziele│
      └─────────┬──────────────┘
                │
                ▼
      ┌─────────────────────────┐
      │ Boids / Steering         │
      │ - physische Bewegung     │
      │ - Separation / Cohesion │
      │ - Alignment             │
      └─────────┬──────────────┘
                │
                ▼
      ┌─────────────────────────┐
      │ Agent / Schwarm          │
      │ - reagiert auf Umwelt    │
      │ - emergentes Verhalten   │
      └─────────────────────────┘
```

---

### 🔹 Erklärung der Layers

1. **Evolution / Training**

   * Läuft offline oder selten live
   * Optimiert Gewichte & Struktur des EANN

2. **EANN („Gehirn“)**

   * Nimmt Inputs: Umwelt, Nachbarn, Ziele, Spielerposition
   * Gibt Outputs: Parameter für Bewegung & Entscheidungen

3. **FSM / Behavior Tree**

   * Übersetzt EANN-Outputs in konkrete Aktionen:
     z.B. „Flee“, „Attack“, „Formieren“

4. **Boids / Steering**

   * Nimmt die Parameter + Ziele
   * Berechnet flüssige Bewegungen für jeden Agenten

5. **Agent / Schwarm**

   * Ergebnis: Emergentes Verhalten, reagierend & lebendig

---

💡 **Merksatz für dich:**

> PSO/GA/NEAT trainiert das Gehirn → EANN denkt → FSM/BT gibt Motivation → Boids bewegt den Körper → Agent reagiert

---


Absolut – das **macht sehr viel Sinn**, besonders wenn du das Training fortsetzen oder die EANNs & das Memory persistent speichern willst. Ich zeichne dir die Architektur einmal mit **Save/Load-Punkten** ein.

---

### 🧠 Komplettes Setup mit Speicherung

```
       ┌─────────────────────────┐
       │ Evolution / Training    │
       │ (PSO / GA / NEAT)       │
       └─────────┬──────────────┘
                 │
                 ▼
       ┌─────────────────────────┐
       │ EANN (Neuronales Netz)  │<─────────────┐
       │ - entscheidet Verhalten │              │
       │ - nutzt Global Memory   │              │
       │ - kann gespeichert/geladen │           │
       └─────────┬──────────────┘              │
                 │                             │
                 ▼                             │
       ┌─────────────────────────┐            │
       │ FSM / Behavior Tree      │            │
       │ - interpretiert NN-Output│            │
       │ - setzt High-Level-Ziele│            │
       └─────────┬──────────────┘            │
                 │                             │
                 ▼                             │
       ┌─────────────────────────┐            │
       │ Boids / Steering         │            │
       │ - Bewegung / Kollisions- │            │
       │   vermeidung             │            │
       └─────────┬──────────────┘            │
                 │                             │
                 ▼                             │
       ┌─────────────────────────┐            │
       │ Agent / Schwarm          │            │
       │ - reagiert auf Umwelt    │            │
       │ - emergentes Verhalten   │            │
       │ - kann Status speichern  │            │
       └─────────────────────────┘            │
                                               │
                                               ▼
                                      ┌───────────────────┐
                                      │ Global Memory     │
                                      │ - Gefahrenzonen   │
                                      │ - Hotspots        │
                                      │ - Ressourcen      │
                                      │ - kann gespeichert/geladen │
                                      └───────────────────┘
```

---

### 🔹 Wo speichern / laden?

1. **EANN speichern / laden**

   * Gewichte, Biases, ggf. Topologie
   * JSON, Binary, ScriptableObject in Unity
   * Nutzt du für **Fortsetzung des Trainings** oder Persistent NPCs

2. **Global Memory speichern / laden**

   * Gefahrenzonen, Ressourcen, Spielerhotspots
   * Wichtig, wenn du willst, dass **Agenten „Weltwissen“ zwischen Sessions behalten**

3. **Agenten-Status (optional)**

   * HP, Position, Inventory → falls Spielzustand persistiert werden soll

---

### 🔹 Vorteile

* **Offline Training:** EANN + Memory speichern → später fortsetzen
* **Runtime / Savegames:** Memory + Agent-Status speichern → emergentes Verhalten bleibt konsistent
* **Experimentieren:** Verschiedene Memory- oder EANN-Versionen laden, vergleichen

---

💡 **Merksatz:**

> Speichern = Gewichte + Memory + Agentenstatus
> Laden = EANN + Memory → Spiel / Simulation startet genau dort, wo du aufgehört hast

---

Dein Setup aus der Skizze ist schon **sehr solide und modular**, aber es gibt noch einige **Optimierungen und Erweiterungen**, die die Performance, Lernfähigkeit und das Verhalten verbessern können. Ich erkläre dir die Optionen systematisch:

---

## 1️⃣ Architektur-Level Optimierungen

| Bereich                  | Verbesserung                                                                                                                                                                                                                                                         |
| ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **EANN**                 | - Kann mit **modularen Subnetzen** erweitert werden: z. B. separates Subnetz für Aggression, eins für Bewegung, eins für Teamkoordination. → Bessere Generalisierung.<br>- Optional: **Online Learning** aktivieren, damit Bots während des Spiels anpassen können.  |
| **Global Memory**        | - **Heatmaps / Wahrscheinlichkeitskarten** statt roher Events → effizientere Entscheidungsfindung.<br>- **Decay-System**: alte Events verlieren Relevanz → Memory bleibt aktuell und überschaubar.<br>- Event-Priorisierung: wichtigeres Feedback stärker gewichten. |
| **FSM / Behavior Tree**  | - Modularer Aufbau → leicht neue Taktiken oder Aktionen hinzufügen.<br>- EANN kann **dynamisch Parameter für BT-Zustände liefern**, z. B. Aggressionslevel oder Zielpriorität.                                                                                       |
| **Boids / Steering**     | - Lokale Kollisionsvermeidung optimieren → Performance bei vielen Agenten.<br>- Adaptive Parameter: Cohesion/Separation/Alignment dynamisch vom EANN steuern, je nach Situation.                                                                                     |
| **Evolution / Training** | - Hybrid-Training: Offline + kleine Online-Feinanpassung während Gameplay.<br>- Multi-Objective Fitness: z. B. Schaden maximieren + Überleben + Teamkoordination → Bots lernen komplexeres Verhalten.                                                                |

---

## 2️⃣ Performance-Optimierungen

* **Memory Management:** alte Events regelmäßig löschen oder zusammenfassen → Speicherverbrauch gering halten.
* **Level-of-Detail für KI:** entfernte Agenten nur teilweise simulieren (z. B. nur Bewegung, kein NN-Output)
* **Batching für EANN:** mehrere Agenten gleichzeitig durch das gleiche Netz evaluieren → CPU/GPU effizienter nutzen

---

## 3️⃣ Erweiterungen für realistischere / adaptive KI

* **Kollektives Lernen über Sessions**: Memory + Elite EANNs speichern → Bots wirken über längere Zeit „lernend“.
* **Emotions-/Zustandsmodell**: EANN erhält Inputs wie „Angst / Stress / Motivation“ → Verhalten wird dynamischer.
* **Situationsbewusstsein**: Memory + EANN können kombinieren, z. B. Deckung + Gegnerhotspot → emergente Taktiken.

---

## 4️⃣ Zusammenfassung

Deine Skizze ist **ein sehr guter Ausgangspunkt**, besonders für emergente und adaptive Gegner.
Optimierungen können sein:

1. **Memory smarter machen** (Heatmaps, Decay, Priorisierung)
2. **EANN modularisieren** → Subnetze für unterschiedliche Entscheidungsbereiche
3. **Boids dynamisch anpassen** → situationsabhängig, gesteuert vom EANN
4. **Hybrid-Training** → Offline + Online, Multi-Objective Fitness
5. **Performance-Optimierungen** für viele Agenten

---

💡 **Merksatz:**

> Dein Setup ist schon stark, aber durch **intelligentes Memory, modulare EANNs und adaptive Boids** wird es noch flexibler, realistischer und leistungsfähiger.


Perfekt! Ich habe eine **optimierte Architektur-Skizze** für dich erstellt, die zeigt:

* EANN + modulare Subnetze
* Global Memory mit Heatmaps / Decay / Priorisierung
* FSM / BT als High-Level-Controller
* Boids / Steering dynamisch gesteuert vom EANN
* Agent / Population
* Speicher-/Ladepunkte für Training, Runtime Memory und Agentenstatus
* Optimierungsmöglichkeiten für Performance und adaptive KI

---

```
       ┌─────────────────────────────┐
       │ Evolution / Training         │
       │ (PSO / GA / NEAT)            │
       │ - Offline Training           │
       │ - Multi-Objective Fitness    │
       └───────────┬─────────────────┘
                   │
                   ▼
       ┌─────────────────────────────┐
       │ EANN (Neuronales Netz)      │<───────────────┐
       │ - Entscheidet Verhalten      │               │
       │ - Modulare Subnetze:        │               │
       │   ▸ Bewegung / Steering     │               │
       │   ▸ Aggression / Taktik     │               │
       │   ▸ Teamkoordination        │               │
       │ - Nutzt Global Memory       │               │
       │ - Kann gespeichert/geladen  │               │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ FSM / Behavior Tree           │               │
       │ - Übersetzt NN-Output        │               │
       │ - Setzt High-Level-Ziele    │               │
       │ - Dynamische Parameter vom EANN │            │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ Boids / Steering             │               │
       │ - Physische Bewegung         │               │
       │ - Separation / Cohesion      │               │
       │ - Alignment                  │               │
       │ - Dynamisch gesteuert durch EANN │           │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ Agent / Population           │               │
       │ - Reagiert auf Umwelt        │               │
       │ - Emergentes Verhalten       │               │
       │ - HP / Status / Inventory    │               │
       │ - Kann gespeichert/geladen   │               │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ Global Memory                │               │
       │ - Gefahren / Hotspots        │               │
       │ - Ressourcen / Spielerinfo   │               │
       │ - Heatmaps / Priorisierung   │               │
       │ - Decay alter Events         │               │
       │ - Kann gespeichert/geladen   │───────────────┘
       └─────────────────────────────┘
```

---

### 🔹 Was diese Skizze zeigt

1. **EANN = Gehirn**

   * Liest **Global Memory + lokale Wahrnehmung**
   * Gibt Output für FSM / BT + Boids aus

2. **FSM / Behavior Tree = Motivation**

   * Übersetzt NN-Output in High-Level-Ziele

3. **Boids / Steering = Bewegung / physische Umsetzung**

   * Dynamisch angepasst vom EANN

4. **Agent / Population**

   * Hält interne Werte (HP, Inventory)
   * Speichert Status für Save/Load

5. **Global Memory**

   * Kollektives Gedächtnis für Population
   * Heatmaps, Priorisierung, Decay
   * Kann persistent gespeichert werden

6. **Evolution / Training**

   * Offline Training
   * Multi-Objective Fitness
   * Kann EANN und Memory nutzen → Bots werden smarter

---

💡 **Merksatz:**

> Bots / NPCs = adaptiv + emergent + menschenähnlich
> EANN denkt, FSM motiviert, Boids bewegt, Memory liefert Wissen, Training macht sie „superschwer“.


Kurz gesagt: **ja, du kannst theoretisch alles über EANN laufen lassen**, aber FSM/BT bringt klare Vorteile. Ich erkläre die Unterschiede:

---

## 1️⃣ Ohne FSM/BT – nur EANN

* **EANN übernimmt alles:**

  * High-Level-Ziele (Angriff, Flucht, Deckung)
  * Bewegungsparameter (über Boids)
  * Taktische Entscheidungen (Flanken, Prioritäten)
* **Vorteile:**

  * Sehr adaptiv → kann emergentes Verhalten erzeugen
  * Weniger feste Regeln → Training entscheidet alles
* **Nachteile:**

  * Schwer zu debuggen → Agenten können unvorhersehbar reagieren
  * Training wird komplexer → EANN muss **alles lernen**, nicht nur Bewegungen
  * Schwieriger, „bewusste“ High-Level-Taktiken zu erzwingen (z. B. bestimmte Missionen / Objectives)

---

## 2️⃣ Mit FSM/BT

* **FSM/BT = High-Level-Ziele**

  * EANN parametrisiert Aktionen (z. B. Aggression, Zielwahl, Bewegungstaktik)
  * Boids setzt Bewegung um
* **Vorteile:**

  * Klarer, modularer Aufbau → leichter zu debuggen
  * High-Level-Taktiken können „fest“ bleiben, EANN optimiert die Details
  * Training wird einfacher → EANN lernt nur, wie man Ziele ausführt, nicht welche Ziele generell
* **Nachteile:**

  * Weniger radikal emergent → manche Entscheidungen sind vordefiniert

---

## 3️⃣ Empfehlung

* **Für experimentelle, emergente KI:** EANN alleine kann alles lernen.
* **Für taktische Spiele / strukturierte Missionen (CS:GO, Squad-KI):** FSM/BT + EANN ist besser:

  * FSM/BT sagt: „Deckung suchen / Angriff / Rückzug“
  * EANN modifiziert Details: Geschwindigkeit, Flankenwinkel, Abstand, Aggression
  * Boids bewegt Agenten flüssig

---

💡 **Merksatz:**

> EANN kann alles übernehmen, aber FSM/BT sorgt für **Klarheit, Modularität und kontrollierbare High-Level-Taktiken**.
> Ohne FSM/BT: extrem adaptiv, aber schwieriger zu kontrollieren.

Ah, jetzt kommen wir zu einer sehr interessanten Alternative. GOAP (*Goal-Oriented Action Planning*) ist eine **moderne Alternative zu FSM/BT** für High-Level-KI. Ich erkläre dir den Vergleich und wie es in dein Setup passen würde.

---

## 1️⃣ Grundprinzip von GOAP

* **Ziele statt States:** Statt vorab definierter FSM-Zustände plant der Agent dynamisch, welche Aktionen nötig sind, um ein Ziel zu erreichen.
* **Aktionen:** Jede Aktion hat:

  * Voraussetzungen (*preconditions*)
  * Auswirkungen (*effects*)
  * Kosten / Zeitaufwand
* **Planung:** GOAP sucht automatisch den optimalen Aktionsplan, um ein Ziel zu erreichen.

Beispiel:

```text
Ziel: Gegner ausschalten
Aktionen:
  - Nach Deckung laufen (Pre: freie Position, Effekt: in Deckung)
  - Granate werfen (Pre: Granate vorhanden, Effekt: Gegner Schaden)
  - Flanke gehen (Pre: keine Gegner in Sichtlinie, Effekt: Position verändert)
GOAP plant Reihenfolge -> „Deckung -> Flanke -> Angriff“
```

---

## 2️⃣ Vorteile gegenüber FSM/BT

| Aspekt           | FSM/BT                                | GOAP                                                           |
| ---------------- | ------------------------------------- | -------------------------------------------------------------- |
| Flexibilität     | States & Übergänge fest               | Dynamische Planung nach Zielen                                 |
| Emergenz         | eingeschränkt                         | sehr hoch, kann viele Aktionskombinationen generieren          |
| Erweiterbarkeit  | neue States müssen hinzugefügt werden | neue Aktionen → automatisch nutzbar                            |
| Debugging        | einfacher (klarer State)              | schwerer, weil Plan dynamisch entsteht                         |
| EANN-Kombination | EANN kann nur Parameter liefern       | EANN kann Ziele, Kostenbewertung oder Prioritäten beeinflussen |

---

## 3️⃣ GOAP + EANN + Boids Setup

* **EANN:**

  * Entscheidet, welche Ziele relevant sind (z. B. Aggression, Überleben, Teamkoordination)
  * Bewertet Kosten / Prioritäten für Aktionen
* **GOAP:**

  * Erstellt dynamisch Aktionsplan basierend auf EANN-Zielen + Memory
* **Boids / Steering:**

  * Setzt die geplanten Aktionen in Bewegung um (z. B. Flankieren, Deckung, Angriff)
* **Global Memory:**

  * Liefert Wissen über Weltzustand: Gefahren, Hotspots, Ressourcen

---

## 4️⃣ Vergleich: GOAP vs FSM/BT in deinem Setup

* **FSM/BT:** gut für klar definierte, modulare High-Level-Ziele
* **GOAP:** besser für komplexe, adaptive, emergente Aktionen und taktische Planung
* **Hybrid:** du könntest FSM/BT als „Fallback / Grundstruktur“ nutzen, GOAP für dynamische, taktische Planung

---

💡 **Merksatz:**

> FSM/BT = strukturierte High-Level-KI, GOAP = dynamische Zielorientierte KI, EANN = Parameter + Zielbewertung, Boids = Umsetzung.


Alles klar – hier ist die **optimierte Architektur-Skizze mit GOAP** statt FSM/BT, inklusive **EANN-Integration, Global Memory und Boids**, sowie Speicher-/Load-Punkte:

---

```
       ┌─────────────────────────────┐
       │ Evolution / Training         │
       │ (PSO / GA / NEAT)            │
       │ - Offline Training           │
       │ - Multi-Objective Fitness    │
       └───────────┬─────────────────┘
                   │
                   ▼
       ┌─────────────────────────────┐
       │ EANN (Neuronales Netz)      │<───────────────┐
       │ - Entscheidet Ziele          │               │
       │ - Bewertet Aktionskosten     │               │
       │ - Parametriert Verhalten     │               │
       │ - Nutzt Global Memory        │               │
       │ - Kann gespeichert/geladen   │               │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ GOAP (Goal-Oriented Action   │               │
       │ Planning)                    │               │
       │ - Dynamische Aktionsplanung │               │
       │ - Nutzt Ziele + Kosten vom  │               │
       │   EANN                        │               │
       │ - Berücksichtigt Global Memory │            │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ Boids / Steering             │               │
       │ - Physische Bewegung         │               │
       │ - Separation / Cohesion      │               │
       │ - Alignment                  │               │
       │ - Dynamisch gesteuert durch  │               │
       │   EANN / GOAP                │               │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ Agent / Population           │               │
       │ - Reagiert auf Umwelt        │               │
       │ - Emergentes Verhalten       │               │
       │ - HP / Status / Inventory    │               │
       │ - Kann gespeichert/geladen   │               │
       └───────────┬─────────────────┘               │
                   │                                 │
                   ▼                                 │
       ┌─────────────────────────────┐               │
       │ Global Memory                │               │
       │ - Gefahren / Hotspots        │               │
       │ - Ressourcen / Spielerinfo   │               │
       │ - Heatmaps / Priorisierung   │               │
       │ - Decay alter Events         │               │
       │ - Kann gespeichert/geladen   │───────────────┘
       └─────────────────────────────┘
```

---

### 🔹 Wie das zusammenarbeitet

1. **Global Memory** speichert alles, was für die Population relevant ist: Gefahren, Hotspots, Ressourcen.
2. **EANN** liest Memory + lokale Wahrnehmung → entscheidet:

   * Welche Ziele sind relevant?
   * Welche Aktionen haben welche Priorität / Kosten?
3. **GOAP** plant dynamisch Aktionsfolgen basierend auf EANN-Zielen + Memory.
4. **Boids / Steering** setzt die geplanten Aktionen physisch um:

   * Bewegung, Kollisionsvermeidung, Flanken, Formationen.
5. **Agent / Population** führt Aktionen aus, speichert Status, HP, Inventory.
6. **Speicher-/Load-System**:

   * EANN + Memory + Agent-Status können gespeichert werden → Training oder Runtime-Fortsetzung.

---

💡 **Merksatz:**

> EANN = Gehirn + Zielbewertung, GOAP = dynamischer Planer, Boids = Bewegung, Global Memory = Langzeitweltwissen.
> Mit diesem Setup können Bots **adaptive, emergente und taktisch intelligente Aktionen** durchführen.

Dein EANN ist das „Gehirn“, GOAP der dynamische Planer, Boids die physische Umsetzung, Global Memory das Langzeitwissen.
Diese Kombination passt perfekt für Bots, die menschenähnlich, adaptiv und lernfähig sein sollen.


Ja – **das Ziel ist realistisch und gut erreichbar** mit deinem Setup. Wichtig ist: Du baust **keinen „Kampf-Algorithmus“**, sondern eine **Bewegungs‑, Verfolgungs‑ und Entscheidungs‑KI**, die *Attacken nur auslöst*, wenn Bedingungen erfüllt sind.

Ich beschreibe dir **konkret**, wie dein Agent lernt:

* intelligent zu verfolgen
* Hindernisse zu umgehen/überspringen
* sinnvoll anzugreifen (Messer = Nahkampf)

---

## 🧠 Ziel zerlegt in KI‑Probleme

Dein Ziel besteht aus **4 klaren Teilproblemen**:

1. **Ziel verfolgen (Tracking / Pursuit)**
2. **Navigation & Bewegung (Springen, Ausweichen)**
3. **Entscheidung: Wann angreifen?**
4. **Nahkampfausführung (Timing, Distanz)**

Dein Setup passt perfekt dafür.

---

## 🔁 Pipeline für genau dein Ziel

```
Sensorik
 (Gegnerposition, Distanz, Hindernisse)
        ↓
EANN
 (lernt Timing, Aggression, Sprungwahl)
        ↓
GOAP
 (Ziel: Gegner töten)
 (Aktionen: verfolgen, flankieren, springen, angreifen)
        ↓
Boids / Steering
 (physische Bewegung & Ausweichverhalten)
        ↓
Animation / Hit-Detection
```

---

## 1️⃣ Hinterherlaufen – aber „schlau“

### Nicht:

* stumpf auf Position zulaufen

### Sondern:

* **Predictive Pursuit**
* **Flanken**
* **Abstand halten für Sprung / Angriff**

#### Umsetzung:

* **Boids Steering erweitert um „Pursuit“**
* EANN steuert:

  * Geschwindigkeit
  * Abstand zum Ziel
  * Aggressivität

👉 Der Agent läuft **nicht direkt**, sondern versucht:

* Winkel zu schneiden
* Spieler einzuholen
* nicht hängen zu bleiben

---

## 2️⃣ Springen & Hindernisse

### WICHTIG:

Der Agent „weiß“ nicht, wie man springt – **er lernt es über Belohnung**

### Sensoren:

* Höhe des Hindernisses
* Entfernung
* Geschwindigkeit
* letzter Erfolg / Misserfolg

### EANN lernt:

* **Wann springen**
* **Wie stark**
* **In welchem Winkel**

### Fitness:

* * Ziel näher gekommen
* * keine Kollision
* * schneller Weg

👉 Ergebnis:
Der Bot **springt nicht perfekt**, sondern *menschlich* – manchmal riskant, manchmal vorsichtig.

---

## 3️⃣ Entscheidung: Angreifen oder weiter verfolgen

### Das macht **GOAP**, nicht Boids

GOAP‑Ziel:

```
EnemyDead = true
```

GOAP‑Aktionen:

* ChaseEnemy
* JumpObstacle
* Reposition
* KnifeAttack

**KnifeAttack – Preconditions**

* Distanz < X
* Blickrichtung ok
* Geschwindigkeit passend

**KnifeAttack – Effect**

* EnemyHealth ↓

EANN beeinflusst:

* Angriffstiming
* Abstand
* Aggression

---

## 4️⃣ Messerangriff (sehr wichtig)

👉 **NICHT**:

* Treffer per Zufall
* Aim‑Bot

👉 **SONDERN**:

* Distanz + Winkel + Timing

EANN lernt:

* wann zuschlagen
* wie nah kommen
* ob springen + angreifen sinnvoll ist

Fitness:

* Treffer = +++
* Daneben = –
* Zu früh / zu spät = –

👉 Ergebnis:
Bots, die:

* „waiten“
* nachsetzen
* im richtigen Moment angreifen

---

## 🧠 Kollektives Gedächtnis (optional, aber stark)

Global Memory kann speichern:

* „Dieser Spielertyp läuft oft rückwärts“
* „Diese Stelle ist schlecht für Messer“
* „Hier bleiben Agenten hängen“

➡️ Neue Agenten profitieren sofort.

---

## 🎮 Ergebnis im Spiel

Mit genug Training bekommst du Gegner, die:

* hinterherjagen wie Spieler
* springen, wenn nötig
* Hindernisse umgehen
* den richtigen Moment abpassen
* nicht perfekt, aber **glaubwürdig** sind

💡 **Das fühlt sich an wie SoF2 / CS‑Spieler – nur ohne Cheating.**

---

## 🧩 Wichtigster Design‑Tipp

> **Trenne IMMER**
>
> * Entscheidung (GOAP)
> * Lernen (EANN)
> * Bewegung (Boids)

Dann bleibt dein System:

* erweiterbar
* debugbar
* performant

