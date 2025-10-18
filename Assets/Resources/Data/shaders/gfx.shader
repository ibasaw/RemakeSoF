// GFX.SHADER

// this file contains shaders that are used by the programmers to

// generate special effects not attached to specific geometry.  This

// also has 2D shaders such as fonts, etc.

white
{
    {
        map *white
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

line
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map *white
        rgbGen vertex
    }
}

console
{
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        map gfx/menus/console/glowline2
        blendFunc GL_ONE GL_ZERO
        depthFunc disable
        rgbGen identityLighting
        tcMod scroll 0 -0.2
    }
    {
        map gfx/menus/console/console
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen identityLighting
    }
    {
        map gfx/menus/console/glowline
        blendFunc GL_ONE GL_ONE
        depthFunc disable
        detail
        rgbGen identityLighting
        tcMod scroll 0 -0.2
    }
    {
        map gfx/menus/console/console_mask
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        detail
        rgbGen identityLighting
    }
}

projectionshadow
{
	polygonOffset
	q3map_nolightmap
	deformvertexes	projectionShadow	
    {
        map *white
        rgbGen const ( 0.000000 0.000000 0.000000 )
    }
}

blobshadow
{
	surfaceparm	nonopaque
	polygonOffset
	q3map_nolightmap
    {
        clampmap gfx/world/blobshadow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

waterwake
{
	surfaceparm	nonopaque
	surfaceparm	trans
	polygonOffset
	q3map_nolightmap
    {
        clampmap gfx/decals/impact/waterwake
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        tcMod stretch sawtooth 0.7 1 0 1
    }
}

markshadow
{
	polygonOffset
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/damage/shadow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen identity
        alphaGen vertex
    }
}

gfx/damage/rivetmark
{
	surfaceparm	nomarks
	surfaceparm	trans
	polygonOffset
	q3map_nolightmap
    {
        map gfx/damage/rivetmark
        alphaFunc GT0
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen identity
    }
}

gfx/damage/shadow
{
	polygonOffset
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/damage/shadow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/levelshots/unknownmap
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/levelshots/unknownmap
        rgbGen vertex
    }
}

gfx/menus/levelshots/arioche
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/levelshots/arioche
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/levelshots/background_load_screen
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/levelshots/background_load_screen
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/misc/water_drop
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/misc/water_drop
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

mp/gfx/menus/levelshots/unknownmap_mp
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map mp/gfx/menus/levelshots/unknownmap_mp
        rgbGen vertex
    }
}

gfx/menus/console/console_mp
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/console/console_mp
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/misc/load_bullet
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/misc/load_bullet
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/load_clip
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/misc/load_clip
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

