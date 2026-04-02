"""
Cross-references original SoF2 .efx files with converted JSON effect files
to find missing and extra effects.
"""

import json
import os
import glob

EFX_DIR = r"d:\sof2_extract\base\effects"
JSON_DIR = r"d:\RemakeSoF\Assets\Resources\Data\Effects"


def get_efx_names():
    """Get all .efx file basenames (without extension) from original SoF2."""
    names = set()
    for f in glob.glob(os.path.join(EFX_DIR, "*.efx")):
        name = os.path.splitext(os.path.basename(f))[0]
        names.add(name)
    return names


def get_json_effect_ids():
    """Get all effect IDs from JSON files and map them back to likely .efx names."""
    effects = {}  # id -> json file
    for f in glob.glob(os.path.join(JSON_DIR, "SoF2_Effects_*.json")):
        basename = os.path.basename(f)
        try:
            with open(f, "r", encoding="utf-8") as fh:
                data = json.load(fh)
        except Exception as e:
            print(f"ERROR reading {basename}: {e}")
            continue

        if isinstance(data, list):
            for effect in data:
                eid = effect.get("id", "")
                effects[eid] = basename
        elif isinstance(data, dict):
            eid = data.get("id", "")
            if eid:
                effects[eid] = basename

    return effects


def id_to_efx_name(effect_id):
    """Convert an effect ID like 'effects/muzzle_flashes/mflash_m4' to likely .efx name."""
    # Strip the 'effects/' prefix and any subdirectory
    name = effect_id
    if name.startswith("effects/"):
        name = name[len("effects/"):]
    # Remove subdirectory prefixes
    parts = name.split("/")
    return parts[-1]  # Just the final name


def main():
    efx_names = get_efx_names()
    json_effects = get_json_effect_ids()

    # Map JSON effect IDs to their likely .efx base names
    json_efx_names = {}
    for eid, jfile in json_effects.items():
        efx_name = id_to_efx_name(eid)
        json_efx_names[efx_name] = (eid, jfile)

    print(f"Original SoF2 .efx files: {len(efx_names)}")
    print(f"JSON effect entries: {len(json_effects)}")
    print(f"Unique base names in JSON: {len(json_efx_names)}")
    print()

    # === GAMEPLAY-RELEVANT CATEGORIES ===
    # Only check effects that matter for the MP remake
    gameplay_prefixes = [
        "impact_",       # bullet impacts on surfaces
        "blood_",        # blood effects
        "gore_",         # gore/dismemberment
        "flesh_",        # flesh impacts
        "mflash_",       # muzzle flashes
        "smoke_",        # weapon smoke
        "shell_",        # shell casings
        "tracer",        # tracer effects
        "explosion_",    # explosions
        "mushroom_",     # mushroom explosions
        "rpg7_",         # RPG effects
        "m203_",         # M203 effects
        "grenade_",      # grenade effects
        "debris_",       # debris
        "arterial_",     # arterial blood
        "knife_",        # knife impacts
        "incendiary_",   # incendiary effects
        "phosphorus_",   # phosphorus effects
        "stun_flash",    # stun grenade
        "head_pop_",     # head pop
        "water_explosion", # water explosions
        "sam_",          # SAM effects
    ]

    # Not needed for MP remake:
    skip_prefixes = [
        "ce_",           # cinematic entity effects (SP only)
        "col",           # columbia level-specific (SP)
        "pra",           # prague level-specific (SP)
        "kam",           # kamchatka level-specific (SP)
        "hk",            # hong kong level-specific (SP)
        "air4_",         # map-specific
        "arm2",          # armory level-specific (SP)
        "finca",         # finca level-specific (SP)
        "shop7_",        # shop level-specific (SP)
        "liner_",        # liner level-specific (SP)
        "osprey_",       # osprey vehicle (SP)
        "boat_",         # boat (SP)
        "heli_",         # helicopter (SP)
        "jon_",          # dev test
        "test",          # test effects
        "maxpid",        # dev test
        "ball",          # dev test
        "sphere",        # dev test
        "linetest",      # dev test
        "paper",         # dev test
        "dirtest",       # dev test
        "railTest",      # dev test
        "emit_",         # dev test
        "puff",          # generic puff (not used in MP)
        "tst",           # test
        "s4",            # test
        "cem",           # cinematic
        "Sanchez",       # SP character specific
        "npc_",          # SP NPC specific
        "dog_",          # SP specific
        "puke",          # SP specific
        "piss_",         # SP specific
        "question",      # SP AI
        "exclaimation",  # SP AI
        "red_dot",       # SP
    ]

    # Find missing gameplay-relevant effects
    print("=" * 70)
    print("GAMEPLAY-RELEVANT EFFECTS (impacts, gore, weapons, explosions)")
    print("=" * 70)

    missing_gameplay = []
    present_gameplay = []
    skipped = []
    uncategorized_missing = []

    for efx in sorted(efx_names):
        # Skip _old variants (superseded)
        if "_old" in efx.lower() or ".oldefx" in efx.lower():
            skipped.append(efx)
            continue

        # Check if it's in a skip category
        is_skipped = False
        for prefix in skip_prefixes:
            if efx.lower().startswith(prefix.lower()):
                is_skipped = True
                skipped.append(efx)
                break
        if is_skipped:
            continue

        in_json = efx in json_efx_names
        if in_json:
            present_gameplay.append(efx)
        else:
            # Is it gameplay-relevant?
            is_gameplay = False
            for prefix in gameplay_prefixes:
                if efx.lower().startswith(prefix.lower()):
                    is_gameplay = True
                    break
            if is_gameplay:
                missing_gameplay.append(efx)
            else:
                uncategorized_missing.append(efx)

    print(f"\n--- MISSING GAMEPLAY EFFECTS ({len(missing_gameplay)}) ---")
    for efx in sorted(missing_gameplay):
        print(f"  MISSING: {efx}.efx")

    print(f"\n--- PRESENT GAMEPLAY EFFECTS ({len(present_gameplay)}) ---")
    for efx in sorted(present_gameplay):
        eid, jfile = json_efx_names[efx]
        print(f"  OK: {efx} -> {eid} ({jfile})")

    print(f"\n--- UNCATEGORIZED / NOT IN JSON ({len(uncategorized_missing)}) ---")
    for efx in sorted(uncategorized_missing):
        print(f"  ??: {efx}.efx")

    # Find effects in JSON that DON'T exist in original .efx (extensions/additions)
    print(f"\n--- EXTENSIONS (in JSON but no matching .efx) ---")
    extensions = []
    for efx_name, (eid, jfile) in sorted(json_efx_names.items()):
        if efx_name not in efx_names:
            extensions.append((efx_name, eid, jfile))
            print(f"  ADDED: {eid} ({jfile})")

    if not extensions:
        print("  (none)")

    print(f"\n--- SKIPPED ({len(skipped)}) ---")
    print(f"  (SP-only, map-specific, _old variants, dev tests)")

    # Summary
    print(f"\n{'=' * 70}")
    print(f"SUMMARY")
    print(f"{'=' * 70}")
    print(f"Total original .efx files:     {len(efx_names)}")
    print(f"Total JSON effect entries:      {len(json_effects)}")
    print(f"Present in JSON:               {len(present_gameplay)}")
    print(f"Missing gameplay-relevant:     {len(missing_gameplay)}")
    print(f"Extensions/additions:          {len(extensions)}")
    print(f"Uncategorized/not in JSON:     {len(uncategorized_missing)}")
    print(f"Skipped (SP/old/test/map):     {len(skipped)}")


if __name__ == "__main__":
    main()
