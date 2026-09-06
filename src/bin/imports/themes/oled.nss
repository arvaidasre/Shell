// OLED black preset - copy this file over imports/theme.nss to use it
theme
{
	name = "modern"
	dark = true
	background
	{
		color = #000
		opacity = 100
		effect = 0
	}
	image.align = 2
	image.color = [auto, color.accent_light2]

	item
	{
		opacity = 50
		radius = 4
		prefix = 0
		padding = [10, 4]
		margin = [4, 2]
	}

	separator
	{
		size = 1
		color = #333
		opacity = 100
		margin = [0, 1]
	}

	border
	{
		enabled = 1
		size = 1
		color = #333
		opacity = 100
		radius = 4
		padding = [0, 2]
	}

	shadow
	{
		enabled = 0
		size = 12
		color = default
		opacity = default
		offset = 8
	}

	font
	{
		name = "Segoe UI Variable Text"
	}

	layout
	{
		popup = -5
	}
}
