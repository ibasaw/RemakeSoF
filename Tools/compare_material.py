"""
Compare generic.material (original) with SoF2_data_per_surface.json (project).
Finds missing surfaces, missing properties, mismatched values.
"""
import json
import re

MATERIAL_PATH = r"d:\sof2_extract\base\ext_data\generic.material"
JSON_PATH = r"d:\RemakeSoF\Assets\Resources\Data\SoF2_data_per_surface.json"


def parse_material(path):
    """Parse the generic.material file into a dict structure."""
    with open(path, 'r') as f:
        text = f.read()

    # Remove comments
    text = re.sub(r'//[^\n]*', '', text)

    surfaces = {}
    # Find top-level blocks: surfacename { ... }
    # We need to handle nested braces
    pos = 0
    while pos < len(text):
        # Find next identifier
        match = re.search(r'(\w[\w-]*)\s*\{', text[pos:])
        if not match:
            break
        name = match.group(1).lower()
        brace_start = pos + match.end()

        # Find matching closing brace
        depth = 1
        i = brace_start
        while i < len(text) and depth > 0:
            if text[i] == '{':
                depth += 1
            elif text[i] == '}':
                depth -= 1
            i += 1
        body = text[brace_start:i - 1]
        surfaces[name] = body
        pos = i

    # Now parse each surface
    result = {}
    for sname, body in surfaces.items():
        if sname == 'ammotypes':
            continue  # skip if somehow parsed wrong
        surface = {}

        # Extract simple properties
        for prop in ['density', 'loudness', 'visibility', 'projectilebounce',
                      'friction', 'damage']:
            m = re.search(rf'\b{prop}\s+([\d.]+)', body, re.IGNORECASE)
            if m:
                surface[prop.lower()] = float(m.group(1))

        # Extract breaksound
        m = re.search(r'breaksound\s+"([^"]+)"', body)
        if m:
            surface['breaksound'] = m.group(1)

        # Extract sound blocks (footstep, land, etc.)
        for sblock in ['footstep', 'footstepstealth', 'footstepprone',
                       'land', 'land_pain', 'land_death',
                       'bouncemetal0', 'bouncemetal1']:
            pattern = rf'{sblock}\s*\{{([^}}]*)\}}'
            m = re.search(pattern, body, re.IGNORECASE)
            if m:
                block_body = m.group(1)
                block = {}
                sm = re.search(r'sound\s+("?)([^\s"]+(?:/[^\s"]+)*)\1', block_body)
                if sm:
                    block['sound'] = sm.group(2)
                dm = re.search(r'decal\s+(\S+)', block_body)
                if dm:
                    block['decal'] = dm.group(1)
                for attr in ['duration', 'fadetime', 'latency']:
                    am = re.search(rf'{attr}\s+(\d+)', block_body)
                    if am:
                        block[attr] = int(am.group(1))
                surface[sblock.lower()] = block

        # Extract ammoTypes
        ammo_match = re.search(r'ammoTypes\s*\{', body, re.IGNORECASE)
        if ammo_match:
            ammo_start = ammo_match.end()
            # Find the matching }
            depth = 1
            i = ammo_start
            while i < len(body) and depth > 0:
                if body[i] == '{':
                    depth += 1
                elif body[i] == '}':
                    depth -= 1
                i += 1
            ammo_body = body[ammo_start:i - 1]

            ammo_types = {}
            # Find each ammo type block
            ammo_pattern = r'("([^"]+)"|(\w+))\s*\{'
            apos = 0
            while apos < len(ammo_body):
                am = re.search(ammo_pattern, ammo_body[apos:])
                if not am:
                    break
                ammo_name = am.group(2) or am.group(3)
                ab_start = apos + am.end()
                # Find matching }
                adepth = 1
                ai = ab_start
                while ai < len(ammo_body) and adepth > 0:
                    if ammo_body[ai] == '{':
                        adepth += 1
                    elif ammo_body[ai] == '}':
                        adepth -= 1
                    ai += 1
                ammo_block = ammo_body[ab_start:ai - 1]

                ammo_entry = {}
                # Parse ammo properties
                for prop in ['effect', 'debris', 'shellsound', 'sound', 'decal']:
                    # Can appear multiple times (e.g. knife has 2 effects)
                    vals = re.findall(rf'{prop}\s+"?([^\s"]+(?:/[^\s"]+)*)"?', ammo_block)
                    if len(vals) == 1:
                        ammo_entry[prop] = vals[0]
                    elif len(vals) > 1:
                        ammo_entry[prop] = vals
                ew = re.search(r'effectwait\s+(\d+)', ammo_block)
                if ew:
                    ammo_entry['effectwait'] = int(ew.group(1))

                ammo_types[ammo_name] = ammo_entry
                apos = ai

            surface['ammotypes'] = ammo_types

        result[sname] = surface

    return result


def normalize_key(k):
    """Normalize key names for comparison."""
    return k.lower().replace('projectilebounce', 'projectilebounce')


def compare_ammo(orig_ammo, json_ammo, surface_name):
    """Compare ammo types between original and JSON."""
    diffs = []

    orig_keys = set(orig_ammo.keys())
    json_keys = set(json_ammo.keys())

    missing_in_json = orig_keys - json_keys
    extra_in_json = json_keys - orig_keys

    if missing_in_json:
        diffs.append(f"  MISSING ammo types: {missing_in_json}")

    if extra_in_json:
        diffs.append(f"  EXTRA ammo types: {extra_in_json}")

    for ammo in sorted(orig_keys & json_keys):
        orig_entry = orig_ammo[ammo]
        json_entry = json_ammo[ammo]

        for prop in set(list(orig_entry.keys()) + list(json_entry.keys())):
            orig_val = orig_entry.get(prop)
            json_val = json_entry.get(prop)

            # Normalize for comparison
            if isinstance(orig_val, list) and isinstance(json_val, list):
                if sorted(orig_val) != sorted(json_val):
                    diffs.append(f"  [{ammo}].{prop}: ORIG={orig_val} vs JSON={json_val}")
            elif isinstance(orig_val, list) and not isinstance(json_val, list):
                if json_val not in orig_val:
                    diffs.append(f"  [{ammo}].{prop}: ORIG={orig_val} vs JSON={json_val}")
            elif not isinstance(orig_val, list) and isinstance(json_val, list):
                if orig_val not in json_val:
                    diffs.append(f"  [{ammo}].{prop}: ORIG={orig_val} vs JSON={json_val}")
            elif orig_val != json_val:
                # Check numeric equality
                try:
                    if float(orig_val) == float(json_val):
                        continue
                except (TypeError, ValueError):
                    pass
                if orig_val is None:
                    diffs.append(f"  [{ammo}].{prop}: MISSING in original, JSON={json_val}")
                elif json_val is None:
                    diffs.append(f"  [{ammo}].{prop}: ORIG={orig_val}, MISSING in JSON")
                else:
                    diffs.append(f"  [{ammo}].{prop}: ORIG={orig_val} vs JSON={json_val}")

    return diffs


def main():
    original = parse_material(MATERIAL_PATH)
    with open(JSON_PATH, 'r', encoding='utf-8-sig') as f:
        project = json.load(f)

    print("=" * 70)
    print("SURFACE COMPARISON: generic.material vs SoF2_data_per_surface.json")
    print("=" * 70)

    orig_surfaces = set(original.keys())
    json_surfaces = set(k.lower() for k in project.keys())

    missing = orig_surfaces - json_surfaces
    extra = json_surfaces - orig_surfaces

    if missing:
        print(f"\nMISSING SURFACES in JSON: {sorted(missing)}")
    else:
        print(f"\nAll {len(orig_surfaces)} original surfaces present in JSON.")

    if extra:
        print(f"EXTRA SURFACES in JSON (not in original): {sorted(extra)}")

    print(f"\nOriginal surfaces: {len(orig_surfaces)}")
    print(f"JSON surfaces: {len(json_surfaces)}")

    # Compare each surface
    all_diffs = {}
    for sname in sorted(orig_surfaces & json_surfaces):
        orig_surf = original[sname]
        # Find the JSON key (case-insensitive)
        json_key = None
        for k in project:
            if k.lower() == sname:
                json_key = k
                break
        if not json_key:
            continue

        json_surf = project[json_key]
        diffs = []

        # Compare surface-level properties
        for prop in ['density', 'loudness', 'visibility', 'projectilebounce',
                      'friction', 'damage', 'breaksound']:
            orig_val = orig_surf.get(prop)
            # Try case variations in JSON
            json_val = None
            for jk in json_surf:
                if jk.lower() == prop:
                    json_val = json_surf[jk]
                    break
                if jk.lower() == 'projectilebounce' and prop == 'projectilebounce':
                    json_val = json_surf[jk]
                    break

            if orig_val is not None and json_val is None:
                diffs.append(f"  Property '{prop}': ORIG={orig_val}, MISSING in JSON")
            elif orig_val is not None and json_val is not None:
                try:
                    if float(orig_val) != float(json_val):
                        diffs.append(f"  Property '{prop}': ORIG={orig_val} vs JSON={json_val}")
                except (TypeError, ValueError):
                    if str(orig_val) != str(json_val):
                        diffs.append(f"  Property '{prop}': ORIG={orig_val} vs JSON={json_val}")

        # Compare sound blocks
        for sblock in ['footstep', 'footstepstealth', 'footstepprone',
                       'land', 'land_pain', 'land_death',
                       'bouncemetal0', 'bouncemetal1']:
            orig_block = orig_surf.get(sblock)
            json_block = None
            for jk in json_surf:
                if jk.lower() == sblock:
                    json_block = json_surf[jk]
                    break

            if orig_block and not json_block:
                diffs.append(f"  Block '{sblock}': PRESENT in original, MISSING in JSON")
            elif orig_block and json_block:
                for bprop in orig_block:
                    ov = orig_block[bprop]
                    jv = json_block.get(bprop) or json_block.get(bprop.lower())
                    if ov and not jv:
                        diffs.append(f"  {sblock}.{bprop}: ORIG={ov}, MISSING in JSON")
                    elif ov and jv and str(ov) != str(jv):
                        try:
                            if float(ov) != float(jv):
                                diffs.append(f"  {sblock}.{bprop}: ORIG={ov} vs JSON={jv}")
                        except (TypeError, ValueError):
                            diffs.append(f"  {sblock}.{bprop}: ORIG={ov} vs JSON={jv}")

        # Compare ammo types
        orig_ammo = orig_surf.get('ammotypes', {})
        json_ammo = None
        for jk in json_surf:
            if jk.lower() == 'ammotypes':
                json_ammo = json_surf[jk]
                break

        if orig_ammo and not json_ammo:
            diffs.append(f"  ammoTypes: PRESENT in original, MISSING in JSON")
        elif orig_ammo and json_ammo:
            ammo_diffs = compare_ammo(orig_ammo, json_ammo, sname)
            diffs.extend(ammo_diffs)

        if diffs:
            all_diffs[sname] = diffs

    print("\n" + "=" * 70)
    if not all_diffs:
        print("PERFECT MATCH! No differences found.")
    else:
        print(f"DIFFERENCES found in {len(all_diffs)} surface(s):")
        print("=" * 70)
        for sname, diffs in sorted(all_diffs.items()):
            print(f"\n--- {sname} ---")
            for d in diffs:
                print(d)

    # Summary
    print("\n" + "=" * 70)
    print("SUMMARY")
    print("=" * 70)
    print(f"Surfaces in original: {len(orig_surfaces)}")
    print(f"Surfaces in JSON: {len(json_surfaces)}")
    if missing:
        print(f"MISSING surfaces: {sorted(missing)}")
    else:
        print("All surfaces present: YES")
    print(f"Surfaces with differences: {len(all_diffs)}")
    if all_diffs:
        print(f"Affected: {sorted(all_diffs.keys())}")


if __name__ == '__main__':
    main()
