#!/usr/bin/env python3
"""
Correct wrongly-modified segments from fix_remaining_oversized.py.
That script matched ALL segments with the same name; some effects had 
multiple "particle" segments where only the larger one needed fixing.

This restores the small segments that were incorrectly divided.
"""

import json
import os

EFFECTS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                           "Assets", "Resources", "Data", "Effects")

fixes = []


def restore(filepath, effect_id, seg_index, correct_size):
    """Record a restoration to apply."""
    fixes.append((filepath, effect_id, seg_index, correct_size))


def apply():
    by_file = {}
    for fp, eid, si, cs in fixes:
        if fp not in by_file:
            by_file[fp] = []
        by_file[fp].append((eid, si, cs))

    total = 0
    for fp, items in by_file.items():
        with open(fp, "r", encoding="utf-8") as f:
            effects = json.load(f)

        for eid, seg_idx, correct_size in items:
            for effect in effects:
                if effect["id"] != eid:
                    continue
                seg = effect["segments"][seg_idx]
                old = dict(seg.get("size", {}))
                if "size" not in seg:
                    seg["size"] = {}
                seg["size"].update(correct_size)
                name = seg.get("name", seg.get("type", "?"))
                print(f"  RESTORE {eid} seg[{seg_idx}] ({name}):")
                print(f"    was: {old}")
                print(f"    now: {seg['size']}")
                total += 1

        with open(fp, "w", encoding="utf-8") as f:
            json.dump(effects, f, indent=2, ensure_ascii=False)
            f.write("\n")

    return total


def main():
    gore_blood = os.path.join(EFFECTS_DIR, "SoF2_Effects_gore_blood.json")
    metal = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_metal.json")
    wood = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_wood.json")
    glass = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_glass.json")
    dirt = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_dirt.json")

    # blood_spurt_mp: segment[0] was blood droplets (0.03-0.05m), wrongly ÷4
    # Original: startMin=0.0305, startMax=0.0508, endMin=0.0381, endMax=0.0457
    restore(gore_blood, "effects/blood_spurt_mp", 0, {
        "startMin": 0.0305, "startMax": 0.0508,
        "endMin": 0.0381, "endMax": 0.0457
    })

    # impact_metal_belt: segment[0] was sparks (tiny), wrongly ÷2
    # Original: startMin=0.04445, startMax=0.0762, endMin=0.02032, endMax=0.01524
    restore(metal, "effects/impact_metal_belt", 0, {
        "startMin": 0.04445, "startMax": 0.0762,
        "endMin": 0.02032, "endMax": 0.01524
    })

    # impact_metal_osprey: segment[0] was sparks (tiny), wrongly ÷2
    # Original: startMin=0.0381, startMax=0.06985
    restore(metal, "effects/impact_metal_osprey", 0, {
        "startMin": 0.0381, "startMax": 0.06985
    })

    # impact_metal-h_belt: segment[0] was sparks (tiny), wrongly ÷2
    # Original: startMin=0.04445, startMax=0.0762, endMin=0.02032, endMax=0.01524
    restore(metal, "effects/impact_metal-h_belt", 0, {
        "startMin": 0.04445, "startMax": 0.0762,
        "endMin": 0.02032, "endMax": 0.01524
    })

    # impact_wood_belt: segment[0] was wood chips (small), wrongly ÷2
    # Original: startMin=0.0762, startMax=0.1143, endMin=0.01905, endMax=0.0254
    restore(wood, "effects/impact_wood_belt", 0, {
        "startMin": 0.0762, "startMax": 0.1143,
        "endMin": 0.01905, "endMax": 0.0254
    })

    # impact_glass: segment[0] was glass shards (tiny), wrongly ÷2
    # Original: startMin=0.0127, startMax=0.0635
    restore(glass, "effects/impact_glass", 0, {
        "startMin": 0.0127, "startMax": 0.0635
    })

    # impact_dirt_belt: segment[0] was dirt bits (0.25m start → 0.6-0.8m end), wrongly ÷2
    # These sizes were borderline (not >1m), should not have been changed
    # Original: startMin=0.254, startMax=0.3048, endMin=0.6096, endMax=0.8128
    restore(dirt, "effects/impact_dirt_belt", 0, {
        "startMin": 0.254, "startMax": 0.3048,
        "endMin": 0.6096, "endMax": 0.8128
    })

    print("=== RESTORING WRONGLY-MODIFIED SEGMENTS ===")
    total = apply()
    print(f"\nRestored {total} segments.")


if __name__ == "__main__":
    main()
