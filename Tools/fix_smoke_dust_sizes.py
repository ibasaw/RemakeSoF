"""
Fix oversized smoke/dust particles across ALL surface impact effects.

Problem: Smoke puffs and dust cloud particles grow to 0.3-0.8m end-sizes.
In SoF2 at 640x480, these looked fine. At modern HD resolutions, they look
like giant blobs flying off walls.

Strategy:
- Smoke/dust/cloud particles (jk_smoke*, bp_smoke*, jk_dirt*, jk_mist except water ripple)
  with endMax > 0.25m get their end-sizes halved (÷2)
- Excludes actual chunk/debris particles (bp_rock, jk_spark, jk_bar, glass bits, etc.)
- Excludes water effects (water ripples are intentionally large)
- Excludes flesh/blood effects (already fixed in Phase 2/5)
- Also halves startMin/startMax if they're > 0.15m (for belt variants)

This brings smoke from 0.5-0.8m → 0.25-0.4m and dust from 0.3-0.56m → 0.15-0.28m
which better matches the visual impression of SoF2 at modern resolutions.
"""

import json
import os
import sys
from pathlib import Path

EFFECTS_DIR = Path(r"d:\RemakeSoF\Assets\Resources\Data\Effects")

# Textures that are smoke/dust/cloud (candidates for size reduction)
SMOKE_DUST_TEXTURES = {
    "gfx/misc/jk_smoke", "gfx/misc/jk_smoke2", "gfx/misc/jk_smoke3",
    "gfx/misc/jk_smoke4", "gfx/misc/jk_smoke5",
    "gfx/misc/bp_smoke01", "gfx/misc/bp_smoke02",
    "gfx/misc/jk_dirt_grey", "gfx/misc/jk_dirt_grey2",
    "gfx/misc/jk_dirt_grey_streaked",
    "gfx/misc/jk_mist",
    "gfx/misc/gb_softfill",
}

# Files to process (all surface impact effects)
TARGET_FILES = [
    "SoF2_Effects_impact_concrete.json",
    "SoF2_Effects_impact_dirt.json",
    "SoF2_Effects_impact_gravel.json",
    "SoF2_Effects_impact_ice.json",
    "SoF2_Effects_impact_metal.json",
    "SoF2_Effects_impact_wood.json",
    "SoF2_Effects_impact_glass.json",
    "SoF2_Effects_impact_knife.json",
    "SoF2_Effects_impact_misc.json",
]

# Effects to SKIP (water, flesh already handled)
SKIP_EFFECT_PREFIXES = [
    "effects/impact_water",
    "effects/impact_flesh",
    "effects/impact_player",
]

# Threshold: only fix segments where endMax > this value
END_SIZE_THRESHOLD = 0.25
# Threshold for start sizes (belt variants have inflated starts)
START_SIZE_THRESHOLD = 0.15
# Division factor
DIVISOR = 2.0

changes = []
total_files = 0


def should_skip_effect(effect_id):
    for prefix in SKIP_EFFECT_PREFIXES:
        if effect_id.startswith(prefix):
            return True
    return False


def is_smoke_dust_texture(texture):
    if not texture:
        return False
    return texture.lower() in {t.lower() for t in SMOKE_DUST_TEXTURES}


def fix_segment_sizes(effect_id, seg_name, segment):
    """Fix oversized smoke/dust particle sizes. Returns number of changes."""
    texture = segment.get("texture", "")
    seg_type = segment.get("type", "")

    # Only fix particle segments (not tail, decal, etc.)
    if seg_type != "particle":
        return 0

    # Only fix smoke/dust textures
    if not is_smoke_dust_texture(texture):
        return 0

    size = segment.get("size")
    if not size:
        return 0

    count = 0
    end_min = size.get("endMin", 0)
    end_max = size.get("endMax", 0)

    # Fix end sizes if above threshold
    if end_max > END_SIZE_THRESHOLD:
        old_end_min = end_min
        old_end_max = end_max
        new_end_min = round(end_min / DIVISOR, 4)
        new_end_max = round(end_max / DIVISOR, 4)
        size["endMin"] = new_end_min
        size["endMax"] = new_end_max
        changes.append(
            f"  {effect_id} / {seg_name or texture}: "
            f"endSize {old_end_min:.4f}-{old_end_max:.4f} → "
            f"{new_end_min:.4f}-{new_end_max:.4f}"
        )
        count += 1

    # Also fix start sizes if they're inflated (belt variants)
    start_min = size.get("startMin", 0)
    start_max = size.get("startMax", 0)
    if start_max > START_SIZE_THRESHOLD:
        old_start_min = start_min
        old_start_max = start_max
        new_start_min = round(start_min / DIVISOR, 4)
        new_start_max = round(start_max / DIVISOR, 4)
        size["startMin"] = new_start_min
        size["startMax"] = new_start_max
        changes.append(
            f"  {effect_id} / {seg_name or texture}: "
            f"startSize {old_start_min:.4f}-{old_start_max:.4f} → "
            f"{new_start_min:.4f}-{new_start_max:.4f}"
        )
        count += 1

    return count


def process_file(filepath):
    global total_files
    with open(filepath, "r", encoding="utf-8") as f:
        data = json.load(f)

    file_changes = 0
    for effect in data:
        effect_id = effect.get("id", "")
        if should_skip_effect(effect_id):
            continue

        segments = effect.get("segments", [])
        for seg in segments:
            seg_name = seg.get("name", "")
            file_changes += fix_segment_sizes(effect_id, seg_name, seg)

    if file_changes > 0:
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        total_files += 1

    return file_changes


print("=" * 70)
print("SMOKE/DUST SIZE FIX — All Surface Impact Effects")
print("=" * 70)
print(f"Threshold: endMax > {END_SIZE_THRESHOLD}m or startMax > {START_SIZE_THRESHOLD}m")
print(f"Divisor: ÷{DIVISOR}")
print()

total_changes = 0
for filename in TARGET_FILES:
    filepath = EFFECTS_DIR / filename
    if not filepath.exists():
        print(f"SKIP (not found): {filename}")
        continue

    before_count = len(changes)
    file_changes = process_file(filepath)
    if file_changes > 0:
        print(f"\n[{filename}] — {file_changes} changes:")
        for c in changes[before_count:]:
            print(c)
        total_changes += file_changes
    else:
        print(f"[{filename}] — no changes needed")

print()
print("=" * 70)
print(f"TOTAL: {total_changes} size changes across {total_files} files")
print("=" * 70)
