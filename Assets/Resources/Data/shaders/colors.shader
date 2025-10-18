textures/colors/black_100
{
	qer_editorimage	textures/tools/editor_images/qer_black
	q3map_nolightmap
    {
        map $whiteimage
        rgbGen const ( 0.000000 0.000000 0.000000 )
    }
}

textures/colors/black_80
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.100000 0.100000 0.100000 )
    }
}

textures/colors/black_80_twosided
{
	qer_editorimage	textures/colors/black_80
	q3map_material	SolidMetal
	cull	disable
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.100000 0.100000 0.100000 )
    }
}

textures/colors/blue
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.160700 0.160700 0.542100 )
    }
}

textures/colors/blue_light
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.545000 0.607800 0.749000 )
    }
}

textures/colors/gold
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.619600 0.494100 0.333300 )
    }
}

textures/colors/grey
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.400000 0.400000 0.400000 )
    }
}

textures/colors/grey_bath
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.431300 0.431300 0.431300 )
    }
}

textures/colors/grey_dark
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.243100 0.203900 0.200000 )
    }
}

textures/colors/grey_warm
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.529400 0.533300 0.505800 )
    }
}

textures/colors/off_white
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.701900 0.674500 0.662700 )
    }
}

textures/colors/red
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.678400 0.054900 0.007800 )
    }
}

textures/colors/red_light
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.733300 0.568600 0.517600 )
    }
}

textures/colors/slight_blue
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.909800 1.000000 1.000000 )
    }
}

textures/colors/white
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 1.000000 1.000000 1.000000 )
    }
}

textures/colors/yellow
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.921500 0.921500 0.529400 )
    }
}

textures/colors/yellow_light
{
    {
        map $lightmap
    }
    {
        map $whiteimage
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen const ( 0.905800 0.905800 0.792100 )
    }
}

textures/colors/black_100_nonsolid
{
	qer_editorimage	textures/tools/editor_images/qer_black
	surfaceparm	nonsolid
	q3map_nolightmap
    {
        map $whiteimage
        rgbGen const ( 0.000000 0.000000 0.000000 )
    }
}

