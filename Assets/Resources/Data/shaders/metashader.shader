// *************************************************

// *   TEST NEW COLOMBIA

// *************************************************

textures/metashader/testcol_0
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/test/grass
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/testcol_1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/test/grass_big_rock
        rgbGen vertex
        tcMod scale 0.05 0.05
    }
}

textures/metashader/testcol_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/test/big_rock
        rgbGen vertex
        tcMod scale 0.05 0.05
    }
}

textures/metashader/testcol_0to1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/test/grass
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/test/grass_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.05 0.05
    }
}

textures/metashader/testcol_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/test/grass
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/test/big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.05 0.05
    }
}

textures/metashader/testcol_1to2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/test/grass_big_rock
        rgbGen vertex
        tcMod scale 0.05 0.05
    }
    {
        map textures/test/big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.05 0.05
    }
}

// *************************************************

// *   CEM1 Metashader

// *************************************************

textures/metashader/cem1_0
{
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
// tcmod scale .1 .1

        map textures/colombia/dirt_terrain1
        rgbGen vertex
    }
}

textures/metashader/cem1_1
{
	q3map_material	ShortGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/grass_green
        rgbGen vertex
        tcMod scale 0.1 0.1
    }
}

textures/metashader/cem1_0to1
{
	q3map_material	ShortGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.1 0.1
    }
    {
        map textures/common/grass_green
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.1 0.1
    }
}

// *************************************************

// *   COL2 Metashader

// *************************************************

textures/metashader/col2_0
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        tcMod scale 0.5 0.5
    }
}

textures/metashader/col2_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col2_2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 32 20 128 350
            ssFademax 1200
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col2_3
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col2_0to1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.5 0.5
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col2_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.5 0.5
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col2_0to3
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.5 0.5
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col2_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col2_1to3
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col2_2to3
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

// *************************************************

// *   COL2A Metashader

// *************************************************

textures/metashader/col2a_0
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col2a_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 32 20 128 350
            ssFademax 1200
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col2a_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col2a_0to1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col2a_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col2a_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

// *************************************************

// *   Kam4 Metashader

// *************************************************

textures/metashader/kam4_0
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_1
        rgbGen vertex
        tcMod scale 0.12 0.12
    }
}

textures/metashader/kam4_1
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/rock_huge_snow
        rgbGen vertex
        tcMod scale 0.06 0.06
    }
}

textures/metashader/kam4_2
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_1
        rgbGen vertex
        tcMod scale 0.12 0.12
    }
}

textures/metashader/kam4_0to1
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.12 0.12
    }
    {
        map textures/kamchatka/rock_huge_snow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.12 0.12
    }
}

textures/metashader/kam4_0to2
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.12 0.12
    }
    {
        map textures/kamchatka/snow_1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.12 0.12
    }
}

textures/metashader/kam4_1to2
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/rock_huge_snow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.12 0.12
    }
    {
        map textures/kamchatka/snow_1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.12 0.12
    }
}

// *************************************************

// *   Kam3b Metashader (this is for the bottom floor of kam3)

// *************************************************

textures/metashader/kam3b_0
{
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/grnd01d
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/kam3b_1
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_1
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/kam3b_2
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_2
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/kam3b_0to1
{
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/grnd01d
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/kamchatka/snow_1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/kam3b_0to2
{
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/grnd01d
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/kamchatka/snow_2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/kam3b_1to2
{
	q3map_material	Snow
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/snow_1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/kamchatka/snow_2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

// *************************************************

// *   COL6 Metashader

// *************************************************

textures/metashader/col6_0
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map models/objects/colombia/jungle/tree05_vines
            surfaceSprites vertical 16 32 48 600
            ssFademax 2000
            ssVariance 1 2
            ssWind 1
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 32 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 128 600
            ssFademax 2000
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6_2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col6_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/finca/ground_fallow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 32 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col6_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

// *************************************************

// *   COL6start Metashader

// *************************************************

textures/metashader/col6start_0
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.7
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6start_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col6start_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 2
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

// *************************************************

// *   COL6end Metashader

// *************************************************

textures/metashader/col6end_0
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map models/objects/colombia/jungle/tree05_vines
            surfaceSprites vertical 16 32 48 600
            ssFademax 2000
            ssVariance 1 2
            ssWind 1
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6end_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 40 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6end_2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col6end_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/finca/ground_fallow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/grass_reeds
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6end_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col6end_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

// *************************************************

// *   COL6wfall Metashader

// *************************************************

textures/metashader/col6wfall_0
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map models/objects/colombia/jungle/tree05_vines
            surfaceSprites vertical 16 32 48 600
            ssFademax 2000
            ssVariance 1 2
            ssWind 1
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6wfall_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 40 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6wfall_2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col6wfall_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/finca/ground_fallow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 900
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col6wfall_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col6wfall_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

// *************************************************

// *   COL7 Metashader

// *************************************************

textures/metashader/col7_0
{
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col7_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 40 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col7_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col7_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col7_0to2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col7_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

// *************************************************

// *   SAM Metashader (BRIAN'S)

// *************************************************

textures/metashader/sam_0
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/sam_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 40 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/sam_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/sam_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/sam_0to2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/sam_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

// *************************************************

// *   JOR1 Metashader

// *************************************************

textures/metashader/jor1_0
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/jordan/sand
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/jordan/shrubery
            surfaceSprites vertical 32 24 256 600
            ssFademax 6500
            ssFadescale 1.5
            ssVariance 0.5 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/jor1_1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/jordan/rock_sandstone
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/jor1_0to1
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/jordan/sand
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/jordan/shrubery
            surfaceSprites vertical 32 24 256 600
            ssFademax 6500
            ssFadescale 1.5
            ssVariance 0.5 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map textures/jordan/rock_sandstone
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

// *************************************************

// *   Kam12 Metashader (this is for the top of the end cave in kam12

// *************************************************

textures/metashader/kam12_0
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/cave_rock01
        rgbGen vertex
        tcMod scale 0.12 0.12
    }
}

textures/metashader/kam12_1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/cave_rock02
        rgbGen vertex
        tcMod scale 0.12 0.12
    }
}

textures/metashader/kam12_0to1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/cave_rock01
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/kamchatka/cave_rock02
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

// *************************************************

// *   TUTORIAL Metashader

// *************************************************

textures/metashader/tut1_0
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        tcMod scale 0.5 0.5
    }
}

textures/metashader/tut1_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/tut1_2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/tut1_0to1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.5 0.5
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/tut1_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.5 0.5
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/tut1_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 20 40 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

// *************************************************

// *   dm_col1 Metashader

// *************************************************

textures/metashader/dm_col1_0
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 80 80 100 750
            ssFademax 4500
            ssFadescale 2
            ssVariance 2 3
            ssWind 0.3
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen exactVertex
    }
}

textures/metashader/dm_col1_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 24 45 400
            ssFademax 3000
            ssFadescale 2.5
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 48 32 256 600
            ssFademax 4000
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
}

textures/metashader/dm_col1_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/dm_col1_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_dirt
            surfaceSprites vertical 20 12 60 300
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.6
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
}

textures/metashader/dm_col1_0to2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/caverock_big
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/dm_col1_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/caverock_big
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map gfx/sprites/ss_grass_caverock
            surfaceSprites vertical 24 18 65 300
            ssFademax 2000
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen exactVertex
    }
}

// *************************************************

// *   COL3_renner Metashader

// *************************************************

textures/metashader/col3_renner_0
{
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col3_renner_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 40 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_renner_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col3_renner_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_renner_0to2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/finca/ground_fallow
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col3_renner_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 24 24 54 600
            ssFademax 2000
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 24 16 48 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 24 28 128 600
            ssFademax 2000
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}









// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT

// OBSOLETE BEYOND THIS POINT






// *************************************************

// *   COL3 Metashader

// *************************************************

textures/metashader/col3_0
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col3_1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 12 42 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 24 42 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 48 24 256 350
            ssFademax 1200
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_3
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/new_big_rock
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col3_0to1
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/dirt_terrain1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 12 42 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 24 48 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_0to3
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col3_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 40 16 38 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_1to3
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 12 42 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col3_2to3
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/grass2_shd
            surfaceSprites vertical 32 24 48 525
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map textures/colombia/new_big_rock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
}





// OBSOLETE BEYOND THIS POINT




// *************************************************

// *   NEWCOL3 Metashader

// *************************************************

textures/metashader/newcol3_0
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        tcMod scale 0.3 0.3
    }
}

textures/metashader/newcol3_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 24 42 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 48 24 256 350
            ssFademax 1200
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/newcol3_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/newcol3_0to1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 24 42 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/newcol3_0to2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.3 0.3
    }
    {
        map textures/colombia/caverock_big
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/newcol3_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map gfx/sprites/grass_tall_shd
            surfaceSprites vertical 48 24 42 400
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_ONE GL_ZERO
        depthWrite
        rgbGen vertex
    }
    {
        map textures/colombia/caverock_big
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
}

// OBSOLETE BEYOND THIS POINT

// *************************************************

// *   COL4 Metashader

// *************************************************

textures/metashader/col4_0
{
	q3map_material	Mud
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col4_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/rock_wet
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/grass_cattail
            surfaceSprites vertical 48 32 32 1000
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col4_2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col4_3
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/cave_rockside
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col4_0to1
{
// {

// map textures/colombia/grass_cattail

// surfaceSprites vertical 48 32 32 1000

// blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA

// rgbGen vertex

// alphaFunc GE192

// depthWrite

// }

	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/dirt_terrain1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col4_0to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/dirt_terrain1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col4_0to3
{
	q3map_material	Mud
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/mudside_b
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/cave_rockside
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
}

textures/metashader/col4_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/caverock_big
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map textures/colombia/mudside_b
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map models/objects/colombia/jungle/grass_tall_shd
            surfaceSprites vertical 48 32 32 1000
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col4_1to3
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/rock_wet
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/caverock_big
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map textures/colombia/grass_cattail
            surfaceSprites vertical 48 32 32 1000
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col4_2to3
{
// {

// map models/objects/colombia/jungle/grass_tall_shd

// surfaceSprites vertical 48 32 32 1000

// blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA

// rgbGen vertex

// alphaFunc GE192

// depthWrite

// }

	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.25 0.25
    }
    {
        map textures/colombia/caverock_big
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
}

// OBSOLETE BEYOND THIS POINT

// *************************************************

// *   COL5 Metashader

// *************************************************

textures/metashader/col5_0
{
	q3map_material	Mud
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col5_1
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map gfx/sprites/grass_tall_shd
            surfaceSprites vertical 48 32 32 1000
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col5_2
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_jungle
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col5_0to1
{
	q3map_material	Mud
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map textures/colombia/ground_plants
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map gfx/sprites/grass_tall_shd
            surfaceSprites vertical 48 32 32 1000
            ssFademax 1500
            ssFadescale 2
            ssVariance 1 2
            ssWind 0.8
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/metashader/col5_0to2
{
	q3map_material	Mud
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/dirt_terrain1
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map textures/colombia/ground_jungle
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
}

textures/metashader/col5_1to2
{
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/ground_plants
        rgbGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map textures/colombia/ground_jungle
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen vertex
        tcMod scale 0.07 0.07
    }
    {
        map gfx/sprites/grass_tall_shd
            surfaceSprites vertical 48 32 32 1000
            ssFademax 1600
            ssFadescale 1.5
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

