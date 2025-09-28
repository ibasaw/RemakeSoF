# Skeleton Loader System

Ein Unity-System zum Laden von SoF2 Skeleton JSON-Dateien und automatischen Erstellen von PCJ-Boxen basierend auf der Bone-Struktur.

## Komponenten

### 1. SkeletonLoader.cs
Das Haupt-Script, das an ein Prefab angehängt werden kann.

**Features:**
- Lädt JSON-Dateien aus dem `Assets/Data/skeletons/` Ordner
- Erstellt automatisch BoxCollider für jeden PCJ-Eintrag (Hitboxes)
- **PCJ-Hitbox-System:** Verwendet PCJ-Array für Hitbox-Definitionen
- **Bone-Child-Erstellung:** PCJ-Boxen werden als Child der entsprechenden Bones erstellt
- **Bone-Referenzierung:** Positioniert Boxen basierend auf BoneAim, BoneRelative, BoneSmooth, BoneExtra
- **Animation-Follow:** Boxen folgen automatisch der Bone-Animation
- Skaliert Koordinaten von Meter zu Zentimeter (durch 100)
- Erstellt nur Boxen wenn entsprechender Bone gefunden wird (kein Fallback)
- **Auto-Load:** Lädt automatisch beim Spielstart (konfigurierbar)
- **Rekursive Bone-Suche:** Durchsucht alle Child-Elemente nach Bones
- **Verbesserte Clear-Funktion:** Entfernt alle PCJ-Boxen rekursiv aus der Bone-Hierarchie

**Verwendung:**
1. Script an ein GameObject anhängen
2. UI-Referenzen setzen (optional)
3. Box-Einstellungen konfigurieren (isTrigger)
4. Über Code oder UI eine Skeleton-Datei laden

### 2. SkeletonLoaderEditor.cs
Ein Custom Editor für bessere Integration in den Unity Inspector.

**Features:**
- Dropdown zur Auswahl der Skeleton-Dateien
- Load/Clear Buttons direkt im Inspector
- Live-Info über erstellte Boxen (rekursive Suche in Bone-Hierarchie)
- Automatische Datei-Erkennung

### 3. SkeletonLoaderUI.cs
UI-Controller für die Dropdown-Auswahl.

**Features:**
- Dropdown mit allen verfügbaren Skeleton-Dateien
- Load/Clear Buttons
- Status-Anzeige
- Automatische UI-Erkennung

## Setup

### Schritt 1: Prefab erstellen
1. Erstelle ein leeres GameObject
2. Füge das `SkeletonLoader` Script hinzu
3. Speichere als Prefab

### Schritt 2: UI Setup (optional)
1. Erstelle ein Canvas mit UI-Elementen:
   - TMP_Dropdown für Dateiauswahl
   - Button für "Load"
   - Button für "Clear"
   - TextMeshPro für Status
2. Füge das `SkeletonLoaderUI` Script hinzu
3. Verbinde die UI-Referenzen

### Schritt 3: Skeleton-Dateien
Platziere deine `.skl.json` Dateien im Ordner `Assets/Data/skeletons/`

## Verwendung

### Automatisch beim Spielstart (Standard)
1. Script an GameObject anhängen
2. `autoLoadOnStart` aktivieren (Standard: true)
3. `autoLoadIndex` setzen (0 = erste Datei, 1 = zweite, etc.)
4. Spiel starten - PCJ-Boxen werden automatisch erstellt

### Über den Inspector (mit Editor Script)
1. Wähle das GameObject mit dem SkeletonLoader
2. Im Inspector: "Refresh Skeleton Files" klicken
3. Wähle eine Skeleton-Datei aus dem Dropdown
4. "Load Selected Skeleton" klicken

### Über Code
```csharp
SkeletonLoader loader = GetComponent<SkeletonLoader>();
loader.LoadSkeletonFile("average_sleeves"); // Lädt spezifische Datei
loader.LoadSelectedSkeleton(); // Lädt die aktuell ausgewählte Datei
loader.ClearAllBoxes(); // Löscht alle erstellten Boxen
```

### Über UI
1. Verwende das SkeletonLoaderUI Script
2. Verbinde die UI-Elemente
3. Benutzer kann über Dropdown auswählen und laden

## PCJ-Box Erstellung

Das System erstellt für jeden Eintrag im PCJ-Array eine Hitbox:

- **Größe:** Berechnet aus `Maxs` und `Mins` Arrays (von Meter zu Zentimeter skaliert)
- **Position:** Basierend auf Bone-Referenzen (BoneAim, BoneRelative, BoneSmooth, BoneExtra)
- **Bone-Suche:** Sucht Bones in folgender Priorität:
  1. BoneAim (Haupt-Referenz)
  2. BoneRelative (Relative Position)
  3. BoneSmooth (Interpolation)
  4. BoneExtra (Zusätzliche Referenz)
- **Bone-Child:** PCJ-Box wird als Child des gefundenen Bones erstellt
- **Animation-Follow:** Boxen folgen automatisch der Bone-Animation und -Transformation
- **Hitbox-System:** PCJ sind Hitboxes für Schadenserkennung
- **Collider:** Nur BoxCollider, keine visuellen Elemente
- **Fallback:** Keine Box wird erstellt wenn kein Bone gefunden wird

## Konfiguration

### SkeletonLoader Einstellungen:
- `isTrigger`: BoxCollider als Trigger konfigurieren
- `autoLoadOnStart`: Lädt automatisch beim Spielstart
- `autoLoadIndex`: Index der Skeleton-Datei zum Auto-Loaden (0 = erste Datei)
- `debugMode`: Aktiviert Debug-Ausgaben

## Unterstützte Dateien

Das System erkennt automatisch alle `.skl.json` Dateien im `Assets/Data/skeletons/` Ordner:
- average_sleeves.skl.json
- dog.skl.json
- female_pants.skl.json
- marine.skl.json
- osprey.skl.json

## Bone-Matching

Das System versucht Bones auf folgende Weise zu finden:
1. Exakte Namensübereinstimmung (case-insensitive)
2. Teilweise Namensübereinstimmung
3. Fallback auf null (Box wird am Ursprung erstellt)

## Debugging

Aktiviere `debugMode` für detaillierte Log-Ausgaben:
- Gefundene Skeleton-Dateien
- Erstellte PCJ-Boxen
- Bone-Zuordnungen
- Hierarchie-Setup
