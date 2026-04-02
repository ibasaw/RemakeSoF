"""
Fixes oversized particle end-sizes in flesh/gore impact effects.

Problem: The raw .efx → JSON conversion preserved SoF2 Quake Unit sizes correctly
(QU × 0.0254 = meters), but in SoF2's 800×600 renderer with simpler alpha blending,
50-60 QU billboard particles looked like brief red splashes. In Unity at 1080p+
with modern alpha blending, these 1.27-1.52m particles look enormous.

Fix: Divide oversized end-sizes by 4 (for >1m values) or 2 (for 0.3-0.6m values)
to match the visual feel of hand-crafted effects (impact_flesh2, impact_flesh_12g)
which already have correct sizes (0.15-0.35m range).

Reference effects (already correct, NOT modified):
  - impact_flesh2:  bloodcloud end 0.254-0.3556m, center end 0.0762-0.1016m
  - impact_flesh_12g: bloodcloud end 0.1778-0.254m
  - impact_flesh_old: bloodcloud end 0.254-0.3556m
"""

import json
import os

BASE = r"d:\RemakeSoF\Assets\Resources\Data\Effects"


def fix_impact_flesh_file():
    path = os.path.join(BASE, "SoF2_Effects_impact_flesh.json")
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    changes = []

    for effect in data:
        eid = effect["id"]
        segs = effect.get("segments", [])

        if eid == "effects/impact_flesh":
            # seg 0: burst blood drops (jk_drop, count 20-30, 1-1.5s)
            # end 1.27/1.524 → 0.3175/0.381 (÷4)
            segs[0]["size"]["endMin"] = 0.3175
            segs[0]["size"]["endMax"] = 0.381
            changes.append(f"{eid} seg[0] blood drops: endMin/Max 1.27/1.524 -> 0.3175/0.381")

            # seg 1: blood mist (jk_dirt_grey, count 1-2, 0.6-0.8s)
            # end 1.016/1.27 → 0.254/0.3175 (÷4)
            segs[1]["size"]["endMin"] = 0.254
            segs[1]["size"]["endMax"] = 0.3175
            changes.append(f"{eid} seg[1] blood mist: endMin/Max 1.016/1.27 -> 0.254/0.3175")

            # seg 2: smoke (jk_smoke4, count 1, 0.3-0.35s)
            # end 1.27/1.524 → 0.3175/0.381 (÷4)
            segs[2]["size"]["endMin"] = 0.3175
            segs[2]["size"]["endMax"] = 0.381
            changes.append(f"{eid} seg[2] smoke: endMin/Max 1.27/1.524 -> 0.3175/0.381")

            # seg 3: physics blood drops (jk_drop, count 2-3, 1-1.5s)
            # end 1.27/1.524 → 0.3175/0.381 (÷4)
            segs[3]["size"]["endMin"] = 0.3175
            segs[3]["size"]["endMax"] = 0.381
            changes.append(f"{eid} seg[3] physics drops: endMin/Max 1.27/1.524 -> 0.3175/0.381")

        elif eid == "effects/impact_flesh_small":
            # seg 0: blood drops (jk_drop, count 2-5) ÷4
            segs[0]["size"]["endMin"] = 0.3175
            segs[0]["size"]["endMax"] = 0.381
            changes.append(f"{eid} seg[0] blood drops: endMin/Max 1.27/1.524 -> 0.3175/0.381")

            # seg 1: blood mist (jk_dirt_grey, count 1-2) ÷2
            segs[1]["size"]["endMin"] = 0.254
            segs[1]["size"]["endMax"] = 0.3175
            changes.append(f"{eid} seg[1] blood mist: endMin/Max 0.508/0.635 -> 0.254/0.3175")

            # seg 2: smoke (jk_smoke4, count 1-2) ÷4
            segs[2]["size"]["endMin"] = 0.3175
            segs[2]["size"]["endMax"] = 0.381
            changes.append(f"{eid} seg[2] smoke: endMin/Max 1.27/1.524 -> 0.3175/0.381")

            # seg 3: physics drops (jk_drop, count 1) ÷4
            segs[3]["size"]["endMin"] = 0.3175
            segs[3]["size"]["endMax"] = 0.381
            changes.append(f"{eid} seg[3] physics drops: endMin/Max 1.27/1.524 -> 0.3175/0.381")

        elif eid == "effects/impact_player":
            # Find particle segment with oversized endMin/Max
            for i, seg in enumerate(segs):
                if seg.get("type") == "particle" and "size" in seg:
                    size = seg["size"]
                    if size.get("endMin") == 1.27 and size.get("endMax") == 1.524:
                        size["endMin"] = 0.3175
                        size["endMax"] = 0.381
                        changes.append(f"{eid} seg[{i}]: endMin/Max 1.27/1.524 -> 0.3175/0.381")

        elif eid == "effects/impact_player_mp":
            # seg 0 "bits": endMin/Max ÷2
            for seg in segs:
                if seg.get("name") == "bits" and "size" in seg:
                    seg["size"]["endMin"] = 0.1905
                    seg["size"]["endMax"] = 0.254
                    changes.append(f"{eid} 'bits': endMin/Max 0.381/0.508 -> 0.1905/0.254")
                    break

    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")

    return path, changes


def fix_gore_file():
    path = os.path.join(BASE, "SoF2_Effects_gore.json")
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    changes = []

    for effect in data:
        eid = effect["id"]
        segs = effect.get("segments", [])

        if eid == "effects/gore_mist_small":
            # seg 0 "bits": endMin/Max ÷2
            for seg in segs:
                if seg.get("name") == "bits" and "size" in seg:
                    seg["size"]["endMin"] = 0.1905
                    seg["size"]["endMax"] = 0.254
                    changes.append(f"{eid} 'bits': endMin/Max 0.381/0.508 -> 0.1905/0.254")
                    break

        elif eid == "effects/flesh_chunks":
            # "bits" segment: start and end ÷2
            for seg in segs:
                if seg.get("name") == "bits" and "size" in seg:
                    seg["size"]["startMin"] = 0.0508
                    seg["size"]["startMax"] = 0.0635
                    seg["size"]["endMin"] = 0.127
                    seg["size"]["endMax"] = 0.1524
                    changes.append(
                        f"{eid} 'bits': start 0.1016/0.127->0.0508/0.0635, "
                        f"end 0.254/0.3048->0.127/0.1524"
                    )
                    break

    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
        f.write("\n")

    return path, changes


if __name__ == "__main__":
    print("=== Fixing oversized effect particle sizes ===\n")

    path1, changes1 = fix_impact_flesh_file()
    print(f"Fixed: {path1}")
    for c in changes1:
        print(f"  - {c}")

    print()

    path2, changes2 = fix_gore_file()
    print(f"Fixed: {path2}")
    for c in changes2:
        print(f"  - {c}")

    total = len(changes1) + len(changes2)
    print(f"\nTotal changes: {total}")
