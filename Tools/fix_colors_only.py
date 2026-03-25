"""
Fix single-channel color arrays in effect JSON files.
Only patches color data by re-parsing RGB blocks from original .efx files.
Does NOT touch any other properties (sizes, decals, names, trails, etc.)
"""

import json
import os
import re
import sys

EFX_DIR = r"d:\sof2_extract\base\effects"
JSON_DIR = r"d:\RemakeSoF\Assets\Resources\Data\Effects"


def get_efx_path(effect_id):
    """Convert effect ID to .efx file path."""
    name = effect_id
    if name.startswith("effects/"):
        name = name[len("effects/"):]
    
    parts = name.split("/")
    
    # Try flat first
    flat_path = os.path.join(EFX_DIR, parts[-1] + ".efx")
    if os.path.exists(flat_path):
        return flat_path
    
    # Try with subdirectory
    subdir_path = os.path.join(EFX_DIR, *parts) + ".efx"
    if os.path.exists(subdir_path):
        return subdir_path
    
    # Try common subdirectories
    for subdir in ["impacts", "muzzle_flashes", "explosions", "chunks", "fire", "confuse_ed"]:
        candidate = os.path.join(EFX_DIR, subdir, parts[-1] + ".efx")
        if os.path.exists(candidate):
            return candidate
    
    return None


def parse_rgb_blocks_from_efx(efx_path):
    """Parse all rgb blocks from an .efx file, returning them in order of appearance.
    Each entry is a dict with startMin/startMax/endMin/endMax as 3-element lists."""
    with open(efx_path, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()
    
    # Find all top-level segments and their rgb blocks
    block_pattern = re.compile(
        r'(Particle|OrientedParticle|Line|Tail|Sound|Decal)\s*\{', re.IGNORECASE
    )
    
    positions = [(m.start(), m.group(1)) for m in block_pattern.finditer(content)]
    
    rgb_blocks = []
    
    for idx, (pos, block_type) in enumerate(positions):
        # Find matching closing brace
        brace_count = 0
        start = content.index('{', pos)
        i = start
        while i < len(content):
            if content[i] == '{':
                brace_count += 1
            elif content[i] == '}':
                brace_count -= 1
                if brace_count == 0:
                    break
            i += 1
        
        block_content = content[start + 1:i]
        
        # Find name if present
        name_match = re.search(r'name\s+(\S+)', block_content)
        seg_name = name_match.group(1) if name_match else None
        
        # Find rgb sub-block
        rgb_match = re.search(r'rgb\s*\{(.*?)\}', block_content, re.DOTALL)
        if rgb_match:
            rgb_content = rgb_match.group(1)
            color = parse_color_from_rgb(rgb_content)
            rgb_blocks.append({
                'segment_index': idx,
                'segment_type': block_type.lower(),
                'segment_name': seg_name,
                'color': color
            })
        else:
            # No rgb block in this segment
            rgb_blocks.append({
                'segment_index': idx,
                'segment_type': block_type.lower(),
                'segment_name': seg_name,
                'color': None
            })
    
    return rgb_blocks


NUM = r'[-+]?\d[\d.]*'

def parse_color_from_rgb(content):
    """Parse an rgb block content into color dict with 3-element arrays."""
    color = {}
    
    # Try 6-value start (startMin R G B startMax R G B)
    start_match = re.search(
        rf'start[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})',
        content
    )
    if start_match:
        vals = [float(start_match.group(i)) for i in range(1, 7)]
        color['startMin'] = [round(v, 4) for v in vals[0:3]]
        color['startMax'] = [round(v, 4) for v in vals[3:6]]
    else:
        # Try 3-value start
        m = re.search(rf'start[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
        if m:
            vals = [round(float(m.group(i)), 4) for i in range(1, 4)]
            color['startMin'] = vals
            color['startMax'] = vals[:]
    
    # Try 6-value end
    end_match = re.search(
        rf'end[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})',
        content
    )
    if end_match:
        vals = [float(end_match.group(i)) for i in range(1, 7)]
        color['endMin'] = [round(v, 4) for v in vals[0:3]]
        color['endMax'] = [round(v, 4) for v in vals[3:6]]
    else:
        m = re.search(rf'end[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
        if m:
            vals = [round(float(m.group(i)), 4) for i in range(1, 4)]
            color['endMin'] = vals
            color['endMax'] = vals[:]
    
    return color if color else None


def has_broken_color(segment):
    """Check if a segment has any single-channel color arrays."""
    color = segment.get('color', {})
    if not color:
        return False
    for key in ['startMin', 'startMax', 'endMin', 'endMax']:
        val = color.get(key)
        if isinstance(val, list) and len(val) == 1:
            return True
    return False


def match_segment(json_seg, efx_rgb_entry):
    """Try to match a JSON segment to an efx rgb entry by name or type."""
    json_name = json_seg.get('name', '').lower() if json_seg.get('name') else ''
    efx_name = (efx_rgb_entry['segment_name'] or '').lower()
    json_type = json_seg.get('type', '').lower()
    efx_type = efx_rgb_entry['segment_type'].lower()
    
    # Type mapping
    type_map = {
        'orientedparticle': 'orientedParticle',
        'particle': 'particle',
        'tail': 'tail',
        'line': 'line',
        'decal': 'decal',
        'sound': 'sound',
    }
    
    efx_mapped = type_map.get(efx_type, efx_type)
    
    if json_name and efx_name and json_name == efx_name:
        return True
    if not json_name and not efx_name and json_type == efx_mapped:
        return True
    
    return False


def fix_colors_in_json(json_path):
    """Fix single-channel color arrays in a JSON file using original .efx data."""
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    if not isinstance(data, list):
        return 0
    
    fixed_count = 0
    
    for effect in data:
        effect_id = effect.get('id', '')
        segments = effect.get('segments', [])
        
        # Check if any segment has broken colors
        has_any_broken = any(has_broken_color(seg) for seg in segments)
        if not has_any_broken:
            continue
        
        # Find the .efx file
        efx_path = get_efx_path(effect_id)
        if not efx_path:
            print(f"  WARN: No .efx for '{effect_id}' - cannot fix colors")
            continue
        
        # Parse RGB blocks from .efx
        efx_rgb_blocks = parse_rgb_blocks_from_efx(efx_path)
        
        # Match segments by order (excluding sound/decal which don't have rgb)
        # Build ordered lists
        json_particle_segs = []
        for i, seg in enumerate(segments):
            if seg.get('type') in ('sound', 'decal'):
                continue
            json_particle_segs.append((i, seg))
        
        efx_particle_blocks = []
        for block in efx_rgb_blocks:
            if block['segment_type'] in ('sound', 'decal'):
                continue
            efx_particle_blocks.append(block)
        
        if len(json_particle_segs) != len(efx_particle_blocks):
            print(f"  WARN: Segment count mismatch for '{effect_id}': JSON={len(json_particle_segs)} vs EFX={len(efx_particle_blocks)}")
            # Try name-based matching instead
            for json_idx, seg in json_particle_segs:
                if not has_broken_color(seg):
                    continue
                for efx_block in efx_particle_blocks:
                    if match_segment(seg, efx_block) and efx_block['color']:
                        seg['color'] = efx_block['color']
                        fixed_count += 1
                        break
            continue
        
        # Fix by position matching
        for (json_idx, seg), efx_block in zip(json_particle_segs, efx_particle_blocks):
            if not has_broken_color(seg):
                continue
            
            if efx_block['color']:
                seg['color'] = efx_block['color']
                fixed_count += 1
            else:
                # efx has no rgb block, but JSON has a (broken) color
                # This means the original .efx uses default white - remove broken color
                print(f"  INFO: '{effect_id}' segment has color in JSON but not in .efx - removing broken color")
                del seg['color']
                fixed_count += 1
    
    # Write back
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    return fixed_count


def main():
    json_files = sorted([
        f for f in os.listdir(JSON_DIR)
        if f.endswith('.json') and f.startswith('SoF2_Effects_')
    ])
    
    print(f"Scanning {len(json_files)} JSON effect files for broken colors...\n")
    
    total_fixed = 0
    
    for fname in json_files:
        fpath = os.path.join(JSON_DIR, fname)
        fixed = fix_colors_in_json(fpath)
        if fixed > 0:
            print(f"  {fname}: fixed {fixed} color(s)")
            total_fixed += fixed
    
    print(f"\nTotal: {total_fixed} color entries fixed")


if __name__ == '__main__':
    main()
