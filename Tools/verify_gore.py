import json

with open(r'd:\RemakeSoF\Assets\Resources\Data\SoF2_DATA.json', 'r', encoding='utf-8') as f:
    data = json.load(f)

gore = data['Gore']
print(f"gore_effects: {len(gore['gore_effects'])}")
for area in gore['gore_areas']:
    loc = area.get('Location', '?')
    fx_count = len(area.get('FX', []))
    blood_count = len(area.get('BloodFX', []))
    print(f"  {loc}: FX={fx_count}, BloodFX={blood_count}")
