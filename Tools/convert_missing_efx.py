"""
Convert the 13 missing SoF2 .efx impact files to JSON and insert them into existing JSON files.
Uses the same unit conversion (Q3 units to Unity meters: / 39.3701) as the existing effects.

This is a one-time conversion tool.
"""
import json
import re
import os
import sys

# Q3 unit to Unity meter conversion factor
Q3_TO_UNITY = 1.0 / 39.3701
# Gravity conversion: Q3 gravity units to Unity gravity modifier
# In existing JSONs, gravity -800 Q3 becomes ~0.2589 Unity (800/3090 ≈ 0.2589)
# So Q3_GRAVITY_SCALE ≈ 1/3090
Q3_GRAVITY_SCALE = 1.0 / 3090.0

EFX_DIR = r"d:\sof2_extract\base\effects"
JSON_DIR = r"d:\RemakeSoF\Assets\Resources\Data\Effects"

# Mapping: efx_basename -> (target_json_file, effect_id, displayName)
MISSING_EFFECTS = {
    "impact_default2": ("SoF2_Effects_impact_default.json", "effects/impact_default2", "Impact Default 2"),
    "impact_flesh2": ("SoF2_Effects_impact_flesh.json", "effects/impact_flesh2", "Impact Flesh 2"),
    "impact_flesh_12g": ("SoF2_Effects_impact_flesh.json", "effects/impact_flesh_12g", "Impact Flesh 12G"),
    "impact_flesh_old": ("SoF2_Effects_impact_flesh.json", "effects/impact_flesh_old", "Impact Flesh Old"),
    "impact_grass2": ("SoF2_Effects_impact_grass.json", "effects/impact_grass2", "Impact Grass 2"),
    "impact_ice_belt": ("SoF2_Effects_impact_ice.json", "effects/impact_ice_belt", "Impact Ice Belt"),
    "impact_leaves-dry_belt": ("SoF2_Effects_impact_leaves.json", "effects/impact_leaves-dry_belt", "Impact Leaves Dry Belt"),
    "impact_leaves-green_belt": ("SoF2_Effects_impact_leaves.json", "effects/impact_leaves-green_belt", "Impact Leaves Green Belt"),
    "impact_mud_belt": ("SoF2_Effects_impact_mud.json", "effects/impact_mud_belt", "Impact Mud Belt"),
    "impact_player": ("SoF2_Effects_impact_flesh.json", "effects/impact_player", "Impact Player"),
    "impact_player_mp": ("SoF2_Effects_impact_flesh.json", "effects/impact_player_mp", "Impact Player MP"),
    "impact_rock_belt": ("SoF2_Effects_impact_rock.json", "effects/impact_rock_belt", "Impact Rock Belt"),
    "impact_wood2": ("SoF2_Effects_impact_wood.json", "effects/impact_wood2", "Impact Wood 2"),
}


def parse_efx(filepath):
    """Parse an .efx file and return a list of blocks."""
    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()

    # Remove comments
    content = re.sub(r'//[^\n]*', '', content)

    blocks = []
    # Find top-level blocks: BlockType\n{ ... }
    # Block types: Particle, Tail, Decal, Light, CameraShake, Sound, Line, Emitter, OrientedParticle
    block_pattern = re.compile(
        r'(Particle|Tail|Decal|Light|CameraShake|Sound|Line|Emitter|OrientedParticle)\s*\{',
        re.IGNORECASE
    )

    pos = 0
    while pos < len(content):
        match = block_pattern.search(content, pos)
        if not match:
            break

        block_type = match.group(1)
        brace_start = match.end() - 1
        # Find matching closing brace
        depth = 0
        i = brace_start
        while i < len(content):
            if content[i] == '{':
                depth += 1
            elif content[i] == '}':
                depth -= 1
                if depth == 0:
                    break
            i += 1

        block_content = content[brace_start + 1:i]
        blocks.append((block_type, block_content))
        pos = i + 1

    return blocks


def parse_values(text):
    """Parse space-separated numeric values from text."""
    nums = re.findall(r'-?\d+(?:\.\d+)?(?:e[+-]?\d+)?', text)
    return [float(n) for n in nums]


def parse_block_properties(block_content):
    """Parse properties from a block's content."""
    props = {}
    lines = block_content.split('\n')

    i = 0
    while i < len(lines):
        line = lines[i].strip()

        if not line:
            i += 1
            continue

        # Check for sub-blocks: name { ... }
        sub_match = re.match(r'^(\w+)\s*$', line)
        if sub_match and i + 1 < len(lines) and lines[i + 1].strip() == '{':
            sub_name = sub_match.group(1)
            # Find closing brace
            depth = 0
            j = i + 1
            sub_lines = []
            while j < len(lines):
                l = lines[j].strip()
                if l == '{':
                    depth += 1
                elif l == '}':
                    depth -= 1
                    if depth == 0:
                        break
                elif depth > 0:
                    sub_lines.append(l)
                j += 1
            props[sub_name] = parse_sub_block(sub_lines)
            i = j + 1
            continue

        # Check for array: name\n[\n...\n]
        arr_match = re.match(r'^(\w+)\s*$', line)
        if arr_match and i + 1 < len(lines) and lines[i + 1].strip() == '[':
            arr_name = arr_match.group(1)
            j = i + 2
            arr_items = []
            while j < len(lines):
                l = lines[j].strip()
                if l == ']':
                    break
                if l:
                    arr_items.append(l)
                j += 1
            props[arr_name] = arr_items
            i = j + 1
            continue

        # Simple key-value: name\tvalue1 value2 ...
        kv_match = re.match(r'^(\w+)\s+(.+)$', line)
        if kv_match:
            key = kv_match.group(1)
            value = kv_match.group(2).strip()
            props[key] = value
            i += 1
            continue

        i += 1

    return props


def parse_sub_block(lines):
    """Parse lines within a sub-block like alpha, size, rgb."""
    props = {}
    for line in lines:
        line = line.strip()
        if not line:
            continue
        kv = re.match(r'^(\w+)\s+(.+)$', line)
        if kv:
            props[kv.group(1)] = kv.group(2).strip()
    return props


def convert_particle(props):
    """Convert a Particle block to JSON segment format."""
    segment = {"type": "particle"}

    # Name
    if 'name' in props:
        segment['name'] = props['name']

    # Shaders -> texture (first one)
    if 'shaders' in props:
        segment['texture'] = props['shaders'][0]

    # Flags
    flags_str = props.get('flags', '')
    if flags_str:
        flag_list = flags_str.split()
        segment['flags'] = flag_list

    spawn_flags_str = props.get('spawnFlags', '')
    if spawn_flags_str:
        segment['spawnFlags'] = spawn_flags_str.split()

    # Particle properties
    particle = {"burst": True}

    # Count
    count_vals = parse_values(props.get('count', '1'))
    particle['countMin'] = int(count_vals[0]) if count_vals else 1
    particle['countMax'] = int(count_vals[1]) if len(count_vals) > 1 else particle['countMin']

    # Life (ms -> seconds)
    life_vals = parse_values(props.get('life', '1000'))
    particle['lifetimeMin'] = round(life_vals[0] / 1000.0, 4)
    particle['lifetimeMax'] = round(life_vals[1] / 1000.0, 4) if len(life_vals) > 1 else particle['lifetimeMin']

    # Delay (ms -> seconds)
    if 'delay' in props:
        delay_vals = parse_values(props['delay'])
        if delay_vals and delay_vals[0] > 0:
            particle['delayMin'] = round(delay_vals[0] / 1000.0, 4)
            particle['delayMax'] = round(delay_vals[1] / 1000.0, 4) if len(delay_vals) > 1 else particle['delayMin']

    # Cullrange
    if 'cullrange' in props:
        cull_vals = parse_values(props['cullrange'])
        if cull_vals:
            particle['cullrange'] = round(cull_vals[0] * Q3_TO_UNITY, 4)

    # Rotation
    if 'rotation' in props:
        rot_vals = parse_values(props['rotation'])
        if len(rot_vals) >= 2:
            particle['rotationMin'] = rot_vals[0]
            particle['rotationMax'] = rot_vals[1]

    if 'rotationDelta' in props:
        rd_vals = parse_values(props['rotationDelta'])
        if len(rd_vals) >= 2:
            particle['rotationSpeedMin'] = rd_vals[0] * 20  # rough conversion
            particle['rotationSpeedMax'] = rd_vals[1] * 20

    # Gravity
    if 'gravity' in props:
        grav_vals = parse_values(props['gravity'])
        if grav_vals:
            # Take first value, convert
            particle['gravityModifier'] = round(abs(grav_vals[0]) * Q3_GRAVITY_SCALE, 4)
            if grav_vals[0] < 0:
                particle['gravityModifier'] = round(particle['gravityModifier'], 4)

    # Velocity (convert Q3 units to Unity)
    if 'velocity' in props:
        vel_vals = parse_values(props['velocity'])
        if len(vel_vals) >= 6:
            # EFX: x y z x y z (min/max) -> JSON: [y, z, x] mapped to Unity XYZ
            # In existing JSONs, the mapping seems to be direct Q3->Unity with unit conversion
            particle['velocityMin'] = [
                round(vel_vals[1] * Q3_TO_UNITY, 4),
                round(vel_vals[2] * Q3_TO_UNITY, 4),
                round(vel_vals[0] * Q3_TO_UNITY, 4)
            ]
            particle['velocityMax'] = [
                round(vel_vals[4] * Q3_TO_UNITY, 4),
                round(vel_vals[5] * Q3_TO_UNITY, 4),
                round(vel_vals[3] * Q3_TO_UNITY, 4)
            ]
        elif len(vel_vals) >= 3:
            particle['velocityMin'] = [
                round(vel_vals[1] * Q3_TO_UNITY, 4),
                round(vel_vals[2] * Q3_TO_UNITY, 4),
                round(vel_vals[0] * Q3_TO_UNITY, 4)
            ]
            particle['velocityMax'] = particle['velocityMin']

    # Origin
    if 'origin' in props:
        orig_vals = parse_values(props['origin'])
        if len(orig_vals) >= 6:
            particle['originMin'] = [
                round(orig_vals[1] * Q3_TO_UNITY, 4),
                round(orig_vals[2] * Q3_TO_UNITY, 4),
                round(orig_vals[0] * Q3_TO_UNITY, 4)
            ]
            particle['originMax'] = [
                round(orig_vals[4] * Q3_TO_UNITY, 4),
                round(orig_vals[5] * Q3_TO_UNITY, 4),
                round(orig_vals[3] * Q3_TO_UNITY, 4)
            ]

    # Radius
    if 'radius' in props:
        rad_vals = parse_values(props['radius'])
        if rad_vals:
            particle['radius'] = round(rad_vals[0] * Q3_TO_UNITY, 4)

    # Bounce
    if 'bounce' in props:
        bounce_vals = parse_values(props['bounce'])
        if bounce_vals:
            particle['bounceMin'] = bounce_vals[0]
            particle['bounceMax'] = bounce_vals[1] if len(bounce_vals) > 1 else bounce_vals[0]

    segment['particle'] = particle

    # Alpha
    if 'alpha' in props and isinstance(props['alpha'], dict):
        alpha = {}
        a = props['alpha']
        start_vals = parse_values(a.get('start', '1'))
        alpha['startMin'] = start_vals[0] if start_vals else 1.0
        alpha['startMax'] = start_vals[1] if len(start_vals) > 1 else alpha['startMin']
        end_vals = parse_values(a.get('end', '0'))
        alpha['endMin'] = end_vals[0] if end_vals else 0.0
        alpha['endMax'] = end_vals[1] if len(end_vals) > 1 else alpha['endMin']
        if 'parm' in a:
            parm_vals = parse_values(a['parm'])
            if parm_vals:
                alpha['parm'] = parm_vals[0]
        if 'flags' in a:
            alpha['curve'] = a['flags']
        segment['alpha'] = alpha

    # Color / RGB
    if 'rgb' in props and isinstance(props['rgb'], dict):
        color = {}
        c = props['rgb']
        if 'start' in c:
            start_vals = parse_values(c['start'])
            if len(start_vals) >= 6:
                color['startMin'] = [round(v, 4) for v in start_vals[0:3]]
                color['startMax'] = [round(v, 4) for v in start_vals[3:6]]
            elif len(start_vals) >= 3:
                color['startMin'] = [round(v, 4) for v in start_vals[0:3]]
                color['startMax'] = color['startMin']
        if 'end' in c:
            end_vals = parse_values(c['end'])
            if len(end_vals) >= 6:
                color['endMin'] = [round(v, 4) for v in end_vals[0:3]]
                color['endMax'] = [round(v, 4) for v in end_vals[3:6]]
            elif len(end_vals) >= 3:
                color['endMin'] = [round(v, 4) for v in end_vals[0:3]]
                color['endMax'] = color['endMin']
        if 'flags' in c:
            color['curve'] = c['flags']
        if color:
            segment['color'] = color

    # Size (convert to Unity units)
    if 'size' in props and isinstance(props['size'], dict):
        size = {}
        s = props['size']
        if 'start' in s:
            start_vals = parse_values(s['start'])
            size['startMin'] = round(start_vals[0] * Q3_TO_UNITY, 4)
            size['startMax'] = round(start_vals[1] * Q3_TO_UNITY, 4) if len(start_vals) > 1 else size['startMin']
        if 'end' in s:
            end_vals = parse_values(s['end'])
            size['endMin'] = round(end_vals[0] * Q3_TO_UNITY, 4)
            size['endMax'] = round(end_vals[1] * Q3_TO_UNITY, 4) if len(end_vals) > 1 else size['endMin']
        if 'parm' in s:
            parm_vals = parse_values(s['parm'])
            if parm_vals:
                size['parm'] = parm_vals[0]
        if 'flags' in s:
            size['curve'] = s['flags']
        segment['size'] = size

    # Impact FX
    if 'impactfx' in props or 'impactFx' in props:
        fx_list = props.get('impactfx', props.get('impactFx', []))
        if fx_list:
            segment['impactFx'] = fx_list

    return segment


def convert_tail(props):
    """Convert a Tail block to JSON segment format."""
    segment = {"type": "tail"}

    if 'name' in props:
        segment['name'] = props['name']

    if 'shaders' in props:
        segment['texture'] = props['shaders'][0]

    flags_str = props.get('flags', '')
    if flags_str:
        segment['flags'] = flags_str.split()

    spawn_flags_str = props.get('spawnFlags', '')
    if spawn_flags_str:
        segment['spawnFlags'] = spawn_flags_str.split()

    trail = {}

    count_vals = parse_values(props.get('count', '1'))
    trail['countMin'] = int(count_vals[0]) if count_vals else 1
    trail['countMax'] = int(count_vals[1]) if len(count_vals) > 1 else trail['countMin']

    life_vals = parse_values(props.get('life', '1000'))
    trail['lifetimeMin'] = round(life_vals[0] / 1000.0, 4)
    trail['lifetimeMax'] = round(life_vals[1] / 1000.0, 4) if len(life_vals) > 1 else trail['lifetimeMin']

    if 'cullrange' in props:
        cull_vals = parse_values(props['cullrange'])
        if cull_vals:
            trail['cullrange'] = round(cull_vals[0] * Q3_TO_UNITY, 4)

    if 'velocity' in props:
        vel_vals = parse_values(props['velocity'])
        if len(vel_vals) >= 6:
            trail['velocityMin'] = [round(v * Q3_TO_UNITY, 4) for v in [vel_vals[1], vel_vals[2], vel_vals[0]]]
            trail['velocityMax'] = [round(v * Q3_TO_UNITY, 4) for v in [vel_vals[4], vel_vals[5], vel_vals[3]]]

    # Width from size
    if 'size' in props and isinstance(props['size'], dict):
        s = props['size']
        if 'start' in s:
            start_vals = parse_values(s['start'])
            trail['startWidth'] = round(start_vals[0] * Q3_TO_UNITY, 4)
        if 'end' in s:
            end_vals = parse_values(s['end'])
            trail['endWidth'] = round(end_vals[0] * Q3_TO_UNITY, 4)

    # Length
    if 'length' in props and isinstance(props['length'], dict):
        l = props['length']
        if 'start' in l:
            length_vals = parse_values(l['start'])
            trail['lengthMin'] = round(abs(length_vals[0]) * Q3_TO_UNITY, 4)
        if 'end' in l:
            length_vals = parse_values(l['end'])
            trail['lengthMax'] = round(abs(length_vals[0]) * Q3_TO_UNITY, 4)

    segment['trail'] = trail

    # Alpha
    if 'alpha' in props and isinstance(props['alpha'], dict):
        alpha = {}
        a = props['alpha']
        start_vals = parse_values(a.get('start', '1'))
        alpha['startMin'] = start_vals[0] if start_vals else 1.0
        alpha['startMax'] = start_vals[1] if len(start_vals) > 1 else alpha['startMin']
        end_vals = parse_values(a.get('end', '0'))
        alpha['endMin'] = end_vals[0] if end_vals else 0.0
        alpha['endMax'] = end_vals[1] if len(end_vals) > 1 else alpha['endMin']
        if 'flags' in a:
            alpha['curve'] = a['flags']
        segment['alpha'] = alpha

    # Color
    if 'rgb' in props and isinstance(props['rgb'], dict):
        color = {}
        c = props['rgb']
        if 'start' in c:
            start_vals = parse_values(c['start'])
            if len(start_vals) >= 3:
                color['startMin'] = [round(v, 4) for v in start_vals[0:3]]
            if len(start_vals) >= 6:
                color['startMax'] = [round(v, 4) for v in start_vals[3:6]]
        if color:
            segment['color'] = color

    return segment


def convert_decal(props):
    """Convert a Decal block to JSON segment format."""
    segment = {"type": "decal"}

    if 'name' in props:
        segment['name'] = props['name']

    if 'shaders' in props:
        segment['texture'] = props['shaders'][0]

    decal = {}

    if 'size' in props and isinstance(props['size'], dict):
        s = props['size']
        if 'start' in s:
            size_vals = parse_values(s['start'])
            decal['sizeMin'] = round(size_vals[0] * Q3_TO_UNITY, 4)
            decal['sizeMax'] = round(size_vals[1] * Q3_TO_UNITY, 4) if len(size_vals) > 1 else decal['sizeMin']

    if 'rotation' in props:
        rot_vals = parse_values(props['rotation'])
        if len(rot_vals) >= 2:
            decal['rotationMin'] = rot_vals[0]
            decal['rotationMax'] = rot_vals[1]

    if 'alpha' in props and isinstance(props['alpha'], dict):
        a = props['alpha']
        if 'start' in a:
            alpha_vals = parse_values(a['start'])
            decal['alphaMin'] = alpha_vals[0]
            decal['alphaMax'] = alpha_vals[1] if len(alpha_vals) > 1 else alpha_vals[0]

    if 'cullrange' in props:
        cull_vals = parse_values(props['cullrange'])
        if cull_vals:
            decal['cullrange'] = round(cull_vals[0] * Q3_TO_UNITY, 4)

    decal['lifetime'] = 30  # Default lifetime for decals

    segment['decal'] = decal
    return segment


def convert_sound(props):
    """Convert a Sound block to JSON segment format."""
    segment = {"type": "sound", "name": "Sound"}

    if 'name' in props:
        segment['name'] = props['name']

    sound = {}
    if 'sounds' in props:
        sound['files'] = props['sounds']

    delay = 0.0
    if 'delay' in props:
        delay_vals = parse_values(props['delay'])
        if delay_vals:
            delay = round(delay_vals[0] / 1000.0, 4)
    sound['delay'] = delay

    if 'cullrange' in props:
        cull_vals = parse_values(props['cullrange'])
        if cull_vals:
            sound['cullrange'] = round(cull_vals[0] * Q3_TO_UNITY, 4)

    segment['sound'] = sound
    return segment


def convert_emitter(props):
    """Convert an Emitter block to JSON segment format."""
    segment = {"type": "emitter"}

    if 'name' in props:
        segment['name'] = props['name']

    if 'models' in props:
        segment['model'] = props['models'][0]

    flags_str = props.get('flags', '')
    if flags_str:
        segment['flags'] = flags_str.split()

    emitter = {}

    count_vals = parse_values(props.get('count', '1'))
    emitter['countMin'] = int(count_vals[0]) if count_vals else 1
    emitter['countMax'] = int(count_vals[1]) if len(count_vals) > 1 else emitter['countMin']

    life_vals = parse_values(props.get('life', '10000'))
    emitter['lifetimeMin'] = round(life_vals[0] / 1000.0, 4)

    if 'cullrange' in props:
        cull_vals = parse_values(props['cullrange'])
        if cull_vals:
            emitter['cullrange'] = round(cull_vals[0] * Q3_TO_UNITY, 4)

    if 'size' in props and isinstance(props['size'], dict):
        s = props['size']
        if 'start' in s:
            size_vals = parse_values(s['start'])
            emitter['sizeMin'] = round(size_vals[0] * Q3_TO_UNITY, 4)
            emitter['sizeMax'] = round(size_vals[1] * Q3_TO_UNITY, 4) if len(size_vals) > 1 else emitter['sizeMin']

    if 'impactfx' in props or 'impactFx' in props:
        fx_list = props.get('impactfx', props.get('impactFx', []))
        if fx_list:
            emitter['impactFx'] = fx_list

    segment['emitter'] = emitter
    return segment


def convert_efx_to_json(efx_path, effect_id, display_name):
    """Convert an entire .efx file to a JSON effect definition."""
    blocks = parse_efx(efx_path)

    effect = {
        "id": effect_id,
        "displayName": display_name,
        "segments": []
    }

    for block_type, block_content in blocks:
        props = parse_block_properties(block_content)

        bt = block_type.lower()
        if bt == 'particle':
            segment = convert_particle(props)
        elif bt == 'tail':
            segment = convert_tail(props)
        elif bt == 'decal':
            segment = convert_decal(props)
        elif bt == 'sound':
            segment = convert_sound(props)
        elif bt == 'emitter':
            segment = convert_emitter(props)
        elif bt in ('line', 'orientedparticle'):
            # Skip Line and OrientedParticle as they're not supported
            continue
        elif bt == 'light':
            continue
        elif bt == 'camerashake':
            continue
        else:
            continue

        effect['segments'].append(segment)

    return effect


def main():
    # Group effects by target JSON file
    by_target = {}
    for efx_name, (target_file, effect_id, display_name) in MISSING_EFFECTS.items():
        if target_file not in by_target:
            by_target[target_file] = []
        by_target[target_file].append((efx_name, effect_id, display_name))

    for target_file, effects in sorted(by_target.items()):
        target_path = os.path.join(JSON_DIR, target_file)

        # Read existing JSON
        with open(target_path, 'r', encoding='utf-8') as f:
            existing = json.load(f)

        existing_ids = {e['id'] for e in existing}

        added = 0
        for efx_name, effect_id, display_name in effects:
            if effect_id in existing_ids:
                print(f"  SKIP {effect_id} (already exists in {target_file})")
                continue

            efx_path = os.path.join(EFX_DIR, f"{efx_name}.efx")
            if not os.path.exists(efx_path):
                print(f"  ERROR: {efx_path} not found!")
                continue

            effect = convert_efx_to_json(efx_path, effect_id, display_name)
            existing.append(effect)
            added += 1
            print(f"  ADDED {effect_id} -> {target_file} ({len(effect['segments'])} segments)")

        if added > 0:
            # Write updated JSON
            with open(target_path, 'w', encoding='utf-8', newline='\n') as f:
                json.dump(existing, f, indent=2, ensure_ascii=False)
                f.write('\n')
            print(f"  -> Saved {target_file} ({len(existing)} effects total)")
        else:
            print(f"  -> No changes to {target_file}")

    print("\nDone! All 13 missing effects converted.")


if __name__ == '__main__':
    main()
