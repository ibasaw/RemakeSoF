models/objects/common/sa2_missle
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/sa2_missle
        rgbGen lightingDiffuse
    }
}

models/objects/common/sam_launcher_alpha
{
	entityMergable
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/sam_launcher_alpha
        rgbGen lightingDiffuse
    }
}

models/objects/common/sink_faucet
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sink_faucet
        rgbGen lightingDiffuse
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

models/objects/common/toilet
{
	q3map_material	Concrete
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/toilet
        rgbGen lightingDiffuse
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.1
        tcGen environment
    }
}

models/objects/common/phone
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/phone
        rgbGen lightingDiffuse
    }
}

models/objects/common/desklamp2
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/desklamp2
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/desklamp2_glow
        blendFunc GL_ONE GL_ONE
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        tcGen environment
    }
}

models/objects/common/desklamp2_bsp
{
	qer_editorimage	models/objects/common/desklamp2
	q3map_material	HollowMetal
	q3map_novertexshadows
	cull	disable
    {
        map $lightmap
        rgbGen vertex
    }
    {
        map models/objects/common/desklamp2
        blendFunc GL_DST_COLOR GL_ZERO
        rgbGen vertex
    }
    {
        map models/objects/common/desklamp2_glow
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen vertex
    }
    {
        map textures/common/env_chrome_add
        blendFunc GL_ONE GL_ONE
        detail
        rgbGen vertex
        tcGen environment
    }
}

models/objects/common/security_camera
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/security_camera
        rgbGen lightingDiffuse
    }
}

models/objects/common/security_camera_outside
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/security_camera_outside
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/security_camera_outside_glow
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 0.05 1 1 1
    }
}

models/objects/common/yugo
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/yugo
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/yugo_light
        blendFunc GL_ONE GL_ONE
        rgbGen lightingDiffuse
    }
}

models/objects/common/taxi_cab
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/taxi_cab
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/taxi_cab_lights
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/common/ashtray
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/ashtray
        rgbGen lightingDiffuse
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

models/objects/common/desklamp1
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/desklamp1
        rgbGen lightingDiffuse
    }
}

models/objects/common/desklamp1_alpha
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/desklamp1_alpha
        blendFunc GL_ONE GL_ONE
        rgbGen lightingDiffuse
    }
}

models/objects/common/candleholder
{
	q3map_flare	shaders/gfx/misc/k_sniper_flash
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/candleholder
        rgbGen lightingDiffuse
    }
}

models/objects/common/sprinkler
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/sprinkler
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/objects/common/lantern
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/lantern
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/lantern_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

models/objects/common/lantern_glass
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/lantern_glass
        blendFunc GL_ONE GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/objects/common/tv
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tv
        rgbGen lightingDiffuse
    }
}

models/objects/common/tv_screen
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tv_screen
        rgbGen lightingDiffuse
    }
}

models/objects/common/search_light
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/search_light
        rgbGen lightingDiffuse
    }
}

models/objects/common/search_light_alpha
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/search_light_alpha
        rgbGen identity
    }
}

models/objects/common/security_camera_light
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/security_camera_light
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 1 0.5 1 1
    }
}

models/objects/common/body_bag
{
	q3map_material	Flesh
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/body_bag
        rgbGen lightingDiffuse
    }
}

models/objects/common/poolball
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/poolball
        rgbGen lightingDiffuse
    }
}

models/objects/common/poolball_8ball
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/poolball_8ball
        rgbGen lightingDiffuse
    }
}

models/objects/common/poolball_green
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/poolball_green
        rgbGen lightingDiffuse
    }
}

models/objects/common/poolball_stripe
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/poolball_stripe
        rgbGen lightingDiffuse
    }
}

models/objects/common/poolball_yellow
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/poolball_yellow
        rgbGen lightingDiffuse
    }
}

models/objects/common/tracklight
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tracklight
        rgbGen lightingDiffuse
    }
}

models/objects/common/tracklight_shader
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tracklight_shader
        blendFunc GL_ONE GL_ONE
        rgbGen lightingDiffuse
    }
}

models/objects/common/trash_can
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/trash_can
        rgbGen lightingDiffuse
    }
}

models/objects/common/urinal
{
	q3map_material	Concrete
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/urinal
        rgbGen lightingDiffuse
    }
}

models/objects/common/payphone
{
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/payphone
        rgbGen lightingDiffuse
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

models/objects/common/shoes
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/shoes
        rgbGen lightingDiffuse
    }
}

models/objects/common/faucet
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/faucet
        rgbGen lightingDiffuse
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

models/objects/common/sam_launcher
{
	entityMergable
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sam_launcher
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        depthWrite
        rgbGen lightingDiffuse
    }
}

models/objects/common/sandbag_section
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sandbag_section
        rgbGen lightingDiffuse
    }
}

models/objects/common/sandbag_end
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sandbag_end
        rgbGen lightingDiffuse
    }
}

models/objects/common/sandbag_corner
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sandbag_corner
        rgbGen lightingDiffuse
    }
}

models/objects/common/sandbag_corner_bsp
{
	qer_editorimage	models/objects/common/sandbag_corner
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sandbag_corner
        rgbGen vertex
    }
}

models/objects/common/sandbag_end_bsp
{
	qer_editorimage	models/objects/common/sandbag_end
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sandbag_end
        rgbGen vertex
    }
}

models/objects/common/sandbag_section_bsp
{
	qer_editorimage	models/objects/common/sandbag_section
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    { 
        map models/objects/common/sandbag_section
        rgbGen vertex
    }
}

models/objects/common/trucksandbag
{
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/trucksandbag
        rgbGen lightingDiffuse
    }
}

models/objects/common/flame
{
	qer_editorimage	models/objects/common/flame
	q3map_flare	gfx/misc/jk_sniper_flash
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/flame
        blendFunc GL_ONE GL_ONE
        rgbGen wave sin 1 0.5 1 2
        tcMod stretch sin 1 0.05 1 10
    }
}

models/objects/common/couch_dirty
{
	q3map_material	Canvas
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/couch_dirty
        rgbGen lightingDiffuse
    }
}

models/objects/common/couch
{
	q3map_material	Canvas
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/couch
        rgbGen lightingDiffuse
    }
}

models/objects/common/chair_dirty
{
	q3map_material	Canvas
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/chair_dirty
        rgbGen lightingDiffuse
    }
}

models/objects/common/chair
{
	q3map_material	Canvas
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/chair
        rgbGen lightingDiffuse
    }
}

models/objects/common/sink_faucet_bsp
{
	qer_editorimage	models/objects/common/sink_faucet
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/sink_faucet
        rgbGen vertex
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.3
        tcGen environment
    }
}

models/objects/common/tv_bsp
{
	qer_editorimage	models/objects/common/tv
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tv
        rgbGen vertex
    }
}

models/objects/common/tv_screen_bsp
{
	qer_editorimage	models/objects/common/tv_screen
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tv_screen
        rgbGen vertex
    }
}

models/objects/common/lantern_bsp
{
	qer_editorimage	models/objects/common/lantern
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/lantern
        rgbGen vertex
    }
    {
        map models/objects/common/lantern_glow
        blendFunc GL_ONE GL_ONE
        detail
    }
}

models/objects/common/phone_bsp
{
	qer_editorimage	models/objects/common/phone
	q3map_material	Plastic
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/phone
        rgbGen vertex
    }
}

models/objects/common/shoes_bsp
{
	qer_editorimage	models/objects/common/shoes
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/shoes
        rgbGen vertex
    }
}

models/objects/common/taxi_cab_bsp
{
	qer_editorimage	models/objects/common/taxi_cab
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/taxi_cab
        rgbGen vertex
    }
    {
        map models/objects/common/taxi_cab_lights
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/common/trash_can_bsp
{
	qer_editorimage	models/objects/common/trash_can
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/trash_can
        rgbGen vertex
    }
}

models/objects/common/toilet_bsp
{
	qer_editorimage	models/objects/common/toilet
	q3map_material	Concrete
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/toilet
        rgbGen vertex
    }
    {
        map textures/common/env_chrome
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        detail
        alphaGen const 0.1
        tcGen environment
    }
}

models/objects/common/tracklight_bsp
{
	qer_editorimage	models/objects/common/tracklight
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tracklight
        rgbGen vertex
    }
}

models/objects/common/tracklight_shader_bsp
{
	qer_editorimage	models/objects/common/tracklight_shader
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/tracklight_shader
        blendFunc GL_ONE GL_ONE
        rgbGen vertex
    }
}

models/objects/common/candleholder_bsp
{
	qer_editorimage	models/objects/common/candleholder
	q3map_flare	shaders/gfx/misc/k_sniper_flash
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/candleholder
        rgbGen vertex
    }
}

models/objects/common/body_bag_bsp
{
	qer_editorimage	models/objects/common/body_bag
	q3map_material	Flesh
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/body_bag
        rgbGen vertex
    }
}

models/objects/common/desklamp1_bsp
{
	qer_editorimage	models/objects/common/desklamp1
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/desklamp1
        rgbGen vertex
    }
}

models/objects/common/desklamp1_alpha_bsp
{
	qer_editorimage	models/objects/common/desklamp1_alpha
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/desklamp1_alpha
        blendFunc GL_ONE GL_ONE
        rgbGen vertex
    }
}

models/objects/common/lantern_glass_bsp
{
	q3map_material	Glass
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/lantern_glass
        blendFunc GL_ONE GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/objects/common/toilet_md3
{
	q3map_material	Plaster
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/toilet_md3
        rgbGen lightingDiffuse
    }
}

models/objects/common/toilet_md3_bsp
{
	qer_editorimage	models/objects/common/toilet_md3
	q3map_material	Plaster
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/toilet_md3
        rgbGen vertex
    }
}

models/objects/common/handcart/handcart
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/handcart/handcart
        rgbGen lightingDiffuse
    }
}

models/objects/common/handcart/handcart_lrg
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/handcart/handcart_lrg
        rgbGen lightingDiffuse
    }
}

models/objects/common/handcart/handcart_bsp
{
	qer_editorimage	models/objects/common/handcart/handcart
	q3map_material	SolidMetal
	q3map_novertexshadows
    {
        map $lightmap
    }
    {
        map models/objects/common/handcart/handcart
        blendFunc GL_DST_COLOR GL_ZERO
    }
}

models/objects/common/forklift/body
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/body
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/cage_forks
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/cage_forks
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/cage_top_shd
{
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/cage_top_shd
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
    }
    {
        map models/objects/common/forklift/cage_top_shd
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/box
{
	q3map_material	HollowWood
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/box
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/body_bsp
{
	qer_editorimage	models/objects/common/forklift/body
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/body
        rgbGen vertex
    }
}

models/objects/common/forklift/box_bsp
{
	qer_editorimage	models/objects/common/forklift/box
	q3map_material	HollowWood
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/box
        rgbGen vertex
    }
}

models/objects/common/forklift/cage_forks_bsp
{
	qer_editorimage	models/objects/common/forklift/cage_forks
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/cage_forks
        rgbGen vertex
    }
}

models/objects/common/forklift/cage_top_shd_bsp
{
	qer_editorimage	models/objects/common/forklift/cage_top_shd
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/cage_top_shd
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
        rgbGen vertex
    }
    {
        map models/objects/common/forklift/cage_top_shd
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/body_blue
{
	qer_editorimage	models/objects/common/forklift/body
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/body_blue
        rgbGen lightingDiffuse
    }
}

models/objects/common/barrels/barrel_hk
{
	q3map_material	HollowMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/barrels/barrel_hk
        rgbGen lightingDiffuse
    }
}

models/objects/common/rope/rope
{
	surfaceparm	fog
	q3map_material	Canvas
	q3map_nolightmap
	q3map_onlyvertexlighting
	fogparms	( 0 0 0 ) 0.0
    {
        map models/objects/common/rope/rope
        rgbGen lightingDiffuse
    }
}

models/objects/common/flag/flag_us
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/flag/flag_us
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/flag/flag_us_add
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/common/flag/flag_hk
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/flag/flag_hk
        blendFunc GL_SRC_ALPHA GL_ONE_MINUS_SRC_ALPHA
        rgbGen lightingDiffuse
    }
}

models/objects/common/flag/flag_prague
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/flag/flag_prague
        rgbGen lightingDiffuse
    }
}

models/objects/common/flag/flag_shop
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/flag/flag_shop
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/flag/flag_shop_add
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/common/flag/flag_texas
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/flag/flag_texas
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/flag/flag_texas_add
        blendFunc GL_ONE GL_ONE
    }
}

models/objects/common/flag/flag_colombia
{
	q3map_nolightmap
	q3map_onlyvertexlighting
	cull	disable
    {
        map models/objects/common/flag/flag_colombia
        rgbGen lightingDiffuse
    }
}

models/characters/bolt_ons/toolkit
{
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/characters/bolt_ons/toolkit
        rgbGen lightingDiffuse
    }
}

models/objects/common/sandbag_corner_rmg_bsp
{
	qer_editorimage	models/objects/common/sandbag_corner
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {

        map models/objects/common/sandbag_corner
        rgbGen lightingDiffuse
    }
}

models/objects/common/sandbag_end_rmg_bsp
{
	qer_editorimage	models/objects/common/sandbag_end
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {

        map models/objects/common/sandbag_end
        rgbGen lightingDiffuse
    }
}

models/objects/common/sandbag_section_rmg_bsp
{
	qer_editorimage	models/objects/common/sandbag_section
	q3map_material	Sand
	q3map_nolightmap
	q3map_onlyvertexlighting
    {

        map models/objects/common/sandbag_section
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/body_rmg_bsp
{
	qer_editorimage	models/objects/common/forklift/body
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/body
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/box_rmg_bsp
{
	qer_editorimage	models/objects/common/forklift/box
	q3map_material	HollowWood
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/box
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/cage_forks_rmg_bsp
{
	qer_editorimage	models/objects/common/forklift/cage_forks
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/cage_forks
        rgbGen lightingDiffuse
    }
}

models/objects/common/forklift/cage_top_shd_rmg_bsp
{
	qer_editorimage	models/objects/common/forklift/cage_top_shd
	q3map_material	SolidMetal
	q3map_nolightmap
	q3map_onlyvertexlighting
    {
        map models/objects/common/forklift/cage_top_shd
        alphaFunc GE128
        blendFunc GL_SRC_ALPHA GL_ZERO
        depthWrite
        rgbGen lightingDiffuse
    }
    {
        map models/objects/common/forklift/cage_top_shd
        blendFunc GL_DST_COLOR GL_ZERO
        depthFunc equal
        rgbGen lightingDiffuse
    }
}


