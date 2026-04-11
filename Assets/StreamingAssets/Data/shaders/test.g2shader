textures/test/fluffsky_l
{
// R		G	B	Light	Angle	Elevation

	q3map_lightimage	textures/test/plaster1
	qer_editorimage	textures/test/greycloud
	q3map_surfacelight	30
	sun 0.75 0.79 1 130 46 40
	surfaceparm	sky
	surfaceparm	noimpact
	q3map_nolightmap
	skyParms	- 256 -
    {
        map textures/test/greycloud
        depthWrite
        tcMod scale 1.5 1.5
        tcMod scroll 0.003 0.003
    }
    {
        map textures/test/greycloud
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.5
        tcMod scale 2.8 3.1
        tcMod scroll 0.0017 0.0015
    }
}

textures/test/metalfloor_wall_14_specular
{
	qer_editorimage	textures/test/metalfloor_wall_14_specular
    {
        map $lightmap
        rgbGen identity
    }
    {
        map textures/test/metalfloor_wall_14_specular
        blendFunc GL_DST_COLOR GL_SRC_ALPHA
        rgbGen identity
        alphaGen lightingSpecular
    }
}

textures/test/numberglass
{
	qer_trans	0.5
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_material	ShatterGlass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map textures/hospital/hos_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.15
        tcGen environment
    }
    {
        map textures/test/numberglass
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

textures/test/ev
{
	qer_editorimage	models/objects/jordan/misc/monitor_alpha
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/jordan/misc/monitor_alpha
        rgbGen exactVertex
    }
}

models/test/rj_red
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	sort	seeThrough
	cull	disable
    {
        map models/test/rj_red
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/test/rj_green
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	sort	decal
	cull	disable
    {
        map models/test/rj_green
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/test/rj_blue
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	sort	banner
	cull	disable
    {
        map models/test/rj_blue
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

