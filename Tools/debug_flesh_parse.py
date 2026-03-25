import re
import sys
sys.path.insert(0, r"D:\RemakeSoF\Tools")
from efx_to_json import parse_efx

# Parse the actual impact_flesh.efx
segments = parse_efx(r"D:\sof2_extract\base\effects\impact_flesh.efx")

for i, seg in enumerate(segments):
    tex = seg.get("texture", "none")
    color = seg.get("color", None)
    size = seg.get("size", None)
    print(f"\nSegment {i}: texture={tex}")
    print(f"  color: {color}")
    print(f"  size: {size}")
