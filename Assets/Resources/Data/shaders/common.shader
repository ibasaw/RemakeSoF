gfx/2d/painflare
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map gfx/2d/painflare
        blendFunc GL_ONE GL_ONE
        rgbGen vertex
    }
}

textures/common/asphault4
{
	q3map_material	Concrete
	aliasShader	textures/common/asphault
    {
        map $lightmap
    }
    {
        map textures/common/asphault4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault1
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/asphault1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault1a
{
	q3map_material	Concrete
	aliasShader	textures/common/asphault1
    {
        map $lightmap
    }
    {
        map textures/common/asphault1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault2
{
	q3map_material	Concrete
	aliasShader	textures/common/asphault
    {
        map $lightmap
    }
    {
        map textures/common/asphault2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault3
{
	q3map_material	Concrete
	aliasShader	textures/common/asphault
    {
        map $lightmap
    }
    {
        map textures/common/asphault3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/asphault
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/chainlinkfence_top
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_material	SolidMetal
	q3map_alphashadow
	cull	disable
    {
        map textures/common/chainlinkfence_top
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
        map textures/common/chainlinkfence_top
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/ceilingspkr
{
	q3map_material	Plaster
	aliasShader	textures/common/ceilingtile01
    {
        map $lightmap
    }
    {
        map textures/common/ceilingspkr
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/ceilingtile01
{
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/common/ceilingtile01
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/ceilingvent
{
	q3map_material	HollowMetal
	aliasShader	textures/common/ceilingtile01
    {
        map $lightmap
    }
    {
        map textures/common/ceilingvent
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cement2
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/cement2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cement3
{
	q3map_material	Concrete
	aliasShader	textures/common/cement2
    {
        map $lightmap
    }
    {
        map textures/common/cement3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/ceramic_roof
{
	q3map_material	Tiles
    {
        map $lightmap
    }
    {
        map textures/common/ceramic_roof
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/ceramic_roof2
{
	q3map_material	Tiles
	aliasShader	textures/common/ceramic_roof
    {
        map $lightmap
    }
    {
        map textures/common/ceramic_roof2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/chainlinkfence
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_alphashadow
	cull	disable
    {
        map textures/common/chainlinkfence
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
        map textures/common/chainlinkfence
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/chainlinkfence_nocull
{
	qer_editorimage	textures/common/chainlinkfence
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_alphashadow
	cull	disable
    {
        map textures/common/chainlinkfence
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
        map textures/common/chainlinkfence
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/chainlinkfence_rust
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_alphashadow
	cull	disable
	aliasShader	textures/common/chainlinkfence
    {
        map textures/common/chainlinkfence_rust
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
        map textures/common/chainlinkfence_rust
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/ceilinglite
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/ceilinglite
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/ceilinglite_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/ceilinglite_noglow
{
	qer_editorimage	textures/common/ceilinglite
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/ceilinglite
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file2_sides
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet2c_top
    {
        map $lightmap
    }
    {
        map textures/common/file2_sides
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet
{
	q3map_material	HollowMetal
	damageShader	textures/common/file_cabinet_d 1
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet1
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet1a_side
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet1c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet1a_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet1b_back
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet1c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet1b_back
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet1c_top
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet1c_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet2
{
	q3map_material	HollowMetal
	damageShader	textures/common/file_cabinet2_d 1
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet2a_side
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet2c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet2a_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet2b_back
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet2c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet2b_back
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet2c_top
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet2c_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet3
{
	q3map_material	HollowMetal
	damageShader	textures/common/file_cabinet3_d 1
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet3_back
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet2c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet3_back
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet3_side
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet2c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet3_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet3_top
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet2c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet3_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet4
{
	q3map_material	HollowMetal
	damageShader	textures/common/file_cabinet4_d 1
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet4_back
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet4_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet4_back
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet4_side
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet4_top
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet4_side
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet4_top
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet4_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinetside
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinetside
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_front
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_sides
{
	q3map_material	HollowMetal
	aliasShader	textures/common/file_cabinet1c_top
    {
        map $lightmap
    }
    {
        map textures/common/file_sides
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file2_front
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file2_front
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lightswitch
{
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/common/lightswitch
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/light_ext2
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
	aliasShader	textures/common/light_ext1
    {
        map $lightmap
    }
    {
        map textures/common/light_ext2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_ext_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_flor
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_flor
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_flor_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light1
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_1
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_2
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_2_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_3
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_3_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_4
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_4_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_blue
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_blue_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_offw
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_offw
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_offw_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_red
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_red_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_ext1
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_ext1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_ext_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/metduct1b
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/metduct1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/env_chrome
        blendFunc GL_DST_COLOR GL_ZERO
        detail
        tcGen environment
    }
}

textures/common/metflr1a
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metflr1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/metflr1b
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metflr1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/metflr2b
{
	q3map_material	SolidMetal
	aliasShader	textures/common/metflr1b
    {
        map $lightmap
    }
    {
        map textures/common/metflr2b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/metduct1a
{
	q3map_material	HollowMetal
	aliasShader	textures/common/metduct1b
    {
        map $lightmap
    }
    {
        map textures/common/metduct1a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/wood_plyweather
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/common/wood_plyweather
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/razorwire2
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	cull	disable
    {
        map textures/common/razorwire2
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
        map textures/common/razorwire2
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/red_light_1
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/red_light_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/red_light_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/rope1
{
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/common/rope1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/water01
{
	surfaceparm	nomarks
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water01
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod scale 0.33 0.33
        tcMod turb 0.25 0.03 0.025 0.25
        tcMod scroll 0.07 -0.03
    }
}

textures/common/outlet
{
	q3map_material	Computer
	aliasShader	textures/common/outlet_decal
    {
        map $lightmap
    }
    {
        map textures/common/outlet
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/outlet_decal
{
	qer_editorimage	textures/common/outlet_patch
	surfaceparm	nomarks
	surfaceparm	nonsolid
	polygonOffset
	q3map_material	Computer
    {
        map $lightmap
    }
    {
        map textures/common/outlet_patch
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cement
{
	q3map_material	Concrete
	aliasShader	textures/common/cement2
    {
        map $lightmap
    }
    {
        map textures/common/cement
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cncr_wall01a
{
	q3map_material	Concrete
	aliasShader	textures/common/cncr_wall01b
    {
        map $lightmap
    }
    {
        map textures/common/cncr_wall01a
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cncr_wall01b
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/cncr_wall01b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cncr_wall01c
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/cncr_wall01c
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/dirt
{
	q3map_material	Dirt
    {
        map $lightmap
    }
    {
        map textures/common/dirt
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/dirtgrassgreentran
{
	q3map_material	Dirt
    {
        map $lightmap
    }
    {
        map textures/common/dirtgrassgreentran
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/dirtgrasstran
{
	q3map_material	Dirt
    {
        map $lightmap
    }
    {
        map textures/common/dirtgrasstran
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/grass
{
	q3map_material	ShortGrass
    {
        map $lightmap
    }
    {
        map textures/common/grass
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/grass_green
{
	q3map_material	ShortGrass
    {
        map $lightmap
    }
    {
        map textures/common/grass_green
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/metal_brushed
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metal_brushed
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/light6
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light6
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light6_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/metal_brushed2
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metal_brushed2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/1_duct
{
	q3map_material	HollowMetal
	aliasShader	textures/common/1_duct3
    {
        map $lightmap
    }
    {
        map textures/common/1_duct
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/1_duct2
{
	q3map_material	HollowMetal
	aliasShader	textures/common/1_duct3
    {
        map $lightmap
    }
    {
        map textures/common/1_duct2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/floor_cement
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/floor_cement
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/floor_woodplanks128
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/common/floor_woodplanks128
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/metal_rust_1
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metal_rust_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/monitor
{
	q3map_material	Computer
	damageShader	textures/common/monitor_d 1
    {
        map $lightmap
    }
    {
        map textures/common/monitor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/step_cement
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/step_cement
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/steps_woodtop
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/common/steps_woodtop
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/razorwire3
{
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	cull	disable
    {
        map textures/common/razorwire3
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
        map textures/common/razorwire3
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/powerfitting
{
	polygonOffset
	q3map_material	SolidMetal
    {
        map textures/common/powerfitting
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
        map textures/common/powerfitting
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/3_cement
{
	q3map_material	Concrete
	aliasShader	textures/common/cement2
    {
        map $lightmap
    }
    {
        map textures/common/3_cement
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/3_flatwood
{
	q3map_material	HollowWood
    {
        map $lightmap
    }
    {
        map textures/common/3_flatwood
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/3_redbrick
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/3_redbrick
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/alleybrick
{
	q3map_material	Concrete
	aliasShader	textures/common/3_redbrick
    {
        map $lightmap
    }
    {
        map textures/common/alleybrick
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/h_cretewall_dirty
{
	q3map_material	Concrete
	aliasShader	textures/common/h_cretewall_plain
    {
        map $lightmap
    }
    {
        map textures/common/h_cretewall_dirty
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/h_cretewall_plain
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/h_cretewall_plain
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/light_bluey
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_bluey
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_bluey_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/cabinet_light
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/cabinet_light
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/cabinet_light_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_chrome
{
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_chrome
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_chrome_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_chrome_decal
{
	qer_editorimage	textures/common/light_chrome
	q3map_flare	gfx/misc/lens_flare
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/light_chrome
        alphaFunc GE128
        rgbGen exactVertex
    }
}

textures/common/light_chrome_decal_glow
{
	qer_editorimage	textures/common/light_chrome
	q3map_flare	gfx/misc/lens_flare
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/light_chrome
        alphaFunc GE128
        rgbGen exactVertex
    }
    {
        map textures/common/light_chrome_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/decal_rust
{
	polygonOffset
	q3map_material	Concrete
    {
        map textures/common/decal_rust
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
        map textures/common/decal_rust
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/decal_rust2
{
	polygonOffset
	q3map_material	Concrete
	aliasShader	textures/common/decal_rust
    {
        map textures/common/decal_rust2
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
        map textures/common/decal_rust2
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/decal_rust3
{
	polygonOffset
	q3map_material	Concrete
	aliasShader	textures/common/decal_rust
    {
        map textures/common/decal_rust3
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
        map textures/common/decal_rust3
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/met_env_nocull
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
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

textures/common/ceramic_roof2_vertex
{
	qer_editorimage	textures/common/ceramic_roof2
	q3map_material	Tiles
	q3map_nolightmap
	q3map_onlyvertexlighting
	aliasShader	textures/common/ceramic_roof
    {
        map textures/common/ceramic_roof2
        rgbGen vertex
    }
}

textures/common/cabinet_light_3000
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/cabinet_light
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/cabinet_light
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/cabinet_light_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/ceilinglite_3000
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/ceilinglite
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/ceilinglite
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/ceilinglite_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light1_3000
{
	q3map_lightimage	textures/colors/white
	qer_editorimage	textures/common/light1
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/light1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light6_3000
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/light6
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light6
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light6_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_bluey_3000
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/common/light_bluey
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_bluey
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_bluey_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_chrome_3000
{
	q3map_lightimage	textures/colors/white
	qer_editorimage	textures/common/light_chrome
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_chrome
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_chrome_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_ext1_3000
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/light_ext1
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_ext1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_ext_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_ext2_3000
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/light_ext2
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_ext2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_ext_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light_flor_3000
{
	q3map_lightimage	textures/colors/white
	qer_editorimage	textures/common/light_flor
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_flor
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light_flor_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_1_3000
{
	q3map_lightimage	textures/colors/yellow
	qer_editorimage	textures/common/lights_1
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_2_3000
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/lights_2
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_2_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_3_3000
{
	q3map_lightimage	textures/colors/white
	qer_editorimage	textures/common/lights_3
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_3_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_4_3000
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/common/lights_4
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_4_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_3000
{
	q3map_lightimage	textures/colors/white
	qer_editorimage	textures/common/lights_5
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_blue_3000
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/common/lights_5_blue
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_blue_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_offw_3000
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/lights_5_offw
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_offw
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_offw_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_red_3000
{
	q3map_lightimage	textures/colors/red_light
	qer_editorimage	textures/common/lights_5_red
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_red_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/red_light_1_3000
{
	q3map_lightimage	textures/colors/red_light
	qer_editorimage	textures/common/red_light_1
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/red_light_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/red_light_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_red_off
{
	qer_editorimage	textures/common/lights_5_red
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/light_chrome_glow
{
	q3map_material	Glass
}

textures/common/handi_switch
{
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/handi_switch
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_1_1500
{
	q3map_lightimage	textures/colors/yellow
	qer_editorimage	textures/common/lights_1
	q3map_surfacelight	1500
	q3map_flare	gfx/misc/lens_flare
    {
        map $lightmap
    }
    {
        map textures/common/lights_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/outlet_nocover
{
	q3map_material	Computer
	aliasShader	textures/common/outlet_decal
    {
        map $lightmap
    }
    {
        map textures/common/outlet_nocover
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/part_board
{
	q3map_material	SolidWood
    {
        map $lightmap
    }
    {
        map textures/common/part_board
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault3_decal
{
	qer_editorimage	textures/common/asphault3
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Concrete
	aliasShader	textures/common/asphault
    {
        map $lightmap
    }
    {
        map textures/common/asphault3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/1_duct3
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/1_duct3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/blinds
{
	q3map_material	Plastic
    {
        map textures/common/blinds
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
        map textures/common/blinds
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/blinds_noalpha
{
	qer_editorimage	textures/common/blinds
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/common/blinds
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/gravel
{
	q3map_material	Gravel
    {
        map $lightmap
    }
    {
        map textures/common/gravel
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/gravel_tran
{
	q3map_material	Gravel
    {
        map $lightmap
    }
    {
        map textures/common/gravel_tran
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/water_murky
{
	qer_editorimage	textures/common/water01
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water01
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod scale 0.33 0.33
        tcMod turb 0.25 0.03 0.025 0.25
        tcMod scroll 0.07 -0.03
    }
}

textures/common/water_murky2
{
	qer_editorimage	textures/common/water_murky
	q3map_tesssize	512
	qer_trans	33
	surfaceparm	nonsolid
	surfaceparm	water
	surfaceparm	trans
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
	deformvertexes	wave	100 sin 0 1 0 1
    {
        map textures/common/water_murky
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
        tcMod entityTranslate
        tcMod turb 0.5 0.05 0.09 0.1
        tcMod scroll 0 0.18
        tcMod scale 0.5 0.5
    }
}

textures/common/water03_new
{
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water_col_fall2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 0.5 0.3
        tcMod turb 0.25 0.03 0 0.5
        tcMod scroll 0 1
    }
    {
        map textures/common/water_col_fall1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 0.5 0.3
        tcMod turb 0.25 0.03 0 0.5
        tcMod scroll 0 2
    }
    {
        map textures/common/water_col_fall1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 0.5 0.3
        tcMod scroll 0 1
    }
}

textures/common/water_run
{
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water_run
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 1 0.8
        tcMod scroll 0 2.8
    }
    {
        map textures/common/water_run
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 1 1.5
        tcMod scroll 0 2.9
    }
}

textures/common/rotor
{
	qer_editorimage	textures/common/rotor
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	q3map_novertexshadows
    {
        clampmap textures/common/rotor
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod rotate 800
    }
}

textures/common/lightswitch_decal
{
	qer_editorimage	textures/common/lightswitch
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/common/lightswitch
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/water03_new2
{
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water_col_fall2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 0.5 0.3
        tcMod turb 0.25 0.03 0 0.25
        tcMod scroll 0 1
    }
    {
        map textures/common/water_col_fall1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 0.5 0.3
        tcMod turb 0.25 0.03 0 0.5
        tcMod scroll 0 2
    }
    {
        map textures/common/water_col_fall1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 0.5 0.3
        tcMod scroll 0 1
    }
}

textures/common/lights_1_off
{
	qer_editorimage	textures/common/lights_1
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_5_offw_off
{
	qer_editorimage	textures/common/lights_5_offw
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_offw
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_5_blue_off
{
	qer_editorimage	textures/common/lights_5_blue
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/red_light_1_off
{
	qer_editorimage	textures/common/red_light_1
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/red_light_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cabinet_light_off
{
	qer_editorimage	textures/common/cabinet_light
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/cabinet_light
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_3_off
{
	qer_editorimage	textures/common/lights_3
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights1_slight_blue_1500
{
	q3map_lightimage	textures/colors/slight_blue
	qer_editorimage	textures/common/lights_1
	q3map_surfacelight	1500
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_2_off
{
	qer_editorimage	textures/common/lights_2
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_5_off
{
	qer_editorimage	textures/common/lights_5
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_5blue3000decal
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/common/lights_5_blue
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_blue
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_blue_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5offw3000decal
{
	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/common/lights_5_offw
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	surfaceparm	nomarks
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_offw
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_offw_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/1_duct3_nosolid
{
	qer_editorimage	textures/common/1_duct3
	surfaceparm	nonsolid
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/1_duct3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_4_off
{
	qer_editorimage	textures/common/lights_4
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault1b
{
	q3map_material	Concrete
	aliasShader	textures/common/asphault1
    {
        map $lightmap
    }
    {
        map textures/common/asphault1b
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_1_slight_blue_1500
{
	q3map_surfacelight	1500
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1_slight_blue_1500
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/cable
{
    {
        map $lightmap
    }
    {
        map textures/common/cable
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/asphault_trans
{
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/asphault_trans
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_1_slight_blue
{
	qer_editorimage	textures/common/lights_1_slight_blue_1500
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1_slight_blue_1500
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_5_red_3000_decal
{
	q3map_lightimage	textures/colors/red_light
	qer_editorimage	textures/common/lights_5_red
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_red_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/red_light_1_3000_decal
{
	q3map_lightimage	textures/colors/red_light
	qer_editorimage	textures/common/red_light_1
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/red_light_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/red_light_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/glass_d
{
	surfaceparm	nonopaque
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/glass_d
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen vertex
    }
}

textures/common/lights_5_decal
{
	qer_editorimage	textures/common/lights_5
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/grass_vert
{
	qer_editorimage	textures/common/grass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/grass
        rgbGen exactVertex
    }
}

textures/common/water_pool
{
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/water_pool
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

textures/common/glass_impact1
{
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/glass_impact1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
    }
}

textures/common/glass_spread
{
	qer_editorimage	textures/common/glass_impact1
	nopicmip
	notc
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/glass_spread
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 1.5 1.5
    }
}

textures/common/lights_3_decal
{
	qer_editorimage	textures/common/lights_3
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_3_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light1_3000_decal
{
	q3map_lightimage	textures/colors/white
	qer_editorimage	textures/common/light1
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/light1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/fence_nocull_nonsolid
{
	qer_editorimage	textures/common/chainlinkfence
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	q3map_alphashadow
	cull	disable
    {
        map textures/common/chainlinkfence
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
        map textures/common/chainlinkfence
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/lights_1_decal
{
	qer_editorimage	textures/common/lights_1
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/file_cabinet_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet1_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet1_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet2_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet2_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet3_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet3_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_cabinet4_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_cabinet4_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file2_front_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file2_front_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/file_front_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/file_front_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/monitor_d
{
	q3map_material	HollowMetal
    {
        map $lightmap
    }
    {
        map textures/common/monitor_d
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_5_offw_b
{
	qer_editorimage	textures/common/lights_5_offw
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_offw_b
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_offw_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_1_3000_decal
{
	q3map_lightimage	textures/colors/yellow
	qer_editorimage	textures/common/lights_1
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_1_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/light1_off
{
	qer_editorimage	textures/common/light1
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/keycard
{
	surfaceparm	nomarks
	polygonOffset
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/keycard
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/common/sign_snake
{
	surfaceparm	nodamage
	surfaceparm	noimpact
	surfaceparm	nomarks
	q3map_material	SolidMetal
	q3map_nolightmap
	cull	disable
    {
        clampmap textures/common/sign_snake
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/common/sign_rj
{
	surfaceparm	nonsolid
	polygonOffset
	q3map_material	HollowWood
	cull	disable
    {
        map textures/common/sign_rj
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
        map textures/common/sign_rj
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

textures/common/light_flor_off
{
	qer_editorimage	textures/common/light_flor
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/light_flor
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/water_murky_dm
{
	qer_editorimage	textures/common/water01
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
    {
        map textures/common/water01
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod turb 0 0 0 0
    }
}

textures/common/water_murky_dm2
{
	qer_editorimage	textures/common/water01
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water01
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod turb 0 0 0 0
    }
}

textures/common/floor_woodplanks128_nonsolid
{
	qer_editorimage	textures/common/floor_woodplanks128
	surfaceparm	nonsolid
	surfaceparm	nonopaque
    {
        map $lightmap
    }
    {
        map textures/common/floor_woodplanks128
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/ceilingtile01_nonsolid
{
	qer_editorimage	textures/common/ceilingtile01
	surfaceparm	nonsolid
	q3map_material	Plaster
    {
        map $lightmap
    }
    {
        map textures/common/ceilingtile01
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/ceilinglite_flicker
{
	qer_editorimage	textures/common/ceilinglite
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/ceilinglite
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/ceilinglite_glow
        blendFunc GL_ONE GL_ONE
        rgbGen wave noise 0 1 0 10
    }
}

textures/common/lights_5_3000_decal
{
	q3map_lightimage	textures/colors/white
	qer_editorimage	textures/common/lights_5
	q3map_surfacelight	3000
	q3map_flare	gfx/misc/lens_flare
	polygonOffset
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/water02orig
{
	qer_editorimage	textures/common/water02
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water02
        depthWrite
        tcMod stretch inversesawtooth 1 0.01 0 0.01
        tcMod turb 0.01 0.5 0 0.03
        tcMod scale 0.25 0.25
    }
}

textures/common/water02
{
	qer_editorimage	textures/common/water01
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water01
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod stretch inversesawtooth 1 0.01 0 0.01
        tcMod turb 0.01 0.5 0 0.03
        tcMod scale 0.25 0.25
    }
}

textures/common/metal_brushed2_nonsolid
{
	qer_editorimage	textures/common/metal_brushed2
	surfaceparm	nonsolid
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metal_brushed2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/metal_brushed_nonsolid
{
	qer_editorimage	textures/common/metal_brushed
	surfaceparm	nonsolid
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metal_brushed
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/env_chrome
{
	q3map_nolightmap
	q3map_onlyvertexlighting
}

textures/common/water_pool_mpfinca
{
	qer_editorimage	textures/common/water_pool
	qer_trans	50
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/water_pool
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/common/crate_nobreak6
{
	surfaceparm	nodamage
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/crate_nobreak6
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/crate_nobreak2
{
	surfaceparm	nodamage
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/crate_nobreak2
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/crate_nobreak3
{
	surfaceparm	nodamage
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/crate_nobreak3
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/crate_nobreak4
{
	surfaceparm	nodamage
	q3map_material	SolidMetal
	aliasShader	textures/common/crate_nobreak3
    {
        map $lightmap
    }
    {
        map textures/common/crate_nobreak4
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/crate_nobreak5
{
	surfaceparm	nodamage
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/crate_nobreak5
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/crate_nobreak1
{
	surfaceparm	nodamage
	q3map_material	SolidMetal
	aliasShader	textures/common/crate_nobreak2
    {
        map $lightmap
    }
    {
        map textures/common/crate_nobreak1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/water_murky_finca
{
	qer_editorimage	textures/common/water_murky
	qer_trans	33
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
	q3map_novertexshadows
	cull	disable
    {
        map textures/common/water_murky
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

textures/common/canvas2_winter
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/common/canvas2_winter
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/canvas_btm_winter
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/common/canvas_btm_winter
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/canvas_desert
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/common/canvas_desert
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/canvas_winter
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/common/canvas_winter
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/canvas2_desert
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/common/canvas2_desert
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/canvas_btm_desert
{
	q3map_material	Canvas
    {
        map $lightmap
    }
    {
        map textures/common/canvas_btm_desert
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/water_murky_finca4
{
	qer_editorimage	textures/common/water_murky_new
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/water_murky_new_finca
        blendFunc GL_DST_COLOR GL_SRC_COLOR
    }
    {
        map textures/common/env_sheen_add_finca
        blendFunc GL_ONE GL_ONE
        tcGen environment
    }
    {
        map textures/common/env_ripple2
        blendFunc GL_DST_COLOR GL_ONE
        tcMod scroll 0.02 0.02
        tcMod scale 0.3 0.3
    }
}

textures/common/water_pool_finca
{
	qer_editorimage	textures/common/water_blue_new
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/water_blue_new
        blendFunc GL_DST_COLOR GL_SRC_COLOR
    }
    {
        map textures/common/env_ripple2
        blendFunc GL_DST_COLOR GL_ONE
        tcGen environment
        tcMod scroll 0.025 0.025
    }
    {
        map textures/common/env_darkwater
        blendFunc GL_DST_COLOR GL_SRC_COLOR
        tcMod scale 0.1 0.3
        tcMod scroll 0.05 0.05
    }
    {
        map textures/common/env_sheen_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
        tcMod scroll 0 0.05
    }
}

textures/common/water_murky_new
{
	qer_editorimage	textures/common/water_murky_new_col8
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_globaltexture
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/common/water_murky_new_col8
        blendFunc GL_ZERO GL_ONE
    }
    {
        map textures/common/env_sheen_add
        blendFunc GL_ONE GL_ONE
        tcGen environment
        tcMod scroll 0 0.025
    }
    {
        map textures/common/env_ripple2
        blendFunc GL_DST_COLOR GL_ONE
        tcGen environment
        tcMod scroll 0.025 0.025
    }
    {
        map textures/common/env_darkwater
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        tcMod scale 0.1 0.3
        tcMod scroll 0.05 0.05
    }
}

textures/common/water_murky_kam1
{
	qer_editorimage	textures/common/water01_kam1
	surfaceparm	nonsolid
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	cull	disable
    {
        map textures/common/water01_kam1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        tcMod scale 0.33 0.33
        tcMod turb 0.25 0.03 0.025 0.05
        tcMod scroll 0.005 -0.03
    }
}

textures/common/rope1_nonsolid
{
	qer_editorimage	textures/common/rope1
	surfaceparm	nonsolid
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/common/rope1
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

textures/common/lights_3_
{
	qer_editorimage	textures/common/lights_3
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_3
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_3_glow
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/lights_5_red_255
{
	q3map_lightimage	textures/colors/red_light
	qer_editorimage	textures/common/lights_5_red
	q3map_flare	gfx/misc/lens_flare
	q3map_material	Glass
    {
        map $lightmap
    }
    {
        map textures/common/lights_5_red
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        map textures/common/lights_5_red_glow
        blendFunc GL_ONE GL_ONE
    }
}

