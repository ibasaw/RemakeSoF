"""
Add reloadSounds to SoF2_Weapons_New.json.
Sound events use normalized time (0.0 - 1.0) within the reload animation / phase.
Times are estimated based on typical SoF2 reload animation flow.
"""
import json

FILE = r"D:\RemakeSoF\Assets\Resources\Data\SoF2_Weapons_New.json"

with open(FILE, "r", encoding="utf-8") as f:
    data = json.load(f)

weapons = {w["id"]: w for w in data}

# ============================================================
# Standard Reloads (single mp_reload animation)
# events: normalized time within the full reload animation
# ============================================================
STANDARD_RELOADS = {
    "m4": {
        # M4: 36 frames @ 20fps = 1.8s
        # clipOut early, clipIn mid, boltPull late, boltRelease near end
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.50, "sound": "clipIn"},
            {"time": 0.72, "sound": "boltPull"},
            {"time": 0.85, "sound": "boltRelease"},
        ]
    },
    "ak74": {
        # AK74: 36 frames @ 20fps = 1.8s
        # clipOut, clipIn, slap (AK-style mag slap), slideRelease
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.50, "sound": "clipIn"},
            {"time": 0.75, "sound": "slap"},
            {"time": 0.88, "sound": "slideRelease"},
        ]
    },
    "msg90a1": {
        # MSG90A1: 36 frames @ 20fps = 1.8s
        # clipOut, clipIn, slap (bolt handle slap)
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.55, "sound": "clipIn"},
            {"time": 0.82, "sound": "slap"},
        ]
    },
    "m60": {
        # M60: 51 frames @ 20fps = 2.55s
        # latchOpen, ammoBelt, latchClose, reloadFinal
        "events": [
            {"time": 0.10, "sound": "latchOpen"},
            {"time": 0.45, "sound": "ammoBelt"},
            {"time": 0.72, "sound": "latchClose"},
            {"time": 0.90, "sound": "reloadFinal"},
        ]
    },
    "usas12": {
        # USAS-12: 37 frames @ 20fps = 1.85s
        # clipOut, clipIn, pat (mag pat), slideRelease
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.50, "sound": "clipIn"},
            {"time": 0.68, "sound": "pat"},
            {"time": 0.85, "sound": "slideRelease"},
        ]
    },
    "ussocom": {
        # US SOCOM: 29 frames @ 20fps = 1.45s
        # clipOut, clipIn, slideBack, slideRelease, hammerBack
        "events": [
            {"time": 0.12, "sound": "clipOut"},
            {"time": 0.42, "sound": "clipIn"},
            {"time": 0.62, "sound": "slideBack"},
            {"time": 0.78, "sound": "slideRelease"},
            {"time": 0.90, "sound": "hammerBack"},
        ]
    },
    "m1911a1": {
        # M1911A1: 29 frames @ 20fps = 1.45s
        # clipOut, clipIn, slideRelease, hammerBack
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.48, "sound": "clipIn"},
            {"time": 0.72, "sound": "slideRelease"},
            {"time": 0.88, "sound": "hammerBack"},
        ]
    },
    "microuzi": {
        # Micro Uzi: 29 frames @ 20fps = 1.45s
        # clipOut, clipIn, bolt
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.55, "sound": "clipIn"},
            {"time": 0.82, "sound": "bolt"},
        ]
    },
    "oicw": {
        # OICW: 36 frames @ 20fps = 1.8s
        # clipOut, clipIn, bolt, boltForward
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.48, "sound": "clipIn"},
            {"time": 0.70, "sound": "bolt"},
            {"time": 0.85, "sound": "boltForward"},
        ]
    },
    "m3a1": {
        # M3A1 Grease Gun: 36 frames @ 20fps = 1.8s
        # coverUp, clipOut, clipIn, coverDown, boltBack, boltForward
        "events": [
            {"time": 0.08, "sound": "coverUp"},
            {"time": 0.20, "sound": "clipOut"},
            {"time": 0.48, "sound": "clipIn"},
            {"time": 0.65, "sound": "coverDown"},
            {"time": 0.78, "sound": "boltBack"},
            {"time": 0.88, "sound": "boltForward"},
        ]
    },
    "mp5": {
        # MP5: 36 frames @ 20fps = 1.8s
        # clipOut, clipIn, bolt
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.55, "sound": "clipIn"},
            {"time": 0.82, "sound": "bolt"},
        ]
    },
    "silver_talon": {
        # Silver Talon: 29 frames @ 20fps = 1.45s (pistol reload)
        # clipOut, clipIn, slideRelease, hammerBack
        "events": [
            {"time": 0.15, "sound": "clipOut"},
            {"time": 0.48, "sound": "clipIn"},
            {"time": 0.72, "sound": "slideRelease"},
            {"time": 0.88, "sound": "hammerBack"},
        ]
    },
}

# ============================================================
# Shell-by-Shell Reloads (3 phases: start, shell, end)
# Each phase has its own events with normalized time within that phase
# ============================================================
SHELL_RELOADS = {
    "m590": {
        # M590 Shotgun: Start 8fr, Shell 2fr, End 8fr @ 20fps
        "startEvents": [
            # Pump open at start of reload
        ],
        "shellEvents": [
            # Each shell insert
            {"time": 0.4, "sound": "reload"},
        ],
        "endEvents": [
            # Pump back and forward to chamber
            {"time": 0.25, "sound": "pumpBack"},
            {"time": 0.65, "sound": "pumpForward"},
        ]
    },
    "mm1": {
        # MM1 Grenade Launcher: Start 24fr, Shell 2fr, End 13fr @ 20fps
        "startEvents": [
            {"time": 0.3, "sound": "drumOpen"},
        ],
        "shellEvents": [
            {"time": 0.4, "sound": "reload"},
        ],
        "endEvents": [
            {"time": 0.5, "sound": "drumClose"},
        ]
    },
}

total_added = 0

for weapon_id, reload_def in STANDARD_RELOADS.items():
    weapon = weapons.get(weapon_id)
    if not weapon:
        print(f"  WARNING: Weapon '{weapon_id}' not found!")
        continue
    
    events = reload_def.get("events", [])
    if not events:
        print(f"  SKIP: {weapon_id} — no reload sound events")
        continue
    
    weapon["reloadSounds"] = {"events": events}
    total_added += 1
    print(f"  + {weapon_id}: {len(events)} reload sound events (standard)")

for weapon_id, reload_def in SHELL_RELOADS.items():
    weapon = weapons.get(weapon_id)
    if not weapon:
        print(f"  WARNING: Weapon '{weapon_id}' not found!")
        continue
    
    rs = {}
    if reload_def.get("startEvents"):
        rs["startEvents"] = reload_def["startEvents"]
    if reload_def.get("shellEvents"):
        rs["shellEvents"] = reload_def["shellEvents"]
    if reload_def.get("endEvents"):
        rs["endEvents"] = reload_def["endEvents"]
    
    if rs:
        weapon["reloadSounds"] = rs
        total_added += 1
        s = sum(len(v) for v in rs.values())
        print(f"  + {weapon_id}: {s} reload sound events (shell-reload)")

with open(FILE, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=4, ensure_ascii=False)

print(f"\nTotal weapons with reload sounds: {total_added}")
print("File saved successfully.")
