"""
Wire all 31 gore/blood effect definitions into appropriate gore_areas in SoF2_DATA.json.

Adds:
- BloodFX arrays (DamageLevel 0-3 blood effects) to each gore area
- Expanded FX arrays (DamageLevel 4-5 dismemberment effects) to each gore area
- Updated gore_effects registry with all referenced effect names
- Fixes blood_mist_small bug (torso area) by replacing with gore_mist_small
"""

import json
import sys
import os

DATA_PATH = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Data", "SoF2_DATA.json")

# === EFFECT WIRING PLAN ===
# 30 of 31 effects used (gut_model skipped: empty definition, no segments)
#
# Dismemberment FX (gore_areas.FX) = DamageLevel 4-5
# Blood FX (gore_areas.BloodFX) = DamageLevel 0-3

# Gore effects registry (Name → original .efx File)
GORE_EFFECTS_REGISTRY = [
    {"Name": "flesh_chunks", "File": "flesh_chunks_mp.efx"},
    {"Name": "gore_mist_small", "File": "gore_mist_small.efx"},
    {"Name": "gore_mist", "File": "gore_mist.efx"},
    {"Name": "gore_mist_small_vergara_head", "File": "gore_mist_small_vergara_head.efx"},
    {"Name": "gore_bit", "File": "gore_bit.efx"},
    {"Name": "gore_bloodygib", "File": "gore_bloodygib.efx"},
    {"Name": "gore_gib_body", "File": "gore_gib_body.efx"},
    {"Name": "gore_gib_small", "File": "gore_gib_small.efx"},
    {"Name": "gore_organ_splat", "File": "gore_organ_splat.efx"},
    {"Name": "gore_organ_splat_gib", "File": "gore_organ_splat_gib.efx"},
    {"Name": "blood_spurt_arterial", "File": "blood_spurt_arterial_mp.efx"},
    {"Name": "blood_spurt_arterial_small", "File": "blood_spurt_arterial_small_mp.efx"},
    {"Name": "blood_spurt_mp", "File": "blood_spurt_mp.efx"},
    {"Name": "blood_squirt_mp", "File": "blood_squirt_mp.efx"},
    {"Name": "blood_squirt_splat_mp", "File": "blood_squirt_splat_mp.efx"},
    {"Name": "mp_blood_squirt_small", "File": "mp_blood_squirt_small.efx"},
    {"Name": "blood_splat_mp", "File": "blood_splat_mp.efx"},
    {"Name": "blood_splat_mp_small", "File": "blood_splat_mp_small.efx"},
    {"Name": "blood_splat_smll_mp", "File": "blood_splat_smll_mp.efx"},
    {"Name": "blood_impact", "File": "blood_impact.efx"},
    {"Name": "blood_drip", "File": "blood_drip.efx"},
    {"Name": "blood_pool_mp", "File": "blood_pool_mp.efx"},
    {"Name": "blood_trail", "File": "blood_trail.efx"},
    {"Name": "blood_jugular_squirt", "File": "blood_jugular_squirt.efx"},
    {"Name": "arterial_squirt_long_mp", "File": "arterial_squirt_long_mp.efx"},
    {"Name": "arterial_squirt_med_mp", "File": "arterial_squirt_med_mp.efx"},
    {"Name": "arterial_squirt_small_mp", "File": "arterial_squirt_small_mp.efx"},
    {"Name": "gut_bleed", "File": "gut_bleed.efx"},
    {"Name": "head_pop_mp", "File": "head_pop_mp.efx"},
    {"Name": "head_pop_mp_temp", "File": "head_pop_mp_temp.efx"},
]

def fx(name, bolt):
    """Create an FX entry."""
    return {"Name": name, "Bolt": bolt}

# === DISMEMBERMENT FX (DamageLevel 4-5) per area ===
AREA_FX = {
    "hand": [
        fx("gore_mist_small", "*hand_<PS>g"),
        fx("blood_spurt_arterial_small", "*hand_<PS>g"),
        fx("arterial_squirt_small_mp", "*hand_<PS>g"),
        fx("mp_blood_squirt_small", "*hand_<PS>g"),
    ],
    "arm_lower": [
        fx("gore_mist_small", "*hand_<PS>g"),
        fx("blood_spurt_arterial_small", "*hand_<PS>g"),
        fx("arterial_squirt_small_mp", "*hand_<PS>g"),
        fx("gore_bit", "*hand_<PS>g"),
        fx("blood_drip", "*hand_<PS>g"),
    ],
    "arm_upper": [
        fx("gore_mist_small", "*shldr_<PS>g"),
        fx("blood_spurt_arterial", "*shldr_<PS>g"),
        fx("arterial_squirt_med_mp", "*shldr_<PS>g"),
        fx("flesh_chunks", "*shldr_<PS>g"),
        fx("gore_gib_small", "*shldr_<PS>g"),
        fx("gore_bloodygib", "*shldr_<PS>g"),
    ],
    "torso": [
        fx("gore_mist_small", "*uchest_<PS>"),
        fx("gore_organ_splat", "*uchest_<PS>"),
        fx("gore_organ_splat_gib", "*uchest_<PS>"),
        fx("gut_bleed", "*uchest_<PS>"),
        fx("blood_spurt_mp", "*uchest_<PS>"),
    ],
    "foot": [
        fx("gore_mist_small", "*foot_<PS>g"),
        fx("blood_spurt_arterial_small", "*foot_<PS>g"),
        fx("mp_blood_squirt_small", "*foot_<PS>g"),
        fx("blood_splat_smll_mp", "*foot_<PS>g"),
    ],
    "leg_lower": [
        fx("gore_mist_small", "*calf_<PS>"),
        fx("blood_spurt_arterial_small", "*calf_<PS>"),
        fx("arterial_squirt_small_mp", "*calf_<PS>"),
        fx("gore_bit", "*calf_<PS>"),
        fx("blood_trail", "*calf_<PS>"),
    ],
    "leg_upper": [
        fx("gore_mist_small", "*hip_<PS>g"),
        fx("blood_spurt_arterial", "*hip_<PS>g"),
        fx("arterial_squirt_long_mp", "*hip_<PS>g"),
        fx("flesh_chunks", "*hip_<PS>g"),
        fx("gore_gib_small", "*hip_<PS>g"),
        fx("gore_mist", "*hip_<PS>g"),
        fx("blood_pool_mp", "*hip_<PS>g"),
    ],
    "hip": [
        fx("gore_mist_small", "*hip_<PS>g"),
        fx("blood_spurt_arterial", "*hip_<PS>g"),
        fx("arterial_squirt_long_mp", "*hip_<PS>g"),
        fx("gore_organ_splat", "*hip_<PS>g"),
        fx("flesh_chunks", "*hip_<PS>g"),
        fx("gut_bleed", "*hip_<PS>g"),
        fx("gore_gib_body", "*hip_<PS>g"),
        fx("blood_trail", "*hip_<PS>g"),
    ],
    "head": [
        fx("blood_spurt_arterial", "*neckg"),
        fx("gore_mist_small", "*neckg"),
        fx("flesh_chunks", "*neckg"),
        fx("head_pop_mp", "*neckg"),
        fx("gore_bloodygib", "*neckg"),
        fx("blood_jugular_squirt", "*neckg"),
        fx("blood_squirt_splat_mp", "*neckg"),
        fx("gore_mist_small_vergara_head", "*neckg"),
    ],
    # head_<PL> currently has no FX - add some
    "head_<PL>": [
        fx("gore_mist_small", "*headg"),
        fx("blood_spurt_arterial_small", "*headg"),
        fx("head_pop_mp_temp", "*headg"),
    ],
    # head_<OL> - mirror of head_<PL>
    "head_<OL>": [
        fx("gore_mist_small", "*headg"),
        fx("blood_spurt_arterial_small", "*headg"),
        fx("head_pop_mp_temp", "*headg"),
    ],
    # Head sub-areas - small blood effects
    "head_back_lower": [
        fx("gore_mist_small", "*head_t"),
        fx("blood_splat_mp_small", "*head_t"),
    ],
    "head_back_upper": [
        fx("gore_mist_small", "*head_t"),
        fx("blood_splat_mp_small", "*head_t"),
    ],
    "head_front_lower": [
        fx("gore_mist_small", "*head_f"),
        fx("blood_splat_mp_small", "*head_f"),
    ],
    "head_front_mid": [
        fx("gore_mist_small", "*head_f"),
        fx("blood_splat_mp_small", "*head_f"),
    ],
    "head_front_upper": [
        fx("gore_mist_small", "*head_f"),
        fx("blood_splat_mp_small", "*head_f"),
    ],
    "head_side": [
        fx("gore_mist_small", "*head_t"),
        fx("blood_splat_mp_small", "*head_t"),
    ],
}

# === BLOOD FX (DamageLevel 0-3) per area ===
AREA_BLOOD_FX = {
    "hand": [
        fx("blood_splat_smll_mp", "*hand_<PS>g"),
        fx("mp_blood_squirt_small", "*hand_<PS>g"),
    ],
    "arm_lower": [
        fx("blood_splat_mp_small", "*hand_<PS>g"),
        fx("blood_squirt_mp", "*hand_<PS>g"),
    ],
    "arm_upper": [
        fx("blood_splat_mp", "*shldr_<PS>g"),
        fx("blood_spurt_mp", "*shldr_<PS>g"),
    ],
    "torso": [
        fx("gore_mist_small", "*uchest_<PS>"),
        fx("blood_spurt_mp", "*uchest_<PS>"),
        fx("blood_impact", "*uchest_<PS>"),
    ],
    "foot": [
        fx("blood_splat_smll_mp", "*foot_<PS>g"),
        fx("mp_blood_squirt_small", "*foot_<PS>g"),
    ],
    "leg_lower": [
        fx("blood_splat_mp_small", "*calf_<PS>"),
        fx("blood_squirt_mp", "*calf_<PS>"),
    ],
    "leg_upper": [
        fx("blood_splat_mp", "*hip_<PS>g"),
        fx("blood_spurt_mp", "*hip_<PS>g"),
    ],
    "hip": [
        fx("blood_splat_mp", "*hip_<PS>g"),
        fx("blood_spurt_mp", "*hip_<PS>g"),
        fx("gut_bleed", "*hip_<PS>g"),
    ],
    "head": [
        fx("gore_mist_small", "*neckg"),
        fx("blood_spurt_mp", "*neckg"),
        fx("blood_splat_mp", "*neckg"),
    ],
    "head_<PL>": [
        fx("gore_mist_small", "*headg"),
        fx("blood_splat_mp_small", "*headg"),
    ],
    "head_<OL>": [
        fx("gore_mist_small", "*headg"),
        fx("blood_splat_mp_small", "*headg"),
    ],
    "head_back_lower": [
        fx("gore_mist_small", "*head_t"),
        fx("blood_drip", "*head_t"),
    ],
    "head_back_upper": [
        fx("gore_mist_small", "*head_t"),
        fx("blood_drip", "*head_t"),
    ],
    "head_front_lower": [
        fx("gore_mist_small", "*head_f"),
        fx("blood_drip", "*head_f"),
    ],
    "head_front_mid": [
        fx("gore_mist_small", "*head_f"),
        fx("blood_drip", "*head_f"),
    ],
    "head_front_upper": [
        fx("gore_mist_small", "*head_f"),
        fx("blood_drip", "*head_f"),
    ],
    "head_side": [
        fx("gore_mist_small", "*head_t"),
        fx("blood_drip", "*head_t"),
    ],
}


def main():
    with open(DATA_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)

    gore = data.get("Gore")
    if gore is None:
        print("ERROR: No 'Gore' section found in SoF2_DATA.json")
        sys.exit(1)

    # 1. Update gore_effects registry
    gore["gore_effects"] = GORE_EFFECTS_REGISTRY
    print(f"Updated gore_effects registry: {len(GORE_EFFECTS_REGISTRY)} entries")

    # 2. Update gore_areas
    areas = gore.get("gore_areas", [])
    updated_count = 0
    for area in areas:
        loc = area.get("Location", "")

        # Update FX if we have new data
        if loc in AREA_FX:
            area["FX"] = AREA_FX[loc]
            updated_count += 1

        # Add BloodFX
        if loc in AREA_BLOOD_FX:
            area["BloodFX"] = AREA_BLOOD_FX[loc]

    print(f"Updated FX for {updated_count} gore areas")

    # 3. Verify all 30 effects are referenced (gut_model excluded)
    all_fx_names = set()
    for area in areas:
        for entry in area.get("FX", []):
            all_fx_names.add(entry["Name"])
        for entry in area.get("BloodFX", []):
            all_fx_names.add(entry["Name"])

    expected = {e["Name"] for e in GORE_EFFECTS_REGISTRY}
    missing = expected - all_fx_names
    extra = all_fx_names - expected
    if missing:
        print(f"WARNING: Effects in registry but not in any area: {missing}")
    if extra:
        print(f"WARNING: Effects in areas but not in registry: {extra}")
    print(f"Total unique effects referenced: {len(all_fx_names)}")

    # 4. Write back
    with open(DATA_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=4, ensure_ascii=False)

    print("Done! SoF2_DATA.json updated successfully.")


if __name__ == "__main__":
    main()
