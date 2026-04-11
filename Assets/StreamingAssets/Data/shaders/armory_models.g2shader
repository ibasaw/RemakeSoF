models/objects/armory/breather
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/armory/breather
        rgbGen lightingDiffuse
    }
}

models/objects/armory/flippers
{
	q3map_material	Rubber
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/armory/flippers
        rgbGen lightingDiffuse
    }
}

models/objects/armory/mask
{
	q3map_material	Rubber
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/armory/mask
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/objects/armory/airtank
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/armory/airtank
        rgbGen lightingDiffuse
    }
    {
        map textures/shop/shop1ent_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.5
        tcGen environment
    }
}

models/objects/armory/dummy
{
	q3map_material	Canvas
	q3map_nolightmap
    {
        map models/objects/armory/dummy
        rgbGen lightingDiffuse
    }
}

models/objects/armory/rope
{
	q3map_material	SolidMetal
    {
        map models/objects/armory/rope
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
        rgbGen lightingDiffuse
    }
    {
        map $lightmap
        blendFunc GL_ONE GL_ZERO
        depthFunc equal
    }
    {
        map models/objects/armory/rope
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
    }
}

