"""Verify all surface+Knife effect references exist in JSON files."""
import json, glob

with open('Assets/Resources/Data/SoF2_data_per_surface.json', 'r', encoding='utf-8-sig') as f:
    data = json.load(f)

knife_effects = {}
for surface_name, surface in data.items():
    ammo = surface.get('ammoTypes', {})
    if 'Knife' in ammo:
        eff = ammo['Knife'].get('effect', '')
        if eff:
            effs = eff if isinstance(eff, list) else [eff]
            for e in effs:
                knife_effects.setdefault(e, []).append(surface_name)

print("All Knife impact effects in surface data:")
for e in sorted(knife_effects):
    surfaces = ', '.join(knife_effects[e])
    print(f"  {e} ({surfaces})")

all_effect_ids = set()
for fpath in glob.glob('Assets/Resources/Data/Effects/SoF2_Effects_*.json'):
    with open(fpath, 'r', encoding='utf-8') as f:
        effects = json.load(f)
    for eff in effects:
        all_effect_ids.add(eff['id'])

print(f"\nTotal effect IDs in JSON: {len(all_effect_ids)}")
print("\nVerification:")
missing = 0
for e in sorted(knife_effects):
    exists = e in all_effect_ids
    status = "EXISTS" if exists else "MISSING"
    if not exists:
        missing += 1
    print(f"  {e}: {status}")

# Also verify ALL ammo types across ALL surfaces
print("\n\n=== FULL SURFACE VERIFICATION (all ammo types) ===")
all_missing = []
for surface_name, surface in data.items():
    ammo = surface.get('ammoTypes', {})
    for ammo_type, ammo_data in ammo.items():
        eff = ammo_data.get('effect', '')
        if eff:
            effs = eff if isinstance(eff, list) else [eff]
            for e in effs:
                if e and e not in all_effect_ids:
                    all_missing.append(f"{surface_name}/{ammo_type}: {e}")

if all_missing:
    print(f"MISSING EFFECTS ({len(all_missing)}):")
    for m in all_missing:
        print(f"  {m}")
else:
    print("ALL surface→ammo→effect references resolve correctly!")
