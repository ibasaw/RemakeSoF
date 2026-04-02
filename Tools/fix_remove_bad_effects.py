"""Fix flags bug in previously converted effects and re-convert them."""
import json
import os

JSON_ROOT = r'd:\RemakeSoF\Assets\Resources\Data\Effects'

# IDs that were added by the converter (need to be removed and re-added)
ids_to_remove = {
    'SoF2_Effects_muzzle_flashes.json': [
        'effects/muzzle_flashes/mflash_ak74_inworld',
        'effects/muzzle_flashes/mflash_m3_inworld',
        'effects/muzzle_flashes/mflash_m4_inworld',
        'effects/muzzle_flashes/mflash_m60_inworld',
        'effects/muzzle_flashes/mflash_mm1_inworld',
        'effects/muzzle_flashes/mflash_pistols_inworld',
        'effects/muzzle_flashes/mflash_uzi_inworld',
        'effects/muzzle_flashes/mflash_m1911a1',
        'effects/muzzle_flashes/mflash_uzi_final',
    ],
    'SoF2_Effects_muzzle_smoke.json': [
        'effects/muzzle_flashes/smoke_m1911a1',
        'effects/muzzle_flashes/smoke_m3',
        'effects/muzzle_flashes/smoke_ussocom',
    ],
    'SoF2_Effects_fire.json': [
        'effects/fire/ground_fire',
        'effects/fire/ground_fire_cheap',
        'effects/fire/ground_fire_static',
        'effects/fire/floor_fire',
        'effects/fire/growing_fire',
        'effects/fire/spill_fire',
        'effects/fire/flame_jet',
        'effects/fire/incendiary_fire',
        'effects/fire/incendiary_fire-tendril',
        'effects/fire/incendiary_fire_trail',
        'effects/fire/rpg7_fire-tendril',
        'effects/fire/smoke_static_large',
        'effects/fire/smoke_static_medium',
        'effects/fire/smoke_static_small',
        'effects/fire/smoke_static_smaller',
    ],
    'SoF2_Effects_explosions.json': [
        'effects/explosions/big_explosion',
        'effects/explosions/vertical_explosion',
        'effects/explosions/glass_small',
        'effects/explosions/explosion_chunk',
        'effects/explosions/phosphorus_chunk',
        'effects/explosions/phosphorus_chunk_smoke',
        'effects/explosions/phosphorus_ember',
        'effects/explosions/phosphorus_grenade',
        'effects/explosions/phosphorus_trail',
        'effects/explosions/rpg7_explosion_huge',
        'effects/explosions/camera_shake_big',
        'effects/explosions/camera_shake_small',
    ],
}

for filename, ids in ids_to_remove.items():
    filepath = os.path.join(JSON_ROOT, filename)
    with open(filepath, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    before = len(data)
    ids_set = set(ids)
    data = [e for e in data if e['id'] not in ids_set]
    after = len(data)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"  {filename}: removed {before - after} effects ({before} -> {after})")

print("\nDone. Now re-run convert_missing_effects.py")
