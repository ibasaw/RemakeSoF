// surfaceSprites <type> <width> <height> <density> <fadedist>

// Type is vertical, oriented, effect

// ssFademax <fademax>

// ssFadescale <fadescale>

// ssVariance <varwidth> <varheight>

// ssHangdown

// ssAnyangle

// ssFaceup

// ssWind <wind>

// ssWindIdle <windidle>

// ssVertSkew <skew>

// ssFXDuration <duration>

// ssFXGrow <growwidth> <growheight>

// ssFXAlphaRange <alphastart> <startend>

// ssFXWeather

textures/common/asphault2_rain
{
	qer_editorimage	textures/common/asphault2
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/asphault2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.7 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/asphault_rain
{
	qer_editorimage	textures/common/asphault
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/asphault
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.7 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/cement2_rain
{
	qer_editorimage	textures/common/cement2
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/cement2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/h_cretewall_plain_rain
{
	qer_editorimage	textures/common/h_cretewall_plain
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/h_cretewall_plain
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/metal_rust_1_rain
{
	qer_editorimage	textures/common/metal_rust_1
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/common/metal_rust_1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/dirtgrassgreentran_rain
{
	qer_editorimage	textures/common/dirtgrassgreentran
	q3map_material	Concrete
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap textures/common/dirtgrassgreentran
        rgbGen vertex
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 800
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/grass_green_rain
{
	qer_editorimage	textures/common/grass_green
	q3map_material	Concrete
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/grass_green
        rgbGen vertex
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 800
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/common/grass_sprites
{
	qer_editorimage	textures/common/grass
	q3map_material	ShortGrass
	q3map_onlyvertexlighting
    {
        map $lightmap
    }
    {
        map textures/common/grass
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scale 0.5 0.5
    }
    {
        map gfx/sprites/grass_reeds
            surfaceSprites vertical 32 16 32 600
            ssFademax 1250
            ssFadescale 1.5
            ssVariance 0.5 0.75
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

// /////////////////////////////

// //

// END STUFF FOR PRA1	     //

// //

// /////////////////////////////

// /////////////////////////////

// //

// STUFF FOR COL8     //

// //

// /////////////////////////////

textures/colombia/col8_path
{
	qer_editorimage	textures/finca/ground_fallow
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/finca/ground_fallow
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scale 0.5 0.5
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 32 54 600
            ssFademax 1250
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
            ssFademax 1250
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
            ssFademax 1250
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/colombia/under_heli
{
	qer_editorimage	textures/finca/ground_fallow
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map $lightmap
    }
    {
        map textures/finca/ground_fallow
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scale 0.5 0.5
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 32 32 54 600
            ssFademax 1250
            ssFadescale 1.5
            ssVariance 1 0.5
            ssWind 0.2
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 48 600
            ssFademax 1250
            ssVariance 1 0.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 128 600
            ssFademax 1250
            ssVariance 1 0.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/colombia/mudside_reeds
{
	qer_editorimage	textures/colombia/mudside_b
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/colombia/mudside_b
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scale 0.5 0.5
    }
    {
        map gfx/sprites/grass_reeds
            surfaceSprites vertical 24 32 54 600
            ssFademax 1250
            ssFadescale 1.5
            ssVariance 1 1.5
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

textures/colombia/col_grass
{
	qer_editorimage	textures/finca/ground_fallow
	q3map_material	LongGrass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/finca/ground_fallow
        blendFunc GL_DST_COLOR GL_ZERO
        tcMod scale 0.5 0.5
    }
    {
        map gfx/sprites/ss_grass_plants
            surfaceSprites vertical 48 24 42 400
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
        map models/objects/colombia/jungle/tree09c_shd
            surfaceSprites vertical 32 28 128 600
            ssFademax 1250
            ssVariance 1 1.5
            ssWind 0.3
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/colombia/jungle/tree02_shd
            surfaceSprites vertical 38 32 256 600
            ssFademax 1250
            ssVariance 1 2
            ssWind 0.5
        alphaFunc GE192
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen vertex
    }
}

// /////////////////////////////

// //

// STUFF FOR ARM1	     //

// //

// /////////////////////////////

textures/armory/gutter1_rain
{
	qer_editorimage	textures/armory/gutter1
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/armory/gutter1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/armory/gutter2_rain
{
	qer_editorimage	textures/armory/gutter2
	polygonOffset
	q3map_material	SolidMetal
    {
        map $lightmap
    }
    {
        map textures/armory/gutter2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/armory/sidewalk_rain
{
	qer_editorimage	textures/armory/sidewalk
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/armory/sidewalk
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/armory/s_curb_rain
{
	qer_editorimage	textures/armory/s_curb
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/armory/s_curb
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/armory/s_asphalt_hole_rain
{
	qer_editorimage	textures/armory/s_asphalt_hole
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/armory/s_asphalt_hole
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.7 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/armory/canopy_top_rain
{
	qer_editorimage	textures/armory/canopy_top
	q3map_material	Canvas
	cull	disable
    {
        map $lightmap
    }
    {
        map textures/armory/canopy_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/armory/dumpsterlid_rain
{
	qer_editorimage	textures/armory/dumpsterlid
	q3map_material	Plastic
    {
        map $lightmap
    }
    {
        map textures/armory/dumpsterlid
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

// /////////////////////////////

// //

// END STUFF FOR ARM1	     //

// //

// /////////////////////////////

// /////////////////////////////////

// START TEMP STUFF FOR COL10 SM

// /////////////////////////////////

textures/sprites/viral_tent_temp
{
	surfaceparm	nonsolid
	surfaceparm	trans
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/colombia/viral_tent2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

// /////////////////////////////////

// END TEMP STUFF FOR COL10 SM

// ////////////////////////////////

// /////////////////////////////

// //

// STUFF FOR Liner1	     //

// //

// /////////////////////////////

textures/liner/deckflr01_rain
{
	qer_editorimage	textures/liner/deckflr01
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/deckflr01
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/liner/forecastle_plate2a_rain
{
	qer_editorimage	textures/liner/forecastle_plate2a
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/forecastle_plate2a
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/liner/deck_floor_rain
{
	qer_editorimage	textures/liner/deck_floor
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/deck_floor
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/liner/cargo_rail_top_rain
{
	qer_editorimage	textures/liner/cargo_rail_top
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/cargo_rail_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/liner/crane_track_rain
{
	qer_editorimage	textures/liner/crane_track
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/crane_track
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/liner/lifeboat_top_rain
{
	qer_editorimage	textures/liner/lifeboat_top
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/lifeboat_top
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/liner/lifeboat_top1_rain
{
	qer_editorimage	textures/liner/lifeboat_top1
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/lifeboat_top1
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/liner/step_dirty_rain
{
	qer_editorimage	textures/liner/step_dirty
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/liner/step_dirty
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.5 2.5 20 1200
            ssVariance 1 0.75
            ssFXDuration 135
            ssFXGrow 6 6
            ssFXAlphaRange 0.35 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

// /////////////////////////////

// //

// END STUFF FOR JOR1	     //

// //

// /////////////////////////////

// /////////////////////////////

// //

// STUFF FOR PRA1	     //

// //

// /////////////////////////////

textures/prague/sidewalk2_rain
{
	qer_editorimage	textures/prague/sidewalk2
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/prague/sidewalk2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/prague/street_rain
{
	qer_editorimage	textures/prague/street
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/prague/street
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/prague/path_rain
{
	qer_editorimage	textures/prague/path
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/prague/path
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/prague/rainzone
{
	qer_editorimage	textures/tools/editor_images/qer_rain
	qer_nocarve
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	trans
	q3map_nolightmap
	cull	disable
    {
        map $whiteimage
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/prague/path2_rain
{
	qer_editorimage	textures/prague/path2
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/prague/path2
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/prague/path2a_rain
{
	qer_editorimage	textures/prague/path2a
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/prague/path2a
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/prague/cement_rain
{
	qer_editorimage	textures/common/cement
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/common/cement
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

textures/prague/roof_guardhouse_rain
{
	qer_editorimage	textures/prague/roof_guardhouse
	q3map_material	Concrete
    {
        map $lightmap
    }
    {
        map textures/prague/roof_guardhouse
        blendFunc GL_DST_COLOR GL_ZERO
    }
    {
        clampmap gfx/sprites/rainhit
            surfaceSprites effect 2.0 2.0 40 600
            ssVariance 1 0.75
            ssFXDuration 155
            ssFXGrow 6 4
            ssFXAlphaRange 0.30 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
    {
        clampmap gfx/sprites/rainring
            surfaceSprites effect 2 2 28 350
            ssVariance 2 1
            ssFaceup
            ssFXDuration 220
            ssFXGrow 2.5 2.5
            ssFXAlphaRange 0.5 0
            ssFXWeather
        blendFunc GL_ONE GL_ONE
    }
}

