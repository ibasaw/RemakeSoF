textures/roam/terrain
{
	surfaceparm	sky
	surfaceparm	nonsolid
	surfaceparm terrain
	q3map_nolightmap
	q3map_onlyvertexlighting
	skyParms	- 1024 -
	sort	middle
	damageShader	textures/cemetery/marble_tile_b 75
	hitLocation	heightmaps/col1density
	hitMaterial	heightmaps/col9a
    {
        map textures/roam/terrain
        rgbGen vertex
        alphaGen blend
    }
}

textures/roam/rockface
{
	q3map_material	Dirt
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/kamchatka/rock_1
        depthWrite
        rgbGen exactVertex
    }
}

textures/roam/marble
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/cemetery/marble_tile
        depthWrite
        rgbGen exactVertex
    }
}

textures/roam/corrugate
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colombia/roof_corrugate
        depthWrite
        rgbGen exactVertex
    }
}

textures/roam/lightimage_test
{
	q3map_lightcolor	( 0 0 1 )
	qer_editorimage	textures/roam/marble
	q3map_surfacelight	600
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/colors/red
        depthWrite
        rgbGen exactVertex
    }
}

