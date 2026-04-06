# QuakeColorLabel System

Custom `VisualElement` für UIToolkit, das Quake/SoF2-farbcodierten Text aus einem Bigchars-Atlas rendert.
Wird im Scoreboard (Spielernamen), Gametype-Messages und Hit-Confirmation verwendet.

---

## Übersicht

| Eigenschaft | Wert |
|-------------|------|
| **Klasse** | `QuakeColorLabel : VisualElement` |
| **Pfad** | `Assets/Scripts/Runtime/Core/QuakeColorLabel.cs` |
| **Namespace** | `Tolik.RemakeSoF.Runtime.Core` |
| **Registration** | `[UxmlElement]` (UIToolkit Custom Element) |
| **Atlas** | 16×16 Grid (256 ASCII-Zeichen), weiß auf transparent |

---

## Color Escape Sequences

### Syntax

| Sequenz | Bedeutung |
|---------|-----------|
| `^0` - `^9` | 10 Basis-Farben (Quake III Original) |
| `^a` - `^z` | 26 erweiterte Farben (case-insensitive) |
| `^^` | Literal `^` Zeichen |
| `\XX` | Hex-Escape für Atlas-Symbol (z.B. `\01` = Char 1, `\FF` = Char 255) |
| `\\` | Literal `\` Zeichen |

### Farbtabelle

#### Basis (^0-^9)

| Code | Farbe | RGB |
|------|-------|-----|
| `^0` | Black | `(0, 0, 0)` |
| `^1` | **Red** | `(1.0, 0.2, 0.2)` |
| `^2` | **Green** | `(0.2, 1.0, 0.2)` |
| `^3` | **Yellow** | `(1.0, 1.0, 0.2)` |
| `^4` | **Blue** | `(0.3, 0.3, 1.0)` |
| `^5` | **Cyan** | `(0.2, 1.0, 1.0)` |
| `^6` | **Magenta** | `(1.0, 0.2, 1.0)` |
| `^7` | **White** | `(1.0, 1.0, 1.0)` — Default |
| `^8` | **Orange** | `(1.0, 0.5, 0.0)` |
| `^9` | **Grey** | `(0.5, 0.5, 0.5)` |

#### Erweitert (^a-^z)

| Code | Farbe | RGB |
|------|-------|-----|
| `^a` | Salmon | `(1.0, 0.5, 0.5)` |
| `^b` | Light Green | `(0.5, 1.0, 0.5)` |
| `^c` | Light Blue | `(0.5, 0.5, 1.0)` |
| `^d` | Olive | `(0.6, 0.6, 0.0)` |
| `^e` | Pink | `(1.0, 0.4, 0.7)` |
| `^f` | Purple | `(0.6, 0.2, 0.8)` |
| `^g` | Dark Green | `(0.0, 0.5, 0.0)` |
| `^h` | Maroon | `(0.6, 0.1, 0.1)` |
| `^i` | Brown | `(0.6, 0.4, 0.2)` |
| `^j` | Gold | `(1.0, 0.84, 0.0)` |
| `^k` | Dark Grey | `(0.3, 0.3, 0.3)` |
| `^l` | Light Grey | `(0.8, 0.8, 0.8)` |
| `^m` | Medium Grey | `(0.6, 0.6, 0.6)` |
| `^n` | Teal | `(0.0, 0.5, 0.5)` |
| `^o` | Olive Green | `(0.5, 0.6, 0.2)` |
| `^p` | Peach | `(1.0, 0.8, 0.6)` |
| `^q` | Rose | `(1.0, 0.4, 0.4)` |
| `^r` | Navy | `(0.1, 0.1, 0.5)` |
| `^s` | Silver | `(0.75, 0.75, 0.75)` |
| `^t` | Tan | `(0.82, 0.71, 0.55)` |
| `^u` | Indigo | `(0.3, 0.0, 0.5)` |
| `^v` | Violet | `(0.56, 0.0, 1.0)` |
| `^w` | Wheat | `(0.96, 0.87, 0.7)` |
| `^x` | Dark Cyan | `(0.0, 0.4, 0.4)` |
| `^y` | Lime | `(0.6, 1.0, 0.2)` |
| `^z` | Coral | `(1.0, 0.5, 0.31)` |

**Gesamt: 36 Farben** (10 Quake III + 26 Erweiterungen)

---

## Properties

| Property | Typ | Beschreibung |
|----------|-----|-------------|
| `Atlas` | `Texture2D` | Bigchars Atlas (16×16 Grid, 256 Zeichen) |
| `Text` | `string` | Text-Inhalt mit Color-Codes |
| `CharWidth` | `float` | Glyph-Breite in Pixel (Default: 24) |
| `CharHeight` | `float` | Glyph-Höhe in Pixel (Default: 24) |
| `MaxWidth` | `float` | Maximale Breite (Optional, 0 = unbegrenzt) |
| `ShowCursor` | `bool` | Zeigt blinkenden Cursor (Console-Eingabe) |

---

## Rendering (Single-Mesh)

Jeder sichtbare Character wird als `GlyphInfo` struct gecacht:
```
struct GlyphInfo
{
    int AsciiCode;     // Atlas-Position (0-255)
    Color32 Tint;      // Farbe aus Color-Map
}
```

- **Parse-Phase:** Text wird in `m_Glyphs` Liste zerlegt (Escape-Sequences ausgewertet)
- **Render-Phase:** Pro Glyph wird ein Sub-Sprite aus dem Atlas gecroppt + eingefärbt
- **Rebuild:** Nur bei Textänderung oder Atlas-Wechsel

---

## Verwendung in Systemen

| System | Größe | Verwendung |
|--------|-------|-----------|
| **Scoreboard** (Spielernamen) | 13×20px | `QuakeColorLabel` pro Player-Row |
| **Gametype Messages** | 16×24px | Stun-Broadcasts: `"You stunned ^1SniperWolf^7!"` |
| **Editor Mock-Spieler** | 13×20px | Test-Namen: `^1Sgt^7.Mayhem`, `^4Fr0stBit3` |
| **Console** (geplant) | variabel | Eingabefeld mit Cursor-Blink |

---

## Cursor-Blink (Console)

| Konstante | Wert |
|-----------|------|
| `k_CursorBlinkMs` | 530ms |

- `m_CursorElement` — Visuelles Cursor-Element
- `m_CursorBlink` — Geplanter Blink-Task via `schedule.Execute()`
- Nur aktiv wenn `ShowCursor = true`

---

## Beispiele

```
"^1Sgt^7.Mayhem"          → "Sgt" in Rot, ".Mayhem" in Weiß
"^4Fr0stBit3"             → "Fr0stBit3" in Blau
"You stunned ^1Wolf^7!"   → "You stunned " in Weiß, "Wolf" in Rot, "!" in Weiß
"^^escaped"               → "^escaped" in Default-Farbe
"\41\42\43"               → Zeichen A, B, C als Atlas-Sprites
```
