models/chunks/wood/wood
{
	q3map_material	SolidWood
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/chunks/wood/wood
        blendFunc GL_ONE GL_ZERO
        rgbGen lightingDiffuse
    }
}

models/chunks/rubber/rubber
{
	q3map_material	Rubber
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/chunks/rubber/rubber
        blendFunc GL_ONE GL_ZERO
        rgbGen lightingDiffuse
    }
}

models/chunks/rock/rock
{
	q3map_material	Rock
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/chunks/rock/rock
        blendFunc GL_ONE GL_ZERO
        rgbGen lightingDiffuse
    }
}

models/chunks/metal/metal
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/chunks/metal/metal
        blendFunc GL_ONE GL_ZERO
        rgbGen lightingDiffuse
    }
    {
        map textures/shop/shop1ent_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.3
        tcGen environment
    }
}

models/chunks/ice/ice
{
	q3map_material	Ice
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/chunks/ice/ice
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/chunks/glass/glass
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/chunks/glass/glass
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.2
        tcGen environment
    }
}

models/chunks/fabric/fabric
{
	q3map_material	Fabric
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/chunks/fabric/fabric
        blendFunc GL_ONE GL_ZERO
        rgbGen lightingDiffuse
    }
}

models/chunks/paper/paper
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/chunks/paper/paper
        rgbGen lightingDiffuse
    }
}

models/chunks/paper/mag_paper2
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/chunks/paper/mag_paper2
        rgbGen lightingDiffuse
    }
}

models/chunks/paper/mag_paper3
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/chunks/paper/mag_paper3
        rgbGen lightingDiffuse
    }
}

models/chunks/paper/mag_paper1
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/chunks/paper/mag_paper1
        rgbGen lightingDiffuse
    }
}

models/chunks/truck_chunks/truck_hood_chunk
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/chunks/truck_chunks/truck_hood_chunk
        rgbGen lightingDiffuse
    }
}

models/chunks/truck_chunks/truck_fender_chunk
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/chunks/truck_chunks/truck_fender_chunk
        rgbGen lightingDiffuse
    }
}

