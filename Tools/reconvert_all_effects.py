"""
Re-converts all SoF2 .efx effect files to JSON using the corrected parser.
Reads existing JSON files to determine which .efx files map to which JSON output,
then re-parses the original .efx files and overwrites the JSON.
"""

import json
import os
import re
import sys

# Import the converter
sys.path.insert(0, os.path.dirname(__file__))
from efx_to_json import parse_efx, convert_efx_to_json

EFX_DIR = r"d:\sof2_extract\base\effects"
JSON_DIR = r"d:\RemakeSoF\Assets\Resources\Data\Effects"


def get_efx_path(effect_id):
    """Convert effect ID to .efx file path."""
    # Strip 'effects/' prefix
    name = effect_id
    if name.startswith("effects/"):
        name = name[len("effects/"):]
    
    # Handle subdirectory IDs (e.g., effects/muzzle_flashes/mflash_m4)
    # These map to flat .efx files in EFX_DIR or subdirectories
    parts = name.split("/")
    
    # Try flat first
    flat_path = os.path.join(EFX_DIR, parts[-1] + ".efx")
    if os.path.exists(flat_path):
        return flat_path
    
    # Try with subdirectory
    subdir_path = os.path.join(EFX_DIR, *parts) + ".efx"
    if os.path.exists(subdir_path):
        return subdir_path
    
    # Try replacing / with subdirectories
    for subdir in ["impacts", "muzzle_flashes", "explosions", "chunks", "fire", "confuse_ed"]:
        candidate = os.path.join(EFX_DIR, subdir, parts[-1] + ".efx")
        if os.path.exists(candidate):
            return candidate
    
    return None


def reconvert_json_file(json_path):
    """Re-convert a single JSON file by re-parsing all its .efx sources."""
    with open(json_path, "r", encoding="utf-8") as f:
        existing = json.load(f)
    
    if not isinstance(existing, list):
        print(f"  SKIP: {os.path.basename(json_path)} is not an array")
        return 0, 0
    
    new_effects = []
    converted = 0
    failed = 0
    
    for effect in existing:
        effect_id = effect.get("id", "")
        efx_path = get_efx_path(effect_id)
        
        if efx_path is None:
            print(f"  WARN: No .efx found for '{effect_id}' — keeping original")
            new_effects.append(effect)
            failed += 1
            continue
        
        try:
            new_effect = convert_efx_to_json(efx_path, effect_id)
            # Preserve displayName from original
            if "displayName" in effect:
                new_effect["displayName"] = effect["displayName"]
            new_effects.append(new_effect)
            converted += 1
        except Exception as e:
            print(f"  FAIL: {effect_id} ({efx_path}): {e}")
            new_effects.append(effect)
            failed += 1
    
    # Write back
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(new_effects, f, indent=2, ensure_ascii=False)
    
    return converted, failed


def main():
    json_files = sorted([
        f for f in os.listdir(JSON_DIR)
        if f.endswith(".json") and f.startswith("SoF2_Effects_")
    ])
    
    print(f"Found {len(json_files)} JSON effect files to re-convert\n")
    
    total_converted = 0
    total_failed = 0
    
    for json_file in json_files:
        json_path = os.path.join(JSON_DIR, json_file)
        print(f"Processing: {json_file}")
        converted, failed = reconvert_json_file(json_path)
        total_converted += converted
        total_failed += failed
        print(f"  -> {converted} converted, {failed} failed/kept\n")
    
    print(f"\nTotal: {total_converted} effects re-converted, {total_failed} failed/kept")


if __name__ == "__main__":
    main()
