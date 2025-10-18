models/characters/female_face/f_chem_taylor
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/female_face/f_female_hit
    {
        map models/characters/female_face/f_chem_taylor
        rgbGen lightingDiffuse
    }
}

models/characters/female_face/h_chem_taylor
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/female_face/h_female_bald_hit
    {
        map models/characters/female_face/h_chem_taylor
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/fplate_chem
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	sort	outside
	cull	disable
	hitLocation	models/characters/chem_suit/fplate_chem_hit
    {
        map models/characters/bolt_ons/helmet_visor_env
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen const 0.2
        tcGen environment
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
        alphaGen const 0.02
        tcGen environment
    }
}

models/characters/chem_suit/hood_chem
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
	hitLocation	models/characters/chem_suit/hood_chem_suit_hit
    {
        map models/characters/chem_suit/hood_chem
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/a_chem
{
	qer_editorimage	models/characters/chem_suit/a_chem
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/chem_suit/a_chem_suit_hit
    {
        map models/characters/chem_suit/a_chem
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/b_chem
{
	qer_editorimage	models/characters/chem_suit/b_chem
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/chem_suit/b_chem_suit_hit
    {
        map models/characters/chem_suit/b_chem
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/a_chem_rus
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/chem_suit/a_chem_suit_hit
    {
        map models/characters/chem_suit/a_chem_rus
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/b_chem_rus
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/chem_suit/b_chem_suit_hit
    {
        map models/characters/chem_suit/b_chem_rus
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/hood_chem_rus
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
	hitLocation	models/characters/chem_suit/hood_chem_suit_hit
    {
        map models/characters/chem_suit/hood_chem_rus
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/a_chem_taylor
{
	q3map_material	Flesh
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/chem_suit/a_chem_suit_hit
    {
        map models/characters/chem_suit/a_chem_taylor
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/b_chem_taylor
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/chem_suit/b_chem_suit_hit
    {
        map models/characters/chem_suit/b_chem_taylor
        rgbGen lightingDiffuse
    }
}

models/characters/chem_suit/hood_chem_taylor
{
	q3map_material	Flesh
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
	hitLocation	models/characters/chem_suit/hood_chem_suit_hit
    {
        map models/characters/chem_suit/hood_chem_taylor
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/f_chem_male_w1
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/average_face/f_average_hit
    {
        map models/characters/average_face/f_chem_male_w1
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/f2_chem_male_w1
{
	qer_editorimage	models/characters/average_face/f_chem_male_w1
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
	hitLocation	models/characters/average_face/f_average_hit
    {
        map models/characters/average_face/f_chem_male_w1
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/h_chem_male
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/average_face/h_average_bald_hit
    {
        map models/characters/average_face/h_chem_male
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/f_chem_male_b1
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/average_face/f_average_hit
    {
        map models/characters/average_face/f_chem_male_b1
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/f2_chem_male_b1
{
	qer_editorimage	models/characters/average_face/f_chem_male_b1
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
	hitLocation	models/characters/average_face/f_average_hit
    {
        map models/characters/average_face/f_chem_male_b1
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/f_chem_mullins
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/average_face/f_average_hit
	hitMaterial	models/characters/average_face/f_average_mat
    {
        map models/characters/average_face/f_chem_mullins
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/f2_chem_mullins
{
	qer_editorimage	models/characters/average_face/f_chem_mullins
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
	hitLocation	models/characters/average_face/f_average_hit
	hitMaterial	models/characters/average_face/f_average_mat
    {
        map models/characters/average_face/f_chem_mullins
        rgbGen lightingDiffuse
    }
}

models/characters/average_face/f_chem_wilson
{
	q3map_material	Flesh
	q3map_nolightmap
	q3map_onlyvertexlighting
	hitLocation	models/characters/average_face/f_average_hit
    {
        map models/characters/average_face/f_chem_wilson
        rgbGen lightingDiffuse
    }
}

