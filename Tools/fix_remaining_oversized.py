#!/usr/bin/env python3
"""
Fix remaining oversized particles across all effect JSON files.

Categories:
1. Gore/blood effects (blood_spurt_mp, gore_gib_body, head_pop_mp_temp)
   - Same visual scale issue as impact_flesh (fixed previously)
   - Apply ÷4 for end sizes >1.0m, ÷2 for start sizes >0.5m
2. Belt-weapon impacts (heavy MG rounds): ÷2 for end sizes >1.0m
3. Muzzle flash/trail effects: ÷2 for oversized clouds
4. Ratio-capped effects: increase startSize so ratio stays within 20x cap

NOT touching:
- Water ripples (1-2m is realistic for bullet-in-water)
- Explosion effects (intended to be massive)
- Debris chunks (flying debris can be large)
"""

import json
import os
import glob

EFFECTS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                           "Assets", "Resources", "Data", "Effects")

changes = []


def fix_sizes(filepath, effect_id, seg_name, size_key, divisor, desc):
    """Record a size fix to apply."""
    changes.append({
        "file": filepath,
        "effect_id": effect_id,
        "seg_name": seg_name,
        "size_key": size_key,
        "divisor": divisor,
        "desc": desc
    })


def apply_fixes():
    # Group changes by file
    by_file = {}
    for ch in changes:
        f = ch["file"]
        if f not in by_file:
            by_file[f] = []
        by_file[f].append(ch)

    total = 0
    for filepath, file_changes in by_file.items():
        with open(filepath, "r", encoding="utf-8") as f:
            effects = json.load(f)

        for ch in file_changes:
            eid = ch["effect_id"]
            sname = ch["seg_name"]
            skey = ch["size_key"]  # "size" or "length"
            div = ch["divisor"]

            for effect in effects:
                if effect["id"] != eid:
                    continue
                for seg in effect.get("segments", []):
                    if seg.get("name", seg.get("type", "")) != sname:
                        continue
                    size = seg.get(skey, {})
                    if not size:
                        continue

                    old_vals = {}
                    for key in ["startMin", "startMax", "endMin", "endMax"]:
                        if key in size:
                            old_vals[key] = size[key]
                            size[key] = round(size[key] / div, 4)

                    new_vals = {k: size[k] for k in old_vals}
                    print(f"  {eid} / {sname} [{skey}] ÷{div}: {old_vals} -> {new_vals}")
                    total += 1

        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(effects, f, indent=2, ensure_ascii=False)
            f.write("\n")

    return total


def fix_ratio_cap(filepath, effect_id, seg_name, size_key="size"):
    """Fix endRatio > 20x by increasing startSize so ratio stays within cap."""
    with open(filepath, "r", encoding="utf-8") as f:
        effects = json.load(f)

    fixed = 0
    for effect in effects:
        if effect["id"] != effect_id:
            continue
        for seg in effect.get("segments", []):
            if seg.get("name", seg.get("type", "")) != seg_name:
                continue
            size = seg.get(size_key, {})
            if not size:
                continue

            start_max = size.get("startMax", size.get("startMin", 0))
            end_max = size.get("endMax", size.get("endMin", 0))

            if start_max <= 0 or end_max <= 0:
                continue

            ratio = end_max / start_max
            if ratio > 20:
                # Set startMin/Max so ratio = 18x (safely under 20x)
                new_start = round(end_max / 18.0, 4)
                old_start_min = size.get("startMin", 0)
                old_start_max = start_max

                size["startMin"] = max(size.get("startMin", 0), round(new_start * 0.8, 4))
                size["startMax"] = new_start

                print(f"  RATIO FIX: {effect_id} / {seg_name}: "
                      f"start {old_start_min:.4f}-{old_start_max:.4f} -> "
                      f"{size['startMin']:.4f}-{size['startMax']:.4f} "
                      f"(ratio {ratio:.0f}x -> ~18x)")
                fixed += 1

    if fixed:
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(effects, f, indent=2, ensure_ascii=False)
            f.write("\n")

    return fixed


def main():
    gore_blood = os.path.join(EFFECTS_DIR, "SoF2_Effects_gore_blood.json")
    concrete = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_concrete.json")
    dirt = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_dirt.json")
    glass = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_glass.json")
    gravel = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_gravel.json")
    ice = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_ice.json")
    metal = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_metal.json")
    wood = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_wood.json")
    misc = os.path.join(EFFECTS_DIR, "SoF2_Effects_impact_misc.json")
    muzzle = os.path.join(EFFECTS_DIR, "SoF2_Effects_muzzle_flashes.json")
    tracers = os.path.join(EFFECTS_DIR, "SoF2_Effects_tracers_trails.json")
    debris_chunks = os.path.join(EFFECTS_DIR, "SoF2_Effects_debris_chunks.json")

    # =========================================================================
    # 1. GORE EFFECTS (÷4 for most, ÷2 for moderate)
    # =========================================================================
    print("=== GORE EFFECTS ===")

    # blood_spurt_mp: mist segment 0→1.016-1.27m ÷4
    fix_sizes(gore_blood, "effects/blood_spurt_mp", "particle", "size", 4,
              "blood mist cloud too large")

    # gore_gib_body: multiple oversized segments
    fix_sizes(gore_blood, "effects/gore_gib_body", "blood_bits", "size", 4,
              "body gib blood bits")
    fix_sizes(gore_blood, "effects/gore_gib_body", "bloodimpact", "size", 4,
              "body gib blood impact splat")
    fix_sizes(gore_blood, "effects/gore_gib_body", "mist", "size", 4,
              "body gib mist cloud 5m")
    fix_sizes(gore_blood, "effects/gore_gib_body", "streaks", "size", 4,
              "body gib streak splats")

    # head_pop_mp_temp: head explosion
    fix_sizes(gore_blood, "effects/head_pop_mp_temp", "smoke_lightgrey", "size", 4,
              "head pop smoke cloud")
    fix_sizes(gore_blood, "effects/head_pop_mp_temp", "flames", "size", 2,
              "head pop flames")
    fix_sizes(gore_blood, "effects/head_pop_mp_temp", "spark_splash", "size", 2,
              "head pop sparks")

    # =========================================================================
    # 2. BELT-WEAPON IMPACT EFFECTS (÷2 for the dust/smoke puffs)
    # =========================================================================
    print("\n=== BELT IMPACT EFFECTS ===")

    # impact_concrete_belt: smokepuff 1.27m ÷2
    fix_sizes(concrete, "effects/impact_concrete_belt", "smokepuff", "size", 2,
              "belt concrete smoke puff")

    # impact_dirt_belt: particle 1.22m ÷2
    fix_sizes(dirt, "effects/impact_dirt_belt", "particle", "size", 2,
              "belt dirt particles")

    # impact_gravel_belt: Dust 1.52m ÷2
    fix_sizes(gravel, "effects/impact_gravel_belt", "Dust", "size", 2,
              "belt gravel dust")

    # impact_ice_belt: smokepuff 1.27m ÷2, dustparticles 1.016m ÷2
    fix_sizes(ice, "effects/impact_ice_belt", "smokepuff", "size", 2,
              "belt ice smoke puff")
    fix_sizes(ice, "effects/impact_ice_belt", "dustparticles", "size", 2,
              "belt ice dust particles")

    # impact_metal_belt: particle 1.016m ÷2
    fix_sizes(metal, "effects/impact_metal_belt", "particle", "size", 2,
              "belt metal particles")

    # impact_metal_osprey: particle 1.016m ÷2
    fix_sizes(metal, "effects/impact_metal_osprey", "particle", "size", 2,
              "osprey metal particles")

    # impact_metal-h_belt: particle 1.016m ÷2
    fix_sizes(metal, "effects/impact_metal-h_belt", "particle", "size", 2,
              "belt heavy metal particles")

    # impact_wood_belt: particle 1.016m ÷2
    fix_sizes(wood, "effects/impact_wood_belt", "particle", "size", 2,
              "belt wood particles")

    # =========================================================================
    # 3. GLASS IMPACT
    # =========================================================================
    print("\n=== GLASS IMPACT ===")

    # impact_glass: particle 1.016m ÷2
    fix_sizes(glass, "effects/impact_glass", "particle", "size", 2,
              "glass shatter particles")

    # =========================================================================
    # 4. MUZZLE FLASH / TRAIL EFFECTS
    # =========================================================================
    print("\n=== MUZZLE / TRAIL EFFECTS ===")

    # m203_trail: particle 1.016m ÷2
    fix_sizes(tracers, "effects/m203_trail", "particle", "size", 2,
              "M203 grenade trail smoke")

    # mflash_m590: particle_cloud 2.54m ÷4
    fix_sizes(muzzle, "effects/mflash_m590", "particle_cloud", "size", 4,
              "M590 shotgun cloud")

    # =========================================================================
    # 5. DEBRIS (only the smoke puff, not the debris itself)
    # =========================================================================
    print("\n=== DEBRIS ===")

    fix_sizes(debris_chunks, "effects/chunks/debris_hollowmetal", "puff", "size", 2,
              "hollow metal debris smoke puff")

    # =========================================================================
    # Apply all size fixes
    # =========================================================================
    print("\nApplying size fixes...")
    total_size = apply_fixes()

    # =========================================================================
    # 6. RATIO CAP FIXES (increase startSize so ratio stays under 20x)
    # Only for non-explosion effects where the visual matters
    # =========================================================================
    print("\n=== RATIO CAP FIXES ===")
    total_ratio = 0

    # impact_computer: bang_light ratio 22.5x
    total_ratio += fix_ratio_cap(misc, "effects/impacts/impact_computer", "bang_light")

    # mflash_m590: particle_cloud (after size fix, recheck)
    total_ratio += fix_ratio_cap(muzzle, "effects/mflash_m590", "particle_cloud")

    # mflash_ussocom_final: streaks ratio 22.4x
    total_ratio += fix_ratio_cap(muzzle, "effects/muzzle_flashes/mflash_ussocom_final", "streaks")

    # debris_computer: bang_light ratio 30x
    total_ratio += fix_ratio_cap(debris_chunks, "effects/chunks/debris_computer", "bang_light")

    print(f"\n=== SUMMARY ===")
    print(f"Size fixes applied: {total_size}")
    print(f"Ratio cap fixes applied: {total_ratio}")
    print(f"Total changes: {total_size + total_ratio}")


if __name__ == "__main__":
    main()
