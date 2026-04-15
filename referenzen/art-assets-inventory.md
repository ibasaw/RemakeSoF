# Art/Assets Inventar — RemakeSoF

## Verzeichnisstruktur: Assets/Art/

```
Art/
├── Characters/       # 3D Character Models (FBX)
├── Chunks/           # Breakable-Debris (Ice, Metal, Rock, Wood)
├── Maps/             # Map-Geometrie (FBX pro Map)
├── Objects/          # Umgebungsobjekte (aktuell nur Fence/)
├── sound/            # Audio (MP3/WAV)
└── Textures/         # Alle Texturen
    ├── colors/       # Einfarbige Utility-Texturen
    ├── gfx/          # Effekte, Decals, UI, Sprites
    ├── models/       # Character, Waffen, Objekt-Texturen
    ├── textures/     # Map-Texturen (pro Location)
    └── UI/           # UI-Elemente (Console, Metagame)
```

## Characters (Assets/Art/Characters/)
| Model | Datei |
|-------|-------|
| Average Armor | average_armor.fbx |
| Average Sleeves | average_sleeves.fbx |
| Chem Suit | chem_suit.fbx |
| Dog | dog.fbx |
| Fat | fat.fbx |
| Female Armor | female_armor.fbx |
| Female Pants | female_pants.fbx |
| Female Skirt | female_skirt.fbx |
| Osprey | osprey.fbx |
| Snow | snow.fbx |
| Suit Long Coat | suit_long_coat.fbx |
| Suit Sleeves | suit_sleeves.fbx |
| Test Male | test_male.fbx |
| **Gore**: brain, guts, lung, bones, chunks, hand, stomach, etc. (16 Gibs) |
| **Hands**: lhand.fbx, rhand.fbx (Viewmodels) |
| **Bolt-Ons**: wine_bottle.fbx |

## Weapons (Assets/Art/Weapons/) — 24 Waffen
Jede Waffe hat World-Model + View-Model (z.B. `m4.fbx` + `m4view.fbx`):
ak74, anm14, f1, knife, l2a2, m15, m1911a1, m3a1, m4, m590, m60, m67, m84,
mdn11, microuzi, mm1, mp5, msg90a1, oicw, rpg7, sig551, silver_talon, smohg92,
usas12, ussocom + m203 (world only) + Shells/

## Maps (Assets/Art/Maps/) — 9 MP-Maps + inf
mp_barn, mp_col1, mp_finca, mp_hos1, mp_jor1, mp_kam2, mp_kam3, mp_pra1, inf

**Fehlende Maps** (aus SoF2 Original): mp_shop, mp_raven, mp_arm1, mp_kok1, mp_air1 u.a.

## Texturen — Character-Skins
Location: `Textures/models/characters/`
Alle Body-Models haben Texturen: average_armor, average_face, average_sleeves,
bolt_ons, caps, chem_suit, dog, fat, female_armor, female_face, female_pants,
female_skirt, gore, osprey, snow, suit_long_coat, suit_sleeves

## Texturen — Waffen
Location: `Textures/models/weapons/`
Alle 24+ Waffen haben Textur-Ordner (inkl. bayonet, m2hbqcb, rpd die keine FBX haben)

## Texturen — Maps
Location: `Textures/textures/`
20+ Locations: airport, armory, cemetery, colombia, common, finca, hongkong,
hospital, jordan, kamchatka, liner, prague, shop, skies, rmg_skies, threewave, tools, etc.

## Texturen — Effekte/Sprites
Location: `Textures/gfx/`
- **misc/**: ~150+ Partikel-Texturen (Muzzle Flash, Explosionen, Rauch, Feuer, Funken, Blut, Debris)
- **damage/**: Einschuss-Decals, Blutflecke, Scorch Marks, Wood/Rock Chunks
- **sprites/**: Vegetation, Regen, Wasser, Bäume
- **decals/**: Impact + Steps
- **2d/**: HUD, Bigchars Font

## Texturen — Sonstige
- **Pick-Ups** (`models/pick_ups/`): Ammo, Health, Flak Jacket, Waffen-Boxen, RMG Items
- **Flags** (`models/flags/`): CTF Red/Blue + Post
- **Items** (`models/Items/`): Binoculars, Nightvision, Thermal
- **Objects** (`models/objects/`): Pro Location (Colombia, Hospital, etc.) — Texturen für Map-Props
- **Menu** (`models/menu/`): Menü-Texturen
- **Colors** (`colors/`): Einfarbige Utility-Texturen (Schwarz, Weiß, Rot, Blau, etc.)

## Sound (Assets/Art/sound/)
- **weapons/**: Alle 24 Waffen-Sounds (Fire, Reload, etc.)
- **pain_death/**: Male, Female, Mullins Varianten
- **player/**: Steps, Jumps, Knife, Pickup, Bullet Impacts
- **effects/**: Glass Break/Tumble
- **ambience/**: Umgebungs-Sounds
- **npc/**: NPC-Sounds
- **radio/**: Radio-Durchsagen
- **misc/**: Sonstige
- **movers/**: Tür/Aufzug-Sounds
- CTF-Sounds: ctf_base, ctf_flag, ctf_return, ctf_win
- Sonstige: frag, self_frag, item_respawn, player_respawn

## Chunks (Assets/Art/Chunks/)
Breakable-Debris-Texturen für: Ice, Metal, Rock, Wood

## Objects (Assets/Art/Objects/)
Aktuell nur: Fence/ (für HideAndSeek Cage)

## Lücken / Erweiterungsmöglichkeiten
1. **Fehlende Maps**: ~6+ SoF2 MP-Maps nicht konvertiert
2. **Zerstörbare Props**: 3D-Modelle für Kisten, Fässer, Möbel etc. fehlen (Texturen vorhanden)
3. **Waffen ohne Model**: bayonet, rpd, m2hbqcb haben Texturen aber kein FBX
4. **m203 Viewmodel**: m203view.fbx fehlt (Unterlauf-Aufsatz)
5. **Vehicles**: Nur Osprey — Trucks, Boote fehlen als 3D
6. **Map-Ladebilder**: Spezifische Loading Screens

## Status: Vollständig für SoF2 MP ✅
Alle wesentlichen Assets für Multiplayer vorhanden. Lücken betreffen zusätzliche Maps und Props.
