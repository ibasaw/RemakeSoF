textures/airport/airsky
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/colors/yellow_light
	q3map_surfacelight	30
	sun 1 0.992157 0.65098 130 46 40
	surfaceparm	sky
	surfaceparm	noimpact
	q3map_nolightmap
	skyParms	- 512 -
}

textures/airport/airsky_nodraw
{
	qer_editorimage	textures/colors/yellow_light
	surfaceparm	sky
	surfaceparm	noimpact
	q3map_nolightmap
	skyParms	- 512 -
}

textures/airport/lightfog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.5 0.5 0.53 ) 14000.0
}

textures/airport/dirtfog
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.9 0.85 0.8 ) 30000.0
}

textures/airport/croomglass
{
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/airport/croomglass
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.15
        tcGen environment
        tcMod scale 0.5 0.5
    }
    {
        map textures/hospital/metalgrime
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        rgbGen vertex
        alphaGen const 0.3
    }
}

textures/airport/escaltor_step
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/escaltor_step
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_floor
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/airport/tile_floor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_floor_tile3
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/airport/tile_floor_tile3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_floor2
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/airport/tile_floor2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/grating
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/airport/grating
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
        map textures/airport/grating
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/poster
{
	surfaceparm	nonsolid
	surfaceparm	trans
	q3map_material	Canvas
	q3map_nolightmap
	cull	disable
    {
        map textures/airport/poster
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/airport/poster2
{
	surfaceparm	nonsolid
	surfaceparm	trans
	q3map_material	Canvas
	q3map_nolightmap
	cull	disable
	aliasShader	textures/airport/poster
    {
        map textures/airport/poster2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/airport/largewindow
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/airport/largewindow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/glass_green
{
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/airport/glass_green
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen const ( 0.330000 0.330000 0.330000 )
    }
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.15
        tcGen environment
    }
}

textures/airport/glasspartition
{
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/airport/glasspartition
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/airport/airportsign1
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/airportsign1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/airportsign2
{
	q3map_material	HollowMetal
	aliasShader	textures/airport/airportsign1
    {
        map $lightmap
    }
    {
        map textures/airport/airportsign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/carpet1
{
	q3map_material	Carpet
    {
        map $lightmap
    }
    {
        map textures/airport/carpet1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/carpet2
{
	q3map_material	Carpet
    {
        map $lightmap
    }
    {
        map textures/airport/carpet2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/floor_rubber
{
	q3map_material	Rubber
	aliasShader	textures/airport/floor_rubber_trim
    {
        map $lightmap
    }
    {
        map textures/airport/floor_rubber
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/floor_rubber_trim
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/airport/floor_rubber_trim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/metal_blue
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/metal_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/metal_red
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/metal_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/metal_white
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/metal_white
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_0
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        clampmap textures/airport/sign_0
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_1
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_1
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_2
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_2
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_3
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_3
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_4
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_4
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_5
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_5
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_6
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_6
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_7
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_7
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_8
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_8
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_9
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_0
    {
        clampmap textures/airport/sign_9
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/sign_gate
{
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        clampmap textures/airport/sign_gate
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/carpet_runner
{
	q3map_material	Carpet
    {
        map $lightmap
    }
    {
        map textures/airport/carpet_runner
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/door_air1
{
	q3map_material	HollowMetal
	aliasShader	textures/airport/door_air2
    {
        map $lightmap
    }
    {
        map textures/airport/door_air1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/door_air2
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/door_air2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_border
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/airport/tile_border
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_marble
{
	q3map_material	Marble
	aliasShader	textures/airport/tile_marble_2
    {
        map $lightmap
    }
    {
        map textures/airport/tile_marble
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/wall_blue2
{
	q3map_material	Plaster
	aliasShader	textures/airport/wall_blue3
    {
        map $lightmap
    }
    {
        map textures/airport/wall_blue2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/wall_brown1
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/airport/wall_brown1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/wall_brown2
{
	q3map_material	Plaster
	aliasShader	textures/airport/wall_brown1
    {
        map $lightmap
    }
    {
        map textures/airport/wall_brown2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/wall_plaster
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/airport/wall_plaster
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/gate_metal
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/gate_metal
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.08
        tcGen environment
    }
}

textures/airport/trashcan
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	HollowMetal
	cull	disable
    {
        map textures/airport/trashcan
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
        map textures/airport/trashcan
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/netting_nocull
{
	qer_editorimage	textures/airport/netting
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	Canvas
	cull	disable
    {
        map textures/airport/netting
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
        map textures/airport/netting
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/woodgrain
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/airport/woodgrain
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/door_cargo1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/door_cargo1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/handrail
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/handrail
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/metal_red_scratches
{
	q3map_material	SolidMetal
	cull	disable
	aliasShader	textures/airport/metal_red
    {
        map $lightmap
    }
    {
        map textures/airport/metal_red_scratches
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/poster1
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/poster1
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
}

textures/airport/poster2_3
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/poster2_3
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
}

textures/airport/poster3
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/poster3
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
}

textures/airport/poster4
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/poster4
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
}

textures/airport/tramcar
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	SolidMetal
	aliasShader	textures/airport/tramcar_ext1
    {
        map textures/airport/tramcar
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
        map textures/airport/tramcar
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/tramcar_ext1
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	SolidMetal
    {
        map textures/airport/tramcar_ext1
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
        map textures/airport/tramcar_ext1
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/tramcar_ext2
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	SolidMetal
    {
        map textures/airport/tramcar_ext2
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
        map textures/airport/tramcar_ext2
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/wall_blue3
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/airport/wall_blue3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/wall_brown3
{
	q3map_material	Plaster
	aliasShader	textures/airport/wall_brown1
    {
        map $lightmap
    }
    {
        map textures/airport/wall_brown3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/wall_tile1b
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/airport/wall_tile1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/monitor
{
	q3map_material	Computer
	damageShader	textures/airport/monitor_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/monitor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/monitor_alt
{
	qer_editorimage	textures/airport/monitor
	q3map_material	Computer
	damageShader	textures/airport/monitor_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/monitor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/woodgrain2
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/airport/woodgrain2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/drain
{
	q3map_material	SolidMetal
	aliasShader	textures/airport/tile_marble_2
    {
        map $lightmap
    }
    {
        map textures/airport/drain
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/escalator_plate
{
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/escalator_plate
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_marble_2
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/airport/tile_marble_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/banner
{
	surfaceparm	nonsolid
	surfaceparm	trans
	q3map_material	Canvas
	cull	disable
    {
        map textures/airport/banner
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
        map textures/airport/banner
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/artposter
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/airport/artposter
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/mensroom
{
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/mensroom
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_marble_border
{
	q3map_material	Marble
	aliasShader	textures/airport/tile_marble_2
    {
        map $lightmap
    }
    {
        map textures/airport/tile_marble_border
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_marble_sm
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/airport/tile_marble_sm
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/womensroom
{
	polygonOffset
	q3map_material	SolidMetal
	aliasShader	textures/airport/mensroom
    {
        map $lightmap
    }
    {
        map textures/airport/womensroom
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/door_bathroom
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/door_bathroom
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        detail
        alphaGen const 0.2
        tcGen environment
    }
}

textures/airport/sidewalk_moving
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/airport/sidewalk_moving
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sidewalk_moving_scroll
{
	qer_editorimage	textures/tools/editor_images/qer_sidewalk_moving
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/airport/sidewalk_moving
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scroll -0.4 0
    }
}

textures/airport/slipplate2
{
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/slipplate2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/roof_tubing
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	trans
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/airport/roof_tubing
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
        map textures/airport/roof_tubing
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/shop_sign1
{
	q3map_material	Computer
	damageShader	textures/airport/shop_sign1_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_subway_2
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/airport/tile_subway_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_subway-1
{
	q3map_material	Marble
    {
        map $lightmap
    }
    {
        map textures/airport/tile_subway-1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/shop_sign2
{
	q3map_material	Computer
	damageShader	textures/airport/shop_sign2_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/divider
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/divider
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/poster_flower
{
	q3map_material	Canvas
	q3map_nolightmap
    {
        map textures/airport/poster_flower
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
}

textures/airport/shop_sign3
{
	q3map_material	Computer
	damageShader	textures/airport/shop_sign3_d 1
	aliasShader	textures/airport/shop_sign1
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/ceiling_galv
{
	qer_editorimage	textures/airport/ceiling_galv
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/ceiling_galv
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/flaps
{
	q3map_material	Rubber
    {
        map $lightmap
    }
    {
        map textures/airport/flaps
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/shop_sign4
{
	q3map_material	Computer
	damageShader	textures/airport/shop_sign4_d 1
	aliasShader	textures/airport/shop_sign1
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_10
{
	q3map_material	Computer
	q3map_nolightmap
	damageShader	textures/airport/sign_10_d 1
	aliasShader	textures/airport/shop_sign1
    {
        map textures/airport/sign_10
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
}

textures/airport/sign_11
{
	q3map_material	Computer
	q3map_nolightmap
	damageShader	textures/airport/sign_11_d 1
	aliasShader	textures/airport/shop_sign1
    {
        map textures/airport/sign_11
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/sign_12
{
	q3map_material	Computer
	q3map_nolightmap
	damageShader	textures/airport/sign_12_d 1
	aliasShader	textures/airport/shop_sign1
    {
        map textures/airport/sign_12
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/sign_glow5
{
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_glow
    {
        map textures/airport/sign_glow5
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/sign_glow4
{
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_glow
    {
        map textures/airport/sign_glow4
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/sign_glow3
{
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_glow
    {
        map textures/airport/sign_glow3
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/sign_glow2
{
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/sign_glow
    {
        map textures/airport/sign_glow2
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/sign_glow
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/sign_glow
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/scale
{
	q3map_material	Glass
	damageShader	textures/airport/scale_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/scale
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/map_airport
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/airport/map_airport
        rgbGen exactVertex
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/sign_bagcalim
{
	q3map_material	Computer
	q3map_nolightmap
	q3map_onlyvertexlighting
	damageShader	textures/airport/sign_bagcalim_d 1
    {
        map textures/airport/sign_bagcalim
        rgbGen exactVertex
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/fusebox_2_decal
{
	qer_editorimage	textures/airport/fusebox_2
	surfaceparm	nomarks
	polygonOffset
	q3map_material	HollowMetal
	damageShader	textures/airport/fusebox_2_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/fusebox_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lockers
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/lockers
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/divider2_nocull
{
	qer_editorimage	textures/airport/divider2
	q3map_material	SolidMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/airport/divider2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/vent3
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/vent3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_timetable
{
	qer_editorimage	textures/airport/sign_departure_back
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/sign_departure_back
    }
    {
        animMap 0.2 textures/airport/departure_arrival textures/airport/departure_arrival2 
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
}

textures/airport/clock
{
	polygonOffset
	q3map_material	Computer
	q3map_nolightmap
	damageShader	textures/airport/clock_d 1
    {
        map textures/shop/clock
        alphaFunc GE128
        blendFunc GL_ONE GL_ZERO
        depthWrite
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/cloth_seats
{
	q3map_material	Carpet
    {
        map $lightmap
    }
    {
        map textures/airport/cloth_seats
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/cloth_seats_nocull
{
	qer_editorimage	textures/airport/cloth_seats
	q3map_material	Carpet
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/airport/cloth_seats
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/metalwall_plane
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/metalwall_plane
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/adverts
{
	surfaceparm	nonsolid
	q3map_material	SolidMetal
    {
        map textures/airport/adverts
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
        map textures/airport/adverts
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/cashreg
{
	q3map_material	Computer
	damageShader	textures/airport/cashreg_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/cashreg
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/cashreg_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/airport/cashreg_back
{
	q3map_material	Plastic
	damageShader	textures/airport/cashreg_back_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/cashreg_back
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/cashreg_side
{
	q3map_material	Plastic
	damageShader	textures/airport/cashreg_side_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/cashreg_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_door_bath
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_door_bath
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_metalwall_2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_metalwall_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_pipes_1
{
	entityMergable
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_pipes_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_cargo1
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/airport/plane_cargo1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_cargo2
{
	q3map_material	Canvas
	aliasShader	textures/airport/plane_cargo1
    {
        map $lightmap
    }
    {
        map textures/airport/plane_cargo2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_girder
{
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	q3map_alphashadow
    {
        map textures/airport/plane_girder
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
        map textures/airport/plane_girder
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/plane_grating
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	HollowMetal
    {
        map textures/airport/plane_grating
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
        map textures/airport/plane_grating
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/plane_grating_nocull
{
	qer_editorimage	textures/airport/plane_grating
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	HollowMetal
	cull	disable
    {
        map textures/airport/plane_grating
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
        map textures/airport/plane_grating
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/plane_wall2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_wall2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_corwall
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_corwall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_girder2
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
    {
        map textures/airport/plane_girder2
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
        map textures/airport/plane_girder2
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/plane_mesh
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
    {
        map textures/airport/plane_mesh
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
        map textures/airport/plane_mesh
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/plane_nowinwall
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_nowinwall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_switchboard
{
	q3map_material	Computer
	damageShader	textures/airport/plane_switchboard_d 1
	aliasShader	textures/airport/plane_computer
    {
        map $lightmap
    }
    {
        map textures/airport/plane_switchboard
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_windowwall
{
	surfaceparm	shotclip
	q3map_material	Plastic
	aliasShader	textures/airport/plane_windowwall2
    {
        map textures/airport/plane_windowwall
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
        map textures/airport/plane_windowwall
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/plane_windowwall2
{
	surfaceparm	shotclip
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/airport/plane_windowwall2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_portdoor
{
	q3map_material	HollowMetal
    {
        map textures/airport/plane_portdoor
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
        map textures/airport/plane_portdoor
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/plane_cargofloor
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_cargofloor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_computer
{
	q3map_material	Computer
	damageShader	textures/airport/plane_computer_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/plane_computer
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/plane_computer_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 1.5 1 0 0.5
    }
    {
        map textures/airport/plane_computer_glow2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.3
    }
}

textures/airport/plane_controlbox
{
	q3map_material	Computer
	damageShader	textures/airport/plane_controlbox_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/plane_controlbox
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_controlbox2
{
	q3map_material	Computer
	damageShader	textures/airport/plane_controlbox2_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/plane_controlbox2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/plane_controlbox2_glow
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0 0.3 0 0.3
    }
}

textures/airport/plane_pipewall
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_pipewall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/vertsign1
{
	q3map_material	SolidMetal
	q3map_nolightmap
	aliasShader	textures/airport/shop_sign1
    {
        map textures/airport/vertsign1
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/vertsign2
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/vertsign2
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/vertsign3
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/vertsign3
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/vertsign4
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/vertsign4
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/vertsign5
{
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        map textures/airport/vertsign5
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

textures/airport/divider3
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/divider3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_controlbox3
{
	q3map_material	Computer
	damageShader	textures/airport/plane_controlbox3_d 1
	aliasShader	textures/airport/plane_controlbox
    {
        map $lightmap
    }
    {
        map textures/airport/plane_controlbox3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_controls
{
	q3map_material	Computer
	damageShader	textures/airport/plane_controls_d 1
    {
        map $lightmap
    }
    {
        animMap 1 textures/airport/plane_controls textures/airport/plane_controls_overlay 
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/plane_controls_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 3 1 0 0.5
    }
    {
        map textures/airport/plane_controls_glow2
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 0.5 0 0.9
    }
}

textures/airport/rollers
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/rollers
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/box_yellow
{
	q3map_material	SolidMetal
	damageShader	textures/airport/box_yellow_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/box_yellow
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/door_plane
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/door_plane
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/door_rubber
{
	q3map_material	Rubber
	aliasShader	textures/airport/door_air2
    {
        map $lightmap
    }
    {
        map textures/airport/door_rubber
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/floor_cement
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/airport/floor_cement
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/generator
{
	q3map_material	SolidMetal
	damageShader	textures/airport/generator_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/generator
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/generator_b
{
	q3map_material	SolidMetal
	damageShader	textures/airport/generator_b_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/generator_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/generator_c
{
	q3map_material	SolidMetal
	damageShader	textures/airport/generator_c_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/generator_c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/girder2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/girder2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug1
{
	q3map_material	Fabric
	aliasShader	textures/airport/lug2
    {
        map $lightmap
    }
    {
        map textures/airport/lug1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug1_b
{
	q3map_material	Fabric
	aliasShader	textures/airport/lug2_b
    {
        map $lightmap
    }
    {
        map textures/airport/lug1_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug1_c
{
	q3map_material	Fabric
	aliasShader	textures/airport/lug2_c
    {
        map $lightmap
    }
    {
        map textures/airport/lug1_c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug2
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/airport/lug2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug2_b
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/airport/lug2_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug2_c
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/airport/lug2_c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug3
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/airport/lug3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug3_b
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/airport/lug3_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/lug3_c
{
	q3map_material	Fabric
    {
        map $lightmap
    }
    {
        map textures/airport/lug3_c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/metalcloset
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/metalcloset
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/metalcloset2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/metalcloset2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/microwave
{
	q3map_material	Computer
	damageShader	textures/airport/microwave_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/microwave
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/microwave_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 0 1 0 0.5
    }
}

textures/airport/popmachine
{
	q3map_material	Computer
    {
        map $lightmap
    }
    {
        map textures/airport/popmachine
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/popmachine_glow1
        blendFunc GL_ONE GL_ONE
        detail
    }
    {
        map textures/airport/popmachine_glow2
        blendFunc GL_ONE GL_ONE
        detail
    }
}

textures/airport/sign_bag1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/sign_bag1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_bag2
{
	q3map_material	SolidMetal
	aliasShader	textures/airport/sign_bag1
    {
        map $lightmap
    }
    {
        map textures/airport/sign_bag2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_bag3
{
	q3map_material	SolidMetal
	aliasShader	textures/airport/sign_bag1
    {
        map $lightmap
    }
    {
        map textures/airport/sign_bag3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/trim_glowing
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/trim_glowing
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/propane
{
	q3map_material	SolidMetal
    {
        map textures/airport/propane
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
        map textures/airport/propane
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/propane_lid
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/propane_lid
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/propane_top
{
	q3map_material	SolidMetal
	cull	disable
    {
        map textures/airport/propane_top
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
        map textures/airport/propane_top
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/bag_control
{
	q3map_material	Computer
	damageShader	textures/airport/bag_control_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/bag_control
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/bag_control_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen wave sin 1 1 0 0.5
    }
}

textures/airport/hang_doora
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/hang_doora
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/hang_doorb
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/hang_doorb
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/hang_doorc
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/hang_doorc
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/hang_doord
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/hang_doord
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/hang_doore
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/hang_doore
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/hang_doorf
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/hang_doorf
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/toolbox
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/toolbox
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/planeside
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/planeside
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_work
{
	q3map_material	SolidMetal
	aliasShader	textures/airport/shop_sign1
    {
        map $lightmap
    }
    {
        map textures/airport/sign_work
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/toolbox_b
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/toolbox_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/toolbox_c
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/toolbox_c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/wall_brown1_twosided
{
	qer_editorimage	textures/airport/wall_brown1
	q3map_material	Plaster
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/airport/wall_brown1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/planeside_red
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/planeside_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_engfins
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	q3map_novertexshadows
	cull	disable
    {
        clampmap textures/airport/plane_engfins
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod rotate 20
    }
}

textures/airport/plane_engfinsb
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	q3map_novertexshadows
	cull	disable
    {
        clampmap textures/airport/plane_engfinsb
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod rotate 7200
    }
}

textures/airport/plane_engine
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_engine
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/planeside_big
{
	q3map_material	HollowMetal
	aliasShader	textures/airport/planeside_bigend
    {
        map $lightmap
    }
    {
        map textures/airport/planeside_big
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/planeside_bigtrans
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/planeside_bigtrans
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/planeside_bigend
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/planeside_bigend
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_cockpitwin
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_cockpitwin
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/planeside_decal
{
	surfaceparm	nomarks
	surfaceparm	nonopaque
	polygonOffset
	q3map_material	SolidMetal
    {
        map textures/airport/planeside_decal
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
        map textures/airport/planeside_decal
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/tramcar_sign2
{
	q3map_material	SolidWood
	aliasShader	textures/airport/map_airport
    {
        map $lightmap
    }
    {
        map textures/airport/tramcar_sign2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_redblue
{
	q3map_material	Computer
	damageShader	textures/airport/sign_redblue_d 1
	aliasShader	textures/airport/shop_sign1
    {
        map $lightmap
    }
    {
        map textures/airport/sign_redblue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_scroll
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/airport/sign_scroll_scroll
        rgbGen vertex
        tcMod scroll 0.25 0
    }
    {
        map textures/airport/sign_scroll
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/airport/tramcar_sign1
{
	q3map_material	SolidWood
	aliasShader	textures/airport/map_airport
    {
        map $lightmap
    }
    {
        map textures/airport/tramcar_sign1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/paper_decal
{
	polygonOffset
	q3map_material	Fabric
    {
        map textures/airport/paper_decal
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
        map textures/airport/paper_decal
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/seat_t
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/airport/seat_t
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/seat_s
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	Plastic
	cull	disable
    {
        map textures/airport/seat_s
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
        map textures/airport/seat_s
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/seat_f
{
	surfaceparm	nonsolid
	surfaceparm	playerclip
	q3map_material	Plastic
	cull	disable
    {
        map textures/airport/seat_f
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
        map textures/airport/seat_f
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/table
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/airport/table
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tabletrim
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/tabletrim
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/striplight
{
	q3map_material	Glass
	q3map_nolightmap
    {
        map textures/airport/striplight
        rgbGen const ( 0.700000 0.700000 0.700000 )
    }
}

textures/airport/vapor
{
	surfaceparm	nonsolid
	q3map_nolightmap
    {
// tcMod scale 0.1 1

// tcMod stretch sin 1 0.5 1 10

        map textures/airport/vapor
        blendFunc GL_ONE GL_ONE
    }
}

textures/airport/departure_arrival
{
	q3map_material	Glass
	damageShader	textures/airport/departure_arrival_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/departure_arrival
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/departure_arrival
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/scanline
        blendFunc GL_ONE GL_ONE
        tcMod scroll 0 -0.2
    }
}

textures/airport/departure_arrival2
{
	q3map_material	Glass
	damageShader	textures/airport/departure_arrival_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/departure_arrival2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/airport/departure_arrival2
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/scanline
        blendFunc GL_ONE GL_ONE
        tcMod scroll 0 -0.2
    }
}

textures/airport/grate
{
	q3map_material	HollowMetal
	damageShader	textures/airport/grate_d 1
    {
        map $lightmap
    }
    {
        map textures/airport/grate
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/bag_control_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/bag_control_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/box_yellow_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/box_yellow_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/cashreg_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/cashreg_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/cashreg_back_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/cashreg_back_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/cashreg_side_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/cashreg_side_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/clock_d
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/shop/clock_d
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/airport/departure_arrival_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/airport/departure_arrival_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/fusebox_2_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/fusebox_2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/generator_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/generator_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/generator_b_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/generator_b_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/generator_c_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/generator_c_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/grate_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/grate_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/microwave_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/airport/microwave_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/monitor_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/monitor_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_computer_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_computer_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_controlbox_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_controlbox_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_controlbox2_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_controlbox2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_controlbox3_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_controlbox3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_controls_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_controls_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/plane_switchboard_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/plane_switchboard_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/popmachine_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/popmachine_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/scale_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/airport/scale_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/shop_sign1_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/shop_sign2_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/shop_sign3_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/shop_sign4_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/shop_sign4_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_10_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/sign_10_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_11_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/sign_11_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_12_d
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/airport/sign_12_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_bagcalim_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/sign_bagcalim_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/sign_redblue_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/airport/sign_redblue_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_subway-1_dirty2
{
	q3map_material	Tiles
	aliasShader	textures/airport/tile_marble_sm
    {
        map $lightmap
    }
    {
        map textures/airport/tile_subway-1_dirty2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_floor_dirty2
{
	q3map_material	Tiles
	aliasShader	textures/airport/tile_floor
    {
        map $lightmap
    }
    {
        map textures/airport/tile_floor_dirty2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_subway-1_dirty1
{
	q3map_material	Tiles
	aliasShader	textures/airport/tile_marble_sm
    {
        map $lightmap
    }
    {
        map textures/airport/tile_subway-1_dirty1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/tile_floor_dirty1
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/liner/tile_floor_dirty1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/airport/airlogo_decal_orig
{
	qer_editorimage	textures/airport/airlogo_decal
	surfaceparm	nomarks
	polygonOffset
	q3map_material	SolidMetal
	q3map_nolightmap
    {
        clampmap textures/airport/airlogo_decal
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

textures/airport/airlogo_decal
{
	qer_editorimage	textures/airport/airlogo_decal
	surfaceparm	nomarks
	surfaceparm	nonopaque
	polygonOffset
	q3map_material	SolidMetal
    {
        map textures/airport/airlogo_decal
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
        map textures/airport/airlogo_decal
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/airport/glass_green_nobreak
{
	qer_editorimage	textures/airport/glass_green
	qer_trans	0.5
	surfaceparm	nonopaque
	q3map_material	BPGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/airport/glass_green
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen const ( 0.330000 0.330000 0.330000 )
    }
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.15
        tcGen environment
    }
}

