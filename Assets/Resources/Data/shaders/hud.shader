gfx/menus/hud/blank
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map *white
        blendFunc GL_ZERO GL_ONE
    }
}

gfx/menus/hud/40mm
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/40mm
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_health_back_huey
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_health_back_huey
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_health_back_truck
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_health_back_truck
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_health_warning
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_health_warning
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen wave sin 2 10 0.5 2
    }
}

gfx/menus/hud/hud_health_frame
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_health_frame
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_health_back
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_health_back
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_armor
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_armor
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/chem_suit_overlay
{
	surfaceparm	trans
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        alphaGen const 0.3
        tcGen environment
        tcMod scale 2 2
    }
}

gfx/menus/hud/chem_suit_overlay2
{
	surfaceparm	trans
	q3map_nolightmap
	q3map_onlyvertexlighting
	sort	nearest
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        alphaGen const 0.25
        tcGen environment
        tcMod scale 3.5 3
    }
}

gfx/menus/hud/alarm
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/alarm
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_back
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_back
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_pop_out
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hud_pop_out
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/c4
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/c4
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/cd
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/cd
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/crouch_jump
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/crouch_jump
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/key_card
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/key_card
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/lock_pick
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/lock_pick
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/passport
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/passport
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/valve
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/valve
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/wire_cutters
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/wire_cutters
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/woot
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/woot
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/forklift
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/forklift
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hud_stealth
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampanimMap 20 gfx/menus/hud/stealth1 gfx/menus/hud/stealth2 gfx/menus/hud/stealth3 gfx/menus/hud/stealth4 gfx/menus/hud/stealth5 gfx/menus/hud/stealth6 gfx/menus/hud/stealth7 gfx/menus/hud/stealth8 
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/slug
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampanimMap 1 gfx/menus/hud/slug1 gfx/menus/hud/slug2 gfx/menus/hud/slug3 gfx/menus/hud/slug4 gfx/menus/hud/slug5 
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/stealth_off
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/stealth_off
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/hit_direction
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/hit_direction
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/computer
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/computer
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/radio
{
	qer_editorimage	gfx/menus/hud/key_card
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/radio
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/info
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/info
        rgbGen vertex
    }
}

gfx/menus/hud/info_blink
{
	qer_editorimage	gfx/menus/hud/info
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/info_walpha
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        alphaGen wave sin 0.5 0.3 1 0.9
    }
    {
        map gfx/menus/hud/info_walpha
        blendFunc GL_ONE GL_SRC_COLOR
    }
}

gfx/menus/hud/parachute
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/parachute
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/briefcase
{
	qer_editorimage	gfx/menus/hud/parachute
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/briefcase
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/chem_suit
{
	qer_editorimage	gfx/menus/hud/parachute
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/chem_suit
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/kam5_pull_pin
{
	qer_editorimage	gfx/menus/hud/parachute
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/kam5_pull_pin
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/lever
{
	qer_editorimage	gfx/menus/hud/parachute
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/hud/lever
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/ak74_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/ak74_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/knife_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/knife_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m3a1_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m3a1_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m4_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m4_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m590_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m590_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m60_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m60_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m67_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m67_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/msg90a1_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/msg90a1_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/oicw_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/oicw_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/usas12_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/usas12_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/mm1_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/mm1_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/rpg7_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/rpg7_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/anm14_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/anm14_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/f1_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/f1_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/l2a2_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/l2a2_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m15_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m15_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m84_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m84_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/mdn11_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/mdn11_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/smohg92_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/smohg92_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m1911a1_icon
{
	qer_editorimage	gfx/menus/hud/weapon_icons/m1911a1_icon
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m1911a1_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/ussocom_icon
{
	qer_editorimage	gfx/menus/hud/weapon_icons/ussocom_icon
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/ussocom_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/microuzi_icon
{
	qer_editorimage	gfx/menus/hud/weapon_icons/microuzi_icon
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/microuzi_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m1911a12_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/m1911a12_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/microuzi2_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/microuzi2_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/ussocom2_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
    {
        map gfx/menus/hud/weapon_icons/ussocom2_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/binoculars_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/weapon_icons/binoculars_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/nightvision_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/weapon_icons/nightvision_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/thermalvision_icon
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/weapon_icons/thermal_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/microuzi_icon_small
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/weapon_icons/microuzi_icon_small
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/m1911a1_icon_small
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/weapon_icons/m1911a1_icon_small
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/ussocom_icon_small
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/weapon_icons/ussocom_icon_small
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/weapon_icons/armor_large_icon
{
}

gfx/menus/hud/weapon_icons/armor_medium_icon
{
}

gfx/menus/hud/weapon_icons/armor_small_icon
{
}

gfx/menus/hud/weapon_icons/health_large_icon
{
}

gfx/menus/hud/weapon_icons/health_small_icon
{
}

gfx/menus/hud/weapon_icons/ammo_45_icon
{
}

gfx/menus/hud/weapon_icons/ammo_9mm_icon
{
}

gfx/menus/hud/weapon_icons/ammo_shotgun_icon
{
}

gfx/menus/hud/weapon_icons/ammo_762_icon
{
}

gfx/menus/hud/weapon_icons/ammo_556_icon
{
}

gfx/menus/hud/weapon_icons/ammo_40mm_icon
{
}

gfx/menus/hud/weapon_icons/ammo_rpg_icon
{
}

gfx/menus/hud/weapon_icons/thermal_icon
{
	qer_editorimage	gfx/menus/hud/weapon_icons/thermal_icon
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map gfx/menus/hud/weapon_icons/thermal_icon
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/crosshairs/ch5
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/crosshairs/ch5
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/crosshairs/ch1
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/crosshairs/ch1
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/crosshairs/ch2
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/crosshairs/ch2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/crosshairs/ch3
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/crosshairs/ch3
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/crosshairs/ch4
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        clampmap gfx/menus/crosshairs/ch4
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/scope/reticle/reticle2
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        clampmap gfx/menus/hud/scope/reticle/reticle2
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/scope/reticle/reticle
{
	nopicmip
	nomipmaps
	notc
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        clampmap gfx/menus/hud/scope/reticle/reticle
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

gfx/menus/hud/scope/background/background
{
	nopicmip
	nomipmaps
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        clampmap gfx/menus/hud/scope/background/background
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthFunc disable
        rgbGen vertex
    }
}

