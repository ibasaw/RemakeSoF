import json

data = json.load(open(r"d:\RemakeSoF\Assets\Resources\Data\Effects\SoF2_Effects_impact_flesh.json"))
for eff in data:
    print(eff["id"] + ":")
    for j, seg in enumerate(eff.get("segments", [])):
        size = seg.get("size", {})
        if size:
            smin = size.get("startMin", "?")
            smax = size.get("startMax", "?")
            emin = size.get("endMin", "?")
            emax = size.get("endMax", "?")
            print(f"  seg{j}: start=[{smin}, {smax}] end=[{emin}, {emax}]")
