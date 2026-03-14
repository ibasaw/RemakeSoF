textures/shop/shop_sky1
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	q3map_surfacelight	35
	sun 0.475207 0.578512 1 60 315 40
	surfaceparm	sky
	surfaceparm	noimpact
	q3map_nolightmap
	skyParms	- 256 -
}

// *********************************

// Test Fog for Cluods on SHOP2 SM

// *********************************

textures/shop/shop_testfog
{
// {

// map textures/test/smoke2

// blendfunc gl_dst_color gl_zero

// tcmod scale -.05 -.05

// tcmod scroll .01 -.01

// }

// {

// map textures/test/smoke2

// blendfunc gl_dst_color gl_zero

// tcmod scale .05 .05

// tcmod scroll .01 -.01

// }

	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	surfaceparm	trans
	q3map_nolightmap
	fogparms	( 0.65 0.69 0.79 ) 2910.0
}

textures/shop/wall1
{
	q3map_material	Plaster
	aliasShader	textures/shop/pillar1
    {
        map $lightmap
    }
    {
        map textures/shop/wall1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/floor2
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/shop/floor2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/pillar1
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/shop/pillar1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/pillar2
{
	q3map_material	Marble
	aliasShader	textures/shop/pillar1
    {
        map $lightmap
    }
    {
        map textures/shop/pillar2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/pillar3
{
	q3map_material	Marble
	aliasShader	textures/shop/pillar1
    {
        map $lightmap
    }
    {
        map textures/shop/pillar3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/pillar4
{
	q3map_material	Marble
	aliasShader	textures/shop/pillar1
    {
        map $lightmap
    }
    {
        map textures/shop/pillar4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/floor1
{
	q3map_material	Tiles
	aliasShader	textures/shop/floor2
    {
        map $lightmap
    }
    {
        map textures/shop/floor1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/cushion
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/shop/cushion
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/glass_1
{
	qer_editorimage	textures/shop/glass_test
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/shop/env_shop
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        alphaGen const 0.15
        tcGen environment
    }
    {
        map textures/hospital/metalgrime
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
        alphaGen const 0.2
    }
}

textures/shop/door1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/door1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/comp_main
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/comp_main
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/wall2b
{
	q3map_material	Plaster
	aliasShader	textures/shop/wall2b_tan
    {
        map $lightmap
    }
    {
        map textures/shop/wall2b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/floor_rubber
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/shop/floor_rubber
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/door2
{
	q3map_material	SolidMetal
	aliasShader	textures/shop/door1
    {
        map $lightmap
    }
    {
        map textures/shop/door2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/comp_monitor
{
	q3map_material	Computer
	damageShader	textures/shop/comp_monitor_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/comp_monitor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/cube_2
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/shop/cube_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/cube_3
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/cube_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/cube_1
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/shop/cube_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/env_shop
{
}

textures/shop/comp_panel
{
	q3map_material	Computer
	damageShader	textures/shop/comp_panel_d 1
    {
        map $lightmap
    }
    {
        animMap 1 textures/shop/comp_panel textures/shop/comp_panel_overlay 
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/comp_panel_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/shop/comp_side
{
	q3map_material	Computer
	damageShader	textures/shop/comp_side_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/comp_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/comp_side_glow
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0 1 0 25
    }
}

textures/shop/cube_table
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/cube_table
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/door3
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/door3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/speaker_1
{
	q3map_material	Computer
	damageShader	textures/shop/speaker_1_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/speaker_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/speaker_2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/speaker_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/conveyor_screen
{
	q3map_material	Computer
	damageShader	textures/shop/conveyor_screen_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/conveyor_screen
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/conveyor_1
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/shop/conveyor_1
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scroll 0 0.5
    }
}

textures/shop/conveyor_front
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/conveyor_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/airduct
{
	q3map_material	HollowMetal
	damageShader	textures/finca/airvent1_d 1
    {
        map $lightmap
    }
    {
        map textures/finca/airvent1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_papertowel
{
	q3map_material	HollowMetal
	damageShader	textures/shop/bath_papertowel_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/bath_papertowel
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_sign1
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/bath_sign1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_sign2
{
	q3map_material	Plastic
	aliasShader	textures/shop/bath_sign1
    {
        map $lightmap
    }
    {
        map textures/shop/bath_sign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bubbler_side1
{
	q3map_material	HollowMetal
	aliasShader	textures/shop/bubbler_front2
    {
        map $lightmap
    }
    {
        map textures/shop/bubbler_side1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bubbler_side2
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/bubbler_side2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bubbler_front1
{
	q3map_material	HollowMetal
	aliasShader	textures/shop/bubbler_front2
    {
        map $lightmap
    }
    {
        map textures/shop/bubbler_front1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_trash2
{
	q3map_material	Plastic
	aliasShader	textures/shop/bath_trash1
    {
        map $lightmap
    }
    {
        map textures/shop/bath_trash2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_trash3
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/bath_trash3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_trash1
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/bath_trash1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bubbler_front2
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/bubbler_front2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bubbler_top
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/bubbler_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_door
{
	q3map_material	SolidWood
	aliasShader	textures/shop/door3
    {
        map $lightmap
    }
    {
        map textures/shop/bath_door
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_trash4
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/bath_trash4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_tiletrim
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/shop/bath_tiletrim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_tile
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/shop/bath_tile
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_tile2
{
	q3map_material	Tiles
	aliasShader	textures/shop/bath_tile
    {
        map $lightmap
    }
    {
        map textures/shop/bath_tile2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_stall1
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/bath_stall1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/floor1_red
{
	q3map_material	Tiles
	aliasShader	textures/shop/floor2
    {
        map $lightmap
    }
    {
        map textures/shop/floor1_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/floor1_tan
{
	q3map_material	Tiles
	aliasShader	textures/shop/floor2
    {
        map $lightmap
    }
    {
        map textures/shop/floor1_tan
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/wall2b_red
{
	q3map_material	Plaster
	aliasShader	textures/shop/wall2b_tan
    {
        map $lightmap
    }
    {
        map textures/shop/wall2b_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/wall2b_tan
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/shop/wall2b_tan
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_walltrim2
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/shop/syc_walltrim2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_carpet2
{
	q3map_material	Carpet
	aliasShader	textures/shop/syc_carpet1
    {
        map $lightmap
    }
    {
        map textures/shop/syc_carpet2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_ceiling
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/shop/syc_ceiling
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_pillar
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/shop/syc_pillar
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_pillar2
{
	q3map_material	Marble
	aliasShader	textures/shop/syc_ceiling
    {
        map $lightmap
    }
    {
        map textures/shop/syc_pillar2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_trimlow
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/shop/syc_trimlow
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_wall
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/shop/syc_wall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_wallbottom
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/shop/syc_wallbottom
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_wallcarving
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/shop/syc_wallcarving
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_walltrim
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/shop/syc_walltrim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_carpet1
{
	q3map_material	Carpet
    {
        map $lightmap
    }
    {
        map textures/shop/syc_carpet1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/syc_carpet3
{
	q3map_material	Carpet
	aliasShader	textures/shop/syc_carpet1
    {
        map $lightmap
    }
    {
        map textures/shop/syc_carpet3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/glass_1_nonbreakable
{
	qer_editorimage	textures/shop/glass_test
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	BPGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/shop/env_shop
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

textures/shop/ent_mirror
{
	qer_editorimage	textures/hospital/metal_sm
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/hospital/metal_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/shop1ent_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.5
        tcGen environment
        tcMod scale 2 2
    }
}

textures/shop/met_env_dark
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/met_env_dark
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        alphaGen const 0.3
        tcGen environment
    }
}

textures/shop/glass_sign_test
{
	q3map_material	ShatterGlass
	q3map_nolightmap
	cull	disable
    {
        map textures/shop/glass_sign_test
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/floor_arrow
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/floor_arrow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/door2_trim
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/door2_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/card_scanner
{
	q3map_material	Computer
	q3map_nolightmap
	q3map_onlyvertexlighting
	damageShader	textures/shop/card_scanner_d 1
    {
        map textures/colors/black_100
    }
    {
        map textures/shop/card_scanner2
        blendFunc GL_ONE GL_ONE
        tcMod scroll 0 -0.3
        tcMod scale 0.5 1
    }
    {
        map textures/shop/card_scanner2
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0.5 0.3 1 1000
        tcMod scroll 0 3
        tcMod scale 10 10
    }
    {
        map textures/shop/card_scanner
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/corkboard_shop2
{
	q3map_material	HollowWood
	aliasShader	textures/shop/corkboard_shop
    {
        map $lightmap
    }
    {
        map textures/shop/corkboard_shop2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/corkboard_shop
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/shop/corkboard_shop
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/clock
{
	q3map_material	Computer
	damageShader	textures/shop/clock_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/clock
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap textures/shop/clock2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        tcMod rotate 7
    }
}

textures/shop/clock2
{
    {
        map textures/shop/clock2
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
        map textures/shop/clock2
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/shop/card_scanner_side
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/card_scanner_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/clock2b
{
}

textures/shop/bath_stall2
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/bath_stall2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_stall_trim
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/bath_stall_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_slidedoor
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_slidedoor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_barrier1
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrier1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrier2
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrier2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrier3
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrier3
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrier4
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrier4
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_pad
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_pad
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_padtrim
{
	q3map_material	Rubber
	aliasShader	textures/shop/metal_pad
    {
        map $lightmap
    }
    {
        map textures/shop/metal_padtrim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_barrier
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_barrier
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/door3b
{
	q3map_material	HollowMetal
	aliasShader	textures/shop/door3
    {
        map $lightmap
    }
    {
        map textures/shop/door3b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/cinderblock2
{
	q3map_material	Concrete
	aliasShader	textures/shop/cinderblock1
    {
        map $lightmap
    }
    {
        map textures/shop/cinderblock2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/cinderblock1
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/shop/cinderblock1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_gray_scratches
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_gray_scratches
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/target_buttoncolor
{
	qer_editorimage	textures/shop/target_button
	q3map_material	Computer
	damageShader	textures/shop/target_button_d 1
    {
        map $lightmap
        tcMod scroll 5 5
        tcMod stretch noise 0 6 0 34
    }
    {
        map textures/shop/target_buttoncolor
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scroll 5 5
    }
    {
        map textures/shop/target_buttongreen
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/trashcan_gray
{
	surfaceparm	nomarks
	q3map_material	HollowMetal
	cull	disable
    {
        map textures/shop/trashcan_gray
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
        map textures/shop/trashcan_gray
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/shop/target_buttoncolor2
{
	qer_editorimage	textures/shop/target_button
	q3map_material	Computer
	damageShader	textures/shop/target_button_d 1
    {
        map $lightmap
        tcMod scroll 5 5
        tcMod stretch noise 0 6 0 34
    }
    {
        map textures/shop/target_buttoncolor
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scroll 5 5
    }
    {
        map textures/shop/target_buttonred
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
    {
        map textures/shop/target_buttonred
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/target3
{
	q3map_material	Canvas
	aliasShader	textures/shop/target1
    {
        map $lightmap
    }
    {
        map textures/shop/target3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/target1
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/shop/target1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/target2
{
	q3map_material	Canvas
	aliasShader	textures/shop/target1
    {
        map $lightmap
    }
    {
        map textures/shop/target2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/gun_mat
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/shop/gun_mat
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign_danger
{
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/sign_danger
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_barrier5
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrier5
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barriera
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barriera
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrierb
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrierb
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrierc
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrierc
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrierd
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrierd
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barriere
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barriere
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrierf
{
	polygonOffset
	q3map_nolightmap
    {
        map textures/shop/metal_barrierf
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/metal_barrier_b
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_barrier_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_track
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_track
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_barrier_edge
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_barrier_edge
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/metal_slidedoor_edge
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/metal_slidedoor_edge
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/floor3
{
	q3map_material	Tiles
	aliasShader	textures/shop/floor2
    {
        map $lightmap
    }
    {
        map textures/shop/floor3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_warning1
{
	polygonOffset
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
	aliasShader	textures/shop/tut_warning2
    {
        map textures/shop/tut_warning1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

textures/shop/tut_course2
{
	polygonOffset
	q3map_material	Plastic
	q3map_nolightmap
    {
        map textures/shop/tut_course2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/tut_course1
{
	polygonOffset
	q3map_material	Plastic
	q3map_nolightmap
    {
        map textures/shop/tut_course1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/tut_grenade_scorch
{
	qer_editorimage	gfx/misc/jk_scorch
	polygonOffset
	q3map_nolightmap
    {
        map gfx/misc/jk_scorch
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/tut_target
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/shop/tut_target
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_shotupblockb
{
	q3map_material	Concrete
	aliasShader	textures/shop/cinderblock1
    {
        map $lightmap
    }
    {
        map textures/shop/tut_shotupblockb
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_shotupblock
{
	q3map_material	Concrete
	aliasShader	textures/shop/cinderblock1
    {
        map $lightmap
    }
    {
        map textures/shop/tut_shotupblock
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_shotupblock_transb
{
	q3map_material	Concrete
	aliasShader	textures/shop/cinderblock1
    {
        map $lightmap
    }
    {
        map textures/shop/tut_shotupblock_transb
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_shotupblock_trans
{
	q3map_material	Concrete
	aliasShader	textures/shop/cinderblock1
    {
        map $lightmap
    }
    {
        map textures/shop/tut_shotupblock_trans
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_course3
{
	polygonOffset
	q3map_material	Plastic
	q3map_nolightmap
    {
        map textures/shop/tut_course3
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/tut_warning2
{
	polygonOffset
	q3map_material	Plastic
	q3map_nolightmap
    {
        map textures/shop/tut_warning2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/tut_diag_m4
{
	q3map_material	HollowWood
	aliasShader	textures/shop/tut_diag_grenade
    {
        map $lightmap
    }
    {
        map textures/shop/tut_diag_m4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_diag_bayonet
{
	q3map_material	HollowWood
	aliasShader	textures/shop/tut_diag_grenade
    {
        map $lightmap
    }
    {
        map textures/shop/tut_diag_bayonet
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/tut_diag_grenade
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/shop/tut_diag_grenade
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/corkboard_safety
{
	q3map_material	HollowWood
	aliasShader	textures/shop/corkboard_shop
    {
        map $lightmap
    }
    {
        map textures/shop/corkboard_safety
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/painting_world2
{
	q3map_material	Canvas
	aliasShader	textures/shop/painting_world
    {
        map $lightmap
    }
    {
        map textures/shop/painting_world2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/elev_numbers
{
	q3map_material	Computer
	damageShader	textures/shop/elev_numbers_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/elev_numbers
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/painting_jet2
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/shop/painting_jet2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/painting_jets
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/shop/painting_jets
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/painting_world
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/shop/painting_world
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/door1_b
{
	q3map_material	SolidMetal
	aliasShader	textures/shop/door1
    {
        map $lightmap
    }
    {
        map textures/shop/door1_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign_exitroof
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/shop/sign_exitroof
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/screen_display2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/screen_display2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/screen_display1_map
        blendFunc GL_ONE GL_ONE
    }
}

textures/shop/screen_display1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/screen_display1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/screen_display2_world
        blendFunc GL_ONE GL_ONE
    }
}

textures/shop/screen_display2a
{
	qer_editorimage	textures/shop/screen_display2
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/screen_display2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/screen_display2_world
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/shop/screen_display2_lights
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.75
    }
    {
        map textures/shop/screen_display2_loops
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.15
    }
}

textures/shop/screen_display2b
{
	qer_editorimage	textures/shop/screen_display2
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/screen_display2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/screen_display2_globe
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/shop/screen_display2_lines
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 12
    }
    {
        map textures/shop/screen_display2_scroll
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.15
    }
}

textures/shop/screen_display1a
{
	qer_editorimage	textures/shop/screen_display1
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/screen_display1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/screen_display1_map
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/shop/screen_display1_rad1
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.25
    }
    {
        map textures/shop/screen_display1_rad2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.5
    }
    {
        map textures/shop/screen_display1_rad3
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.75
    }
}

textures/shop/screen_desk1
{
	q3map_material	Computer
	damageShader	textures/shop/screen_desk1_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/screen_desk1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/screen_desk1_glow2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0.2 0.1 1 1
        tcMod scroll 0 -0.5
    }
    {
        map textures/shop/screen_desk1_alpha
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
    }
}

textures/shop/tank_blue
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/tank_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/door_heavy
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/door_heavy
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/door_heavytrim
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/door_heavytrim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/door_4
{
	q3map_material	SolidMetal
	aliasShader	textures/shop/door3
    {
        map $lightmap
    }
    {
        map textures/shop/door_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/mag_1_front
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/mag_1_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/mag_1
{
	q3map_material	Plastic
    {
        map textures/shop/mag_1
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
        map textures/shop/mag_1
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/shop/shoplogo_wall
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/shoplogo_wall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign9
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign9
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign10
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign10
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign11
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign11
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign12
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign12
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign13
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign13
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign14
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign14
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign2
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign4
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign5
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign7
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign7
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign8
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign8
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign1
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/window_shop
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/shop/window_shop
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign15
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign15
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign16
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign16
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign17
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign17
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign18
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign18
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign19
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign19
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign20
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign20
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/sign21
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/sign21
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/window_shop_vertex
{
	qer_editorimage	textures/shop/window_shop
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/shop/window_shop
        rgbGen vertex
    }
}

textures/shop/dryerase_board
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/dryerase_board
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/card_scanner_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/card_scanner_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/airduct_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/finca/airvent1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_papertowel_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/bath_papertowel_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/clock_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/shop/clock_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/comp_monitor_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/shop/comp_monitor_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/comp_panel_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/comp_panel_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/comp_side_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/comp_side_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/conveyor_screen_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/shop/conveyor_screen_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/screen_desk1_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/screen_desk1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/elev_numbers_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/elev_numbers_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/speaker_1_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/speaker_1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/target_button_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/target_button_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/elev_numbers_1
{
	qer_editorimage	textures/shop/elev_numbers
	q3map_material	SolidMetal
	damageShader	textures/shop/elev_numbers_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/elev_numbers
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/elev_numbers_glow1
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0 25 0 1
    }
}

textures/shop/elev_numbers_2
{
	qer_editorimage	textures/shop/elev_numbers
	q3map_material	SolidMetal
	damageShader	textures/shop/elev_numbers_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/elev_numbers
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/elev_numbers_glow2
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0 25 0 1
    }
}

textures/shop/elev_numbers_7
{
	qer_editorimage	textures/shop/elev_numbers
	q3map_material	SolidMetal
	damageShader	textures/shop/elev_numbers_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/elev_numbers
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/elev_numbers_glow7
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0 25 0 1
    }
}

textures/shop/elev_numbers_99
{
	qer_editorimage	textures/shop/elev_numbers
	q3map_material	SolidMetal
	damageShader	textures/shop/elev_numbers_d 1
    {
        map $lightmap
    }
    {
        map textures/shop/elev_numbers
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/elev_numbers_glow99
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0 25 0 1
    }
}

textures/shop/wall_panel
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/wall_panel_glow
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scroll -0.2 1
    }
    {
        map textures/shop/wall_panel_glow2
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 1 1 0.5 0.5
        tcMod scale 0.5 0
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
        tcMod scale 0.5 0.5
    }
}

textures/shop/screen_shop2
{
	qer_editorimage	textures/shop/screen_display_globe
	q3map_material	SolidMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/shop/screen_display1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/shop/screen_display_globe
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/conveyor_screen_effect
{
	qer_editorimage	textures/shop/conveyor_screen
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
	damageShader	textures/shop/conveyor_screen_d 1
    {
        map textures/colors/black_100
    }
    {
        map textures/shop/conveyor_screen_glow2
        blendFunc GL_ONE GL_ZERO
        detail
        tcMod scroll 0 -0.35
    }
    {
        map textures/shop/conveyor_screen_glow1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
    }
    {
        map textures/shop/conveyor_screen_glow3
        blendFunc GL_ONE GL_ONE
        detail
        tcMod scroll 0 -0.35
    }
    {
        map textures/shop/conveyor_screen_alpha
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
    }
}

textures/shop/comp_monitor_scan1
{
	qer_editorimage	textures/shop/comp_monitor
	q3map_material	Computer
	damageShader	textures/shop/comp_monitor_d 1
    {
        map $lightmap
    }
    {
        animMap 0.4 textures/shop/comp_monitor_scan1 textures/shop/comp_monitor_scan2 
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/comp_monitor_scan2
{
	qer_editorimage	textures/shop/comp_monitor
	q3map_material	Computer
	damageShader	textures/shop/comp_monitor_d 1
    {
        map $lightmap
    }
    {
        animMap 0.4 textures/shop/comp_monitor_scan3 textures/shop/comp_monitor_scan4 
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/pillar3_env
{
	qer_editorimage	textures/shop/pillar3
	q3map_material	Marble
	aliasShader	textures/shop/pillar1
    {
        map $lightmap
    }
    {
        map textures/shop/pillar3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        detail
        alphaGen const 0.5
        tcGen environment
    }
}

textures/shop/floor1_env
{
	qer_editorimage	textures/shop/floor1
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/shop/floor1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        detail
        alphaGen const 0.5
        tcGen environment
    }
}

textures/shop/bath_sign1b
{
	qer_editorimage	textures/shop/bath_sign1
	polygonOffset
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/bath_sign1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/batch_sign2b
{
	qer_editorimage	textures/shop/bath_sign2
	polygonOffset
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/bath_sign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/bath_sign2b
{
	qer_editorimage	textures/shop/bath_sign2
	polygonOffset
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/shop/bath_sign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/con15_temp
{
	qer_editorimage	textures/armory/con15
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/armory/con15
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/shoplogo_wall_decal
{
	qer_editorimage	textures/shop/shoplogo_wall
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/shop/shoplogo_wall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/screen_display2_globe_alpha
{
	q3map_material	Glass
	q3map_nolightmap
	cull	disable
    {
        map textures/shop/screen_display2_globe_alpha
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/shop/conveyor_1_noscroll
{
	qer_editorimage	textures/shop/conveyor_1
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/shop/conveyor_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/glass_1_rain
{
	qer_editorimage	textures/hospital/metalgrime
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/shop/env_shop
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
    {
        map textures/prague/w_rain
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod scroll 0.04 -2
    }
}

textures/shop/glass_safety_thin_original
{
	qer_editorimage	textures/shop/glass_safety_thin
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	shotclip
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/shop/glass_safety_thin
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.25
        tcGen environment
    }
}

textures/shop/glass_safety_thin
{
	qer_editorimage	textures/shop/glass_safety_thin
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	shotclip
	q3map_material	BPGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/shop/env_shop
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.15
        tcGen environment
    }
    {
        map textures/shop/glass_safety_thin
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen const 0.2
    }
}

textures/shop/wall2b_decal
{
	qer_editorimage	textures/shop/wall2b
	polygonOffset
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/shop/wall2b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

// *********************************

// Test Fog for Cluods on SHOP2 SM

// *********************************

textures/shop/mp_shop_fog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	surfaceparm nodrop
	surfaceparm	trans
	q3map_nolightmap
	fogparms	( 0.658824 0.666667 0.701961 ) 896.0
}

textures/shop/glass_1_nobrk_2side
{
	qer_editorimage	textures/shop/glass_test
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	BPGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/shop/env_shop
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

textures/shop/pillar1_vertex
{
	qer_editorimage	textures/shop/pillar1
	q3map_material	Marble
	q3map_onlyvertexlighting
    {
        map $lightmap
    }
    {
        map textures/shop/pillar1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/wall1_vertex
{
	qer_editorimage	textures/shop/wall1
	q3map_material	Plaster
	q3map_onlyvertexlighting
    {
        map $lightmap
    }
    {
        map textures/shop/wall1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/2_elevcar_roof_temp
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/shop/2_elevcar_roof_temp
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/shop/trashcan_gray_marks
{
	qer_editorimage	textures/shop/trashcan_gray
	q3map_material	HollowMetal
	cull	disable
    {
        map textures/shop/trashcan_gray
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
        map textures/shop/trashcan_gray
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

