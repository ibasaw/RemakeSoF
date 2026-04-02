#!/usr/bin/env python3
"""
Comprehensive audit of ALL SoF2 effect JSON files.
Checks for:
1. Oversized particles (endSize > 1.0m for non-explosion/smoke effects)
2. Suspicious gravity values
3. Missing required fields
4. Size ratio issues (endRatio > 20x, which is capped by EffectFactory)
5. Velocity sanity (values > 50 m/s for non-explosion particles)
6. Lifetime sanity (< 0 or > 30s for non-smoke effects)
7. Count sanity (> 200 particles)
"""

import json
import os
import glob

EFFECTS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..",
                           "Assets", "Resources", "Data", "Effects")

# Effects where large sizes are expected
LARGE_SIZE_ALLOWED = {
    "effects/fire/smoke_grenade",
    "effects/fire/smoke_grenade_flash",
    "effects/explosions/",  # prefix match
    "effects/debris/",      # prefix match
}

# Effects where high velocity is expected
HIGH_VEL_ALLOWED = {
    "effects/explosions/",
    "effects/debris/",
    "effects/fire/smoke_grenade",
    "tracers",
    "shell_",
}


def is_allowed_large(effect_id, allowed_set):
    for pattern in allowed_set:
        if pattern.endswith("/"):
            if effect_id.startswith(pattern):
                return True
        else:
            if pattern in effect_id:
                return True
    return False


def check_size(effect_id, seg_name, seg_type, size_data, issues):
    """Check particle sizes for sanity."""
    if not size_data:
        return

    start_min = size_data.get("startMin", 0)
    start_max = size_data.get("startMax", start_min)
    end_min = size_data.get("endMin", start_min)
    end_max = size_data.get("endMax", start_max)

    max_size = max(abs(start_min), abs(start_max), abs(end_min), abs(end_max))

    # Check for oversized non-explosion particles
    if max_size > 1.0 and not is_allowed_large(effect_id, LARGE_SIZE_ALLOWED):
        issues.append({
            "type": "OVERSIZED",
            "effect": effect_id,
            "segment": seg_name,
            "detail": f"Max size {max_size:.3f}m (>{1.0}m)",
            "values": {"startMin": start_min, "startMax": start_max,
                       "endMin": end_min, "endMax": end_max}
        })

    # Check endRatio > 20x (capped by EffectFactory)
    if start_max > 0 and end_max > 0:
        ratio = end_max / start_max
        if ratio > 20:
            issues.append({
                "type": "RATIO_CAPPED",
                "effect": effect_id,
                "segment": seg_name,
                "detail": f"End/start ratio {ratio:.1f}x (capped at 20x by EffectFactory)",
                "values": {"startMax": start_max, "endMax": end_max}
            })


def check_velocity(effect_id, seg_name, vel_data, issues):
    """Check velocity for sanity."""
    if not vel_data:
        return
    max_vel = max(abs(v) for v in vel_data)
    if max_vel > 50.0 and not is_allowed_large(effect_id, HIGH_VEL_ALLOWED):
        issues.append({
            "type": "HIGH_VELOCITY",
            "effect": effect_id,
            "segment": seg_name,
            "detail": f"Max velocity component {max_vel:.3f} m/s",
            "values": vel_data
        })


def check_lifetime(effect_id, seg_name, particle_data, issues):
    """Check lifetime for sanity."""
    if not particle_data:
        return
    lt_max = particle_data.get("lifetimeMax", particle_data.get("lifetimeMin", 0))
    if lt_max > 30.0 and "smoke_grenade" not in effect_id:
        issues.append({
            "type": "LONG_LIFETIME",
            "effect": effect_id,
            "segment": seg_name,
            "detail": f"Lifetime {lt_max}s (>30s)"
        })
    if lt_max < 0:
        issues.append({
            "type": "NEGATIVE_LIFETIME",
            "effect": effect_id,
            "segment": seg_name,
            "detail": f"Lifetime {lt_max}s (<0)"
        })


def check_count(effect_id, seg_name, particle_data, issues):
    """Check particle count."""
    if not particle_data:
        return
    count_max = particle_data.get("countMax", particle_data.get("countMin", 1))
    if count_max > 200:
        issues.append({
            "type": "HIGH_COUNT",
            "effect": effect_id,
            "segment": seg_name,
            "detail": f"Count {count_max} (>200)"
        })


def check_gravity(effect_id, seg_name, particle_data, issues):
    """Check gravity for obviously wrong values."""
    if not particle_data:
        return
    grav = particle_data.get("gravityModifier", 0)
    # Positive gravity = particles fall up (wrong for most effects)
    if grav > 0.5:
        issues.append({
            "type": "POSITIVE_GRAVITY",
            "effect": effect_id,
            "segment": seg_name,
            "detail": f"gravityModifier={grav} (positive = upward, usually wrong)"
        })
    # Extremely strong gravity
    if abs(grav) > 100:
        issues.append({
            "type": "EXTREME_GRAVITY",
            "effect": effect_id,
            "segment": seg_name,
            "detail": f"gravityModifier={grav} (very strong)"
        })


def audit_effects():
    json_files = sorted(glob.glob(os.path.join(EFFECTS_DIR, "SoF2_Effects_*.json")))
    
    total_effects = 0
    total_segments = 0
    issues = []
    all_ids = []

    for json_file in json_files:
        filename = os.path.basename(json_file)
        with open(json_file, "r", encoding="utf-8") as f:
            try:
                effects = json.load(f)
            except json.JSONDecodeError as e:
                issues.append({
                    "type": "JSON_ERROR",
                    "effect": filename,
                    "segment": "",
                    "detail": str(e)
                })
                continue

        for effect in effects:
            total_effects += 1
            eid = effect.get("id", "MISSING_ID")
            all_ids.append(eid)

            if "id" not in effect:
                issues.append({
                    "type": "MISSING_ID",
                    "effect": f"index in {filename}",
                    "segment": "",
                    "detail": "Effect has no 'id' field"
                })
                continue

            segments = effect.get("segments", [])
            if not segments:
                issues.append({
                    "type": "NO_SEGMENTS",
                    "effect": eid,
                    "segment": "",
                    "detail": "Effect has no segments"
                })
                continue

            for seg in segments:
                total_segments += 1
                seg_type = seg.get("type", "unknown")
                seg_name = seg.get("name", seg_type)

                if seg_type in ("particle", "orientedParticle", "line", "tail"):
                    particle = seg.get("particle", {})
                    check_lifetime(eid, seg_name, particle, issues)
                    check_count(eid, seg_name, particle, issues)
                    check_gravity(eid, seg_name, particle, issues)

                    vel_min = particle.get("velocityMin")
                    vel_max = particle.get("velocityMax")
                    if vel_min:
                        check_velocity(eid, seg_name, vel_min, issues)
                    if vel_max:
                        check_velocity(eid, seg_name, vel_max, issues)

                if seg_type in ("particle", "orientedParticle", "line", "tail"):
                    size = seg.get("size", {})
                    check_size(eid, seg_name, seg_type, size, issues)

                if seg_type in ("particle", "orientedParticle", "line", "tail"):
                    length = seg.get("length", {})
                    if length:
                        max_len = max(
                            abs(length.get("startMin", 0)),
                            abs(length.get("startMax", 0)),
                            abs(length.get("endMin", 0)),
                            abs(length.get("endMax", 0))
                        )
                        if max_len > 5.0 and not is_allowed_large(eid, LARGE_SIZE_ALLOWED):
                            issues.append({
                                "type": "LONG_TRAIL",
                                "effect": eid,
                                "segment": seg_name,
                                "detail": f"Trail length {max_len:.3f}m"
                            })

    # Check for duplicate IDs
    seen = set()
    for eid in all_ids:
        if eid in seen:
            issues.append({
                "type": "DUPLICATE_ID",
                "effect": eid,
                "segment": "",
                "detail": f"Effect ID appears multiple times"
            })
        seen.add(eid)

    # Print results
    print(f"=== EFFECTS AUDIT ===")
    print(f"Files scanned: {len(json_files)}")
    print(f"Total effects: {total_effects}")
    print(f"Total segments: {total_segments}")
    print(f"Issues found: {len(issues)}")
    print()

    if issues:
        # Group by type
        by_type = {}
        for issue in issues:
            t = issue["type"]
            if t not in by_type:
                by_type[t] = []
            by_type[t].append(issue)

        for issue_type, items in sorted(by_type.items()):
            print(f"--- {issue_type} ({len(items)}) ---")
            for item in items:
                detail = item["detail"]
                eid = item["effect"]
                seg = item["segment"]
                vals = item.get("values", "")
                if seg:
                    print(f"  {eid} / {seg}: {detail}")
                else:
                    print(f"  {eid}: {detail}")
                if vals:
                    print(f"    values: {vals}")
            print()
    else:
        print("No issues found! All effects look good.")


if __name__ == "__main__":
    audit_effects()
