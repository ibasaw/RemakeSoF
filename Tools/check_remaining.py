#!/usr/bin/env python3
"""Quick check of remaining oversized non-explosion/debris effects."""
import json, os

EFFECTS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                           "Assets", "Resources", "Data", "Effects")

SKIP_PREFIXES = ["effects/explosions/", "effects/chunks/", "effects/debris/",
                 "effects/fire/smoke_grenade"]

for fn in sorted(os.listdir(EFFECTS_DIR)):
    if not fn.startswith("SoF2_Effects_") or not fn.endswith(".json"):
        continue
    fp = os.path.join(EFFECTS_DIR, fn)
    with open(fp, "r", encoding="utf-8") as f:
        effects = json.load(f)
    for effect in effects:
        eid = effect.get("id", "?")
        skip = any(eid.startswith(p) for p in SKIP_PREFIXES)
        if skip:
            continue
        for i, seg in enumerate(effect.get("segments", [])):
            st = seg.get("type", "")
            sn = seg.get("name", st)
            if st in ("particle", "orientedParticle", "line", "tail"):
                size = seg.get("size", {})
                vals = [abs(size.get(k, 0)) for k in ("startMin", "startMax", "endMin", "endMax")]
                mx = max(vals) if vals else 0
                if mx > 1.0:
                    keys = {k: size[k] for k in ("startMin", "startMax", "endMin", "endMax") if k in size}
                    print(f"  {eid} seg[{i}] {sn} ({st}): max={mx:.3f}m  {keys}")
