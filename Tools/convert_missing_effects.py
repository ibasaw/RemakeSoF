"""
Comprehensive EFX to JSON converter for the 3 missing categories:
1. Inworld muzzle flashes (3rd-person)
2. Fire/Incendiary effects
3. Explosion variants

Conversion rules (from /memories/repo/effect-conversion-rules.md):
- Distance/Size: SoF2 QU * 0.0254 = Unity meters
- Time: SoF2 ms / 1000 = Unity seconds
- World-space effects: SoF2 [X,Y,Z] -> Unity [Y,Z,X]  (remap axes)
- Muzzle effects: NO remap (muzzle bone X=forward matches local space)
- Gravity: particle gravityModifier = -(SoF2_value * 0.0254 / 9.81)
- Rotation: degrees stay as degrees
"""

import json
import os
import re
import sys


EFX_ROOT = r'd:\sof2_extract\base\effects'
JSON_ROOT = r'd:\RemakeSoF\Assets\Resources\Data\Effects'

SCALE = 0.0254  # QU to meters
GRAVITY_SCALE = 0.0254 / 9.81  # For particle gravity modifier


def parse_efx(filepath):
    """Parse a .efx file into a list of segment dicts."""
    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        text = f.read()

    # Find all top-level blocks: Particle, Line, OrientedParticle, Light, CameraShake, Decal, Sound, Emitter, Tail
    segments = []
    # Regex to find top-level type { ... }
    # We need to handle nested braces
    i = 0
    while i < len(text):
        # Look for a type keyword at start of line
        m = re.match(r'(Particle|Line|OrientedParticle|Light|CameraShake|Decal|Sound|Emitter|Tail)\s*\{', text[i:])
        if m:
            seg_type = m.group(1)
            # Find matching closing brace
            brace_count = 0
            start = i + m.start()
            j = i + m.end() - 1  # position of opening {
            brace_count = 1
            j += 1
            while j < len(text) and brace_count > 0:
                if text[j] == '{':
                    brace_count += 1
                elif text[j] == '}':
                    brace_count -= 1
                j += 1
            body = text[i + m.end():j - 1]
            segments.append((seg_type, body))
            i = j
        else:
            i += 1
    return segments


def parse_values(line):
    """Parse space-separated values from a line, return as list of floats or strings."""
    parts = line.strip().split()
    result = []
    for p in parts:
        try:
            # Handle scientific notation like 2e+004
            if 'e' in p.lower() or '.' in p:
                result.append(float(p))
            else:
                result.append(int(p))
        except ValueError:
            result.append(p)
    return result


def parse_sub_block(body, block_name):
    """Parse a sub-block like rgb { ... } or alpha { ... } from segment body."""
    pattern = rf'{block_name}\s*\{{([^}}]*)\}}'
    m = re.search(pattern, body)
    if not m:
        return None
    content = m.group(1)
    result = {}
    for line in content.strip().split('\n'):
        line = line.strip()
        if not line:
            continue
        parts = line.split(None, 1)
        if len(parts) >= 2:
            key = parts[0]
            vals = parse_values(parts[1])
            result[key] = vals
        elif len(parts) == 1:
            result[parts[0]] = True
    return result


def parse_shaders(body):
    """Parse shaders [ ... ] block."""
    m = re.search(r'shaders\s*\[\s*([^\]]*)\]', body)
    if not m:
        return None
    content = m.group(1).strip()
    shaders = [s.strip() for s in content.split('\n') if s.strip()]
    return shaders[0] if shaders else None


def parse_sounds(body):
    """Parse sounds [ ... ] block."""
    m = re.search(r'sounds\s*\[\s*([^\]]*)\]', body)
    if not m:
        return None
    content = m.group(1).strip()
    sounds = [s.strip() for s in content.split('\n') if s.strip()]
    return sounds


def get_simple_value(body, key):
    """Get a simple key-value line from segment body."""
    pattern = rf'^\s*{key}\s+(.+)$'
    m = re.search(pattern, body, re.MULTILINE)
    if m:
        return parse_values(m.group(1))
    return None


def convert_origin_world(vals):
    """Convert origin values from SoF2 [X,Y,Z] to Unity [Y,Z,X] with scaling."""
    if len(vals) == 6:
        # originMin and originMax: SoF2 [X1,Y1,Z1, X2,Y2,Z2] -> Unity
        x1, y1, z1, x2, y2, z2 = [v * SCALE for v in vals]
        return [round(y1, 6), round(z1, 6), round(x1, 6)], [round(y2, 6), round(z2, 6), round(x2, 6)]
    elif len(vals) == 3:
        x, y, z = [v * SCALE for v in vals]
        return [round(y, 6), round(z, 6), round(x, 6)]
    return None


def convert_origin_muzzle(vals):
    """Convert origin for muzzle effects - NO remap, just scale."""
    if len(vals) == 6:
        return [round(v * SCALE, 6) for v in vals[:3]], [round(v * SCALE, 6) for v in vals[3:]]
    elif len(vals) == 3:
        return [round(v * SCALE, 6) for v in vals]
    return None


def convert_velocity_world(vals):
    """Convert velocity from SoF2 to Unity world space with remap."""
    if len(vals) == 6:
        x1, y1, z1, x2, y2, z2 = [v * SCALE for v in vals]
        return [round(y1, 4), round(z1, 4), round(x1, 4)], [round(y2, 4), round(z2, 4), round(x2, 4)]
    elif len(vals) == 3:
        x, y, z = [v * SCALE for v in vals]
        return [round(y, 4), round(z, 4), round(x, 4)]
    return None


def convert_velocity_muzzle(vals):
    """Convert velocity for muzzle effects - NO remap, just scale."""
    if len(vals) == 6:
        return [round(v * SCALE, 4) for v in vals[:3]], [round(v * SCALE, 4) for v in vals[3:]]
    elif len(vals) == 3:
        return [round(v * SCALE, 4) for v in vals]
    return None


def convert_acceleration_world(vals):
    """Convert acceleration with world-space remap."""
    if len(vals) == 6:
        x1, y1, z1, x2, y2, z2 = [v * SCALE for v in vals]
        return [round(y1, 4), round(z1, 4), round(x1, 4)], [round(y2, 4), round(z2, 4), round(x2, 4)]
    elif len(vals) == 3:
        x, y, z = [v * SCALE for v in vals]
        return [round(y, 4), round(z, 4), round(x, 4)]
    return None


def convert_acceleration_muzzle(vals):
    """Convert acceleration for muzzle effects - NO remap, just scale."""
    if len(vals) == 6:
        return [round(v * SCALE, 4) for v in vals[:3]], [round(v * SCALE, 4) for v in vals[3:]]
    elif len(vals) == 3:
        return [round(v * SCALE, 4) for v in vals]
    return None


def convert_gravity(vals):
    """Convert gravity to Unity particle gravityModifier."""
    if len(vals) == 1:
        return round(-(vals[0] * GRAVITY_SCALE), 6)
    elif len(vals) == 2:
        return round(-(vals[0] * GRAVITY_SCALE), 6), round(-(vals[1] * GRAVITY_SCALE), 6)
    return None


def convert_segment(seg_type, body, is_muzzle=False):
    """Convert a single segment to JSON dict."""
    result = {}

    # Name
    name_val = get_simple_value(body, 'name')
    if name_val:
        result['name'] = ' '.join(str(v) for v in name_val)

    # Type mapping
    type_map = {
        'Particle': 'particle',
        'OrientedParticle': 'orientedParticle',
        'Line': 'line',
        'Light': 'light',
        'CameraShake': 'cameraShake',
        'Decal': 'decal',
        'Sound': 'sound',
        'Emitter': 'emitter',
        'Tail': 'tail'
    }
    result['type'] = type_map.get(seg_type, seg_type.lower())

    # Sound type
    if seg_type == 'Sound':
        sounds = parse_sounds(body)
        if sounds:
            result['sounds'] = sounds
        return result

    # CameraShake
    if seg_type == 'CameraShake':
        cs = {}
        life = get_simple_value(body, 'life')
        if life:
            cs['duration'] = round(life[0] / 1000, 4)
        bounce = get_simple_value(body, 'bounce')
        if bounce:
            cs['intensity'] = bounce[0]
        radius = get_simple_value(body, 'radius')
        if radius:
            cs['radius'] = round(radius[0] * SCALE, 4)
        result['cameraShake'] = cs
        return result

    # Light
    if seg_type == 'Light':
        lt = {}
        life = get_simple_value(body, 'life')
        if life:
            lt['lifetime'] = round(life[0] / 1000, 4)
        size_block = parse_sub_block(body, 'size')
        if size_block:
            if 'start' in size_block:
                vals = size_block['start']
                lt['range'] = round(vals[0] * SCALE, 4)
                if len(vals) > 1:
                    lt['rangeMax'] = round(vals[1] * SCALE, 4)
            if 'end' in size_block:
                vals = size_block['end']
                lt['rangeEnd'] = round(vals[0] * SCALE, 4)
        lt['intensity'] = 2.0
        result['light'] = lt
        return result

    # Decal
    if seg_type == 'Decal':
        dc = {}
        rotation = get_simple_value(body, 'rotation')
        if rotation and len(rotation) >= 2:
            dc['rotationMin'] = rotation[0]
            dc['rotationMax'] = rotation[1]
        elif rotation:
            dc['rotationMin'] = rotation[0]

        alpha_block = parse_sub_block(body, 'alpha')
        if alpha_block and 'start' in alpha_block:
            vals = alpha_block['start']
            if len(vals) >= 1:
                dc['lifetime'] = 30  # Default decal lifetime

        size_block = parse_sub_block(body, 'size')
        if size_block and 'start' in size_block:
            vals = size_block['start']
            if len(vals) >= 2:
                dc['sizeMin'] = round(vals[0] * SCALE, 4)
                dc['sizeMax'] = round(vals[1] * SCALE, 4)
            elif len(vals) == 1:
                dc['sizeMin'] = round(vals[0] * SCALE, 4)
                dc['sizeMax'] = round(vals[0] * SCALE, 4)

        shader = parse_shaders(body)
        if shader:
            dc['texture'] = shader

        result['decal'] = dc
        return result

    # --- Standard particle/line/orientedParticle/tail segment ---

    # Texture (shader)
    shader = parse_shaders(body)
    if shader:
        result['texture'] = shader

    # Flags - only parse top-level flags, not flags inside sub-blocks (alpha/size/rgb)
    # We need to find 'flags' lines that are NOT inside a { } sub-block
    CURVE_FLAGS = {'linear', 'nonlinear', 'clamp', 'wave', 'random'}
    VALID_FLAGS = {'useAlpha', 'usePhysics', 'impactKills', 'depthHack', 'impactFx'}
    VALID_SPAWN_FLAGS = {'rgbComponentInterpolation', 'evenDistribution', 'absoluteAccel',
                         'orgOnSphere', 'orgOnCylinder', 'axisFromSphere'}

    flags_val = get_simple_value(body, 'flags')
    spawn_flags = get_simple_value(body, 'spawnFlags')
    flag_list = []
    if flags_val:
        for f in flags_val:
            if isinstance(f, str) and f in VALID_FLAGS:
                flag_list.append(f)
    if spawn_flags:
        for f in spawn_flags:
            if isinstance(f, str) and f in VALID_SPAWN_FLAGS:
                flag_list.append(f)
    if flag_list:
        result['flags'] = flag_list

    # Particle block
    particle = {}

    # Count
    count = get_simple_value(body, 'count')
    if count:
        particle['countMin'] = count[0]
        particle['countMax'] = count[1] if len(count) > 1 else count[0]

    # Lifetime
    life = get_simple_value(body, 'life')
    if life:
        particle['lifetimeMin'] = round(life[0] / 1000, 4)
        particle['lifetimeMax'] = round(life[1] / 1000, 4) if len(life) > 1 else round(life[0] / 1000, 4)

    # Delay
    delay = get_simple_value(body, 'delay')
    if delay:
        particle['delayMin'] = round(delay[0] / 1000, 4)
        particle['delayMax'] = round(delay[1] / 1000, 4) if len(delay) > 1 else round(delay[0] / 1000, 4)

    # Rotation
    rotation = get_simple_value(body, 'rotation')
    if rotation:
        particle['rotationMin'] = rotation[0]
        particle['rotationMax'] = rotation[1] if len(rotation) > 1 else rotation[0]

    # RotationDelta (= rotationSpeed in our JSON)
    rot_delta = get_simple_value(body, 'rotationDelta')
    if rot_delta:
        particle['rotationSpeedMin'] = rot_delta[0]
        particle['rotationSpeedMax'] = rot_delta[1] if len(rot_delta) > 1 else rot_delta[0]

    # Cullrange
    cullrange = get_simple_value(body, 'cullrange')
    if cullrange:
        particle['cullrange'] = round(cullrange[0] * SCALE, 4)

    # Origin
    origin = get_simple_value(body, 'origin')
    if origin:
        if is_muzzle:
            converted = convert_origin_muzzle(origin)
        else:
            converted = convert_origin_world(origin)
        if converted:
            if isinstance(converted, tuple) and len(converted) == 2:
                particle['originMin'] = converted[0]
                particle['originMax'] = converted[1]
            else:
                particle['originMin'] = converted
                particle['originMax'] = converted

    # Velocity
    velocity = get_simple_value(body, 'velocity')
    if velocity:
        if is_muzzle:
            converted = convert_velocity_muzzle(velocity)
        else:
            converted = convert_velocity_world(velocity)
        if converted:
            if isinstance(converted, tuple) and len(converted) == 2:
                particle['velocityMin'] = converted[0]
                particle['velocityMax'] = converted[1]
            else:
                particle['velocityMin'] = converted
                particle['velocityMax'] = converted

    # Acceleration
    accel = get_simple_value(body, 'acceleration')
    if accel:
        if is_muzzle:
            converted = convert_acceleration_muzzle(accel)
        else:
            converted = convert_acceleration_world(accel)
        if converted:
            if isinstance(converted, tuple) and len(converted) == 2:
                particle['accelerationMin'] = converted[0]
                particle['accelerationMax'] = converted[1]
            else:
                particle['accelerationMin'] = converted
                particle['accelerationMax'] = converted

    # Gravity
    gravity = get_simple_value(body, 'gravity')
    if gravity:
        grav = convert_gravity(gravity)
        if isinstance(grav, tuple):
            particle['gravityModifierMin'] = grav[0]
            particle['gravityModifierMax'] = grav[1]
        else:
            particle['gravityModifier'] = grav

    # Bounce
    bounce = get_simple_value(body, 'bounce')
    if bounce:
        particle['bounce'] = bounce[0]

    # Burst (count == 0 or count is 1)
    if not count or (count and count[0] <= 1 and (len(count) == 1 or count[1] <= 1)):
        if not count:
            particle['burst'] = True

    if particle:
        result['particle'] = particle

    # RGB
    rgb_block = parse_sub_block(body, 'rgb')
    if rgb_block:
        color = {}
        if 'start' in rgb_block:
            vals = rgb_block['start']
            nums = [v for v in vals if isinstance(v, (int, float))]
            if len(nums) >= 6:
                color['startMin'] = [round(nums[0], 4), round(nums[1], 4), round(nums[2], 4)]
                color['startMax'] = [round(nums[3], 4), round(nums[4], 4), round(nums[5], 4)]
            elif len(nums) >= 3:
                color['startMin'] = [round(nums[0], 4), round(nums[1], 4), round(nums[2], 4)]
        if 'end' in rgb_block:
            vals = rgb_block['end']
            nums = [v for v in vals if isinstance(v, (int, float))]
            if len(nums) >= 6:
                color['endMin'] = [round(nums[0], 4), round(nums[1], 4), round(nums[2], 4)]
                color['endMax'] = [round(nums[3], 4), round(nums[4], 4), round(nums[5], 4)]
            elif len(nums) >= 3:
                color['endMin'] = [round(nums[0], 4), round(nums[1], 4), round(nums[2], 4)]
        if 'flags' in rgb_block:
            flags = rgb_block['flags']
            if isinstance(flags, list):
                color['curve'] = flags[0] if flags else 'linear'
        if color:
            result['color'] = color

    # Alpha
    alpha_block = parse_sub_block(body, 'alpha')
    if alpha_block:
        alpha = {}
        if 'start' in alpha_block:
            vals = alpha_block['start']
            nums = [v for v in vals if isinstance(v, (int, float))]
            if len(nums) >= 2:
                alpha['startMin'] = round(nums[0], 4)
                alpha['startMax'] = round(nums[1], 4)
            elif len(nums) == 1:
                alpha['startMin'] = round(nums[0], 4)
                alpha['startMax'] = round(nums[0], 4)
        if 'end' in alpha_block:
            vals = alpha_block['end']
            nums = [v for v in vals if isinstance(v, (int, float))]
            if len(nums) >= 2:
                alpha['endMin'] = round(nums[0], 4)
                alpha['endMax'] = round(nums[1], 4)
            elif len(nums) == 1:
                alpha['endMin'] = round(nums[0], 4)
                alpha['endMax'] = round(nums[0], 4)
            else:
                alpha['endMin'] = 0
                alpha['endMax'] = 0
        if 'parm' in alpha_block:
            vals = alpha_block['parm']
            nums = [v for v in vals if isinstance(v, (int, float))]
            if nums:
                alpha['parm'] = nums[0]
        if 'flags' in alpha_block:
            flags = alpha_block['flags']
            if isinstance(flags, list):
                flag_strs = [str(f) for f in flags if isinstance(f, str)]
                if 'nonlinear' in flag_strs:
                    alpha['curve'] = 'nonlinear'
                elif 'linear' in flag_strs:
                    alpha['curve'] = 'linear'
        if alpha:
            result['alpha'] = alpha

    # Size
    size_block = parse_sub_block(body, 'size')
    if size_block:
        size = {}
        if 'start' in size_block:
            vals = size_block['start']
            nums = [v for v in vals if isinstance(v, (int, float))]
            if len(nums) >= 2:
                size['startMin'] = round(nums[0] * SCALE, 6)
                size['startMax'] = round(nums[1] * SCALE, 6)
            elif len(nums) == 1:
                size['startMin'] = round(nums[0] * SCALE, 6)
                size['startMax'] = round(nums[0] * SCALE, 6)
        if 'end' in size_block:
            vals = size_block['end']
            nums = [v for v in vals if isinstance(v, (int, float))]
            if len(nums) >= 2:
                size['endMin'] = round(nums[0] * SCALE, 6)
                size['endMax'] = round(nums[1] * SCALE, 6)
            elif len(nums) == 1:
                size['endMin'] = round(nums[0] * SCALE, 6)
                size['endMax'] = round(nums[0] * SCALE, 6)
        if 'flags' in size_block:
            flags = size_block['flags']
            if isinstance(flags, list):
                flag_strs = [str(f) for f in flags if isinstance(f, str)]
                if 'nonlinear' in flag_strs:
                    size['curve'] = 'nonlinear'
                elif 'linear' in flag_strs:
                    size['curve'] = 'linear'
        if size:
            result['size'] = size

    return result


def convert_efx_file(filepath, effect_id, is_muzzle=False):
    """Convert a complete .efx file to a JSON effect dict."""
    segments = parse_efx(filepath)
    if not segments:
        print(f"  WARNING: No segments found in {filepath}")
        return None

    # Build a nice display name from the id
    name_part = effect_id.split('/')[-1]
    display_name = name_part.replace('_', ' ').replace('-', ' ').title()

    effect = {
        'id': effect_id,
        'displayName': display_name,
        'segments': []
    }

    for seg_type, body in segments:
        converted = convert_segment(seg_type, body, is_muzzle=is_muzzle)
        if converted:
            effect['segments'].append(converted)

    return effect


def main():
    # Define all effects to convert, grouped by target JSON file
    conversions = {
        # 1. INWORLD MUZZLE FLASHES - add to existing muzzle_flashes.json
        'muzzle_flashes': {
            'target_json': os.path.join(JSON_ROOT, 'SoF2_Effects_muzzle_flashes.json'),
            'is_muzzle': True,
            'effects': [
                ('muzzle_flashes/mflash_ak74_inworld', 'effects/muzzle_flashes/mflash_ak74_inworld'),
                ('muzzle_flashes/mflash_m3_inworld', 'effects/muzzle_flashes/mflash_m3_inworld'),
                ('muzzle_flashes/mflash_m4_inworld', 'effects/muzzle_flashes/mflash_m4_inworld'),
                ('muzzle_flashes/mflash_m60_inworld', 'effects/muzzle_flashes/mflash_m60_inworld'),
                ('muzzle_flashes/mflash_mm1_inworld', 'effects/muzzle_flashes/mflash_mm1_inworld'),
                ('muzzle_flashes/mflash_pistols_inworld', 'effects/muzzle_flashes/mflash_pistols_inworld'),
                ('muzzle_flashes/mflash_uzi_inworld', 'effects/muzzle_flashes/mflash_uzi_inworld'),
                ('muzzle_flashes/mflash_m1911a1', 'effects/muzzle_flashes/mflash_m1911a1'),
                ('muzzle_flashes/mflash_m1911a1_final', 'effects/muzzle_flashes/mflash_m1911a1_final'),
                ('muzzle_flashes/mflash_uzi_final', 'effects/muzzle_flashes/mflash_uzi_final'),
            ]
        },
        # Add smoke effects to muzzle_smoke.json
        'muzzle_smoke': {
            'target_json': os.path.join(JSON_ROOT, 'SoF2_Effects_muzzle_smoke.json'),
            'is_muzzle': True,
            'effects': [
                ('muzzle_flashes/smoke_m1911a1', 'effects/muzzle_flashes/smoke_m1911a1'),
                ('muzzle_flashes/smoke_m3', 'effects/muzzle_flashes/smoke_m3'),
                ('muzzle_flashes/smoke_ussocom', 'effects/muzzle_flashes/smoke_ussocom'),
            ]
        },
        # 2. FIRE/INCENDIARY EFFECTS - new JSON file
        'fire': {
            'target_json': os.path.join(JSON_ROOT, 'SoF2_Effects_fire.json'),
            'is_muzzle': False,
            'effects': [
                ('fire/ground_fire', 'effects/fire/ground_fire'),
                ('fire/ground_fire_cheap', 'effects/fire/ground_fire_cheap'),
                ('fire/ground_fire_static', 'effects/fire/ground_fire_static'),
                ('fire/floor_fire', 'effects/fire/floor_fire'),
                ('fire/growing_fire', 'effects/fire/growing_fire'),
                ('fire/spill_fire', 'effects/fire/spill_fire'),
                ('fire/flame_jet', 'effects/fire/flame_jet'),
                ('fire/incendiary_fire', 'effects/fire/incendiary_fire'),
                ('fire/incendiary_fire-tendril', 'effects/fire/incendiary_fire-tendril'),
                ('fire/incendiary_fire_trail', 'effects/fire/incendiary_fire_trail'),
                ('fire/rpg7_fire-tendril', 'effects/fire/rpg7_fire-tendril'),
                ('fire/smoke_static_large', 'effects/fire/smoke_static_large'),
                ('fire/smoke_static_medium', 'effects/fire/smoke_static_medium'),
                ('fire/smoke_static_small', 'effects/fire/smoke_static_small'),
                ('fire/smoke_static_smaller', 'effects/fire/smoke_static_smaller'),
            ]
        },
        # 3. EXPLOSION VARIANTS - add to existing explosions.json
        'explosions': {
            'target_json': os.path.join(JSON_ROOT, 'SoF2_Effects_explosions.json'),
            'is_muzzle': False,
            'effects': [
                ('explosions/big_explosion', 'effects/explosions/big_explosion'),
                ('explosions/vertical_explosion', 'effects/explosions/vertical_explosion'),
                ('explosions/glass_small', 'effects/explosions/glass_small'),
                ('explosions/explosion_chunk', 'effects/explosions/explosion_chunk'),
                ('explosions/phosphorus_chunk', 'effects/explosions/phosphorus_chunk'),
                ('explosions/phosphorus_chunk_smoke', 'effects/explosions/phosphorus_chunk_smoke'),
                ('explosions/phosphorus_ember', 'effects/explosions/phosphorus_ember'),
                ('explosions/phosphorus_grenade', 'effects/explosions/phosphorus_grenade'),
                ('explosions/phosphorus_trail', 'effects/explosions/phosphorus_trail'),
                ('explosions/rpg7_explosion_huge', 'effects/explosions/rpg7_explosion_huge'),
                ('explosions/camera_shake_big', 'effects/explosions/camera_shake_big'),
                ('explosions/camera_shake_small', 'effects/explosions/camera_shake_small'),
            ]
        },
    }

    total_added = 0
    total_files = 0

    for category, config in conversions.items():
        target_json = config['target_json']
        is_muzzle = config['is_muzzle']
        effects_to_add = config['effects']

        # Load existing JSON if it exists
        if os.path.exists(target_json):
            with open(target_json, 'r', encoding='utf-8') as f:
                existing = json.load(f)
            existing_ids = {e['id'] for e in existing}
        else:
            existing = []
            existing_ids = set()

        new_effects = []
        for efx_subpath, effect_id in effects_to_add:
            if effect_id in existing_ids:
                print(f"  SKIP (already exists): {effect_id}")
                continue

            efx_path = os.path.join(EFX_ROOT, efx_subpath + '.efx')
            if not os.path.exists(efx_path):
                print(f"  ERROR: File not found: {efx_path}")
                continue

            effect = convert_efx_file(efx_path, effect_id, is_muzzle=is_muzzle)
            if effect:
                new_effects.append(effect)
                print(f"  CONVERTED: {effect_id} ({len(effect['segments'])} segments)")

        if new_effects:
            existing.extend(new_effects)
            with open(target_json, 'w', encoding='utf-8') as f:
                json.dump(existing, f, indent=2, ensure_ascii=False)
            total_added += len(new_effects)
            total_files += 1
            print(f"\n  -> Added {len(new_effects)} effects to {os.path.basename(target_json)}")
        else:
            print(f"\n  -> No new effects for {category}")

    print(f"\n{'='*50}")
    print(f"TOTAL: Added {total_added} new effects across {total_files} JSON files")


if __name__ == '__main__':
    main()
