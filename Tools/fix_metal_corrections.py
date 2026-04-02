#!/usr/bin/env python3
"""
Final targeted corrections for metal effect wrongly-modified segments.
1. Remove wrongly-added "size" from tail segments (seg[0]) in 3 metal effects
2. Restore spark particle (seg[1]) original sizes in 3 metal effects
"""

import json
import os

EFFECTS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                           "Assets", "Resources", "Data", "Effects")

METAL_FILE = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_metal.json")

# Original sizes for spark particles (before the wrong ÷2)
ORIGINAL_SPARK_SIZES = {
    "effects/impact_metal_belt": {
        "startMin": 0.04445, "startMax": 0.0762,
        "endMin": 0.02032, "endMax": 0.01524
    },
    "effects/impact_metal_osprey": {
        "startMin": 0.0381, "startMax": 0.06985
    },
    "effects/impact_metal-h_belt": {
        "startMin": 0.04445, "startMax": 0.0762,
        "endMin": 0.02032, "endMax": 0.01524
    },
}


def main():
    with open(METAL_FILE, "r", encoding="utf-8") as f:
        effects = json.load(f)

    total_fixes = 0
    for effect in effects:
        eid = effect["id"]
        if eid not in ORIGINAL_SPARK_SIZES:
            continue

        segs = effect["segments"]

        # Fix 1: Remove wrongly-added "size" from tail seg[0]
        if segs[0]["type"] == "tail" and "size" in segs[0]:
            del segs[0]["size"]
            print(f"  {eid} seg[0] (tail): removed wrongly-added 'size' key")
            total_fixes += 1

        # Fix 2: Restore spark particle seg[1] original sizes
        if segs[1]["type"] == "particle":
            old_size = dict(segs[1].get("size", {}))
            segs[1]["size"] = ORIGINAL_SPARK_SIZES[eid]
            new_size = segs[1]["size"]
            print(f"  {eid} seg[1] (spark): {old_size} -> {new_size}")
            total_fixes += 1

    with open(METAL_FILE, "w", encoding="utf-8") as f:
        json.dump(effects, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print(f"\nApplied {total_fixes} corrections to metal effects.")


if __name__ == "__main__":
    main()
