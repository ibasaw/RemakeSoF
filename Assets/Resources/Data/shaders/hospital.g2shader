textures/hospital/hos1_fog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.1 0.1 0.12 ) 5000.0
}

textures/hospital/hos3_fog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.5 0.64 0.6 ) 9000.0
}

textures/hospital/hos3_brushfog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	fog
	surfaceparm	trans
	q3map_nolightmap
	fogparms	( 0.3 0.44 0.4 ) 12000.0
}

textures/hospital/hos4_fog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.05 0.05 0.065 ) 9000.0
}

textures/hospital/glass1
{
	qer_editorimage	textures/hospital/metalgrime
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.15
        tcGen environment
    }
    {
        map textures/hospital/metalgrime
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen const 0.2
    }
}

textures/hospital/glass_safety_no_shoot
{
	qer_editorimage	textures/hospital/glass_safety
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	shotclip
	q3map_material	BPGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/hospital/glass_safety
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/hospital/hospdoors
{
	qer_trans	0.5
	surfaceparm	trans
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
	damageShader	textures/hospital/hospdoors_d 1
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE
        alphaGen const 0.15
        tcGen environment
    }
    {
        map textures/hospital/hospdoors
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/hospital/hospdoors_side
{
	qer_trans	0.5
	surfaceparm	trans
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE
        alphaGen const 0.15
        tcGen environment
    }
    {
        map textures/hospital/hospdoors_side
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/hospital/door_surgery
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	surfaceparm	shotclip
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/door_surgery
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/door_surgery2
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	q3map_material	HollowWood
	aliasShader	textures/hospital/door_halls1
    {
        map $lightmap
    }
    {
        map textures/hospital/door_surgery2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/fence1
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
    {
        map textures/hospital/fence1
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
        map textures/hospital/fence1
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hospital/wall_scuff
{
	qer_editorimage	textures/tools/editor_images/qer_wall_scuff
	polygonOffset
	q3map_material	Concrete
	q3map_nolightmap
    {
        map textures/hospital/wall_scuff
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/hospital/sign1
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
    {
        map textures/hospital/sign1
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign2
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
	aliasShader	textures/hospital/sign1
    {
        map textures/hospital/sign2
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign3
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
	aliasShader	textures/hospital/sign1
    {
        map textures/hospital/sign3
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign4
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
	aliasShader	textures/hospital/sign1
    {
        map textures/hospital/sign4
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign5
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
	aliasShader	textures/hospital/sign1
    {
        map textures/hospital/sign5
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign6
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
	aliasShader	textures/hospital/sign1
    {
        map textures/hospital/sign6
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign7
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
	aliasShader	textures/hospital/sign1
    {
        map textures/hospital/sign7
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign8
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
	aliasShader	textures/hospital/sign1
    {
        map textures/hospital/sign8
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/sign_arrow
{
	polygonOffset
	q3map_material	HollowMetal
	q3map_nolightmap
    {
        map textures/hospital/sign_arrow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/exit
{
	polygonOffset
	q3map_material	Computer
	q3map_nolightmap
	damageShader	textures/hospital/exit_d 1
    {
        map textures/hospital/exit
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/met_env
{
	qer_editorimage	textures/hospital/metal_sm
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/metal_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
// Next line should be 'blendfunc GL_DST_COLOR GL_SRC_ALPHA'

        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

textures/hospital/hostank_rust
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/hostank_rust
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_sheen_add
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen const 0.2
        tcGen environment
    }
}

textures/hospital/metbright_env
{
	qer_editorimage	textures/hospital/metal2_sm
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/metal2_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.4
        tcGen environment
    }
}

textures/hospital/fire_exting1
{
	q3map_material	SolidMetal
	damageShader	textures/hospital/fire_exting1_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/fire_exting1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/fire_hose1
{
	q3map_material	SolidMetal
	damageShader	textures/hospital/fire_hose1_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/fire_hose1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/curtain_privacy_nocull
{
	qer_editorimage	textures/hospital/curtain_privacy
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	Plastic
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hospital/curtain_privacy
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/door_halls2
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	surfaceparm	shotclip
	q3map_material	HollowWood
	aliasShader	textures/hospital/door_halls1
    {
        map $lightmap
    }
    {
        map textures/hospital/door_halls2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/door_halls1
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	surfaceparm	shotclip
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/door_halls1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cabinets_flr2
{
	q3map_material	HollowWood
	aliasShader	textures/hospital/cabinets_flr
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cabinets_flr3
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cabinets_flr
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/corkboard2
{
	q3map_material	HollowWood
	aliasShader	textures/hospital/corkboard
    {
        map $lightmap
    }
    {
        map textures/hospital/corkboard2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/corkboard
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/corkboard
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/wood_partition
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/wood_partition
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_dept_guide
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_dept_guide
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_wagenotice
{
	q3map_material	Glass
	aliasShader	textures/hospital/sign_mittens
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_wagenotice
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/curtain_privacy
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hospital/curtain_privacy
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_mittens
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_mittens
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/sign_map
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_map
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/countertop1
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/countertop1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.2
        tcGen environment
    }
}

textures/hospital/door_elevator1
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/door_elevator1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_buttons
{
	polygonOffset
	q3map_material	SolidMetal
	damageShader	textures/hospital/elevator_buttons_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_buttons
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
    {
        map textures/hospital/elevator_buttons_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hospital/elevator_arrows
{
	polygonOffset
	q3map_material	Computer
	damageShader	textures/hospital/elevator_arrows_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_arrows
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
    {
        map textures/hospital/elevator_arrows_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hospital/elevator_buttons_int
{
	polygonOffset
	q3map_material	Computer
	damageShader	textures/hospital/elevator_buttons_int_d 1
    {
        map $lightmap
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_ZERO
        tcGen environment
    }
    {
        map textures/hospital/elevator_buttons_int
        blendFunc GL_ONE_MINUS_SRC_ALPHA GL_SRC_ALPHA
        detail
        rgbGen vertex
    }
    {
        map textures/hospital/elevator_buttons_int_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/hospital/elevator_floor
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_floor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_wall
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_wall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_ceiling
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_ceiling
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hospital/elevator_ceiling_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/cabinets_flr3_side
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr3_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cabinets_flr3_top
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr3_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cabinets_flr_side
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/corkboard3
{
	q3map_material	HollowWood
	aliasShader	textures/hospital/corkboard
    {
        map $lightmap
    }
    {
        map textures/hospital/corkboard3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/countertop_sink
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/countertop_sink
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_helipad
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	polygonOffset
	q3map_material	Concrete
    {
        map textures/hospital/sign_helipad
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
        map textures/hospital/sign_helipad
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
    {
        map textures/hospital/sign_helipad_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/hospital/mast
{
	surfaceparm	nonsolid
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/hospital/mast
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
        map textures/hospital/mast
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hospital/red
{
	q3map_nolightmap
    {
        map textures/hospital/red
    }
}

textures/hospital/red_pulse_1
{
	qer_editorimage	textures/hospital/red
	q3map_nolightmap
    {
        map textures/hospital/red
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen wave inversesawtooth 0 1 0 0.5
    }
}

textures/hospital/red_pulse_2
{
	qer_editorimage	textures/hospital/red
	q3map_nolightmap
    {
        map textures/hospital/red
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen wave inversesawtooth 0 1 0.5 0.5
    }
}

textures/hospital/hosextwall2
{
	surfaceparm	nonsolid
	polygonOffset
	q3map_material	HollowWood
	damageShader	textures/hospital/hosextwall2_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/hosextwall2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/floorvinyl02
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
// blendFunc GL_DST_COLOR GL_SRC_ALPHA

// alphaGen lightingSpecular

        map textures/hospital/floorvinyl02
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/floorvinyl01
{
	q3map_material	Marble
	aliasShader	textures/hospital/floorvinyl02
    {
        map $lightmap
    }
    {
// blendFunc GL_DST_COLOR GL_SRC_ALPHA

// alphaGen lightingSpecular

        map textures/hospital/floorvinyl01
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/floorcheckr01
{
	q3map_material	Marble
	aliasShader	textures/hospital/floorvinyl02
    {
        map $lightmap
    }
    {
// blendFunc GL_DST_COLOR GL_SRC_ALPHA

// alphaGen lightingSpecular

        map textures/hospital/floorcheckr01
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/glass1_twosided
{
	qer_editorimage	textures/hospital/metalgrime
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	cull	disable
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.15
        tcGen environment
    }
    {
        map textures/hospital/metalgrime
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen const 0.2
    }
}

textures/hospital/brck03
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/brck03
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cable_gnd
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/cable_gnd
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cable_steel_rust
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/cable_steel_rust
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/concrete1
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/concrete1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/concrete2
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/concrete2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/concrete3
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/concrete3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/door_wood01
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hospital/door_wood01
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_track
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_track
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/grass
{
	q3map_material	ShortGrass
    {
        map $lightmap
    }
    {
        map textures/hospital/grass
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/handrail_wood
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/hospital/handrail_wood
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hosextwall_trim
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/hosextwall_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hoswall_3_temp
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/hoswall_3_temp
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hosextwall1
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/hosextwall1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/wall03
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/wall03
        blendFunc GL_DST_COLOR GL_SRC_COLOR
        depthWrite
    }
    {
        map textures/hospital/env_hostile
        blendFunc GL_DST_ALPHA GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/container_bhaz
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hospital/container_bhaz
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/container_bhaz2
{
	entityMergable
	q3map_material	Plastic
	aliasShader	textures/hospital/container_bhaz
    {
        map $lightmap
    }
    {
        map textures/hospital/container_bhaz2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/container_bhazlid
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/container_bhazlid
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/container_bhlarge
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hospital/container_bhlarge
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/container_bhlarge_lid
{
	q3map_material	Plastic
	aliasShader	textures/hospital/container_bhaz
    {
        map $lightmap
    }
    {
        map textures/hospital/container_bhlarge_lid
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/container_bhlargetop
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hospital/container_bhlargetop
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/metal_sm
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/metal_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/metal2_sm
{
	q3map_material	SolidMetal
	aliasShader	textures/hospital/metal_sm
    {
        map $lightmap
    }
    {
        map textures/hospital/metal2_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/nonslip
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/hospital/nonslip
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/paint_rustred
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/paint_rustred
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/paint_white
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/paint_white
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/pitmetal
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/pitmetal
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/scrapecrete
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/scrapecrete
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_back
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_back
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_back2
{
	q3map_material	HollowMetal
	aliasShader	textures/hospital/sign_back
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_back2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_noentry
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_noentry
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_nosmoke
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_nosmoke
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/tubing_galvanized
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/tubing_galvanized
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/wall_trim_blue
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/wall_trim_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/wall_trim_blue2
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/wall_trim_blue2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/wall_trim_green2
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/wall_trim_green2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/wall_trim_yellow
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/wall_trim_yellow
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/wall01
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/wall01
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_blue_front
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_blue_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_blue_left
{
	q3map_material	HollowMetal
	aliasShader	textures/hospital/3panel_blue_front
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_blue_left
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_blue_right
{
	q3map_material	HollowMetal
	aliasShader	textures/hospital/3panel_blue_front
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_blue_right
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_green_front
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_green_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_green_left
{
	q3map_material	HollowMetal
	aliasShader	textures/hospital/3panel_green_front
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_green_left
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_green_right
{
	q3map_material	HollowMetal
	aliasShader	textures/hospital/3panel_green_front
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_green_right
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_yellow_front
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_yellow_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_yellow_left
{
	q3map_material	HollowMetal
	aliasShader	textures/hospital/3panel_yellow_front
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_yellow_left
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/3panel_yellow_right
{
	q3map_material	HollowMetal
	aliasShader	textures/hospital/3panel_yellow_front
    {
        map $lightmap
    }
    {
        map textures/hospital/3panel_yellow_right
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/directory_blue1
{
	q3map_material	Glass
	damageShader	textures/hospital/directory_blue1_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/directory_blue1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/directory_green1
{
	q3map_material	Glass
	damageShader	textures/hospital/directory_green1_d 1
	aliasShader	textures/hospital/directory_blue1
    {
        map $lightmap
    }
    {
        map textures/hospital/directory_green1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/brassstrip
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/brassstrip
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/metalgrime
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/metalgrime
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/caution_sign
{
	q3map_material	Plastic
	cull	disable
    {
        map textures/hospital/caution_sign
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
        map textures/hospital/caution_sign
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/hospital/met_env_2sided
{
	qer_editorimage	textures/hospital/metal_sm
	q3map_material	SolidMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hospital/metal_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.4
        tcGen environment
    }
}

textures/hospital/o2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/o2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/tower
{
	q3map_material	HollowMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hospital/tower
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cabinets_flr_decal
{
	qer_editorimage	textures/hospital/cabinets_flr
	polygonOffset
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/cabinets_flr2_decal
{
	qer_editorimage	textures/hospital/cabinets_flr2
	polygonOffset
	q3map_material	HollowWood
	aliasShader	textures/hospital/cabinets_flr_decal
    {
        map $lightmap
    }
    {
        map textures/hospital/cabinets_flr2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/fire_hose1_decal
{
	qer_editorimage	textures/common/env_chrome
	polygonOffset
	q3map_material	SolidMetal
	damageShader	textures/hospital/fire_hose1_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/fire_hose1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/fire_exiting1_decal
{
	qer_editorimage	textures/common/env_chrome
	polygonOffset
	q3map_material	SolidMetal
	damageShader	textures/hospital/fire_exting1_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/fire_exting1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/sign_map_decal
{
	qer_editorimage	textures/hospital/sign_map
	polygonOffset
	q3map_material	Glass
	damageShader	textures/hospital/sign_map_d 1
    {
        map $lightmap
    }
    {
        map textures/hospital/sign_map
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/wall3a
{
	qer_editorimage	textures/hospital/wall03
	q3map_material	Plaster
	q3map_novertexshadows
    {
        map $lightmap
    }
    {
        map textures/hospital/wall03
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/directory_blue1_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hospital/directory_blue1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/directory_green1_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hospital/directory_green1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_arrows_d
{
	q3map_material	SolidMetal
	damageShader	textures/hospital/elevator_arrows_d2 1
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_arrows_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hospital/elevator_arrows_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0.5 1 0 100
    }
}

textures/hospital/elevator_buttons_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_buttons_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_buttons_int_d
{
	q3map_material	SolidMetal
	damageShader	textures/hospital/elevator_buttons_int_d2 1
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_buttons_int_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hospital/elevator_buttons_int_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0.5 20 1 1000
    }
}

textures/hospital/exit_d
{
	q3map_material	HollowMetal
	damageShader	textures/hospital/exit_d2 1
    {
        map $lightmap
    }
    {
        map textures/hospital/exit_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/hospital/exit
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0.2 0.5 0.05 1000
    }
}

textures/hospital/exit_d2
{
	qer_editorimage	textures/hospital/exit_d
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/exit_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_arrows_d2
{
	qer_editorimage	textures/hospital/elevator_arrows_d
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_arrows_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_buttons_int_d2
{
	qer_editorimage	textures/hospital/elevator_buttons_int_d
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_buttons_int_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/fire_exting1_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/fire_exting1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/fire_hose1_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/fire_hose1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hosextwall2_d
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/hospital/hosextwall2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hospdoors_d
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/hospital/hospdoors_d
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/hospital/wall3a_decal
{
	qer_editorimage	textures/hospital/wall03
	q3map_material	Plaster
	q3map_novertexshadows
    {
        map $lightmap
    }
    {
        map textures/hospital/wall03
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/elevator_handrail
{
	qer_editorimage	textures/hospital/elevator_handrail
	polygonOffset
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/elevator_handrail
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/hospital/glass_safety
{
	qer_editorimage	textures/hospital/glass_safety
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	shotclip
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/hospital/glass_safety
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/hospital/hazardtape
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hospital/hazardtape
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hazardtape_dsided_decal
{
	qer_editorimage	textures/hospital/hazardtape
	polygonOffset
	q3map_material	Plastic
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hospital/hazardtape
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/met_env_nonsolid
{
	qer_editorimage	textures/hospital/metal_sm
	surfaceparm	nonsolid
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/hospital/metal_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
// Next line should be 'blendfunc GL_DST_COLOR GL_SRC_ALPHA'

        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

textures/hospital/hazardtape_env_decalorig
{
	qer_editorimage	textures/hospital/hazardtape
	polygonOffset
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/hospital/hazardtape
        rgbGen lightingDiffuse
    }
}

textures/hospital/hazardtape_env_decal
{
	qer_editorimage	textures/hospital/hazardtape
	polygonOffset
	q3map_material	Plastic
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/hospital/hazardtape
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/door_surgery_mp
{
	qer_editorimage	textures/hospital/door_surgery
	surfaceparm	playerclip
	surfaceparm	shotclip
	q3map_material	HollowWood
	q3map_nolightmap
	q3map_alphashadow
	q3map_onlyvertexlighting
	q3map_novertexshadows
    {
        map textures/hospital/door_surgery
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
    }
}

textures/hospital/scrapecrete_decal
{
	qer_editorimage	textures/hospital/scrapecrete
	polygonOffset
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/hospital/scrapecrete
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hoswall_1_temp
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/hospital/hoswall_1_temp
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hoswall_2_temp
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/hoswall_2_temp
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hoswall_2trim_temp
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/hoswall_2_temp
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/hospital/hoswall_trim_temp
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/hospital/hoswall_trim_temp
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

