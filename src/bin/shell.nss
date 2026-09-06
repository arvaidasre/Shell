settings
{
	priority=1
	exclude.where = !process.is_explorer
	showdelay = 150
	// Minimal - like Windows 7: no duplicates, with search
	modify.remove.duplicate=1
	tip.enabled=true
}

import 'imports/theme.nss'
import 'imports/images.nss'

import 'imports/modify.nss'

// Minimal mode - no extra menus, only the system Win7-style menu.
// Search/filter works automatically - just start typing with the menu open.

// Taskbar kept so right-click on the taskbar keeps working
import 'imports/taskbar.nss'
