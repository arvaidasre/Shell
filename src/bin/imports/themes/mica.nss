// Windows 11 Mica preset - copy this file over imports/theme.nss to use it
theme
{
	name = "modern"
	dark = sys.dark
	background
	{
		color = default
		opacity = 85
		effect = 4
	}
	image.align = 2
	image.color = theme.islight ? [auto, color.accent] : [auto, color.accent_light2]

	item
	{
		opacity = 50
		radius = 8
		prefix = 0
		padding = [10, 4]
		margin = [4, 2]
	}

	separator
	{
		size = 1
		color = default
		opacity = default
		margin = [0, 1]
	}

	border
	{
		enabled = 1
		size = 1
		color = #000
		opacity = theme.islight ? 12 : 25
		radius = 8
		padding = [0, 2]
	}

	shadow
	{
		enabled = 1
		size = 16
		color = default
		opacity = theme.islight ? default : 15
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
