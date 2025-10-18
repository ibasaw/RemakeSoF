textures/tools/_black
{
	qer_editorimage	textures/tools/editor_images/qer_black
	surfaceparm	noimpact
	surfaceparm	nomarks
	q3map_nolightmap
    {
        map textures/tools/_black
        blendFunc GL_ONE GL_ZERO
        rgbGen identity
    }
}

textures/tools/_mirror
{
	qer_editorimage	textures/tools/editor_images/qer_mirror
	portal
	q3map_nolightmap
    {
        map textures/tools/_mirror
        blendFunc GL_ONE GL_ONE_MINUS_SRC_ALPHA
        depthWrite
    }
}

textures/tools/_sky
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	q3map_nolightmap
	skyParms	- 1 -
}

textures/tools/_portal
{
	qer_editorimage	textures/tools/editor_images/qer_portal
	surfaceparm	nonopaque
	portal
	q3map_nolightmap
    {
        map textures/tools/_mirror
        blendFunc GL_ONE GL_ONE_MINUS_SRC_ALPHA
        alphaGen portal 4096
    }
}

textures/tools/_blockplayer
{
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	trans
}

textures/tools/_blocknpc
{
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	monsterclip
	surfaceparm	trans
}

textures/tools/_clip
{
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_clip_metalstep
{
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	trans
	q3map_material	SolidMetal
	q3map_nolightmap
}

textures/tools/_cushion
{
	qer_nocarve
	surfaceparm	nodamage
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	trans
}

textures/tools/_hint
{
	qer_nocarve
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	trans
	q3map_hint
	q3map_structural
}

textures/tools/_skip
{
	qer_nocarve
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	trans
	q3map_structural
}

textures/tools/_origin
{
	qer_nocarve
	surfaceparm	nodraw
	surfaceparm	nonsolid
	q3map_nolightmap
	q3map_origin
}

textures/tools/_areaportal
{
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_nolightmap
	q3map_areaportal
	q3map_structural
}

textures/tools/_trigger
{
	qer_nocarve
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	nonopaque
	q3map_nolightmap
}

textures/tools/_nodrop
{
	qer_nocarve
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm nodrop
	q3map_nolightmap
}

textures/tools/_caulk
{
	surfaceparm	nomarks
	surfaceparm	nodraw
	q3map_nolightmap
}

textures/tools/_nodraw
{
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_nodraw_solid
{
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_ladder
{
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	ladder
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_clusterportal
{
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm detail
	surfaceparm	trans
	q3map_clusterportal
}

textures/tools/_fogblack
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	qer_nocarve
	surfaceparm	nonsolid
	surfaceparm	fog
	surfaceparm nodrop
	surfaceparm	trans
	fogparms	( 0.4 0.4 0.4 ) 256.0
	cull	back
}

textures/tools/_terrain
{
	qer_editorimage	textures/tools/_terrain
	qer_nocarve
	qer_trans	0.3
	surfaceparm	nodraw
	q3map_nolightmap
}

textures/tools/fog_colombia
{
	qer_editorimage	textures/tools/editor_images/qer_fogblack
	qer_nocarve
	surfaceparm	nonsolid
	surfaceparm	fog
	fogparms	( 0.4 0.4 0.45 ) 960.0
	cull	disable
}

textures/tools/caulk_noblock
{
	qer_editorimage	textures/tools/_caulk
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	q3map_nolightmap
}

textures/tools/_roam
{
	qer_trans	0.4
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	outside
	surfaceparm terrain
	q3map_nolightmap
}

textures/tools/_outside
{
	qer_trans	0.2
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	outside
	surfaceparm	trans
}

textures/tools/rick_roam
{
	qer_editorimage	textures/tools/_roam
	qer_trans	0.4
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm terrain
	q3map_nolightmap
}

textures/tools/_blocknpc_shotclip
{
	qer_editorimage	textures/tools/_shotclip
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
}

textures/tools/_shoot
{
	qer_editorimage	textures/tools/_shoot
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_nodraw_water
{
	qer_editorimage	textures/tools/_nodraw
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	water
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_nodraw_water_two
{
	qer_editorimage	textures/tools/_nodraw
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	water
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_repel
{
	qer_editorimage	textures/tools/_repel
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	abseil
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_ladder_metal
{
	qer_editorimage	textures/tools/_ladder
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	ladder
	surfaceparm	trans
	q3map_material	HollowMetal
	q3map_nolightmap
}

textures/tools/_ladder_wood
{
	qer_editorimage	textures/tools/_ladder
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	ladder
	surfaceparm	trans
	q3map_material	HollowWood
	q3map_nolightmap
}

textures/tools/_clip_woodstep
{
	qer_editorimage	textures/tools/_clip
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	trans
	q3map_material	HollowWood
	q3map_nolightmap
}

textures/tools/_shoot_wood
{
	qer_editorimage	textures/tools/_shoot
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	SolidWood
	q3map_nolightmap
}

textures/tools/_clip_grass
{
	qer_editorimage	textures/tools/_clip
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	trans
	q3map_material	ShortGrass
	q3map_nolightmap
}

textures/tools/_shoot_metal
{
	qer_editorimage	textures/tools/_shoot
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	SolidMetal
	q3map_nolightmap
}

textures/tools/_blockbot
{
	qer_editorimage	textures/tools/_blocknpc
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	botclip
	surfaceparm	trans
}

textures/tools/_clip_asphalt
{
	qer_editorimage	textures/tools/_clip
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	trans
	q3map_material	Concrete
	q3map_nolightmap
}

textures/tools/_nodraw_clip
{
	qer_editorimage	textures/tools/_nodraw
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_projectile_clip
{
	qer_editorimage	textures/tools/_projectile_clip
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	nonopaque
	surfaceparm	slime
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_clip_trees
{
	qer_editorimage	textures/tools/_clip
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	SolidWood
	q3map_nolightmap
}

textures/tools/_clip_car
{
	qer_editorimage	textures/tools/_clip_metalstep
	qer_trans	0.5
	surfaceparm	nodamage
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_novertexshadows
}

textures/tools/_slick
{
	qer_editorimage	textures/tools/_slick
	surfaceparm	slick
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonopaque
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_shoot_sandbag
{
	qer_editorimage	textures/tools/_shoot
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	Sand
	q3map_nolightmap
}

textures/tools/_blocknpc_nosee
{
	qer_editorimage	textures/tools/_blocknpc
	qer_trans	0.5
	surfaceparm	noimpact
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	trans
}

textures/tools/_caulk_concrete
{
	qer_editorimage	textures/tools/_caulk
	surfaceparm	nomarks
	surfaceparm	nodraw
	q3map_material	Concrete
	q3map_nolightmap
}

textures/tools/_shoot_stone
{
	qer_editorimage	textures/tools/_shoot
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	Rock
	q3map_nolightmap
}

textures/tools/_clip_grass_finca
{
	qer_editorimage	textures/tools/_clip
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	ShortGrass
	q3map_nolightmap
}

textures/tools/_shoot_computer
{
	qer_editorimage	textures/tools/_shoot
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	Computer
	q3map_nolightmap
}

textures/tools/_clip_fall
{
	qer_editorimage	textures/tools/_clip
	qer_trans	0.5
	surfaceparm	nomarks
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	monsterclip
	surfaceparm nodrop
	surfaceparm	trans
	q3map_nolightmap
}

textures/tools/_shoot_cloth
{
	qer_editorimage	textures/tools/_shoot
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	playerclip
	surfaceparm	monsterclip
	surfaceparm	shotclip
	surfaceparm	trans
	q3map_material	Fabric
	q3map_nolightmap
}

textures/region
{
	q3map_nolightmap
    {
        map textures/region
    }
}

noshader
{
	qer_editorimage	textures/tools/_noshader
	qer_nocarve
	qer_trans	0.5
	surfaceparm	nodraw
	surfaceparm	nonsolid
	surfaceparm	trans
    {
        map textures/tools/_noshader
    }
}

