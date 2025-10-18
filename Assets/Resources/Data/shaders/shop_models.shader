models/objects/shop/misc/copy_machine
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/shop/misc/copy_machine
        rgbGen lightingDiffuse
    }
}

models/objects/shop/misc/water_cooler
{
	q3map_material	Water
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/shop/misc/water_cooler
        rgbGen lightingDiffuse
    }
}

models/objects/shop/misc/water_cooler_alpha
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/shop/misc/water_cooler_alpha
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
    {
        map textures/shop/shop1ent_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

models/objects/shop/misc/helipad_glass
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/shop/misc/helipad_glass
        rgbGen lightingDiffuse
    }
    {
        map models/objects/shop/misc/helipad_glass_glow
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/shop/misc/helipad_glass_bsp
{
	qer_editorimage	models/objects/shop/misc/helipad_glass
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/shop/misc/helipad_glass
        rgbGen vertex
    }
    {
        map models/objects/shop/misc/helipad_glass_glow
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/shop/misc/copy_machine_bsp
{
	qer_editorimage	models/objects/shop/misc/copy_machine
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/shop/misc/copy_machine
        rgbGen vertex
    }
}

models/objects/shop/misc/helipad_lights
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/shop/misc/helipad_lights
        rgbGen lightingDiffuse
    }
}

models/objects/shop/misc/helipad_lights_bsp
{
	qer_editorimage	models/objects/shop/misc/helipad_lights
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/shop/misc/helipad_lights
        rgbGen vertex
    }
}

