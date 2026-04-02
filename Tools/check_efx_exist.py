import os

files = {
    'muzzle_flashes': [
        'mflash_ak74_inworld', 'mflash_m3_inworld', 'mflash_m4_inworld',
        'mflash_m60_inworld', 'mflash_mm1_inworld', 'mflash_pistols_inworld',
        'mflash_uzi_inworld', 'mflash_m1911a1', 'mflash_m1911a1_final',
        'mflash_uzi_final', 'smoke_m1911a1', 'smoke_m3', 'smoke_ussocom'
    ],
    'fire': [
        'ground_fire', 'ground_fire_cheap', 'ground_fire_static',
        'floor_fire', 'growing_fire', 'spill_fire', 'flame_jet',
        'incendiary_fire', 'incendiary_fire-tendril', 'incendiary_fire_trail',
        'rpg7_fire-tendril', 'smoke_static_large', 'smoke_static_medium',
        'smoke_static_small', 'smoke_static_smaller'
    ],
    'explosions': [
        'big_explosion', 'vertical_explosion', 'glass_small',
        'explosion_chunk', 'phosphorus_chunk', 'phosphorus_chunk_smoke',
        'phosphorus_ember', 'phosphorus_grenade', 'phosphorus_trail',
        'rpg7_explosion_huge', 'camera_shake_big', 'camera_shake_small'
    ]
}

for folder, names in files.items():
    for name in names:
        path = f'd:/sof2_extract/base/effects/{folder}/{name}.efx'
        exists = os.path.exists(path)
        size = os.path.getsize(path) if exists else 0
        status = "OK" if exists else "MISSING"
        print(f'  {status}: {folder}/{name}.efx ({size} bytes)')
