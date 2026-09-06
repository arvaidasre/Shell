menu(where=sel.count>0 type='file|dir|drive|namespace|back' mode="multiple" title=loc.file_manage image=icon.copy_as_path)
{
	menu(separator="after" title=loc.copy_path image=icon.copy_path)
	{
		item(where=sel.count > 1 title=loc.copy_multiple_paths cmd=command.copy(sel(false, "\n")))
		item(mode="single" title=@sel.path tip=sel.path cmd=command.copy(sel.path))
		item(mode="single" type='file' separator="before" find='.lnk' title=loc.open_file_location)
		separator
		item(mode="single" where=@sel.parent.len>3 title=sel.parent cmd=@command.copy(sel.parent))
		separator
		item(mode="single" type='file|dir|back.dir' title=sel.file.name cmd=command.copy(sel.file.name))
		item(mode="single" type='file' where=sel.file.len != sel.file.title.len title=@sel.file.title cmd=command.copy(sel.file.title))
		item(mode="single" type='file' where=sel.file.ext.len>0 title=sel.file.ext cmd=command.copy(sel.file.ext))
	}

	item(mode="single" type="file" title=loc.change_extension image=icon.rename cmd=if(input(loc.change_extension, loc.enter_extension),
		io.rename(sel.path, path.join(sel.dir, sel.file.title + "." + input.result))))

	menu(separator="after" image=icon.select_all title=loc.selection)
	{
		item(title=loc.select_all image=icon.select_all cmd=command.select_all)
		item(title=loc.invert_selection image=icon.invert_selection cmd=command.invert_selection)
		item(title=loc.deselect image=icon.select_none cmd=command.select_none)
	}

	item(type='file|dir|back.dir|drive' title=loc.take_ownership image=icon.run_as_administrator admin
		cmd args='/K takeown /f "@sel.path" @if(sel.type==1,null,"/r /d y") && icacls "@sel.path" /grant *S-1-5-32-544:F @if(sel.type==1,"/c /l","/t /c /l /q")')
	separator
	menu(title=loc.show_hide image=icon.view)
	{
		item(title=loc.system_files image=inherit cmd='@command.togglehidden')
		item(title=loc.file_name_extensions image=inherit cmd='@command.toggleext')
	}

	menu(type='file|dir|back.dir' mode="single" title=loc.attributes image=icon.properties)
	{
		$atrr = io.attributes(sel.path)
		item(title=loc.hidden checked=io.attribute.hidden(atrr)
			cmd args='/c ATTRIB @if(io.attribute.hidden(atrr),"-","+")H "@sel.path"' window=hidden)

		item(title=loc.system checked=io.attribute.system(atrr)
			cmd args='/c ATTRIB @if(io.attribute.system(atrr),"-","+")S "@sel.path"' window=hidden)

		item(title=loc.readonly checked=io.attribute.readonly(atrr)
			cmd args='/c ATTRIB @if(io.attribute.readonly(atrr),"-","+")R "@sel.path"' window=hidden)

		item(title=loc.archive checked=io.attribute.archive(atrr)
			cmd args='/c ATTRIB @if(io.attribute.archive(atrr),"-","+")A "@sel.path"' window=hidden)
		separator
		item(title=loc.created keys=io.dt.created(sel.path, 'y/m/d') cmd=io.dt.created(sel.path,2000,1,1))
		item(title=loc.modified keys=io.dt.modified(sel.path, 'y/m/d') cmd=io.dt.modified(sel.path,2000,1,1))
		item(title=loc.accessed keys=io.dt.accessed(sel.path, 'y/m/d') cmd=io.dt.accessed(sel.path,2000,1,1))
	}

	menu(mode="single" type='file' find='.dll|.ocx' separator="before" title=loc.register_server image=icon.settings)
	{
		item(title=loc.register admin cmd='regsvr32.exe' args='@sel.path.quote' invoke="multiple")
		item(title=loc.unregister admin cmd='regsvr32.exe' args='/u @sel.path.quote' invoke="multiple")
	}

	menu(mode="single" type='back' expanded=true)
	{
		menu(separator="before" title=loc.new_folder image=icon.new_folder)
		{
			item(title=loc.datetime cmd=io.dir.create(sys.datetime("ymdHMSs")))
			item(title=loc.guid cmd=io.dir.create(str.guid))
		}

		menu(title=loc.new_file image=icon.new_file)
		{
			$dt = sys.datetime("ymdHMSs")
			item(title='TXT' cmd=io.file.create('@(dt).txt', 'Hello World!'))
			item(title='XML' cmd=io.file.create('@(dt).xml', '<root>Hello World!</root>'))
			item(title='JSON' cmd=io.file.create('@(dt).json', '[]'))
			item(title='HTML' cmd=io.file.create('@(dt).html', "<html>\n\t<head>\n\t</head>\n\t<body>Hello World!\n\t</body>\n</html>"))
		}
	}

	item(where=!wnd.is_desktop title=loc.folder_options image=icon.folder_options cmd=command.folder_options)
}
