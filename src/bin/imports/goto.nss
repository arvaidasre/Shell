menu(type='*' where=window.is_taskbar||sel.count mode=mode.multiple title="Go to" sep=sep.both image=icon.open_folder)
{
	menu(title='Folder' image=icon.open_folder)
	{
		item(title='Windows' image=inherit cmd=sys.dir)
		item(title='System' image=inherit cmd=sys.bin)
		item(title='Program Files' image=inherit cmd=sys.prog)
		item(title='Program Files x86' image=inherit cmd=sys.prog32)
		item(title='ProgramData' image=inherit cmd=sys.programdata)
		item(title='Apps' image=inherit cmd='shell:appsfolder')
		item(title='Users' image=inherit cmd=sys.users)
		separator
		item(title='Desktop' image=inherit cmd=user.desktop)
		item(title='Downloads' image=inherit cmd=user.downloads)
		item(title='Pictures' image=inherit cmd=user.pictures)
		item(title='Documents' image=inherit cmd=user.documents)
		item(title='Start menu' image=inherit cmd=user.startmenu)
		item(title='Profile' image=inherit cmd=user.dir)
		item(title='AppData' image=inherit cmd=user.appdata)
		item(title='Temp files' image=inherit cmd=user.temp)
	}
	item(title="Control Panel" image=inherit cmd='shell:::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}')
	item(title='All Control Panel Items' image=inherit cmd='shell:::{ED7BA470-8E54-465E-825C-99712043E01C}')
	item(title="Run" image=inherit cmd='shell:::{2559a1f3-21d7-11d4-bdaf-00c04f60b9f0}')
	menu(where=sys.ver.major >= 10 title="Settings" sep=sep.before image=icon.settings)
	{
		// https://docs.microsoft.com/en-us/windows/uwp/launch-resume/launch-settings-app
		item(title='System' image=inherit cmd='ms-settings:')
		item(title='About' image=inherit cmd='ms-settings:about')
		item(title='System info' image=inherit cmd-line='/K systeminfo')
		item(title='Search' cmd='search-ms:' image=inherit)
		item(title='USB' image=inherit cmd='ms-settings:usb')
		item(title='Windows Update' image=inherit cmd='ms-settings:windowsupdate')
		item(title='Windows Security' image=inherit cmd='ms-settings:windowsdefender')
		menu(title='Apps' image=inherit)
		{
			item(title='Apps & features' image=inherit cmd='ms-settings:appsfeatures')
			item(title='Default apps' image=inherit cmd='ms-settings:defaultapps')
			item(title='Startup' image=inherit cmd='ms-settings:startupapps')
		}
		menu(title='Personalization' image=inherit)
		{
			item(title='Personalization' image=inherit cmd='ms-settings:personalization')
			item(title='Background' image=inherit cmd='ms-settings:personalization-background')
			item(title='Colors' image=inherit cmd='ms-settings:colors')
			item(title='Themes' image=inherit cmd='ms-settings:themes')
			item(title='Taskbar' image=inherit cmd='ms-settings:taskbar')
		}
		menu(title='Network' image=inherit)
		{
			item(title='Status' image=inherit cmd='ms-settings:network-status')
			item(title='Connections' image=inherit cmd='shell:::{7007ACC7-3202-11D1-AAD2-00805FC1270E}')
		}
	}
}
