"""
Converts SoF2 .efx effect files to JSON format for the RemakeSoF effect system.
Uses the same schema as the existing SoF2_Effects_*.json files.

Unit conversion: SoF2 uses Quake units (1 unit = 0.0254m / ~1 inch).
- Position/velocity/size: multiply by 0.0254 to get Unity meters
- Time: SoF2 uses milliseconds → divide by 1000 for seconds
- Gravity: SoF2 uses units/sec² → multiply by 0.0254
- Colors: already 0.0-1.0 range
- Alpha: already 0.0-1.0 range
- Rotation: degrees, kept as-is
"""

import re
import json
import sys
import os

UNIT_SCALE = 0.0254  # Quake units to meters


def parse_efx(filepath):
    """Parse a .efx file and return a list of segment dicts."""
    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    segments = []
    # Split into top-level blocks (Particle, Sound, OrientedParticle, Line, Tail, Decal)
    block_pattern = re.compile(
        r'(Particle|OrientedParticle|Line|Tail|Sound|Decal)\s*\{', re.IGNORECASE
    )

    positions = [(m.start(), m.group(1)) for m in block_pattern.finditer(content)]

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
        segment = parse_block(block_content, block_type)
        if segment:
            segments.append(segment)

    return segments


def parse_block(block_content, block_type):
    """Parse a single effect block into a segment dict."""
    segment = {}

    type_map = {
        'particle': 'particle',
        'orientedparticle': 'orientedParticle',
        'line': 'line',
        'tail': 'tail',
        'sound': 'sound',
        'decal': 'decal',
    }
    segment['type'] = type_map.get(block_type.lower(), block_type.lower())

    if segment['type'] == 'sound':
        return parse_sound_block(block_content, segment)
    elif segment['type'] == 'decal':
        return parse_decal_block(block_content, segment)
    else:
        return parse_particle_block(block_content, segment)


def parse_sound_block(content, segment):
    """Parse a Sound block."""
    # Name
    name_match = re.search(r'name\s+(\S+)', content)
    if name_match:
        segment['name'] = name_match.group(1)

    # Sound files
    sounds_match = re.search(r'sounds\s*\[(.*?)\]', content, re.DOTALL)
    if sounds_match:
        files = [s.strip() for s in sounds_match.group(1).strip().split('\n') if s.strip()]
        segment['sound'] = {
            'files': files,
            'delay': 0.0
        }

    return segment


def parse_decal_block(content, segment):
    """Parse a Decal block."""
    name_match = re.search(r'name\s+(\S+)', content)
    if name_match:
        segment['name'] = name_match.group(1)

    # Shaders
    shaders_match = re.search(r'shaders\s*\[(.*?)\]', content, re.DOTALL)
    if shaders_match:
        shaders = [s.strip() for s in shaders_match.group(1).strip().split('\n') if s.strip()]
        if shaders:
            segment['texture'] = shaders[0]

    # Size
    size_match = re.search(r'size\s+([\d.]+)\s*([\d.]+)?', content)
    if size_match:
        segment['decal'] = {
            'sizeMin': float(size_match.group(1)) * UNIT_SCALE,
            'sizeMax': float(size_match.group(2)) * UNIT_SCALE if size_match.group(2) else float(size_match.group(1)) * UNIT_SCALE
        }

    return segment


def parse_particle_block(content, segment):
    """Parse a Particle/OrientedParticle/Line/Tail block."""
    # Remove sub-blocks first so their 'flags'/'end' keywords don't interfere
    # with top-level parsing
    sub_blocks = {}
    for block_name in ['rgb', 'alpha', 'size', 'length']:
        pattern = re.compile(block_name + r'\s*\{(.*?)\}', re.DOTALL)
        match = pattern.search(content)
        if match:
            sub_blocks[block_name] = match.group(1)
            content = content[:match.start()] + content[match.end():]

    # Also extract shaders and impactfx blocks
    shaders_match = re.search(r'shaders\s*\[(.*?)\]', content, re.DOTALL)
    shader_tex = None
    if shaders_match:
        shaders = [s.strip() for s in shaders_match.group(1).strip().split('\n') if s.strip()]
        if shaders:
            shader_tex = shaders[0]
        content = content[:shaders_match.start()] + content[shaders_match.end():]

    impactfx_match = re.search(r'impactfx\s*\[(.*?)\]', content, re.DOTALL)
    if impactfx_match:
        content = content[:impactfx_match.start()] + content[impactfx_match.end():]

    # Now parse top-level fields safely
    # Name
    name_match = re.search(r'name\s+(\S+)', content)
    if name_match:
        segment['name'] = name_match.group(1)

    # Flags (top-level, not inside sub-blocks)
    flags_match = re.search(r'^\s*flags\s+(.+)$', content, re.MULTILINE)
    if flags_match:
        flags_str = flags_match.group(1).strip()
        flags = [f.strip() for f in re.split(r'\s+', flags_str) if f.strip()]
        if flags:
            segment['flags'] = flags

    # Spawn flags
    spawn_match = re.search(r'spawnFlags\s+(.+)', content)
    if spawn_match:
        spawn_flags = [f.strip() for f in re.split(r'\s+', spawn_match.group(1).strip()) if f.strip()]
        if spawn_flags:
            segment['spawnFlags'] = spawn_flags

    # Texture
    if shader_tex:
        segment['texture'] = shader_tex

    # Build particle sub-object
    particle = {}

    # Count: "count N" or "count N M"
    count_match = re.search(r'count\s+([\d.]+)\s*([\d.]+)?', content)
    if count_match:
        val1 = float(count_match.group(1))
        val2 = float(count_match.group(2)) if count_match.group(2) else None
        if val2 is not None:
            particle['countMin'] = int(val1)
            particle['countMax'] = int(val2)
        else:
            particle['countMin'] = int(val1)
            particle['countMax'] = int(val1)

    # Life (milliseconds → seconds)
    life_match = re.search(r'life\s+([\d.]+)\s*([\d.]+)?', content)
    if life_match:
        life_min = float(life_match.group(1)) / 1000.0
        life_max = float(life_match.group(2)) / 1000.0 if life_match.group(2) else life_min
        particle['lifetimeMin'] = round(life_min, 4)
        particle['lifetimeMax'] = round(life_max, 4)

    # Delay (milliseconds → seconds)
    delay_match = re.search(r'delay\s+([\d.]+)\s*([\d.]+)?', content)
    if delay_match:
        particle['delayMin'] = round(float(delay_match.group(1)) / 1000.0, 4)
        particle['delayMax'] = round(float(delay_match.group(2)) / 1000.0 if delay_match.group(2) else float(delay_match.group(1)) / 1000.0, 4)

    # Rotation
    rot_match = re.search(r'rotation\s+([-\d.e]+)\s*([-\d.e]+)?', content)
    if rot_match:
        particle['rotationMin'] = float(rot_match.group(1))
        particle['rotationMax'] = float(rot_match.group(2)) if rot_match.group(2) else float(rot_match.group(1))

    # Rotation delta/speed
    rotd_match = re.search(r'rotationDelta\s+([-\d.e]+)\s*([-\d.e]+)?', content)
    if rotd_match:
        particle['rotationSpeedMin'] = float(rotd_match.group(1))
        particle['rotationSpeedMax'] = float(rotd_match.group(2)) if rotd_match.group(2) else float(rotd_match.group(1))

    # Velocity (6 values: min xyz, max xyz)
    vel_match = re.search(r'velocity\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)', content)
    if vel_match:
        vals = [float(vel_match.group(i)) for i in range(1, 7)]
        particle['velocityMin'] = [round(v * UNIT_SCALE, 4) for v in vals[0:3]]
        particle['velocityMax'] = [round(v * UNIT_SCALE, 4) for v in vals[3:6]]

    # Origin (6 values: min xyz, max xyz)
    origin_match = re.search(r'origin\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)\s+([-\d.e]+)', content)
    if origin_match:
        vals = [float(origin_match.group(i)) for i in range(1, 7)]
        particle['originMin'] = [round(v * UNIT_SCALE, 4) for v in vals[0:3]]
        particle['originMax'] = [round(v * UNIT_SCALE, 4) for v in vals[3:6]]

    # Gravity
    grav_match = re.search(r'gravity\s+([-\d.e]+)\s*([-\d.e]+)?', content)
    if grav_match:
        g1 = float(grav_match.group(1)) * UNIT_SCALE
        particle['gravityModifier'] = round(g1, 4)

    # Elasticity
    elast_match = re.search(r'elasticity\s+([-\d.e]+)', content)
    if elast_match:
        particle['elasticity'] = float(elast_match.group(1))

    # Determine burst vs continuous
    if 'countMin' in particle:
        if particle.get('delayMax', 0) == 0 and particle['countMin'] > 0:
            particle['burst'] = True
        else:
            particle['burst'] = False

    segment['particle'] = particle

    # Process sub-blocks
    if 'rgb' in sub_blocks:
        color = parse_color_block(sub_blocks['rgb'])
        if color:
            segment['color'] = color

    if 'alpha' in sub_blocks:
        segment['alpha'] = parse_alpha_block(sub_blocks['alpha'])

    if 'size' in sub_blocks:
        segment['size'] = parse_size_block(sub_blocks['size'])

    if 'length' in sub_blocks:
        segment['length'] = parse_size_block(sub_blocks['length'])

    return segment


def parse_color_block(content):
    """Parse an rgb { } block."""
    color = {}

    NUM = r'[-+]?\d[\d.]*'
    start_match = re.search(rf'start[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
    if start_match:
        vals = [float(start_match.group(i)) for i in range(1, 7)]
        color['startMin'] = [round(v, 4) for v in vals[0:3]]
        color['startMax'] = [round(v, 4) for v in vals[3:6]]
    else:
        m = re.search(rf'start[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
        if m:
            vals = [round(float(m.group(i)), 4) for i in range(1, 4)]
            color['startMin'] = vals
            color['startMax'] = vals

    end_match = re.search(rf'end[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
    if end_match:
        vals = [float(end_match.group(i)) for i in range(1, 7)]
        color['endMin'] = [round(v, 4) for v in vals[0:3]]
        color['endMax'] = [round(v, 4) for v in vals[3:6]]
    else:
        m = re.search(rf'end[ \t]+({NUM})[ \t]+({NUM})[ \t]+({NUM})', content)
        if m:
            vals = [round(float(m.group(i)), 4) for i in range(1, 4)]
            color['endMin'] = vals
            color['endMax'] = vals

    return color if color else None


def parse_alpha_block(content):
    """Parse an alpha { } block."""
    alpha = {}

    start_match = re.search(r'start[ \t]+([-+]?\d[\d.]*)[ \t]*([-+]?\d[\d.]*)?', content)
    if start_match:
        alpha['startMin'] = float(start_match.group(1))
        alpha['startMax'] = float(start_match.group(2)) if start_match.group(2) else alpha['startMin']
    else:
        alpha['startMin'] = 1.0
        alpha['startMax'] = 1.0

    end_match = re.search(r'end[ \t]+([-+]?\d[\d.]*)[ \t]*([-+]?\d[\d.]*)?', content)
    if end_match:
        alpha['endMin'] = float(end_match.group(1))
        alpha['endMax'] = float(end_match.group(2)) if end_match.group(2) else alpha['endMin']
    else:
        alpha['endMin'] = 0.0
        alpha['endMax'] = 0.0

    parm_match = re.search(r'parm[ \t]+([-+]?\d[\d.]*)[ \t]*([-+]?\d[\d.]*)?', content)
    if parm_match:
        alpha['parm'] = float(parm_match.group(1))
        if parm_match.group(2):
            alpha['parmMax'] = float(parm_match.group(2))

    return alpha


def parse_size_block(content):
    """Parse a size { } block."""
    size = {}

    start_match = re.search(r'start[ \t]+([-+]?\d[\d.]*)[ \t]*([-+]?\d[\d.]*)?', content)
    if start_match:
        size['startMin'] = round(float(start_match.group(1)) * UNIT_SCALE, 4)
        size['startMax'] = round(float(start_match.group(2)) * UNIT_SCALE, 4) if start_match.group(2) else size['startMin']
    else:
        size['startMin'] = round(1.0 * UNIT_SCALE, 4)
        size['startMax'] = round(1.0 * UNIT_SCALE, 4)

    end_match = re.search(r'end[ \t]+([-+]?\d[\d.]*)[ \t]*([-+]?\d[\d.]*)?', content)
    if end_match:
        size['endMin'] = round(float(end_match.group(1)) * UNIT_SCALE, 4)
        size['endMax'] = round(float(end_match.group(2)) * UNIT_SCALE, 4) if end_match.group(2) else size['endMin']

    parm_match = re.search(r'parm[ \t]+([-+]?\d[\d.]*)[ \t]*([-+]?\d[\d.]*)?', content)
    if parm_match:
        size['parm'] = float(parm_match.group(1))
        if parm_match.group(2):
            size['parmMax'] = float(parm_match.group(2))

    flags_match = re.search(r'flags\s+(.+)', content)
    if flags_match:
        flags = [f.strip().lower() for f in flags_match.group(1).split() if f.strip()]
        if 'clamp' in flags:
            size['curve'] = 'clamp'
        elif 'linear' in flags:
            size['curve'] = 'linear'

    return size


def convert_efx_to_json(efx_path, effect_id):
    """Convert a single .efx file to a JSON effect definition."""
    segments = parse_efx(efx_path)
    basename = os.path.splitext(os.path.basename(efx_path))[0]

    display_name = basename.replace('_', ' ').title()
    if '_mp' in basename:
        display_name = display_name.replace(' Mp', ' (MP)')

    return {
        'id': effect_id,
        'displayName': display_name,
        'segments': segments
    }


def main():
    efx_dir = r'd:\sof2_extract\base\effects'
    output_dir = r'd:\RemakeSoF\Assets\Resources\Data\Effects'

    # List of gore/blood effects to convert (missing from current JSON files)
    gore_blood_files = [
        # Blood effects (MP versions preferred)
        'arterial_squirt_long_mp.efx',
        'arterial_squirt_med_mp.efx',
        'arterial_squirt_small_mp.efx',
        'blood_drip.efx',
        'blood_impact.efx',
        'blood_jugular_squirt.efx',
        'blood_pool_mp.efx',
        'blood_splat_mp.efx',
        'blood_splat_mp_small.efx',
        'blood_splat_smll_mp.efx',
        'blood_spurt_mp.efx',
        'blood_squirt_mp.efx',
        'blood_squirt_splat_mp.efx',
        'blood_trail.efx',
        'mp_blood_squirt_small.efx',
        # Gore effects
        'gore_bit.efx',
        'gore_bloodygib.efx',
        'gore_gib_body.efx',
        'gore_gib_small.efx',
        'gore_mist.efx',
        'gore_mist_small_vergara_head.efx',
        'gore_organ_splat.efx',
        'gore_organ_splat_gib.efx',
        # Gut/head
        'gut_bleed.efx',
        'gut_model.efx',
        'head_pop_mp.efx',
        'head_pop_mp_temp.efx',
    ]

    effects = []
    converted = 0
    failed = []

    for filename in gore_blood_files:
        filepath = os.path.join(efx_dir, filename)
        if not os.path.exists(filepath):
            print(f'  SKIP (not found): {filename}')
            failed.append(filename)
            continue

        basename = os.path.splitext(filename)[0]
        effect_id = f'effects/{basename}'

        try:
            effect = convert_efx_to_json(filepath, effect_id)
            effects.append(effect)
            converted += 1
            print(f'  OK: {filename} -> {effect_id} ({len(effect["segments"])} segments)')
        except Exception as e:
            import traceback
            print(f'  FAIL: {filename}: {e}')
            traceback.print_exc()
            failed.append(filename)

    # Write output
    output_path = os.path.join(output_dir, 'SoF2_Effects_gore_blood.json')
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(effects, f, indent=2, ensure_ascii=False)

    print(f'\nConverted {converted}/{len(gore_blood_files)} effects to {output_path}')
    if failed:
        print(f'Failed/skipped: {", ".join(failed)}')


if __name__ == '__main__':
    main()
