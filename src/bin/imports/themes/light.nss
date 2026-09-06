// Classic light preset - copy this file over imports/theme.nss to use it
theme
{
	name = "modern"
	dark = false
	background
	{
		color = #f9f9f9
		opacity = 100
		effect = 0
	}
	image.align = 2
	image.color = [auto, color.accent]

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
		color = #e0e0e0
		opacity = 100
		margin = [0, 1]
	}

	border
	{
		enabled = 1
		size = 1
		color = #000
		opacity = 12
		radius = 4
		padding = [0, 2]
	}

	shadow
	{
		enabled = 1
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
