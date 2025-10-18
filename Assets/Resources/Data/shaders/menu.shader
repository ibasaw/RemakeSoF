gfx/menus/black
{
    {
        map *white
        rgbGen const ( 0.000000 0.000000 0.000000 )
    }
}

gfx/menus/save
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/save
        rgbGen vertex
    }
}

gfx/menus/backdrop/rmg_backdrop
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/rmg_backdrop
        rgbGen vertex
    }
}

gfx/menus/backdrop/inv_back_small
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/inv_back_small
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/inv_back_large
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/inv_back_large
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/guage
{
	qer_editorimage	gfx/menus/backdrop/guage1
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampanimMap 20 gfx/menus/backdrop/guage1 gfx/menus/backdrop/guage2 gfx/menus/backdrop/guage3 gfx/menus/backdrop/guage4 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage8 gfx/menus/backdrop/guage8 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage4 gfx/menus/backdrop/guage4 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage5 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage6 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage7 gfx/menus/backdrop/guage8 gfx/menus/backdrop/guage8 gfx/menus/backdrop/guage8 gfx/menus/backdrop/guage8 gfx/menus/backdrop/guage8 gfx/menus/backdrop/guage7 
        rgbGen vertex
    }
}

gfx/menus/backdrop/menu_multiplayer_back
{
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        clampmap gfx/menus/backdrop/menu_back_mask_mp
        depthFunc disable
        rgbGen vertex
    }
    {
        map gfx/menus/backdrop/menu_scanline
        blendFunc GL_ONE_MINUS_DST_ALPHA GL_ONE
        detail
        rgbGen vertex
        tcMod scroll 0 -0.2
        tcMod scale 1 2
    }
}

gfx/menus/backdrop/menu_multiplayer_joinserver
{
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        clampmap gfx/menus/backdrop/menu_join_mask_mp
        depthFunc disable
        rgbGen vertex
    }
    {
        map gfx/menus/backdrop/menu_scanline
        blendFunc GL_ONE_MINUS_DST_ALPHA GL_ONE
        detail
        rgbGen vertex
        tcMod scroll 0 -0.2
        tcMod scale 1 2
    }
}

gfx/menus/backdrop/requestor
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/backdrop/requestor
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/menu_key_turned
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/backdrop/menu_key_turned
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/backdrop/pra1_sof2_logo
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/backdrop/pra1_sof2_logo
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/menu_back_old
{
	qer_editorimage	gfx/menus/backdrop/menu_back
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        clampmap gfx/menus/backdrop/menu_back
        depthFunc disable
        rgbGen vertex
    }
    {
        map gfx/menus/backdrop/menu_scanline
        blendFunc GL_ONE_MINUS_DST_ALPHA GL_ONE
        detail
        rgbGen vertex
        tcMod scroll 0 -0.2
        tcMod scale 1 2
    }
}

gfx/menus/backdrop/menu_back
{
	qer_editorimage	gfx/menus/backdrop/menu_back
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        map gfx/menus/backdrop/menu_back
        rgbGen vertex
    }
    {
        map gfx/menus/backdrop/menu_back_final
        blendFunc GL_ONE GL_ZERO
        detail
    }
    {
        map gfx/menus/backdrop/menu_scanline
        blendFunc GL_ONE_MINUS_DST_ALPHA GL_ONE
        detail
        tcMod scroll 0 -0.2
        tcMod scale 1 2
    }
}

gfx/menus/backdrop/sof2_logo_test
{
	qer_editorimage	gfx/menus/backdrop/menu_back
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        clampmap gfx/menus/icons/sof2_logo
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/icons/sof2_logo
        blendFunc GL_ONE_MINUS_DST_ALPHA GL_ONE
        detail
        rgbGen vertex
        tcMod scale 1 2
    }
}

gfx/menus/backdrop/outfit_back2
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/backdrop/outfit_back2
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/backdrop/outfit_briefing
{
	qer_editorimage	gfx/menus/backdrop/outfit_back2
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/backdrop/outfit_briefing
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/backdrop/globe1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/globe1
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/globe2
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/globe2
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/earth_color
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/target
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/target
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/backdrop/target_glow
{
	qer_editorimage	gfx/menus/backdrop/target
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/backdrop/target
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
        alphaGen wave sin 0.5 1 2 1
        tcMod stretch sin 0.05 0.5 0.5 0.5
    }
}

gfx/menus/backdrop/earthmap_air1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_air1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_shop1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_shop1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_col1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_col1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_finca1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_finca1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_liner1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_liner1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_arm1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_arm1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_hk1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_hk1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_arm2_hos1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_arm2_hos1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_shop3
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_shop3
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_kam1
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_kam1
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/earthmap_shop5
{
	qer_editorimage	gfx/menus/backdrop/earth_color
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/earth_color
        depthFunc disable
        rgbGen vertex
    }
    {
        clampmap gfx/menus/backdrop/earthmap_shop5
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/globe_glow
{
	qer_editorimage	gfx/menus/backdrop/globe1
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/globe1
        depthFunc disable
        rgbGen vertex
    }
    {
        map gfx/menus/backdrop/globe_glow
        blendFunc GL_ONE GL_ONE
    }
}

gfx/menus/backdrop/globe_pulse
{
	qer_editorimage	gfx/menus/backdrop/globe1
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/backdrop/globe1
        depthFunc disable
        rgbGen vertex
    }
    {
        map gfx/menus/backdrop/globe_glow
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 1 0.5 0 1
    }
}

gfx/menus/misc/bar
{
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        map gfx/menus/misc/bar
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

gfx/menus/misc/button
{
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        map gfx/menus/misc/button
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

gfx/menus/misc/cap
{
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        map gfx/menus/misc/cap
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

gfx/menus/misc/cap_solid
{
	nopicmip
	nomipmaps
	q3map_nolightmap
    {
        map gfx/menus/misc/cap_solid
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen vertex
    }
}

gfx/menus/misc/double_helix
{
	nopicmip
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map gfx/menus/misc/double_helix
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

gfx/menus/misc/ar_medal_damage1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_damage1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_grenade1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_grenade1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_kill_all
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_kill_all
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_knife1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_knife1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_mission10
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_mission10
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_mission25
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_mission25
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_mission50
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_mission50
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_sharpshooter1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_sharpshooter1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
    }
}

gfx/menus/misc/ar_medal_sniper1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_sniper1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_untouched1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_untouched1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
    }
}

gfx/menus/misc/gold_star
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/gold_star
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/misc/ar_medal_osprey
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/misc/ar_medal_osprey
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

models/menu/logo_metal
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/menu/logo_metal
        rgbGen lightingDiffuse
    }
}

gfx/menus/hud/objective_screen/objective_screen
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/objective_screen/objective_screen
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
    {
        map gfx/menus/hud/objective_screen/objective_screen_glow
        blendFunc GL_ONE GL_ONE
        depthFunc disable
        rgbGen wave sin 1 2 10 1
    }
}

gfx/menus/hud/binoculars/binoculars_background
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/binoculars/binoculars_background
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/night_vision/night_vision_background
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        clampmap gfx/menus/hud/night_vision/night_vision_background
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/thermal/thermal_background
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        clampmap gfx/menus/hud/thermal/thermal_background
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
    }
}

gfx/menus/weapon_renders/ak74
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/ak74
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/binoculars
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/binoculars
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/knife
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/knife
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/anm14
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/anm14
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m67
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m67
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m3a1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m3a1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m4_m203
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m4
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m590
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m590
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m60
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m60
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m1911a1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m1911a1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/mdn11
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/mdn11
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/microuzi
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/microuzi
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/mm1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/mm1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/msg90a1
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/msg90a1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m84
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m84
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/ussocom
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/ussocom
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/rpg7
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/rpg7
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/thermal
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/thermal
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/nightvision
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/nightvision
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/usas12
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/usas12
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/oicw
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/oicw
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/m15
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/m15
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/smohg92
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/smohg92
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/armor
{
	qer_editorimage	gfx/menus/weapon_renders/smohg92
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/armor
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/weapon_renders/ussocom_mp
{
	qer_editorimage	gfx/menus/weapon_renders/ussocom
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/weapon_renders/ussocom_mp
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
    }
}

gfx/menus/icons/icon_torr_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_torr_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_game_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_game_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_lock_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_lock_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_options_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_options_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_quit_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_quit_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_stats_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_stats_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_credits_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_credits_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/button_blank
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/icons/button_blank
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/icons/icon_return_glow
{
	qer_editorimage	gfx/menus/icons/icon_stats_glow
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/icon_return_glow
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.5
    }
}

gfx/menus/icons/icon_create_server_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map mp/gfx/menus/icons/icon_create_server_glow_mp
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_join_server_glow
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map mp/gfx/menus/icons/icon_join_server_glow_mp
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
        alphaGen wave sin 0.5 0.3 1 0.9
    }
}

gfx/menus/icons/icon_torr
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/icons/icon_torr
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/icons/icon_game
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/icons/icon_game
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/icons/icon_lock
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/icons/icon_lock
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/icons/icon_options
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/icons/icon_options
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/icons/icon_quit
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/icons/icon_quit
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/icons/icon_credits
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/icons/icon_credits
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/icons/sof2_logo
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/sof2_logo
        rgbGen vertex
    }
    {
        map gfx/menus/icons/env_logo
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.8
        tcMod rotate 3
        tcMod scroll 0.05 0.09
    }
    {
        map gfx/menus/icons/sof2_logo
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
    }
}

gfx/menus/icons/env_logo
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/env_logo
    }
}

gfx/menus/icons/eax_icon
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/icons/eax_icon
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/rmg_vergara
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/rmg_vergara
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/rmg_nachrade
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/rmg_nachrade
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
    }
}

gfx/menus/rmg/rmg_devient
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/rmg_deviant
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/rmg_kam_dem
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/rmg_kam_dem
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/rmg_hk_dem
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/rmg_hk_dem
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/rmg_jor_dem
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/rmg_jor_dem
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/rmg_col_dem
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/rmg_col_dem
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/automap_bg
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/rmg/automap_bg
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

gfx/menus/rmg/rmg_inf
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/rmg/rmg_inf
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

fonts/title
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap fonts/title
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

fonts/medium
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap fonts/medium
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

fonts/small
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap fonts/small
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        depthWrite
        rgbGen vertex
    }
}

// menu shader

