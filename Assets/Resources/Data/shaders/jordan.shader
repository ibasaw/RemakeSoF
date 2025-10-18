textures/jordan/barrack_brick
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/jordan/barrack_brick
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/barrack_deckfloor
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/barrack_deckfloor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/barrack_deckstep
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/barrack_deckstep
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/barrack_wall
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/barrack_wall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/barrack_deckunder
{
	q3map_material	HollowWood
	aliasShader	textures/jordan/barrack_deckfloor
    {
        map $lightmap
    }
    {
        map textures/jordan/barrack_deckunder
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/barrack_stepunder
{
	q3map_material	HollowWood
	aliasShader	textures/jordan/barrack_deckstep
    {
        map $lightmap
    }
    {
        map textures/jordan/barrack_stepunder
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comcentr_brick
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/comcentr_brick
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comcentr_brickwndow
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/comcentr_brickwndow
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comcentr_doormetal
{
	q3map_material	HollowMetal
	aliasShader	textures/jordan/door_ext_open
    {
        map $lightmap
    }
    {
        map textures/jordan/comcentr_doormetal
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comcentr_foundation
{
	q3map_material	Concrete
	aliasShader	textures/jordan/brick_topdrip
    {
        map $lightmap
    }
    {
        map textures/jordan/comcentr_foundation
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comcentr_trimside
{
	q3map_material	Rock
    {
        map $lightmap
    }
    {
        map textures/jordan/comcentr_trimside
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comcentr_trimtop
{
	q3map_material	Concrete
	aliasShader	textures/jordan/comcentr_trimside
    {
        map $lightmap
    }
    {
        map textures/jordan/comcentr_trimtop
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/rock_sandstone
{
	q3map_material	Rock
    {
        map $lightmap
    }
    {
        map textures/jordan/rock_sandstone
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/brick_topdrip
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_topdrip
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comcentr_brickupper
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_topdrip
    {
        map $lightmap
    }
    {
        map textures/jordan/comcentr_brickupper
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/floor_bunker
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/jordan/floor_bunker
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wall_stucco02
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/jordan/wall_stucco02
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/garagedoor_botm
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/jordan/garagedoor_botm
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/garagedoor_top
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/garagedoor_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/shelf_end
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/jordan/shelf_end
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/shelf_post
{
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	q3map_alphashadow
    {
        map textures/jordan/shelf_post
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
        map textures/jordan/shelf_post
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/shelf_top
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/jordan/shelf_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/shelf_x
{
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	q3map_alphashadow
    {
        map textures/jordan/shelf_x
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
        map textures/jordan/shelf_x
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/brick_blocks
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_blocks
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/brick_damaged
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_damaged
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/pillar_forarch
{
	q3map_material	Rock
    {
        map $lightmap
    }
    {
        map textures/jordan/pillar_forarch
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/arch_stucco
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/jordan/arch_stucco
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/brick_damaged2
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_damaged2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/floor_woodsml
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/jordan/floor_woodsml
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/door_ext_open
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/jordan/door_ext_open
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/door_int_open
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/door_int_open
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/doors_ext_lock
{
	q3map_material	HollowWood
	aliasShader	textures/jordan/doors2_ext_lock
    {
        map $lightmap
    }
    {
        map textures/jordan/doors_ext_lock
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_medium
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/jordan/window_medium
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/door_jrdn_bunker
{
	q3map_material	HollowWood
	aliasShader	textures/jordan/door_int_open
    {
        map $lightmap
    }
    {
        map textures/jordan/door_jrdn_bunker
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/brick_white
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_white
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/roof_cement
{
	q3map_material	Rock
    {
        map $lightmap
    }
    {
        map textures/jordan/roof_cement
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wall_lobby
{
	q3map_material	Rock
    {
        map $lightmap
    }
    {
        map textures/jordan/wall_lobby
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/arch_lobby
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/jordan/arch_lobby
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/canopy_market_edge
{
	q3map_material	Canvas
    {
        map textures/jordan/canopy_market_edge
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
        map textures/jordan/canopy_market_edge
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/canopy_market_top
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/jordan/canopy_market_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/crate_box
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/jordan/crate_box
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wood_plain
{
	q3map_material	SolidWood
	aliasShader	textures/jordan/wood_plain_beatup
    {
        map $lightmap
    }
    {
        map textures/jordan/wood_plain
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wood_plain_beatup
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/jordan/wood_plain_beatup
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wood_plain_dirty
{
	q3map_material	SolidWood
	aliasShader	textures/jordan/wood_plain_beatup
    {
        map $lightmap
    }
    {
        map textures/jordan/wood_plain_dirty
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/brick_withcement
{
	q3map_material	Concrete
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_withcement
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/railing
{
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	cull	disable
    {
        map textures/jordan/railing
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
        map textures/jordan/railing
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/brick_beige
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_beige
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/brick_brown
{
	q3map_material	Rock
	aliasShader	textures/jordan/brick_blocks
    {
        map $lightmap
    }
    {
        map textures/jordan/brick_brown
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/canopy_green_top
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/jordan/canopy_green_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/ladder_forpole
{
	surfaceparm	trans
	q3map_material	SolidMetal
    {
        map textures/jordan/ladder_forpole
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
        map textures/jordan/ladder_forpole
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/doors2_ext_lock
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/doors2_ext_lock
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/panel_formetalwall
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/jordan/panel_formetalwall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/vent_formetalwall
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/vent_formetalwall
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wall_metal
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/jordan/wall_metal
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/metal_gray
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/jordan/metal_gray
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/canopy_market_edge_2sided
{
	qer_editorimage	textures/jordan/canopy_market_edge
	q3map_material	Canvas
	cull	disable
    {
        map textures/jordan/canopy_market_edge
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
        map textures/jordan/canopy_market_edge
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/canopy_market_top_2sided
{
	qer_editorimage	textures/jordan/canopy_market_top
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/jordan/canopy_market_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comptr_big_side
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/jordan/comptr_big_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/comptr_sides
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/jordan/comptr_sides
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wood_comcntr
{
	q3map_material	SolidWood
	aliasShader	textures/jordan/wood_plain_beatup
    {
        map $lightmap
    }
    {
        map textures/jordan/wood_comcntr
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/wall_int_wndw
{
	q3map_material	Concrete
}

textures/jordan/door_int_comlock
{
	q3map_material	HollowWood
	aliasShader	textures/jordan/door_int_open
    {
        map $lightmap
    }
    {
        map textures/jordan/door_int_comlock
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/support
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	HollowMetal
    {
        map textures/jordan/support
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
        map textures/jordan/support
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/window_4
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/window_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_6
{
	q3map_material	Glass
	damageShader	textures/jordan/window_6_d 1
    {
        map $lightmap
    }
    {
        map textures/jordan/window_6
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_7
{
	q3map_material	Concrete
	damageShader	textures/jordan/window_7_d 1
	aliasShader	textures/jordan/window_6
    {
        map $lightmap
    }
    {
        map textures/jordan/window_7
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_5
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/jordan/window_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_6_decal
{
	qer_editorimage	textures/jordan/window_6
	polygonOffset
	q3map_material	Glass
	damageShader	textures/jordan/window_6_d 1
    {
        map $lightmap
    }
    {
        map textures/jordan/window_6
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_4_decal
{
	qer_editorimage	textures/jordan/window_4
	polygonOffset
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/jordan/window_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_7_decal
{
	qer_editorimage	textures/jordan/window_7
	polygonOffset
	q3map_material	Glass
	damageShader	textures/jordan/window_7_d 1
	aliasShader	textures/jordan/window_6_decal
    {
        map $lightmap
    }
    {
        map textures/jordan/window_7
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/metal_gray_dsided
{
	qer_editorimage	textures/jordan/metal_gray
	q3map_material	SolidMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/jordan/metal_gray
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/support_twosided
{
	qer_editorimage	textures/jordan/support
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	HollowMetal
	cull	disable
    {
        map textures/jordan/support
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
        map textures/jordan/support
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/jordan/window_7_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/jordan/window_7_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/window_6_d
{
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/jordan/window_6_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/jordan/railing_test
{
	qer_editorimage	textures/jordan/railing
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	cull	disable
    {
        map textures/jordan/railing
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
        map textures/jordan/railing
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

