models/weapons/m4/m4
{
// sort	nearest

	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nosteps
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m4/m4
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/m4/m4_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/m4/m4_menu
{
// sort	nearest

	qer_editorimage	models/weapons/m4/m4
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nosteps
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m4/m4
        rgbGen lightingDiffuse
    }
}

models/weapons/knife/knife
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/knife/knife
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/knife/knife_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        depthWrite
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/knife/knife_bsp
{
    {
        map $lightmap
    }
    {
        map models/weapons/knife/knife
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

models/weapons/knife/knife_prague
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/knife/knife_prague
        rgbGen lightingDiffuse
    }
}

models/weapons/knife/knife_blur
{
	qer_editorimage	models/weapons/knife/knife
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/knife/knife
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/knife/knife_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        depthWrite
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/knife/knife_menu
{
	qer_editorimage	models/weapons/knife/knife
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/knife/knife
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen lightingDiffuse
    }
}

models/weapons/ak74/ak74
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/ak74/ak74
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/ak74/ak74_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/ak74/ak74_bsp
{
    {
        map $lightmap
    }
    {
        map models/weapons/ak74/ak74
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

models/weapons/ak74/ak74_menu
{
	qer_editorimage	models/weapons/ak74/ak74
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/ak74/ak74
        rgbGen lightingDiffuse
    }
}

models/weapons/m203/m203
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m203/m203
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/m203/m203_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/m203/m203_empty
{
	qer_editorimage	models/weapons/m203/m203
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/weapons/m203/m203
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/m203/m203_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/m203/m203_menu
{
	qer_editorimage	models/weapons/m203/m203
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m203/m203
        rgbGen lightingDiffuse
    }
}

models/weapons/m60/m60
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m60/m60
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/m60/m60_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/m60/m60_menu
{
	qer_editorimage	models/weapons/m60/m60
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m60/m60
        rgbGen lightingDiffuse
    }
}

models/weapons/microuzi/microuzi
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/microuzi/microuzi
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/microuzi/microuzi_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/microuzi/microuzi_menu
{
	qer_editorimage	models/weapons/microuzi/microuzi
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/microuzi/microuzi
        rgbGen lightingDiffuse
    }
}

models/weapons/msg90a1/msg90a1
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/msg90a1/msg90a1
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/msg90a1/msg90a1_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/msg90a1/msg90a1_menu
{
	qer_editorimage	models/weapons/msg90a1/msg90a1
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/msg90a1/msg90a1
        rgbGen lightingDiffuse
    }
}

models/weapons/usas12/usas12
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/usas12/usas12
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/usas12/usas12_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/usas12/usas12_menu
{
	qer_editorimage	models/weapons/usas12/usas12
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/usas12/usas12
        rgbGen lightingDiffuse
    }
}

models/weapons/ussocom/ussocom
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/ussocom/ussocom
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/ussocom/ussocom_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/ussocom/ussocom_extras
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/ussocom/ussocom_extras
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/ussocom/ussocom_extras_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
    {
        map models/weapons/ussocom/ussocom_extras_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

models/weapons/ussocom/ussocom_beam
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/misc/jk_soccomm_glow
        blendFunc GL_ONE GL_ONE
        tcMod rotate 15
    }
    {
        clampmap gfx/misc/jk_soccomm_glow
        blendFunc GL_ONE GL_ONE
        tcMod rotate -10
    }
}

models/weapons/ussocom/ussocom_menu
{
	qer_editorimage	models/weapons/ussocom/ussocom
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/ussocom/ussocom
        rgbGen lightingDiffuse
    }
}

models/weapons/ussocom/ussocom_extras_menu
{
	qer_editorimage	models/weapons/ussocom/ussocom_extras
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/ussocom/ussocom_extras
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/ussocom/ussocom_extras_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

models/hands/hands
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/hands/hands
        rgbGen lightingDiffuse
    }
}

models/weapons/rpd/rpd
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/rpd/rpd
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/rpd/rpd_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/rpg7/rpg7
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/rpg7/rpg7
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/rpg7/rpg7_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/rpg7/rpg7_menu
{
	qer_editorimage	models/weapons/rpg7/rpg7
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/rpg7/rpg7
        rgbGen lightingDiffuse
    }
}

models/weapons/m1911a1/m1911a1
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m1911a1/m1911a1
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/m1911a1/m1911a1_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/m1911a1/m1911a1_menu
{
	qer_editorimage	models/weapons/m1911a1/m1911a1
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m1911a1/m1911a1
        rgbGen lightingDiffuse
    }
}

models/weapons/m3a1/m3a1
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m3a1/m3a1
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/m3a1/m3a1_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/m3a1/m3a1_menu
{
	qer_editorimage	models/weapons/m3a1/m3a1
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m3a1/m3a1
        rgbGen lightingDiffuse
    }
}

models/weapons/m590/m590
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m590/m590
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/m590/m590_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        detail
        alphaGen lightingSpecular
    }
}

models/weapons/m590/m590_menu
{
	qer_editorimage	models/weapons/m590/m590
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/m590/m590
        rgbGen lightingDiffuse
    }
}

models/weapons/mm1/mm1
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/mm1/mm1
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/mm1/mm1_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        alphaGen lightingSpecular
    }
}

models/weapons/mm1/mm1_menu
{
	qer_editorimage	models/weapons/mm1/mm1
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/mm1/mm1
        rgbGen lightingDiffuse
    }
}

models/weapons/oicw/oicw
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/oicw/oicw
        rgbGen lightingDiffuse
    }
    {
        map models/weapons/oicw/oicw_spec
        blendFunc GL_SRC_ALPHA GL_ONE
        alphaGen lightingSpecular
    }
}

models/weapons/oicw/oicw_menu
{
	qer_editorimage	models/weapons/oicw/oicw
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/oicw/oicw
        rgbGen lightingDiffuse
    }
}

models/weapons/bayonet/bayonet
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/weapons/bayonet/bayonet
        rgbGen lightingDiffuse
    }
}

