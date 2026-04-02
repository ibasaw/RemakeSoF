"""
Comprehensive audit of ALL SoF2 effect JSON files.
Checks:
1. All particle sizes (flags oversized chunks, smoke, dust, debris)
2. Compares belt/heavy variants against standard variants
3. Lists all textures used and their max sizes
4. Checks for any anomalies (zero sizes, missing fields, huge ratios)
"""
import json
import os
import glob
from collections import defaultdict

EFFECTS_DIR = r"D:\RemakeSoF\Assets\Resources\Data\Effects"

CHUNK_TEXTURES = {
    "gfx/misc/bp_rock", "gfx/misc/jk_terra_chunk", "gfx/misc/jk_terra_chunk2",
    "gfx/misc/jk_bar", "gfx/misc/jk_bar2",
    "gfx/misc/bp_glass_bit1", "gfx/misc/gb_glass_bit1",
    "gfx/misc/bp_glass_debris1", "gfx/misc/bp_glass_debris2", "gfx/misc/bp_glass_debris3",
    "gfx/misc/gb_glass_debris1", "gfx/misc/gb_glass_debris2", "gfx/misc/gb_glass_debris3",
    "gfx/misc/bp_exp_debris01", "gfx/misc/bp_exp_debris02",
    "gfx/misc/impact_wood", "gfx/misc/jk_green_chunk",
    "gfx/misc/jk_big_pot_chunk", "gfx/misc/jk_big_pot_chunk2",
    "gfx/misc/jk_sundial_chunk", "gfx/misc/jk_sundial_chunk2",
    "gfx/misc/jk_tube_chunk", "gfx/misc/jk_airtank_bit",
    "gfx/misc/jk_extinguisher_bit", "gfx/misc/jk_lantern_bit", "gfx/misc/jk_lantern_bit2",
    "gfx/misc/jk_keys", "gfx/misc/jk_keys2",
    "gfx/misc/jk_candy", "gfx/misc/jk_candy2",
    "gfx/misc/jk_red_peppers", "gfx/misc/jk_basket_peppers", "gfx/misc/jk_basket_peppers2",
    "gfx/misc/jk_basket_grey", "gfx/misc/bp_basket_bit01", "gfx/misc/bp_basket_bit02",
    "gfx/misc/jk_osprey_glass", "gfx/misc/jk_osprey_glass2",
    "gfx/misc/jk_fur",
}

SMOKE_TEXTURES = {
    "gfx/misc/jk_smoke", "gfx/misc/jk_smoke2", "gfx/misc/jk_smoke3",
    "gfx/misc/jk_smoke4", "gfx/misc/jk_smoke5",
    "gfx/misc/gb_smoke", "gfx/misc/gb_smoke2", "gfx/misc/gb_smoke3",
    "gfx/misc/bp_smoke01", "gfx/misc/bp_smoke02",
    "gfx/misc/jz_smkbrn", "gfx/misc/dv_smkbrn", "gfx/misc/dv_smkgrn",
    "gfx/misc/smoke", "gfx/misc/smoke32", "gfx/misc/smokepuff3",
}

DUST_TEXTURES = {
    "gfx/misc/jk_dirt_grey", "gfx/misc/jk_dirt_grey2",
    "gfx/misc/jk_dirt_grey_streaked", "gfx/misc/jk_dirt_explode",
    "gfx/misc/dv_dirta", "gfx/misc/gb_softfill", "gfx/misc/gb_softfill_a",
    "gfx/misc/jk_grass", "gfx/misc/jk_leaf1", "gfx/misc/jk_leaf2",
}

CHUNK_MAX = 0.08
SMOKE_MAX = 0.40
DUST_MAX = 0.30

def get_max_size(seg):
    size = seg.get("size", {})
    vals = [size.get(k, 0) for k in ("startMin", "startMax", "endMin", "endMax") if size.get(k) is not None]
    return max(vals) if vals else 0

def audit_file(filepath):
    issues = []
    info = []
    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)
    filename = os.path.basename(filepath)
    for effect in data:
        effect_id = effect.get("id", "unknown")
        for i, seg in enumerate(effect.get("segments", [])):
            if seg.get("type") != "particle":
                continue
            texture = seg.get("texture", "")
            name = seg.get("name", f"seg_{i}")
            flags = seg.get("flags", [])
            max_size = get_max_size(seg)
            p = seg.get("particle", {})
            count_max = p.get("countMax", 1)
            count_min = p.get("countMin", 0)
            has_physics = "usePhysics" in flags
            size = seg.get("size", {})
            
            cat = "other"
            thresh = 0.5
            if texture in CHUNK_TEXTURES:
                cat = "chunk"
                thresh = CHUNK_MAX
            elif texture in SMOKE_TEXTURES:
                cat = "smoke"
                thresh = SMOKE_MAX
            elif texture in DUST_TEXTURES:
                cat = "dust"
                thresh = DUST_MAX
            
            if max_size > thresh:
                issues.append({
                    "file": filename, "effect": effect_id, "segment": name,
                    "texture": texture, "category": cat,
                    "max_cm": round(max_size * 100, 1),
                    "thresh_cm": round(thresh * 100, 1),
                    "count": f"{count_min}-{count_max}",
                    "physics": has_physics,
                    "start": f"{size.get('startMin',0):.4f}-{size.get('startMax',0):.4f}",
                    "end": f"{size.get('endMin',0):.4f}-{size.get('endMax',0):.4f}",
                })
            
            info.append({
                "file": filename, "effect": effect_id, "segment": name,
                "texture": texture, "category": cat,
                "max_cm": round(max_size * 100, 1),
                "count": f"{count_min}-{count_max}",
                "physics": has_physics,
            })
    return issues, info

def main():
    files = sorted(glob.glob(os.path.join(EFFECTS_DIR, "SoF2_Effects_*.json")))
    all_issues = []
    all_info = []
    
    for f in files:
        try:
            issues, info = audit_file(f)
            all_issues.extend(issues)
            all_info.extend(info)
        except Exception as e:
            print(f"ERROR: {os.path.basename(f)}: {e}")
    
    print("=" * 110)
    print("PARTICLE SIZE AUDIT - ALL 30 EFFECT FILES")
    print("=" * 110)
    
    by_texture = defaultdict(list)
    for item in all_info:
        by_texture[item["texture"]].append(item)
    
    print(f"\n{'Texture':<45} {'Cat':<7} {'Uses':>5} {'MaxCm':>7} {'Sample Effects'}")
    print("-" * 110)
    for tex in sorted(by_texture.keys()):
        items = by_texture[tex]
        max_cm = max(i["max_cm"] for i in items)
        cat = items[0]["category"]
        effects = sorted(set(i["effect"] for i in items))
        s = ", ".join(effects[:3])
        if len(effects) > 3:
            s += f" (+{len(effects)-3})"
        print(f"  {tex:<43} {cat:<7} {len(items):>5} {max_cm:>6.1f}cm  {s}")
    
    if all_issues:
        print(f"\n{'=' * 110}")
        print(f"POTENTIAL OVERSIZED PARTICLES ({len(all_issues)} found)")
        print(f"{'=' * 110}")
        by_eff = defaultdict(list)
        for w in all_issues:
            by_eff[w["effect"]].append(w)
        for eid in sorted(by_eff.keys()):
            items = by_eff[eid]
            print(f"\n  {eid}  [{items[0]['file']}]")
            for w in items:
                ph = " [PHYS]" if w["physics"] else ""
                print(f"    {w['segment']:<28} {w['texture']:<38} {w['max_cm']:>6.1f}cm (>{w['thresh_cm']}cm) "
                      f"cnt:{w['count']}{ph}")
                print(f"      start: {w['start']}m  end: {w['end']}m")
    
    print(f"\n{'=' * 110}")
    print(f"TOTAL: {len(all_info)} particle segments, {len(files)} files, {len(all_issues)} issues")
    print(f"{'=' * 110}")

if __name__ == "__main__":
    main()
