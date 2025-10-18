// skyparms work like this:

// q3map_sun <red> <green> <blue> <intensity> <degrees> <elevation>

// color will be normalized, so it doesn't matter what range you use

// intensity falls off with angle but not distance 100 is a fairly bright sun

// degree of 0 = from the east, 90 = north, etc.  altitude of 0 = sunrise/set, 90 = noon

textures/skies/air5
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	100
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 130 0 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/air5 512 -
}

textures/skies/air4
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/air5 512 -
}

textures/skies/air3
{
// * q3map_lightimage }

	q3map_lightimage	textures/colors/yellow_light
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 1 0.992157 0.65098 130 46 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/air3 512 -
}

textures/skies/air3_nosun
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/air3 512 -
}

textures/skies/arm1
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.2 0.2 0.26 60 0 65
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/nynight 0 -
}

textures/skies/cem1
{
// * q3map_lightimage }

	lightcolor	( 0.254902 0.270588 0.313726 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	70
	q3map_lightsubdivide	512
	sun 0.8 0.8 0.8 300 0 50
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/cemetary 0 -
}

textures/skies/col
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	100
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 130 0 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col 512 -
}

textures/skies/col1
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	10
	sun 0.75 0.79 1 130 0 75
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

textures/skies/col2
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	40
	sun 1 0.615686 0.356863 175 225 45
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col2 512 -
}

textures/skies/col3
{
// surfaceparm	nodraw

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	65
	sun 1 0.6156 0.358 175 0 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col3 512 -
}

textures/skies/col4
{
// surfaceparm	nodraw

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	90
	q3map_lightsubdivide	512
	sun 0.8 0.9 0.9 200 0 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col4 512 -
}

textures/skies/col6
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	80
	sun 0.75 0.79 1 130 5 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

textures/skies/col6_br
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	65
	q3map_lightsubdivide	256
	sun 0.75 0.79 1 100 45 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

textures/skies/col6_chop2048
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	50
	q3map_lightsubdivide	4096
	sun 0.96 0.843 0.505 65 340 25
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

textures/skies/col9
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	90
	q3map_lightsubdivide	512
	sun 0.79 0.75 0.8 100 0 35
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

textures/skies/col10
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	100
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 130 0 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col10 512 -
}

textures/skies/finca1
{
// * q3map_lightimage }

	q3map_lightimage	textures/colors/grey_bath
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	6
	q3map_lightsubdivide	512
	sun 0.623529 0.721569 1 10 120 50
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/arm1 256 -
}

textures/skies/hk2
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	22
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 62 135 20
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 0 -
}

textures/skies/hk3
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 85 135 30
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 0 -
}

textures/skies/hk4
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 85 135 60
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 0 -
}

textures/skies/hk5
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 85 135 80
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 0 -
}

textures/skies/hk7
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 0 0 0
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 0 -
}

textures/skies/jor1
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	1000
	q3map_lightsubdivide	512
	sun 1 1 0.75 140 0 90
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/jor1 512 -
}

textures/skies/jor3
{
// * q3map_lightimage }

	qer_editorimage	textures/colors/blue_light
	q3map_surfacelight	35
	sun 0.75 0.79 1 130 0 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/jor1 512 -
}

textures/skies/jor4
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	100
	q3map_lightsubdivide	512
	sun 0.7 0.6 0.5 5 35 120
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 256 -
}

textures/skies/pra6
{
// * q3map_lightimage }

	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.47 0.57 1 60 0 65
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/pra6 256 -
}

textures/skies/sam
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	100
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 140 0 90
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/sam 512 -
}

textures/skies/shop
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 0 0 0
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/rmg_skies/cld_morn_mts 512 -
}

textures/skies/shoptemp
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_lightsubdivide	512
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/sam 512 -
}

textures/skies/shoptemp2
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	55
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 30 0 70
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/sam 512 -
}

textures/skies/finca1a
{
// * q3map_lightimage }

	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.55 0.59 1 50 315 80
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/arm1 256 -
}

textures/skies/pra5
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	60
	sun 0.74902 0.788235 1 200 5 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	- 512 -
}

textures/skies/hk4temp
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 105 135 60
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/hk1 512 -
}

textures/skies/dm_hk
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	64
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 96 330 60
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/hk1 0 -
}

textures/skies/hk1
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 85 315 60
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/hk1 512 -
}

textures/skies/dm_pra
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	30
	q3map_lightsubdivide	512
	sun 0.4752 0.5785 1 60 315 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/nynight 0 -
}

textures/skies/dm_col1
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	80
	sun 0.67451 0.87451 0.992157 384 270 30
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col10 512 -
}

textures/skies/hossky_night
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/colors/blue_light
	q3map_surfacelight	10
	sun 0.75 0.79 1 50 46 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	q3map_nolightmap
	skyParms	- 512 -
}

textures/skies/hos3_sky
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	10
	sun 0.75 0.79 1 50 46 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/nynight 512 -
}

textures/skies/dm_col1b
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col10 512 -
}

textures/skies/dm_hos1
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	10
	sun 0.75 0.79 1 64 46 40
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/nynight 1024 -
}

// skyparms work like this:

// q3map_sun <red> <green> <blue> <intensity> <degrees> <elevation>

// color will be normalized, so it doesn't matter what range you use

// intensity falls off with angle but not distance 100 is a fairly bright sun

// degree of 0 = from the east, 90 = north, etc.  altitude of 0 = sunrise/set, 90 = noon

textures/skies/raven
{
// * q3map_lightimage }

	qer_editorimage	textures/tools/editor_images/qer_sky
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/air5 512 -
}

textures/skies/inf
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	sun 0.2 0.2 0.258824 384 270 30
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/arm1 512 -
}

textures/skies/tut1
{
	lightcolor	( 0.584314 0.580392 0.466667 )
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	55
	q3map_lightsubdivide	512
	sun 0.996078 0.988235 0.854902 200 0 80
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/sam 512 -
}

textures/skies/col8
{
// surfaceparm	nodraw

	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	55
	sun 0.75 0.79 1 256 5 35
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col8 512 -
}

textures/skies/new_hk2
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	22
	q3map_lightsubdivide	512
	sun 0.75 0.79 1 62 135 20
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/hk2 512 -
}

textures/skies/col2_jersey
{
	qer_editorimage	textures/tools/editor_images/qer_sky
	q3map_surfacelight	55
	sun 1 0.615686 0.356863 150 190 55
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/col2 512 -
}

textures/skies/inf_shop
{
	q3map_lightimage	textures/colors/blue_light
	qer_editorimage	textures/tools/editor_images/qer_sky
	surfaceparm	sky
	surfaceparm	noimpact
	surfaceparm	nomarks
	notc
	q3map_nolightmap
	skyParms	textures/skies/nynight 1024 -
}

