import json, os

json_dir = r'd:\RemakeSoF\Assets\Resources\Data\Effects'
ids = set()
for f in os.listdir(json_dir):
    if f.endswith('.json'):
        with open(os.path.join(json_dir, f)) as fh:
            data = json.load(fh)
            for e in data:
                ids.add(e['id'])

originals = [
    'impact_canvas','impact_canvas_12g',
    'impact_concrete','impact_concrete_12g','impact_concrete_belt','impact_concrete_osprey','impact_concrete2',
    'impact_default','impact_default2',
    'impact_dirt','impact_dirt_12g','impact_dirt_belt','impact_dirt3',
    'impact_flesh','impact_flesh_12g','impact_flesh_old','impact_flesh_small','impact_flesh2',
    'impact_glass',
    'impact_grass','impact_grass_12g','impact_grass_belt','impact_grass2','impact_grass3',
    'impact_gravel','impact_gravel_12g','impact_gravel_belt','impact_gravel3',
    'impact_ice','impact_ice_12g','impact_ice_belt','impact_ice2',
    'impact_leaves-dry','impact_leaves-dry_12g','impact_leaves-dry_belt',
    'impact_leaves-green','impact_leaves-green_12g','impact_leaves-green_belt',
    'impact_metal','impact_metal_12g','impact_metal_belt','impact_metal_osprey','impact_metal3','impact_metal3-h','impact_metal-h','impact_metal-h_12g','impact_metal-h_belt',
    'impact_mud','impact_mud_12g','impact_mud_belt',
    'impact_player','impact_player_mp',
    'impact_rock','impact_rock_12g','impact_rock_belt',
    'impact_short_grass','impact_short_grass_12g','impact_short_grass_belt','impact_short_grass3',
    'impact_snow','impact_snow_12g','impact_snow_belt','impact_snow2',
    'impact_water','impact_water_12g','impact_water_belt','impact_water2',
    'impact_wood','impact_wood_12g','impact_wood_belt','impact_wood2','impact_wood3'
]

missing = []
for o in originals:
    found = False
    for eid in ids:
        if eid.endswith('/' + o):
            found = True
            break
    if not found:
        missing.append(o)

impact_ids = [i for i in ids if 'impact' in i or 'knife' in i]
print(f'Total original impacts: {len(originals)}')
print(f'Total JSON impact IDs (incl knife/custom): {len(impact_ids)}')
print(f'Missing count: {len(missing)}')
for m in missing:
    print(f'  X {m}')
if len(missing) == 0:
    print('SUCCESS: ALL 72 ORIGINAL IMPACTS PRESENT')
