textures/hongkong/hk1_waterfog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	surfaceparm	trans
	q3map_nolightmap
	fogparms	( 0.17 0.2 0.18 ) 2210.0
}

textures/hongkong/hkstreets
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	q3map_material	Concrete
	fogparms	( 0.59 0.55 0.5 ) 4200.0
}

textures/hongkong/hkprison
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	q3map_material	Concrete
	fogparms	( 0.3 0.3 0.39 ) 15000.0
}

textures/hongkong/hkwarehouse
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	q3map_material	Concrete
	fogparms	( 0.3 0.3 0.39 ) 10000.0
}

textures/hongkong/hkdocks
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	q3map_material	Concrete
	fogparms	( 0.59 0.55 0.5 ) 2150.0
}

textures/hongkong/hkdocksu
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	q3map_material	Concrete
	fogparms	( 0.59 0.55 0.5 ) 9500.0
}

textures/hongkong/hkwareout
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	q3map_material	Concrete
	fogparms	( 0.59 0.55 0.5 ) 12500.0
}

textures/hongkong/wood_top
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/wood_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/brick1
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/brick1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/canopy1
{
	polygonOffset
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/canopy1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/canopy1_edge
{
	polygonOffset
	q3map_material	Canvas
	cull	disable
    {
        map textures/hongkong/canopy1_edge
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/canopy1_edge
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/cement2
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/cement2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/chain
{
	q3map_material	SolidMetal
    {
        map textures/hongkong/chain
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/chain
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/container1
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/container3
    {
        map $lightmap
    }
    {
        map textures/hongkong/container1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container1b
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/container3b
    {
        map $lightmap
    }
    {
        map textures/hongkong/container1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container1c
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/container1c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container2
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/container3
    {
        map $lightmap
    }
    {
        map textures/hongkong/container2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container2b
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/container3b
    {
        map $lightmap
    }
    {
        map textures/hongkong/container2b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container2c
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/container2c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container3
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/container3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container3b
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/container3b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/container3c
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/container3c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/crate1
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/crate1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/crate1b
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/crate1c
    {
        map $lightmap
    }
    {
        map textures/hongkong/crate1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/crate1c
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/crate1c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/crate2
{
	q3map_material	HollowWood
    {
        map textures/hongkong/crate2
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/crate2
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/crate2b
{
	q3map_material	HollowWood
    {
        map textures/hongkong/crate2b
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/crate2b
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/dockwood
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/dockwood
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/door_garage3
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage2
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/door_garage3
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage3
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door1
{
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/door2
    {
        map $lightmap
    }
    {
        map textures/hongkong/door1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door2
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/door2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door3
{
	q3map_material	SolidWood
	aliasShader	textures/hongkong/door2
    {
        map $lightmap
    }
    {
        map textures/hongkong/door3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door4
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/door4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/floor_ocean
{
	q3map_material	Water
    {
        map $lightmap
    }
    {
        map textures/hongkong/floor_ocean
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/fueldrum2b_top
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/fueldrum2b_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/fueldrum2c
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/fueldrum2c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/fueldrum3b_top
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/fueldrum2b_top
    {
        map $lightmap
    }
    {
        map textures/hongkong/fueldrum3b_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/fueldrum3c
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/fueldrum2c
    {
        map $lightmap
    }
    {
        map textures/hongkong/fueldrum3c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/fueldrum4b_top
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/fueldrum2b_top
    {
        map $lightmap
    }
    {
        map textures/hongkong/fueldrum4b_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/fueldrum4c
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/fueldrum2c
    {
        map $lightmap
    }
    {
        map textures/hongkong/fueldrum4c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/grate1
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/hongkong/grate1
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/grate1
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/light
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/light
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/light_pole
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/light_pole
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal_white
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal_white
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal1
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal1_trim
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal1_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/neon_hn1a
{
	q3map_material	Computer
	damageShader	textures/hongkong/neon_hn1a_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/neon_hn1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/neon_hn1a_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/neon_hn1b
{
	q3map_material	Computer
	damageShader	textures/hongkong/neon_hn1b_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/neon_hn1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/neon_hn1b_glow
        blendFunc GL_ONE GL_ONE
        alphaGen wave sin 0 5 0 16
    }
}

textures/hongkong/neon_ss1
{
	q3map_material	Computer
	damageShader	textures/hongkong/neon_ss1_d 1
    {
        map textures/hongkong/neon_ss1
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/neon_ss1
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/neon_ss1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/neon_ss1b
{
	q3map_material	SolidMetal
	damageShader	textures/hongkong/neon_ss1b_d 1
    {
        map textures/hongkong/neon_ss1b
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/neon_ss1b
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/neon_ss1b_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/palette
{
	q3map_material	SolidWood
	cull	disable
    {
        map textures/hongkong/palette
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/palette
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/roof3
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/roof3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sidewalk
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/sidewalk
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sidewalk_edge
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/sidewalk_edge
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco1
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/stucco2
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco2
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco3
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/stucco2
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco4
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/stucco2
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco5a
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/stucco2
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco5a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco6
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco6
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/tire
{
	polygonOffset
	q3map_material	Rubber
    {
        map textures/hongkong/tire
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/tire
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/tire_end
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/hongkong/tire_end
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window1
{
	q3map_material	Glass
	damageShader	textures/hongkong/window1_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window3
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window5
{
	q3map_material	Glass
	aliasShader	textures/hongkong/window_4
    {
        map $lightmap
    }
    {
        map textures/hongkong/window5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/wood_pile
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/wood_pile
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window2b
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window2b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window6b
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window6b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/curb_stripe
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/curb_stripe
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/palette_edge
{
	q3map_material	SolidWood
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/palette_edge
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco_trim
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_grate2
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_grate2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_grate1
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_grate1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/roof_met3
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/roof_met3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/roof_met2
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/roof_met2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/roof_met1
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/roof_met1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/support_beam2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/support_beam2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box1b
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box1c
    {
        map $lightmap
    }
    {
        map textures/hongkong/box1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box1c
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/box1c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box1d
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box1c
    {
        map $lightmap
    }
    {
        map textures/hongkong/box1d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box1e
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box1c
    {
        map $lightmap
    }
    {
        map textures/hongkong/box1e
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box2a
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/box2a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box2b
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box3a
    {
        map $lightmap
    }
    {
        map textures/hongkong/box2b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box2c
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box2a
    {
        map $lightmap
    }
    {
        map textures/hongkong/box2c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box3a
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/box3a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box3b
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box2a
    {
        map $lightmap
    }
    {
        map textures/hongkong/box3b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box3c
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box3a
    {
        map $lightmap
    }
    {
        map textures/hongkong/box3c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box3d
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box2a
    {
        map $lightmap
    }
    {
        map textures/hongkong/box3d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/roof_met1side
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/roof_met1side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/roof_met2side
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/roof_met2side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/support_beam1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/support_beam1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/box1a
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/box1c
    {
        map $lightmap
    }
    {
        map textures/hongkong/box1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/shelf_1d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/shelf_1d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/shelf_1c
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/shelf_1c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/shelf_1b
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/shelf_1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/shelf_1a
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/shelf_1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/shelf_1a_nocull
{
	qer_editorimage	textures/hongkong/shelf_1a
	q3map_material	SolidMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/shelf_1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal_diamondplt
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal_diamondplt
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_trim
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_brick
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_brick
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_rust
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/cell_beam
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_rust
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_bars
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_bars
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_lock
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_lock
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/roof_met1b
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/roof_met1
    {
        map $lightmap
    }
    {
        map textures/hongkong/roof_met1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_bars2
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	q3map_nolightmap
	cull	disable
    {
        map textures/hongkong/cell_bars2
        alphaFunc GE128
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
    {
        map textures/hongkong/cell_bars2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/hongkong/cell_beam2
{
// * surfaceparm alphashadow

	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/hongkong/cell_beam2
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/cell_beam2
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/cell_beam_btm
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_beam_btm
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_beam
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_beam
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_window3
{
	surfaceparm	trans
	q3map_material	Glass
	damageShader	textures/hongkong/cell_window3_d 1
	aliasShader	textures/hongkong/cell_window1
    {
        map textures/hongkong/cell_window3
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/cell_window3
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/cell_window2
{
	q3map_material	Glass
	damageShader	textures/hongkong/cell_window2_d 10
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_window2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_window1
{
	surfaceparm	trans
	q3map_material	Glass
    {
        map textures/hongkong/cell_window1
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/cell_window1
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/cell_brick2
{
	q3map_material	Concrete
	aliasShader	textures/hongkong/cell_brick
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_brick2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/ceiling_interior_smoked
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/ceiling_interior_smoked
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/crate_j
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/crate_j
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/crate_j_side
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/crate_j_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/crate_j2
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/crate_j_side
    {
        map $lightmap
    }
    {
        map textures/hongkong/crate_j2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/deck_exterior
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/deck_exterior
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/floor_interior
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/floor_interior
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_cargo_door
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_cargo_door
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_cargo_door2
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_cargo_door2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_crossbeams
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_crossbeams
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_door_wood
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_door_wood
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_door_wood2
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/junk_crate3_side
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_door_wood2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_exterior_plank
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_exterior_plank
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_fuel_barrel_top
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/fueldrum2b_top
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_fuel_barrel_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_magic_eye
{
	q3map_material	HollowWood
	damageShader	textures/hongkong/junk_magic_eye_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_magic_eye
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_mast
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_mast
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_ornate_wood
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_ornate_wood
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_red_wood_trim
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_red_wood_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_sail
{
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_sail
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/fuel_barrel
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/fuel_barrel
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_bed
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_bed
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_bed_side
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_bed_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_bedroll
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_bedroll
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_bedroll_end
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_bedroll_end
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_crate3_alphaside
{
	q3map_material	HollowWood
    {
        map textures/hongkong/junk_crate3_alphaside
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/junk_crate3_alphaside
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/junk_crate3_side
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_crate3_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_crate3_top
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/junk_crate3_side
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_crate3_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_excelsior
{
	q3map_material	DryLeaves
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_excelsior
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_step
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_step
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall1
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/building_wall10
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall2
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/building_wall10
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall3
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/building_wall10
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/rust_2
{
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/rust
    {
        map $lightmap
    }
    {
        map textures/hongkong/rust_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_1
{
	q3map_material	Glass
	damageShader	textures/hongkong/window_1_d 1
	aliasShader	textures/hongkong/window_3
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/brace
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidWood
    {
        map textures/hongkong/brace
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/brace
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/seawall
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/seawall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall4
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/building_wall5
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_brick3
{
	q3map_material	Concrete
	aliasShader	textures/hongkong/cell_brick
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_brick3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/wall_brick
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/wall_brick
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall5
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall6
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/building_wall5
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall6
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/laben_1
{
	qer_editorimage	textures/hongkong/laben_1
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/laben_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/laben_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/laben_2
{
	qer_editorimage	textures/hongkong/laben_2
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/laben_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/laben_2_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/laben_3
{
	qer_editorimage	textures/hongkong/laben_3
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/laben_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/laben_3_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/rust
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/rust
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_brick4
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_brick4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_3
{
	q3map_material	Glass
	damageShader	textures/hongkong/window_3_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_4
{
	q3map_material	Glass
	damageShader	textures/hongkong/window_4_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_5
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	q3map_material	Glass
	damageShader	textures/hongkong/window_5_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_a
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign1_a_d 1
	aliasShader	textures/hongkong/sign1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_b
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign1_b_d 1
	aliasShader	textures/hongkong/sign1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_c
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign1_c_d 1
	aliasShader	textures/hongkong/sign1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_3_glow
{
	qer_editorimage	textures/hongkong/window_3
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/window_3_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/building_wall7
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/building_wall5
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall7
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign10
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign10_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign10
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign11
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign11_d 1
	aliasShader	textures/hongkong/sign2
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign11
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign11glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 1 0.5 400 40
    }
}

textures/hongkong/sign12
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign12_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign12
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign2
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign2_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign3
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign3_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign4
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign4_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign5
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign6
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign6
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign7
{
	q3map_material	HollowWood
	damageShader	textures/hongkong/sign7_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign7
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign8
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign8_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign8
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign8_a
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign9
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign9_d 1
	aliasShader	textures/hongkong/sign15
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign9
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_4_glow
{
	qer_editorimage	textures/hongkong/window_4
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/window_4glow
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.25
        tcGen environment
    }
}

textures/hongkong/sign14
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign14_d 1
	aliasShader	textures/hongkong/neon_ss1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign14
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign13
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign13_d 1
	aliasShader	textures/hongkong/neon_ss1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign13
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_1_glow
{
	qer_editorimage	textures/hongkong/window_1
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/window_1_glow
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.25
        tcGen environment
    }
}

textures/hongkong/cell_window2_glow
{
	qer_editorimage	textures/hongkong/cell_window2
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_window2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/cell_window2_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/awning
{
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/awning
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/antenna1
{
	surfaceparm	nonsolid
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/hongkong/antenna1
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/antenna1
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/sign_mount
{
	surfaceparm	nonsolid
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/hongkong/sign_mount
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/sign_mount
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/bridge_rail
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
    {
        map textures/hongkong/bridge_rail
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/bridge_rail
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/antenna3
{
	surfaceparm	nonsolid
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/hongkong/antenna3
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/antenna3
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/bridge_stone
{
	q3map_material	Gravel
    {
        map $lightmap
    }
    {
        map textures/hongkong/bridge_stone
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_metal
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_metal
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall8
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall8
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/sign1_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/board_salon
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/hongkong/board_salon
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall10
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall10
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/building_wall9
{
	q3map_material	Plaster
	aliasShader	textures/hongkong/building_wall5
    {
        map $lightmap
    }
    {
        map textures/hongkong/building_wall9
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_gar4trim
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar4trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage4
{
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/door_garage3
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/gate_accord
{
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
    {
        map textures/hongkong/gate_accord
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/gate_accord
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/sign1a
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/sign1a_d 1
	aliasShader	textures/hongkong/sign1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1b
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/sign1b_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/basemetal1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/basemetal1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_plate
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_plate
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_gar4sign
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar4sign
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_gar4signb
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar4signb
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_wire
{
	q3map_material	SolidMetal
    {
        map textures/hongkong/door_wire
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/door_wire
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/ele_freight
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/ele_freight
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/ele_top
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/ele_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/barbwire
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	cull	disable
    {
        map textures/hongkong/barbwire
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/barbwire
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/door_wire_base
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_wire_base
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/ele_controlbox
{
	q3map_material	Computer
	damageShader	textures/hongkong/ele_controlbox_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/ele_controlbox
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/ele_controlbox_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.5 0 0.3
    }
    {
        map textures/hongkong/ele_controlbox_glow2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.5 0 0.7
    }
}

textures/hongkong/ele_controlbox2
{
	q3map_material	Computer
	damageShader	textures/hongkong/ele_controlbox2_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/ele_controlbox2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/ele_controlbox2_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.5 0 0.3
    }
    {
        map textures/hongkong/ele_controlbox2_glow2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.6 0 0.8
    }
}

textures/hongkong/girder_2b
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/hongkong/girder_2b
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/girder_2b
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/hoist
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/hoist
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/hoist_pole1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/hoist_pole1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/hoist_pole2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/hoist_pole2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/hoist_side
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/hoist_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stairs
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/stairs
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sup_beam01b2
{
	q3map_material	HollowMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/sup_beam01b2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/whiteboard
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hongkong/whiteboard
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/prisonsmoke
{
	qer_editorimage	textures/hongkong/smoke3
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_nolightmap
    {
        map textures/hongkong/smoke3
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.25
    }
}

textures/hongkong/sign_treasure
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_treasure
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_donotenter
{
	surfaceparm	nonsolid
	q3map_material	HollowMetal
    {
        map textures/hongkong/sign_donotenter
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/sign_donotenter
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/sign_bluearrow
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/neon_ss1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_bluearrow
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal_grate
{
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/metal_guncase
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal_grate
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_four
{
	polygonOffset
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/cell_one
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_four
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_one
{
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_one
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_six
{
	polygonOffset
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/cell_one
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_six
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_three
{
	polygonOffset
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/cell_one
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_three
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_two
{
	polygonOffset
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/cell_one
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_two
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/dryerase
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/dryerase
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/dryerase3
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/dryerase
    {
        map $lightmap
    }
    {
        map textures/hongkong/dryerase3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_five
{
	polygonOffset
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/cell_one
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_five
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal_guncase
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal_guncase
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/dryerase2
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/dryerase
    {
        map $lightmap
    }
    {
        map textures/hongkong/dryerase2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal_panel
{
	q3map_material	Computer
	damageShader	textures/hongkong/metal_panel_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal_panel
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/metal_panel_glow
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0 0.3 0 0.5
    }
}

textures/hongkong/door5
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/door2
    {
        map $lightmap
    }
    {
        map textures/hongkong/door5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/wanted_poster_decal
{
	qer_editorimage	textures/hongkong/wanted_poster
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hongkong/wanted_poster
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/hklamp_base
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/hklamp_base
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/hklamp
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/hklamp
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_1_decal
{
	qer_editorimage	textures/hongkong/window_1
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	damageShader	textures/hongkong/window_1_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/tile2
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/hongkong/tile2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco10
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco10
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco7
{
	q3map_material	Concrete
	aliasShader	textures/hongkong/stucco2
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco7
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/stucco8
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/stucco8
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/tile1
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/hongkong/tile1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/banner2
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	Canvas
	cull	disable
	aliasShader	textures/hongkong/banner1
    {
        map $lightmap
    }
    {
        map textures/hongkong/banner2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign15
{
	q3map_material	Plastic
	damageShader	textures/hongkong/sign15_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign15
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign15_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/door_gar5signb
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar5signb
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_gar5trim
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar5trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage5
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/door_garage3
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_gar5signa
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar5signa
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign16
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign16_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign16
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign16_glowb
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave noise 0.5 10 3 25
    }
    {
        map textures/hongkong/sign16_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign16a
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign16a_d 1
	aliasShader	textures/hongkong/neon_ss1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign16a
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign16a_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/door_gar5signc
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar5signc
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/banner1
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/banner1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign16b
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign16b_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign16b
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign16b_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave random 0 3 0 1
    }
}

textures/hongkong/sign17
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign17_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign17
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign17_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign17a
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign17a_d 1
	aliasShader	textures/hongkong/sign15
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign17a
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign17a_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign_ad1
{
	q3map_material	Computer
	aliasShader	textures/hongkong/sign2
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_ad1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign_ad1_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign_ad1a
{
	q3map_material	Computer
	aliasShader	textures/hongkong/sign2
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_ad1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign_ad1a_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave random 0 5 0 1
    }
}

textures/hongkong/sign_ad1b
{
	q3map_material	Computer
	aliasShader	textures/hongkong/sign2
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_ad1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign_ad1b_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign_ad2
{
	q3map_material	SolidWood
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_ad2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18
{
	q3map_material	HollowWood
	damageShader	textures/hongkong/sign18_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18a
{
	q3map_material	SolidMetal
	damageShader	textures/hongkong/sign18a_d 1
	aliasShader	textures/hongkong/sign1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18b
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign18b_d 1
	aliasShader	textures/hongkong/sign1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18c
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign18c_d 1
	aliasShader	textures/hongkong/sign1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign19
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign19
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign20
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign20a_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign20
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign20a
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign20_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign20a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign8b
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign8b_d 1
	aliasShader	textures/hongkong/sign15
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign8b
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign8b_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign20_neon
{
	qer_editorimage	textures/hongkong/sign20
	q3map_material	SolidMetal
	damageShader	textures/hongkong/sign20a_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign20
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign20_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0.05 100 0.5 500
    }
}

textures/hongkong/sign20a_neon
{
	qer_editorimage	textures/hongkong/sign20a
	q3map_material	SolidMetal
	damageShader	textures/hongkong/sign20_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign20a
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign20a_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave noise 1 1 5 50
    }
}

textures/hongkong/window2_decal
{
	qer_editorimage	textures/hongkong/window2
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	damageShader	textures/hongkong/window2_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_sliding_decal
{
	qer_editorimage	textures/hongkong/door_sliding
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_sliding
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/grate_decal
{
	qer_editorimage	textures/hongkong/grate
	surfaceparm	nomarks
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/grate
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_ad3
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/sign_ad3_d 1
	aliasShader	textures/hongkong/sign2
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_ad3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign22
{
	q3map_material	HollowWood
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign22
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door3_decal
{
	qer_editorimage	textures/hongkong/door3_single
	surfaceparm	nomarks
	polygonOffset
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/door3_single
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign8_north
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign8_north_d 1
	aliasShader	textures/hongkong/sign15
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign8_north
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/air_cond3
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/air_cond3_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/air_cond3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/security_rail
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
    {
        map textures/hongkong/security_rail
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/security_rail
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/sign_phoenix
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign_phoenix_d 1
	aliasShader	textures/hongkong/neon_ss1
    {
        map $lightmap
        alphaGen wave sin 0 5 0 10
    }
    {
        map textures/hongkong/sign_phoenix
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign_phoenix2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave noise 0 5 0 20
        tcMod turb 0 0.0001 0 1
    }
}

textures/hongkong/sign23
{
	q3map_material	SolidWood
	aliasShader	textures/hongkong/sign15
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign23
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/air_cond2
{
	q3map_material	Computer
	damageShader	textures/hongkong/air_cond2_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/air_cond2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/air_cond1
{
	q3map_material	Computer
	damageShader	textures/hongkong/air_cond1_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/air_cond1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door2b
{
	q3map_material	SolidWood
	aliasShader	textures/hongkong/door2
    {
        map $lightmap
    }
    {
        map textures/hongkong/door2b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_2_decal
{
	qer_editorimage	textures/hongkong/window_2
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	damageShader	textures/hongkong/window_2_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_gar4signb_blue
{
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/door_gar4signb
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar4signb_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_gar4trim_blue
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar4trim_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage4_blue
{
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/door_garage3
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage4_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_lawyer
{
	q3map_material	Computer
	damageShader	textures/hongkong/sign_lawyer_d 1
	aliasShader	textures/hongkong/neon_ss1
    {
        map $lightmap
        alphaGen wave sin 0 5 0 16
    }
    {
        map textures/hongkong/sign_lawyer
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign_lawyer2
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/door_gar4sign_blue
{
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/door_gar4sign
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_gar4sign_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_luggage
{
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign_luggage_d 1
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_luggage
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window1b_decal
{
	surfaceparm	nomarks
	polygonOffset
	q3map_material	ShatterGlass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window1b_decal
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/street_arrow3
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/street_arrow3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/street_arrow2
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hongkong/street_arrow2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/street_arrow2_light
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/street_arrow1
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hongkong/street_arrow1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/street_arrow1_light
        alphaFunc GE128
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/street_arrow4
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hongkong/street_arrow4
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/street_arrow4_light
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hongkong/sign_donotenter_b
{
	surfaceparm	nonsolid
	q3map_material	SolidMetal
    {
        map textures/hongkong/sign_donotenter_b
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/sign_donotenter_b
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/gang_logo_decal
{
	qer_editorimage	textures/tools/editor_images/qer_gang_logo
	surfaceparm	nomarks
	polygonOffset
	q3map_nolightmap
    {
        map textures/hongkong/gang_logo
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/hongkong/cell_window2glow_decal
{
	qer_editorimage	textures/hongkong/cell_window2
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_window2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/cell_window2_glow
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.25
        tcGen environment
    }
}

textures/hongkong/window_3_glow_decal
{
	qer_editorimage	textures/hongkong/window_3
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	damageShader	textures/hongkong/window_3_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/window_3_glow
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.25
        tcGen environment
    }
}

textures/hongkong/light_metalrim
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/light_metalrim
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/light_metalrim_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/light_metalrim_b
{
	qer_editorimage	textures/hongkong/light_metalrim
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/light_metalrim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/bill_1_decal
{
	qer_editorimage	textures/hongkong/bill_1
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Canvas
	aliasShader	textures/hongkong/bill_2_decal
    {
        map $lightmap
    }
    {
        map textures/hongkong/bill_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/bill_2_decal
{
	qer_editorimage	textures/hongkong/bill_2
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/hongkong/bill_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/bill_3_decal
{
	qer_editorimage	textures/hongkong/bill_3
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Canvas
	aliasShader	textures/hongkong/bill_2_decal
    {
        map $lightmap
    }
    {
        map textures/hongkong/bill_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/bill_4_decal
{
	qer_editorimage	textures/hongkong/bill_4
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/hongkong/bill_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/bill_5_decal
{
	qer_editorimage	textures/hongkong/bill_5
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/hongkong/bill_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_beam_nosolid
{
	qer_editorimage	textures/hongkong/cell_beam
	surfaceparm	nonsolid
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_beam
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage2_decal
{
	qer_editorimage	textures/hongkong/door_garage2
	surfaceparm	nomarks
	polygonOffset
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign14_decal
{
	qer_editorimage	textures/hongkong/sign14
	surfaceparm	nomarks
	polygonOffset
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign14
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_4_glow_decal
{
	qer_editorimage	textures/hongkong/window_4
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	damageShader	textures/hongkong/window_4_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/window_4glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/window_5_decal
{
	qer_editorimage	textures/hongkong/window_5
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	polygonOffset
	q3map_material	Glass
	damageShader	textures/hongkong/window_5_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_garage_decal
{
	qer_editorimage	textures/hongkong/door_garage
	surfaceparm	nomarks
	polygonOffset
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_garage
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door1_decal
{
	surfaceparm	nomarks
	polygonOffset
	q3map_material	SolidMetal
	aliasShader	textures/hongkong/door3_decal
    {
        map $lightmap
    }
    {
        map textures/hongkong/door1_decal
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window5_decal
{
	qer_editorimage	textures/hongkong/window5
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	aliasShader	textures/hongkong/window_4_glow_decal
    {
        map $lightmap
    }
    {
        map textures/hongkong/window5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_docks
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_docks
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_smside
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_smside
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_2
{
	q3map_material	Computer
	damageShader	textures/hongkong/panel_2_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_3
{
	q3map_material	Computer
	damageShader	textures/hongkong/panel_3_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_4
{
	q3map_material	Computer
	damageShader	textures/hongkong/panel_4_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/panel_4_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.2 0 5
    }
}

textures/hongkong/panel_fuse
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/panel_fuse_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_fuse
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_grate
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/panel_grate_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_grate
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_grate2
{
	q3map_material	HollowMetal
	damageShader	textures/hongkong/panel_grate2_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_grate2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_metal
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_metal
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_sm1
{
	q3map_material	Computer
	damageShader	textures/hongkong/panel_sm1_d 1
    {
        map $lightmap
    }
    {
        animMap 3 textures/hongkong/panel_sm1 textures/hongkong/panel_sm1_overlay 
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/panel_sm1_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.15 0 3
    }
}

textures/hongkong/panel_1
{
	q3map_material	Computer
	damageShader	textures/hongkong/panel_1_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/panel_1_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 1
    }
    {
        map textures/hongkong/panel_1_glow2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.2 0 25
    }
    {
        map textures/hongkong/panel_1_glow3
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.5
    }
}

textures/hongkong/panel_fuse2
{
	q3map_material	HollowMetal
	aliasShader	textures/hongkong/panel_fuse
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_fuse2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_edge
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_edge
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_grate3
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_grate3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_grate4
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_grate4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_sm2
{
	q3map_material	Computer
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_sm2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_sm3
{
	q3map_material	Computer
	damageShader	textures/hongkong/panel_sm3_d 1
    {
        map $lightmap
    }
    {
        animMap 1 textures/hongkong/panel_sm3 textures/hongkong/panel_sm3_overlay 
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/panel_sm3_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.5 0 0.5
    }
}

textures/hongkong/chest_top
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/chest_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/chest_lock
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/chest_lock
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/chest_side
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/chest_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/chest_front
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/chest_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cd_disk
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hongkong/cd_disk
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cd_disk_side
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hongkong/cd_disk_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign21
{
	q3map_material	SolidWood
	aliasShader	textures/hongkong/sign19
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign21
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/girder_2c
{
	qer_editorimage	textures/hongkong/girder_2b
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	q3map_alphashadow
	q3map_novertexshadows
	cull	disable
    {
        map textures/hongkong/girder_2b
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/girder_2b
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/window3_decal
{
	qer_editorimage	textures/hongkong/window3
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/banner1_decal
{
	qer_editorimage	textures/hongkong/banner1
	polygonOffset
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/banner1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_ad1_decal
{
	qer_editorimage	textures/hongkong/sign_ad1
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_ad1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hongkong/sign_ad1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hongkong/sign3_decal
{
	qer_editorimage	textures/hongkong/sign3
	polygonOffset
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign3_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign9_decal
{
	qer_editorimage	textures/hongkong/sign9
	polygonOffset
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign9_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign9
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/hk1fog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	q3map_material	Concrete
	fogparms	( 0.59 0.55 0.5 ) 12000.0
}

textures/hongkong/hk1_water
{
	qer_editorimage	textures/liner/water_linertest
	surfaceparm	water
	q3map_material	Water
	q3map_globaltexture
    {
        map $lightmap
    }
    {
        map textures/liner/water_linertest
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scale 0.15 0.15
        tcMod scroll 0.3 0
    }
}

textures/hongkong/air_cond1_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/air_cond1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/air_cond2_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/air_cond2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/air_cond3_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/air_cond3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_window2_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_window2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_window3_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_window3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/ele_controlbox_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/ele_controlbox_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/ele_controlbox2_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/ele_controlbox2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_magic_eye_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_magic_eye_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/metal_panel_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/metal_panel_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/neon_ss1_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/neon_ss1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/neon_ss1b_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/neon_ss1b_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_4_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_4_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_2_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_3_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_1_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_fuse_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_fuse_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_sm3_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_sm3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_sm1_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_sm1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_grate_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_grate_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/panel_grate2_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/panel_grate2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_ad3_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_ad3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_luggage_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_luggage_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_lawyer_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_lawyer_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign_phoenix_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign_phoenix_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_a_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_a_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_b_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_b_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_c_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_c_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign10_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign10_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign12_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign10_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign13_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign13_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign14_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign14_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign11_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign11_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign15_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign15_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign16_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign16_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign16a_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign16a_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign16b_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign16b_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign17_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign17_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign17a_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign17a_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18a_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18a_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18b_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18b_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign18c_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign18c_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1b_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1b_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign2_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign3_d
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_1_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_2_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window1_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window2_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign4_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign4_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign7_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign7_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign20a_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign20_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign20_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign20a_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign8_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign8_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign8_north_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign8_north_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign8b_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign8b_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign9_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign9_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/street_arrow1_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/street_arrow1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/street_arrow2_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/street_arrow2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/street_arrow4_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/street_arrow4_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_3_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_4_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_4_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/window_5_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/window_5_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/neon_hn1a_d
{
	qer_editorimage	textures/hongkong/neon_hn1a
	q3map_material	HollowMetal
	damageShader	textures/hongkong/neon_hn1a_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/neon_hn1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/neon_hn1b_d
{
	qer_editorimage	textures/hongkong/neon_hn1b
	q3map_material	HollowMetal
	damageShader	textures/hongkong/neon_hn1b_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/neon_hn1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/junk_sail_animated
{
	qer_editorimage	textures/hongkong/junk_sail
	q3map_material	Canvas
	cull	disable
	deformvertexes	wave	2 sin 10 10 0.25 0.25
    {
        map $lightmap
    }
    {
        map textures/hongkong/junk_sail
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/x_beam_b
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
    {
        map textures/hongkong/x_beam_b
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map textures/hongkong/x_beam_b
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hongkong/curb_stripe_decal
{
	qer_editorimage	textures/hongkong/curb_stripe
	polygonOffset
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hongkong/curb_stripe
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/pra6_morgue
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	fog
	surfaceparm	trans
	q3map_nolightmap
	fogparms	( 0.8 0.8 0.85 ) 712.0
}

textures/hongkong/sign1_a_decal
{
	qer_editorimage	textures/hongkong/sign1_a
	polygonOffset
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign1_a_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_b_decal
{
	qer_editorimage	textures/hongkong/sign1_b
	polygonOffset
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign1_b_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1_c_decal
{
	qer_editorimage	textures/hongkong/sign1_c
	polygonOffset
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign1_c_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1_c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign4_decal
{
	qer_editorimage	textures/hongkong/sign4
	polygonOffset
	q3map_material	SolidWood
	damageShader	textures/hongkong/sign4_d 1
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/pra5_heli_light
{
	qer_editorimage	textures/hongkong/smoke3
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_nolightmap
    {
        map textures/hongkong/smoke3
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.25
        tcMod scroll 0.2 0
    }
}

textures/hongkong/colombian_waterfog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	fog
	surfaceparm	trans
	q3map_nolightmap
	fogparms	( 0.09 0.11 0.08 ) 800.0
}

textures/hongkong/cell_bars_nomarks
{
	qer_editorimage	textures/hongkong/cell_bars
	surfaceparm	nomarks
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_bars
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/cell_rust_nomarks
{
	qer_editorimage	textures/hongkong/cell_rust
	surfaceparm	noimpact
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/cell_rust
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sup_beam01b2_nonsolid
{
	qer_editorimage	textures/hongkong/sup_beam01b2
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	HollowMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hongkong/sup_beam01b2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/hk1_dock_water
{
	qer_editorimage	textures/liner/water_linertest
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_globaltexture
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/liner/water_linertest
        blendFunc GL_ONE GL_SRC_COLOR
        tcMod scale 0.15 0.15
        tcMod scroll -0.002 -0.01
    }
    {
        map textures/common/env_sheen_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
        tcMod scroll 0.025 0.025
    }
}

textures/hongkong/door_sliding
{
	qer_editorimage	textures/hongkong/door_sliding
	surfaceparm	nomarks
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_sliding
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/door_sliding_d
{
	qer_editorimage	textures/hongkong/door_sliding
	surfaceparm	nomarks
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hongkong/door_sliding_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/laben_tille
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/laben_tille
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hongkong/sign1a_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hongkong/sign1a_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

models/objects/hongkong/lights/cell
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/hongkong/lights/cell
        rgbGen lightingDiffuse
    }
}

models/objects/hongkong/vehicles/car_hk
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map models/objects/hongkong/vehicles/car_hk
        rgbGen lightingDiffuse
    }
    {
        map models/objects/hongkong/vehicles/car_hk_light
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/hongkong/vehicles/car_hk_light
{
}

