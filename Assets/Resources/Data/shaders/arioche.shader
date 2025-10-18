// skyparms work like this:

// q3map_sun <red> <green> <blue> <intensity> <degrees> <elevation>

// color will be normalized, so it doesn't matter what range you use

// intensity falls off with angle but not distance 100 is a fairly bright sun

// degree of 0 = from the east, 90 = north, etc.  altitude of 0 = sunrise/set, 90 = noon

// automap shader

gfx/menus/rmg/automap_sp
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap *automap
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

// SKIES

sky_desert_day
{
	lightcolor	( 0.301961 0.329412 0.466667 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 1 0.819608 0.54902 90 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_desert_night
{
	lightcolor	( 0.2 0.2 0.2 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 0.317647 0.352941 0.509804 76 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_jungle_morning
{
	lightcolor	( 0.101961 0.119412 0.136667 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 1 0.866667 0.592157 230 270 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_jungle_day
{
	lightcolor	( 0.401961 0.429412 0.456667 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 1 0.866667 0.592157 255 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_jungle_night
{
	lightcolor	( 0.1 0.11 0.15 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 0.317647 0.352941 0.509804 40 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_grassyhills_day
{
	lightcolor	( 0.301961 0.329412 0.466667 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 0.945098 0.945098 0.972549 90 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_grassyhills_night
{
	lightcolor	( 0.3 0.3 0.3 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 0.317647 0.352941 0.509804 76 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_snowy_day
{
	lightcolor	( 0.301961 0.329412 0.466667 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 0.843137 0.87451 0.92549 85 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

sky_snowy_night
{
	lightcolor	( 0.2 0.2 0.2 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	35
	q3map_lightsubdivide	512
	sun 0.317647 0.352941 0.509804 76 270 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

day
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	100
	q3map_lightsubdivide	512
	sun 0.79 0.79 0.9 120 270 65
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

night
{
	lightcolor	( 0.2 0.2 0.2 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	65
	q3map_lightsubdivide	512
	sun 0.317647 0.352941 0.509804 95 270 65
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

// Fogs for Arioche levels

ar_fog_city
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.7 0.66 0.61 ) 2000.0
}

ar_fog_desert_day
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.75 0.65 0.52 ) 5000.0
}

ar_fog_desert_night
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.05 0.05 0.07 ) 5000.0
}

ar_fog_jungle_morning
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.7 0.7 0.7 ) 4300.0
}

ar_fog_jungle_day
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.75 0.75 0.66 ) 4800.0
}

ar_fog_jungle_night
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.07 0.07 0.1 ) 4300.0
}

ar_fog_grassyhills_day
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.7 0.66 0.61 ) 5000.0
}

ar_fog_grassyhills_night
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.07 0.07 0.1 ) 5000.0
}

ar_fog_snowy_day
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.5 0.5 0.57 ) 5000.0
}

ar_fog_snowy_night
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.05 0.05 0.07 ) 5000.0
}

// Fogs for Arioche levels

ar_fog_none
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0 0 0 ) 0.0
}

// Arioche terrain shaders

textures/arioche/grassy1_0
{
	qer_editorimage textures/colombia/dirt_terrain1
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen exactVertex
    }
}

textures/arioche/grassy1_1
{
	qer_editorimage textures/common/grass_green
	q3map_material	ShortGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/grass_green
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

textures/arioche/water1
{
	qer_editorimage	textures/colombia/water_terrain
	surfaceparm	water
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/colombia/water_terrain
        blendFunc GL_ONE GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen exactVertex
        alphaGen const 0.9
        tcMod turb 0 0.08 0.04 0.08
        tcMod scroll -0.05 -0.001
        tcMod scale 0.5 0.5
    }
    {
        map textures/colombia/water_terrain2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen exactVertex
        tcMod turb 0 0.08 0.04 0.08
        tcMod scale 0.5 0.5
    }
}

textures/arioche/jungle_1
{
	qer_editorimage textures/colombia/mudside_b
	q3map_material	Mud
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen exactVertex
        tcMod scale 0.4 0.4
    }
}

textures/arioche/jungle_2
{
	qer_editorimage textures/colombia/new_grass
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
	sort	1
    {
        map textures/colombia/new_grass
        rgbGen exactVertex
        tcMod scale 0.4 0.4
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 60 32 64 600
            ssFademax 5000
            ssFadescale 3
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
}

textures/arioche/jungle_3
{
	qer_editorimage textures/colombia/new_grass_big_rock
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_grass_big_rock
        rgbGen exactVertex
        tcMod scale 0.4 0.4
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 60 32 64 600
            ssFademax 5000
            ssFadescale 3
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
}

textures/arioche/jungle_4
{
	qer_editorimage textures/colombia/new_big_rock
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen exactVertex
        tcMod scale 0.4 0.4
    }
}

textures/arioche/jungle_flat
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
	sort	1
    {
        map textures/colombia/new_grass
        rgbGen exactVertex
        tcMod scale 0.25 0.25
    }
}

textures/arioche/snow1_0
{
	qer_editorimage	textures/kamchatka/snow01
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow01
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
    {
        map gfx/sprites/ss_snowgrass2
            surfaceSprites vertical 4 2 64 600
            ssVariance 4 4
            ssWind 0.8
            ssWindidle 0
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen exactVertex
    }
}

textures/arioche/snow1_3
{
	qer_editorimage	textures/kamchatka/rock_huge
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/rock_huge
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

textures/arioche/snow1_1
{
	qer_editorimage	textures/kamchatka/rock_huge_snow
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/rock_huge_snow
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

textures/arioche/snow2_0
{
	qer_editorimage	textures/kamchatka/ice
	q3map_material	Ice
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/ice
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

textures/arioche/snow2_2
{
	qer_editorimage	textures/kamchatka/snow_2
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_2
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

textures/arioche/desert1_0
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/jordan/sand
        rgbGen exactVertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/jordan/shrubbery2
            surfaceSprites vertical 48 32 128 600
            ssFademax 5000
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
}

textures/arioche/desert1_1
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/jordan/dirt_sand
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
    {
        map textures/jordan/shrubery
            surfaceSprites vertical 48 32 128 600
            ssFademax 5000
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
}

textures/arioche/desert1_2
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/jordan/rock_sandstone
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

textures/arioche/desert_flat
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/jordan/sand
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

textures/arioche/grassy_flat
{
	qer_editorimage	textures/common/grass_green
	q3map_material	ShortGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/grass_green
        rgbGen exactVertex
        tcMod scale 0.5 0.5
    }
}

