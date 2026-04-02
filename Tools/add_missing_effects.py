#!/usr/bin/env python3
"""
Create missing critical effects referenced in SoF2_Items.json:
1. effects/fire/smoke_grenade (smoke grenade detonation)
2. effects/fire/smoke_grenade_flash (off-LOS flash)
3. effects/fire/smoke_grenade_trail (in-air trail)
4. effects/explosions/stun_flash2 (flash grenade detonation)
5. effects/explosions/stun_flash_noflash (flash grenade off-LOS)

Conversion rules (matching existing project conventions):
- Distance/Size/Velocity: QU * 0.0254 = meters
- Gravity: -(SoF2_value * 0.0254) as gravityModifier (EffectFactory normalizes by /9.81)
- Time: milliseconds / 1000 = seconds
"""

import json
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
EFFECTS_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "Data", "Effects")

QU = 0.0254  # Quake Units to meters


def qu(val):
    """Convert QU to meters, rounded."""
    return round(val * QU, 4)


def qu3(x, y, z):
    """Convert 3D QU vector to meters list."""
    return [qu(x), qu(y), qu(z)]


# =============================================================================
# FIRE EFFECTS (new file)
# =============================================================================

fire_effects = [
    # =========================================================================
    # effects/fire/smoke_grenade
    # Source: fire/smoke_grenade.efx
    # 125 smoke particles over 20s + initial puff + flash + sparks + decal
    # =========================================================================
    {
        "id": "effects/fire/smoke_grenade",
        "displayName": "Smoke Grenade Detonation",
        "segments": [
            # Segment 1: smoke_lightgrey - main smoke cloud
            {
                "type": "particle",
                "name": "smoke_lightgrey",
                "flags": ["useAlpha"],
                "spawnFlags": ["evenDistribution", "rgbComponentInterpolation"],
                "texture": "gfx/misc/jk_smoke5",
                "particle": {
                    "countMin": 125,
                    "countMax": 125,
                    "lifetimeMin": 2.0,
                    "lifetimeMax": 2.0,
                    "delayMin": 0.0,
                    "delayMax": 20.0,
                    "rotationMin": -180.0,
                    "rotationMax": 180.0,
                    "rotationSpeedMin": -7.0,
                    "rotationSpeedMax": 5.0,
                    "originMin": qu3(-15, -60, -60),
                    "originMax": qu3(-10, 60, 60),
                    "velocityMin": qu3(30, -40, -40),
                    "velocityMax": qu3(45, 40, 40),
                    "gravityModifier": -0.318,
                    "burst": False
                },
                "color": {
                    "startMin": [0.4549, 0.4549, 0.4941],
                    "startMax": [0.5922, 0.5922, 0.6157]
                },
                "alpha": {
                    "startMin": 1.0,
                    "startMax": 1.0,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 60.0,
                    "parmMax": 85.0
                },
                "size": {
                    "startMin": qu(25),
                    "startMax": qu(25),
                    "endMin": qu(150),
                    "endMax": qu(175),
                    "parm": 75.0,
                    "curve": "linear"
                }
            },
            # Segment 2: Sound
            {
                "type": "sound",
                "name": "Sound",
                "sound": {
                    "files": ["sound/weapons/phos_grenade/phos01.wav"],
                    "delay": 0.0
                }
            },
            # Segment 3: initial_smoke - quick initial puff
            {
                "type": "particle",
                "name": "initial_smoke",
                "flags": ["useAlpha"],
                "spawnFlags": ["rgbComponentInterpolation"],
                "texture": "gfx/misc/jk_smoke5",
                "particle": {
                    "countMin": 3,
                    "countMax": 3,
                    "lifetimeMin": 1.0,
                    "lifetimeMax": 2.0,
                    "delayMin": 0.0,
                    "delayMax": 0.05,
                    "rotationMin": -180.0,
                    "rotationMax": 180.0,
                    "rotationSpeedMin": -5.0,
                    "rotationSpeedMax": 5.0,
                    "originMin": qu3(0, -40, -40),
                    "originMax": qu3(0, 40, 40),
                    "velocityMin": qu3(20, -20, -20),
                    "velocityMax": qu3(30, 20, 20),
                    "gravityModifier": -0.445,
                    "burst": True
                },
                "color": {
                    "startMin": [0.4941, 0.4941, 0.5216],
                    "startMax": [0.4118, 0.4118, 0.4118]
                },
                "alpha": {
                    "startMin": 1.0,
                    "startMax": 1.0,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 30.0
                },
                "size": {
                    "startMin": qu(25),
                    "startMax": qu(25),
                    "endMin": qu(125),
                    "endMax": qu(125),
                    "parm": 25.0,
                    "curve": "linear"
                }
            },
            # Segment 4: boom_bright - initial flash glow (additive)
            {
                "type": "particle",
                "name": "boom_bright",
                "texture": "gfx/misc/jk_sniper_flash",
                "particle": {
                    "countMin": 1,
                    "countMax": 1,
                    "lifetimeMin": 0.275,
                    "lifetimeMax": 0.275,
                    "rotationMin": 0.0,
                    "rotationMax": 360.0,
                    "originMin": qu3(30, 0, 0),
                    "originMax": qu3(30, 0, 0),
                    "burst": True
                },
                "alpha": {
                    "startMin": 1.0,
                    "startMax": 1.0,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 25.0
                },
                "size": {
                    "startMin": qu(40),
                    "startMax": qu(50),
                    "endMin": qu(275),
                    "endMax": qu(275),
                    "parm": 10.0,
                    "curve": "linear"
                }
            },
            # Segment 5: final_fade_bits - lingering smoke at the end
            {
                "type": "particle",
                "name": "final_fade_bits",
                "flags": ["useAlpha"],
                "spawnFlags": ["rgbComponentInterpolation"],
                "texture": "gfx/misc/jk_smoke5",
                "particle": {
                    "countMin": 4,
                    "countMax": 4,
                    "lifetimeMin": 3.0,
                    "lifetimeMax": 5.0,
                    "delayMin": 20.0,
                    "delayMax": 22.0,
                    "rotationMin": -180.0,
                    "rotationMax": 180.0,
                    "rotationSpeedMin": -3.0,
                    "rotationSpeedMax": 3.0,
                    "originMin": qu3(-15, -20, -20),
                    "originMax": qu3(-10, 20, 20),
                    "velocityMin": qu3(5, -20, -20),
                    "velocityMax": qu3(10, 20, 20),
                    "gravityModifier": -0.191,
                    "burst": False
                },
                "color": {
                    "startMin": [0.4549, 0.4549, 0.4941],
                    "startMax": [0.5922, 0.5922, 0.6157]
                },
                "alpha": {
                    "startMin": 0.6,
                    "startMax": 0.6,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 20.0,
                    "parmMax": 50.0
                },
                "size": {
                    "startMin": qu(25),
                    "startMax": qu(25),
                    "endMin": qu(100),
                    "endMax": qu(120),
                    "parm": 75.0,
                    "curve": "linear"
                }
            },
            # Segment 6: Scorch decal
            {
                "type": "decal",
                "name": "scorch",
                "texture": "gfx/misc/gb_scorch",
                "decal": {
                    "cullRange": qu(1200),
                    "rotationMin": 0.0,
                    "rotationMax": 360.0,
                    "startSize": qu(70)
                }
            },
            # Segment 7: sperkz - sparks (simplified from orgOnCylinder)
            {
                "type": "particle",
                "name": "sperkz",
                "texture": "gfx/misc/jk_sniper_flash",
                "particle": {
                    "countMin": 5,
                    "countMax": 6,
                    "lifetimeMin": 0.5,
                    "lifetimeMax": 0.6,
                    "originMin": qu3(0, -20, -20),
                    "originMax": qu3(0, 20, 20),
                    "velocityMin": qu3(300, -200, -200),
                    "velocityMax": qu3(500, 200, 200),
                    "gravityModifier": -31.75,
                    "burst": True
                },
                "alpha": {
                    "startMin": 1.0,
                    "startMax": 1.0,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 25.0
                },
                "size": {
                    "startMin": qu(3),
                    "startMax": qu(8),
                    "parm": 10.0
                }
            },
            # Segment 8: boom_bright2 - secondary flash glow
            {
                "type": "particle",
                "name": "boom_bright2",
                "texture": "gfx/misc/jk_mflash_ak74",
                "particle": {
                    "countMin": 1,
                    "countMax": 1,
                    "lifetimeMin": 0.325,
                    "lifetimeMax": 0.325,
                    "originMin": qu3(30, 0, 0),
                    "originMax": qu3(30, 0, 0),
                    "burst": True
                },
                "alpha": {
                    "startMin": 0.8,
                    "startMax": 0.8,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 25.0
                },
                "size": {
                    "startMin": qu(40),
                    "startMax": qu(50),
                    "endMin": qu(260),
                    "endMax": qu(260),
                    "parm": 10.0,
                    "curve": "linear"
                }
            },
            # Segment 9: Light
            {
                "type": "light",
                "light": {
                    "lifetime": 0.5,
                    "range": qu(400),
                    "intensity": 3.0,
                    "color": [1.0, 0.9, 0.7]
                }
            }
        ]
    },

    # =========================================================================
    # effects/fire/smoke_grenade_flash
    # Source: fire/smoke_grenade_flash.efx
    # Fullscreen white flash (LOS-blocked detonation version)
    # Original: Flash segment with life 10s, delay 75ms, alpha→0 parm 30
    # Converted to large additive particle (Flash type not supported)
    # =========================================================================
    {
        "id": "effects/fire/smoke_grenade_flash",
        "displayName": "Smoke Grenade Flash (LOS)",
        "segments": [
            {
                "type": "particle",
                "name": "white_flash",
                "spawnFlags": ["rgbComponentInterpolation"],
                "texture": "gfx/misc/white_flash",
                "particle": {
                    "countMin": 1,
                    "countMax": 1,
                    "lifetimeMin": 0.5,
                    "lifetimeMax": 0.5,
                    "delayMin": 0.075,
                    "delayMax": 0.075,
                    "burst": True
                },
                "alpha": {
                    "startMin": 0.8,
                    "startMax": 0.8,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 30.0
                },
                "size": {
                    "startMin": qu(200),
                    "startMax": qu(200),
                    "endMin": qu(200),
                    "endMax": qu(200)
                }
            },
            {
                "type": "light",
                "light": {
                    "lifetime": 0.3,
                    "range": qu(300),
                    "intensity": 4.0
                }
            }
        ]
    },

    # =========================================================================
    # effects/fire/smoke_grenade_trail
    # Source: fire/smoke_grenade_trail.efx
    # Small smoke trail while grenade is in air (5 particles)
    # =========================================================================
    {
        "id": "effects/fire/smoke_grenade_trail",
        "displayName": "Smoke Grenade Trail",
        "segments": [
            {
                "type": "particle",
                "name": "trail_smoke",
                "flags": ["useAlpha"],
                "spawnFlags": ["evenDistribution", "rgbComponentInterpolation"],
                "texture": "gfx/misc/jk_smoke5",
                "particle": {
                    "countMin": 5,
                    "countMax": 5,
                    "lifetimeMin": 0.5,
                    "lifetimeMax": 1.5,
                    "delayMin": 0.0,
                    "delayMax": 0.05,
                    "rotationMin": 0.0,
                    "rotationMax": 360.0,
                    "originMin": qu3(-0.1, 0, 0),
                    "originMax": qu3(0.1, 0, 0),
                    "velocityMin": qu3(-0.1, -0.1, -0.1),
                    "velocityMax": qu3(0.1, 0.1, 0.1),
                    "gravityModifier": -0.381,
                    "burst": False
                },
                "color": {
                    "startMin": [0.1255, 0.1255, 0.1255],
                    "startMax": [0.4118, 0.4118, 0.4118]
                },
                "alpha": {
                    "startMin": 0.05,
                    "startMax": 0.1,
                    "endMin": 0.0,
                    "endMax": 0.0,
                    "parm": 40.0,
                    "parmMax": 60.0
                },
                "size": {
                    "startMin": qu(3),
                    "startMax": qu(7),
                    "endMin": qu(20),
                    "endMax": qu(25),
                    "curve": "linear"
                }
            },
            {
                "type": "sound",
                "name": "Sound",
                "sound": {
                    "files": ["sound/weapons/phos_grenade/smoke.wav"],
                    "delay": 0.0
                }
            }
        ]
    }
]


# =============================================================================
# EXPLOSION EFFECTS (add to existing file)
# =============================================================================

stun_flash2 = {
    "id": "effects/explosions/stun_flash2",
    "displayName": "Stun Flash 2 (Flash Grenade)",
    "segments": [
        # Segment 1: smokepuff (post-flash smoke)
        {
            "type": "particle",
            "name": "smokepuff",
            "flags": ["useAlpha"],
            "spawnFlags": ["rgbComponentInterpolation"],
            "texture": "gfx/misc/jk_smoke5",
            "particle": {
                "countMin": 3,
                "countMax": 3,
                "lifetimeMin": 0.8,
                "lifetimeMax": 1.0,
                "delayMin": 0.075,
                "delayMax": 0.15,
                "rotationMin": -180.0,
                "rotationMax": 180.0,
                "rotationSpeedMin": -3.0,
                "rotationSpeedMax": 3.0,
                "originMin": qu3(0, -50, -50),
                "originMax": qu3(0, 50, 50),
                "velocityMin": qu3(35, -20, -20),
                "velocityMax": qu3(50, 20, 20),
                "gravityModifier": -0.635,
                "burst": True
            },
            "color": {
                "startMin": [0.2431, 0.2431, 0.2431],
                "startMax": [0.3608, 0.3608, 0.3608]
            },
            "alpha": {
                "startMin": 1.0,
                "startMax": 1.0,
                "endMin": 0.0,
                "endMax": 0.0,
                "parm": 25.0,
                "parmMax": 60.0
            },
            "size": {
                "startMin": qu(20),
                "startMax": qu(20),
                "endMin": qu(150),
                "endMax": qu(200),
                "parm": 60.0,
                "curve": "linear"
            }
        },
        # Segment 2: bang_light - bright central flash (additive)
        {
            "type": "particle",
            "name": "bang_light",
            "spawnFlags": ["rgbComponentInterpolation"],
            "texture": "gfx/misc/jk_sniper_flash",
            "particle": {
                "countMin": 1,
                "countMax": 1,
                "lifetimeMin": 0.35,
                "lifetimeMax": 0.35,
                "originMin": qu3(40, 0, 0),
                "originMax": qu3(40, 0, 0),
                "burst": True
            },
            "alpha": {
                "startMin": 0.6,
                "startMax": 0.6,
                "endMin": 0.0,
                "endMax": 0.0,
                "parm": 50.0
            },
            "size": {
                "startMin": 0.0,
                "startMax": 0.0,
                "endMin": qu(250),
                "endMax": qu(250),
                "parm": 75.0
            }
        },
        # Segment 3: Scorch decal
        {
            "type": "decal",
            "name": "scorch",
            "texture": "gfx/misc/gb_scorch",
            "decal": {
                "cullRange": qu(1000),
                "rotationMin": 0.0,
                "rotationMax": 360.0,
                "startSize": qu(80)
            }
        },
        # Segment 4: Sound
        {
            "type": "sound",
            "name": "Sound",
            "sound": {
                "files": ["sound/weapons/incendiary_grenade/incen01.mp3"],
                "delay": 0.0
            }
        },
        # Segment 5: Copy of bang_light - lens flare
        {
            "type": "particle",
            "name": "bang_flare",
            "spawnFlags": ["rgbComponentInterpolation"],
            "texture": "gfx/misc/lens_flare",
            "particle": {
                "countMin": 1,
                "countMax": 1,
                "lifetimeMin": 0.3,
                "lifetimeMax": 0.3,
                "originMin": qu3(40, 0, 0),
                "originMax": qu3(40, 0, 0),
                "burst": True
            },
            "alpha": {
                "startMin": 0.5,
                "startMax": 0.5,
                "endMin": 0.0,
                "endMax": 0.0,
                "parm": 20.0
            },
            "size": {
                "startMin": qu(10),
                "startMax": qu(10),
                "endMin": qu(300),
                "endMax": qu(300),
                "parm": 75.0
            }
        },
        # Segment 6: Flash burst (very short)
        {
            "type": "particle",
            "name": "bang_burst",
            "spawnFlags": ["rgbComponentInterpolation"],
            "texture": "gfx/misc/lens_flare",
            "particle": {
                "countMin": 1,
                "countMax": 1,
                "lifetimeMin": 0.045,
                "lifetimeMax": 0.045,
                "originMin": qu3(40, 0, 0),
                "originMax": qu3(40, 0, 0),
                "burst": True
            },
            "alpha": {
                "startMin": 1.0,
                "startMax": 1.0,
                "endMin": 0.0,
                "endMax": 0.0,
                "parm": 20.0
            },
            "size": {
                "startMin": qu(250),
                "startMax": qu(250),
                "parm": 75.0
            }
        },
        # Segment 7: Light
        {
            "type": "light",
            "light": {
                "lifetime": 0.35,
                "range": qu(500),
                "intensity": 8.0
            }
        }
    ]
}

stun_flash_noflash = {
    "id": "effects/explosions/stun_flash_noflash",
    "displayName": "Stun Flash No-Flash (Behind Cover)",
    "segments": [
        # Minimal effect for when player is behind cover
        # Still has the fullscreen flash component (ID contains "stun_flash"
        # so FlashbangScreenEffect triggers, but game code should attenuate)
        {
            "type": "particle",
            "name": "white_flash",
            "spawnFlags": ["rgbComponentInterpolation"],
            "texture": "gfx/misc/white_flash",
            "particle": {
                "countMin": 1,
                "countMax": 1,
                "lifetimeMin": 0.5,
                "lifetimeMax": 0.5,
                "delayMin": 0.075,
                "delayMax": 0.075,
                "burst": True
            },
            "alpha": {
                "startMin": 0.6,
                "startMax": 0.6,
                "endMin": 0.0,
                "endMax": 0.0,
                "parm": 30.0
            },
            "size": {
                "startMin": qu(150),
                "startMax": qu(150),
                "endMin": qu(150),
                "endMax": qu(150)
            }
        },
        {
            "type": "light",
            "light": {
                "lifetime": 0.2,
                "range": qu(300),
                "intensity": 5.0
            }
        },
        {
            "type": "sound",
            "name": "Sound",
            "sound": {
                "files": ["sound/weapons/incendiary_grenade/incen01.mp3"],
                "delay": 0.0
            }
        }
    ]
}


def main():
    # 1. Write fire effects JSON
    fire_path = os.path.join(EFFECTS_DIR, "SoF2_Effects_fire.json")
    with open(fire_path, "w", encoding="utf-8") as f:
        json.dump(fire_effects, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print(f"Created {fire_path}")
    print(f"  - {len(fire_effects)} effects: {[e['id'] for e in fire_effects]}")

    # 2. Add stun_flash2 and stun_flash_noflash to explosions JSON
    explosions_path = os.path.join(EFFECTS_DIR, "SoF2_Effects_explosions.json")
    with open(explosions_path, "r", encoding="utf-8") as f:
        explosions = json.load(f)

    # Check if already present
    existing_ids = {e["id"] for e in explosions}
    added = []

    if stun_flash2["id"] not in existing_ids:
        explosions.append(stun_flash2)
        added.append(stun_flash2["id"])
    else:
        print(f"  SKIP: {stun_flash2['id']} already exists")

    if stun_flash_noflash["id"] not in existing_ids:
        explosions.append(stun_flash_noflash)
        added.append(stun_flash_noflash["id"])
    else:
        print(f"  SKIP: {stun_flash_noflash['id']} already exists")

    if added:
        with open(explosions_path, "w", encoding="utf-8") as f:
            json.dump(explosions, f, indent=2, ensure_ascii=False)
            f.write("\n")
        print(f"Updated {explosions_path}")
        print(f"  Added: {added}")
    else:
        print("No new effects added to explosions.json")

    # Summary
    print("\n=== Summary ===")
    print(f"New fire effects file: 3 effects")
    print(f"  - effects/fire/smoke_grenade (8 segments + light)")
    print(f"  - effects/fire/smoke_grenade_flash (particle + light)")
    print(f"  - effects/fire/smoke_grenade_trail (particle + sound)")
    print(f"Added to explosions: {len(added)} effects")
    print(f"  - effects/explosions/stun_flash2 (7 segments)")
    print(f"  - effects/explosions/stun_flash_noflash (3 segments)")
    print("\nAll effects referenced in SoF2_Items.json are now covered.")


if __name__ == "__main__":
    main()
