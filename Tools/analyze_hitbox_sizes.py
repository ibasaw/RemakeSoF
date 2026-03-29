"""
Analyzes SoF2 character hitbox width factors against body proportions.

SoF2 character models are ~1.8m tall (standing).
The hitbox sizes are computed from bone-to-bone distances × widthFactor.

This script checks whether the current widthFactors produce anatomically
plausible hitbox widths for a typical human of ~1.8m height.

Reference human proportions (approximate):
- Head width: ~0.16m (8.9% of height)
- Neck width: ~0.10m
- Chest width: ~0.38m (21% of height)
- Gut/waist width: ~0.32m
- Groin/hip width: ~0.35m
- Upper arm width: ~0.09m
- Forearm width: ~0.07m
- Hand width: ~0.08m (palm)
- Thigh width: ~0.15m
- Calf width: ~0.10m
- Foot width: ~0.09m, length: ~0.26m
"""

# Approximate bone-to-bone distances in meters for standard SoF2 model (~1.8m)
# These are estimates based on typical game character skeleton proportions

BONE_DISTANCES = {
    "Head (cranium→cervical)":       0.22,   # skull height
    "Neck (cervical→thoracic)":      0.08,   # short neck segment
    "Chest (thoracic→upperLumbar)":  0.25,   # rib cage height
    "Gut (upperLumbar→lowerLumbar)": 0.15,   # abdomen
    "Groin (lowerLumbar→pelvis)":    0.12,   # pelvis height

    "Shoulder (clavical→humerus)":   0.15,   # collarbone to shoulder joint
    "UpperArm (humerus→radius)":     0.28,   # bicep/upper arm
    "Hand (radius→hand)":           0.25,   # forearm

    "Thigh (femurYZ→tibia)":         0.42,   # upper leg
    "Calf (tibia→tarsal)":           0.38,   # lower leg
    "Foot (parent dist)":            0.38,   # used for estimation only
}

# Current widthFactors from ClientHitboxSystem.cs
HITBOX_PARAMS = [
    # (Name, bone_key, widthFactor, lengthScale, is_end_bone, lengthFactor)
    ("Head",           "Head (cranium→cervical)",       0.85, 1.3,  False, None),
    ("Neck",           "Neck (cervical→thoracic)",      0.25, 1.0,  False, None),
    ("Chest",          "Chest (thoracic→upperLumbar)",  0.65, 1.0,  False, None),
    ("Gut",            "Gut (upperLumbar→lowerLumbar)", 0.55, 1.0,  False, None),
    ("Groin",          "Groin (lowerLumbar→pelvis)",    0.45, 1.0,  False, None),

    ("Shoulder",       "Shoulder (clavical→humerus)",   0.22, 1.0,  False, None),
    ("UpperArm",       "UpperArm (humerus→radius)",     0.22, 0.85, False, None),
    ("Hand",           "Hand (radius→hand)",            0.18, 0.75, False, None),

    ("Thigh",          "Thigh (femurYZ→tibia)",         0.25, 1.0,  False, None),
    ("Calf",           "Calf (tibia→tarsal)",           0.18, 0.9,  False, None),
    ("Foot",           "Foot (parent dist)",            0.20, None, True,  0.28),
]

# Approximate real human body part widths for comparison (in meters, ~1.8m tall person)
HUMAN_WIDTHS = {
    "Head":      0.18,   # head diameter (side-to-side)
    "Neck":      0.12,   # neck diameter
    "Chest":     0.36,   # torso width at chest
    "Gut":       0.32,   # torso width at waist
    "Groin":     0.30,   # hip width

    "Shoulder":  0.10,   # shoulder joint diameter
    "UpperArm":  0.10,   # upper arm diameter
    "Hand":      0.08,   # hand width (palm)

    "Thigh":     0.15,   # thigh diameter
    "Calf":      0.10,   # calf diameter
    "Foot":      0.09,   # foot width (side-to-side)
}

def analyze():
    print("=" * 85)
    print(f"{'Region':<15} {'BoneDist':>8} {'LenScale':>8} {'ScaledLen':>9} {'WidthFac':>8} {'BoxWidth':>8} {'Human~':>7} {'Ratio':>6} {'Status':>10}")
    print("=" * 85)

    for name, bone_key, width_factor, length_scale, is_end, length_factor in HITBOX_PARAMS:
        bone_dist = BONE_DISTANCES[bone_key]

        if is_end:
            # End bone uses parent bone distance × lengthFactor
            estimated_len = bone_dist * length_factor
            box_width = estimated_len * width_factor
            scaled_len = estimated_len
        else:
            scaled_len = bone_dist * length_scale
            box_width = scaled_len * width_factor

        human_w = HUMAN_WIDTHS[name]
        ratio = box_width / human_w

        if ratio < 0.6:
            status = "TOO SMALL"
        elif ratio > 1.6:
            status = "TOO BIG"
        elif ratio < 0.8:
            status = "small"
        elif ratio > 1.3:
            status = "big"
        else:
            status = "OK"

        print(f"{name:<15} {bone_dist:>7.3f}m {(length_scale or length_factor):>7.2f}  {scaled_len:>8.4f}m {width_factor:>7.2f}  {box_width:>7.4f}m {human_w:>6.3f}m {ratio:>5.1f}x  {status:>9}")

    print("=" * 85)
    print()
    print("Legend:")
    print("  BoneDist   = estimated distance between two bones (meters)")
    print("  LenScale   = lengthScale multiplier")
    print("  ScaledLen  = BoneDist × LenScale (= box height/Y)")
    print("  WidthFac   = widthFactor multiplier")
    print("  BoxWidth   = ScaledLen × WidthFac (= box X and Z)")
    print("  Human~     = approximate real human body part width")
    print("  Ratio      = BoxWidth / Human~ (1.0 = anatomically accurate)")
    print("  Status     = <0.6x TOO SMALL, 0.6-0.8 small, 0.8-1.3 OK, 1.3-1.6 big, >1.6 TOO BIG")


if __name__ == "__main__":
    analyze()
