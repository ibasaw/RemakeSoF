#!/usr/bin/env python3
"""
Replace hand-crafted blood_spurt_arterial and blood_spurt_arterial_small
in SoF2_Effects_gore.json with authentic SoF2 data from the already-converted
arterial_squirt entries in SoF2_Effects_gore_blood.json.

Original SoF2 .efx files:
- blood_spurt_arterial_mp.efx = FxRunner sequence: arterial_squirt_long_mp -> med -> small
- blood_spurt_arterial_small_mp.efx = FxRunner sequence: arterial_squirt_med_mp -> small

Since our JSON format doesn't support FxRunner sequences, we use the primary
(first/strongest) sub-effect data for each:
- blood_spurt_arterial -> arterial_squirt_long_mp data (strongest initial squirt)
- blood_spurt_arterial_small -> arterial_squirt_small_mp data (weakest squirt)
"""

import json
import copy
import os

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)
EFFECTS_DIR = os.path.join(PROJECT_ROOT, "Assets", "Resources", "Data", "Effects")

GORE_JSON = os.path.join(EFFECTS_DIR, "SoF2_Effects_gore.json")
GORE_BLOOD_JSON = os.path.join(EFFECTS_DIR, "SoF2_Effects_gore_blood.json")


def find_effect(effects, effect_id):
    """Find an effect entry by its id field."""
    for i, effect in enumerate(effects):
        if effect["id"] == effect_id:
            return i, effect
    return -1, None


def main():
    # Read both JSON files
    with open(GORE_JSON, "r", encoding="utf-8") as f:
        gore_effects = json.load(f)

    with open(GORE_BLOOD_JSON, "r", encoding="utf-8") as f:
        gore_blood_effects = json.load(f)

    # Find source effects in gore_blood.json
    _, squirt_long = find_effect(gore_blood_effects, "effects/arterial_squirt_long_mp")
    _, squirt_small = find_effect(gore_blood_effects, "effects/arterial_squirt_small_mp")

    if squirt_long is None:
        print("ERROR: Could not find effects/arterial_squirt_long_mp in gore_blood.json")
        return
    if squirt_small is None:
        print("ERROR: Could not find effects/arterial_squirt_small_mp in gore_blood.json")
        return

    # Find target effects in gore.json
    idx_arterial, old_arterial = find_effect(gore_effects, "effects/blood_spurt_arterial")
    idx_arterial_small, old_arterial_small = find_effect(gore_effects, "effects/blood_spurt_arterial_small")

    if idx_arterial < 0:
        print("ERROR: Could not find effects/blood_spurt_arterial in gore.json")
        return
    if idx_arterial_small < 0:
        print("ERROR: Could not find effects/blood_spurt_arterial_small in gore.json")
        return

    # Print old data for comparison
    print("=== BEFORE ===")
    print(f"blood_spurt_arterial: {len(old_arterial['segments'])} segments")
    for seg in old_arterial["segments"]:
        print(f"  - {seg.get('type', '?')}: {seg.get('name', '?')}")
    print(f"blood_spurt_arterial_small: {len(old_arterial_small['segments'])} segments")
    for seg in old_arterial_small["segments"]:
        print(f"  - {seg.get('type', '?')}: {seg.get('name', '?')}")

    # Replace blood_spurt_arterial with arterial_squirt_long_mp data
    new_arterial = copy.deepcopy(squirt_long)
    new_arterial["id"] = "effects/blood_spurt_arterial"
    new_arterial["displayName"] = "Blood Spurt Arterial (MP)"
    gore_effects[idx_arterial] = new_arterial

    # Replace blood_spurt_arterial_small with arterial_squirt_small_mp data
    new_arterial_small = copy.deepcopy(squirt_small)
    new_arterial_small["id"] = "effects/blood_spurt_arterial_small"
    new_arterial_small["displayName"] = "Blood Spurt Arterial Small (MP)"
    gore_effects[idx_arterial_small] = new_arterial_small

    # Print new data
    print("\n=== AFTER ===")
    print(f"blood_spurt_arterial: {len(new_arterial['segments'])} segments (from arterial_squirt_long_mp)")
    for seg in new_arterial["segments"]:
        vel = seg.get("particle", {}).get("velocityMin", [])
        print(f"  - {seg.get('type', '?')}: {seg.get('name', '?')} vel={vel}")
    print(f"blood_spurt_arterial_small: {len(new_arterial_small['segments'])} segments (from arterial_squirt_small_mp)")
    for seg in new_arterial_small["segments"]:
        vel = seg.get("particle", {}).get("velocityMin", [])
        print(f"  - {seg.get('type', '?')}: {seg.get('name', '?')} vel={vel}")

    # Write back
    with open(GORE_JSON, "w", encoding="utf-8") as f:
        json.dump(gore_effects, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print("\nDone! Updated SoF2_Effects_gore.json with authentic SoF2 arterial data.")


if __name__ == "__main__":
    main()
