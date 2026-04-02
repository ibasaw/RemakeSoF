"""
Cross-reference check: Do all surface-data effect IDs point to existing effects?
Do all defined effects get used somewhere?
"""
import json
import os
import glob
from collections import defaultdict

EFFECTS_DIR = r"D:\RemakeSoF\Assets\Resources\Data\Effects"
DATA_DIR = r"D:\RemakeSoF\Assets\Resources\Data"

def load_all_effects():
    """Load all effect IDs from all effect JSON files."""
    effect_ids = set()
    files = glob.glob(os.path.join(EFFECTS_DIR, "SoF2_Effects_*.json"))
    for f in sorted(files):
        with open(f, 'r', encoding='utf-8-sig') as fh:
            data = json.load(fh)
        for effect in data:
            eid = effect.get("id", "")
            if eid:
                effect_ids.add(eid)
    return effect_ids

def load_surface_references():
    """Load all effect IDs referenced from surface data."""
    surface_file = os.path.join(DATA_DIR, "SoF2_data_per_surface.json")
    refs = set()
    with open(surface_file, 'r', encoding='utf-8-sig') as f:
        data = json.load(f)
    
    # Walk entire structure looking for effect IDs
    def walk(obj, path=""):
        if isinstance(obj, dict):
            for k, v in obj.items():
                if isinstance(v, str) and v.startswith("effects/"):
                    refs.add(v)
                walk(v, f"{path}.{k}")
        elif isinstance(obj, list):
            for i, item in enumerate(obj):
                walk(item, f"{path}[{i}]")
    
    walk(data)
    return refs

def load_weapon_ammo_types():
    """Load weapon ammo types from weapon JSON."""
    weapons = {}
    for fname in ["SoF2_Weapons_New.json", "SoF2_Weapons.json"]:
        fpath = os.path.join(DATA_DIR, fname)
        if os.path.exists(fpath):
            with open(fpath, 'r', encoding='utf-8-sig') as f:
                data = json.load(f)
            for weapon in data:
                name = weapon.get("name", weapon.get("displayName", "unknown"))
                ammo = weapon.get("ammo", {})
                ammo_type = ammo.get("type", "unknown")
                weapons[name] = ammo_type
    return weapons

def load_gore_references():
    """Load gore effect references from weapon data."""
    refs = set()
    for fname in ["SoF2_Weapons_New.json", "SoF2_Weapons.json"]:
        fpath = os.path.join(DATA_DIR, fname)
        if os.path.exists(fpath):
            with open(fpath, 'r', encoding='utf-8-sig') as f:
                content = f.read()
            # Search for effect references
            import re
            for match in re.finditer(r'"effects/[^"]+?"', content):
                ref = match.group().strip('"')
                refs.add(ref)
    return refs

def main():
    print("=" * 100)
    print("EFFECT CROSS-REFERENCE AUDIT")
    print("=" * 100)
    
    # Load data
    all_effects = load_all_effects()
    surface_refs = load_surface_references()
    gore_refs = load_gore_references()
    weapons = load_weapon_ammo_types()
    
    all_refs = surface_refs | gore_refs
    
    print(f"\nDefined effects: {len(all_effects)}")
    print(f"Referenced by surfaces: {len(surface_refs)}")
    print(f"Referenced by weapons (gore): {len(gore_refs)}")
    print(f"Total unique references: {len(all_refs)}")
    
    # Check: references that don't exist
    missing = all_refs - all_effects
    if missing:
        print(f"\n❌ MISSING EFFECTS ({len(missing)}) - Referenced but not defined:")
        for m in sorted(missing):
            print(f"    {m}")
    else:
        print(f"\n✅ All referenced effects exist!")
    
    # Check: effects that are never referenced
    unused = all_effects - all_refs
    # Some effects are sub-effects or triggered programmatically
    if unused:
        print(f"\n⚠️  POTENTIALLY UNUSED EFFECTS ({len(unused)}):")
        for u in sorted(unused):
            print(f"    {u}")
    
    # Show weapon -> ammo mapping
    print(f"\n{'=' * 100}")
    print("WEAPON AMMO TYPE MAPPING")
    print(f"{'=' * 100}")
    for name, ammo in sorted(weapons.items()):
        print(f"  {name:<30} → {ammo}")
    
    # Check surface data structure
    surface_file = os.path.join(DATA_DIR, "SoF2_data_per_surface.json")
    with open(surface_file, 'r', encoding='utf-8-sig') as f:
        surface_data = json.load(f)
    
    print(f"\n{'=' * 100}")
    print("SURFACE TYPE COVERAGE")
    print(f"{'=' * 100}")
    
    # Get all surface types
    surface_types = set()
    ammo_types_used = set()
    
    if isinstance(surface_data, dict):
        for surface_name, surface_info in surface_data.items():
            surface_types.add(surface_name)
            if isinstance(surface_info, dict):
                impacts = surface_info.get("impacts", surface_info.get("ammoEffects", {}))
                if isinstance(impacts, dict):
                    for ammo_type in impacts.keys():
                        ammo_types_used.add(ammo_type)
    elif isinstance(surface_data, list):
        for entry in surface_data:
            st = entry.get("surfaceType", entry.get("name", ""))
            surface_types.add(st)
            impacts = entry.get("impacts", entry.get("ammoEffects", {}))
            if isinstance(impacts, dict):
                for ammo_type in impacts.keys():
                    ammo_types_used.add(ammo_type)
    
    print(f"\nSurface types defined: {len(surface_types)}")
    for s in sorted(surface_types):
        print(f"  {s}")
    
    print(f"\nAmmo types in surface data: {len(ammo_types_used)}")
    for a in sorted(ammo_types_used):
        print(f"  {a}")
    
    # Cross-check: weapon ammo types vs surface ammo types
    weapon_ammos = set(weapons.values())
    missing_ammo = weapon_ammos - ammo_types_used
    if missing_ammo:
        print(f"\n❌ Weapon ammo types NOT in surface data: {missing_ammo}")
    
    extra_ammo = ammo_types_used - weapon_ammos
    if extra_ammo:
        print(f"\n⚠️  Surface ammo types with no weapon: {extra_ammo}")

if __name__ == "__main__":
    main()
