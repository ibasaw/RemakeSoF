models/flags/flag_post
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/flags/flag_post
        rgbGen lightingDiffuse
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.3
        tcGen environment
    }
}

models/flags/flag_blue
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/flags/flag_blue
        rgbGen lightingDiffuse
    }
}

models/flags/flag_red
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/flags/flag_red
        rgbGen lightingDiffuse
    }
}

gfx/menus/hud/team_blue
{
	nomipmaps
	nopicmip
	noglfog
	noTC
	{
		map		gfx/menus/hud/team_blue
		depthfunc	disable
		rgbgen		identity
		blendfunc	GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
	}
}

gfx/menus/hud/team_red
{
	nomipmaps
	nopicmip
	noglfog
	noTC
	{
		map		gfx/menus/hud/team_red
		depthfunc	disable
		rgbgen		identity
		blendfunc	GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
	}
}
