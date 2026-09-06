menu(type='*' where=window.is_taskbar||sel.count mode=mode.multiple title=loc.go_to sep=sep.both image=icon.open_folder)
{
	menu(title=loc.folder image=icon.open_folder)
	{
		item(title=loc.windows image=inherit cmd=sys.dir)
		item(title=loc.system image=inherit cmd=sys.bin)
		item(title=loc.program_files image=inherit cmd=sys.prog)
		item(title=loc.program_files_x86 image=inherit cmd=sys.prog32)
		item(title=loc.programdata image=inherit cmd=sys.programdata)
		item(title=loc.apps image=inherit cmd='shell:appsfolder')
		item(title=loc.users image=inherit cmd=sys.users)
		separator
		item(title=loc.desktop image=inherit cmd=user.desktop)
		item(title=loc.downloads image=inherit cmd=user.downloads)
		item(title=loc.pictures image=inherit cmd=user.pictures)
		item(title=loc.documents image=inherit cmd=user.documents)
		item(title=loc.start_menu image=inherit cmd=user.startmenu)
		item(title=loc.profile image=inherit cmd=user.dir)
		item(title=loc.appdata image=inherit cmd=user.appdata)
		item(title=loc.temp_files image=inherit cmd=user.temp)
	}
	item(title=loc.control_panel image=inherit cmd='shell:::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}')
	item(title=loc.all_control_panel_items image=inherit cmd='shell:::{ED7BA470-8E54-465E-825C-99712043E01C}')
	item(title=loc.run image=inherit cmd='shell:::{2559a1f3-21d7-11d4-bdaf-00c04f60b9f0}')
	menu(where=sys.ver.major >= 10 title=loc.settings sep=sep.before image=icon.settings)
	{
		// https://docs.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app
		item(title=loc.system image=inherit cmd='ms-settings:')
		item(title=loc.about image=inherit cmd='ms-settings:about')
		item(title=loc.system_info image=inherit cmd-line='/K systeminfo')
		item(title=loc.search cmd='search-ms:' image=inherit)
		item(title=loc.usb image=inherit cmd='ms-settings:usb')
		item(title=loc.windows_update image=inherit cmd='ms-settings:windowsupdate')
		item(title=loc.windows_defender image=inherit cmd='ms-settings:windowsdefender')
		menu(title=loc.apps image=inherit)
		{
			item(title=loc.apps_features image=inherit cmd='ms-settings:appsfeatures')
			item(title=loc.default_apps image=inherit cmd='ms-settings:defaultapps')
			item(title=loc.startup image=inherit cmd='ms-settings:startupapps')
		}
		menu(title=loc.personalization image=inherit)
		{
			item(title=loc.personalization image=inherit cmd='ms-settings:personalization')
			item(title=loc.background image=inherit cmd='ms-settings:personalization-background')
			item(title=loc.colors image=inherit cmd='ms-settings:colors')
			item(title=loc.themes image=inherit cmd='ms-settings:themes')
			item(title=loc.taskbar image=inherit cmd='ms-settings:taskbar')
		}
		menu(title=loc.network image=inherit)
		{
			item(title=loc.status image=inherit cmd='ms-settings:network-status')
			item(title=loc.connections image=inherit cmd='shell:::{7007ACC7-3202-11D1-AAD2-00805FC1270E}')
		}
	}
}
