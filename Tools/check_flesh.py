import json
with open(r'd:\RemakeSoF\Assets\Resources\Data\Effects\SoF2_Effects_impact_flesh.json') as f:
    data = json.load(f)
for e in data:
    segs = [s['type'] for s in e['segments']]
    eid = e['id']
    print(f'{eid} -> {len(e["segments"])} segments: {segs}')
