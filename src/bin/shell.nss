settings
{
	priority=1
	exclude.where = !process.is_explorer
	showdelay = 150
	// Minimal - like Windows 7: no duplicates, with search
	modify.remove.duplicate=1
	tip.enabled=true
}

// ===== Language / Kalba / Язык =====
// Choose the menu language: "en" (English), "lt" (Lietuvių), "ru" (Русский).
// Can you help translate Shell into your language? Open an issue on GitHub.
$ui_lang = "en"

$loc_path='imports\lang\'
import lang loc_path + "en.nss"
// path.exists is CWD-relative (install dir), but the import below
// resolves in the config dir - so check the absolute config path.
$loc_abs = path.parent(app.cfg) + '\imports\lang\'
import lang if(path.exists(loc_abs + ui_lang + ".nss"),
               loc_path + ui_lang + ".nss",
               loc_path + "en.nss")

import 'imports/theme.nss'
import 'imports/images.nss'
// Theme presets (mica, oled, light) live in imports/themes/ -
// copy one over imports/theme.nss to use it.

import 'imports/modify.nss'

// Minimal mode - no extra menus, only the system Win7-style menu.
// Search/filter works automatically - just start typing with the menu open.
// Optional menus (Go to, Terminal, Development, File management) ship in
// imports/ - add an import line below to enable them, e.g.:
// import 'imports/goto.nss'

// Taskbar kept so right-click on the taskbar keeps working
import 'imports/taskbar.nss'
