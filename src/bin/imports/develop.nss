menu(mode="multiple" title='Development' vis=key.shift() sep=sep.bottom image=icon.open_with)
{
	menu(mode="single" title='Editors' image=icon.open_file_location)
	{
		item(title='Visual Studio Code' image=inherit cmd='code' args='"@sel.path"')
		separator
		item(type='file' mode="single" title='Notepad' image=inherit cmd='@sys.bin\notepad.exe' args='"@sel.path"')
	}

	menu(mode="multiple" title='dotnet' image=icon.run_with_powershell)
	{
		item(title='run' cmd-line='/K dotnet run' image=inherit)
		item(title='watch' cmd-line='/K dotnet watch')
		item(title='clean' image=inherit cmd-line='/K dotnet clean')
		separator
		item(title='build (debug)' cmd-line='/K dotnet build')
		item(title='build (release)' cmd-line='/K dotnet build -c release /p:DebugType=None')

		menu(mode="multiple" sep=sep.both title='publish' image=icon.share)
		{
			$publish='dotnet publish -r win-x64 -c release --output publish /*/p:CopyOutputSymbolsToPublishDirectory=false*/'
			item(title='publish single file' sep=sep.bottom cmd-line='/K @publish --no-self-contained /p:PublishSingleFile=true')
			item(title='framework-dependent' cmd-line='/K @publish')
			item(title='self-contained' cmd-line='/K @publish --self-contained true')
			item(title='single file' cmd-line='/K @publish /p:PublishSingleFile=true /p:PublishTrimmed=false')
		}
		
		item(title='ef migration InitialCreate' cmd-line='/K dotnet ef migrations add InitialCreate')
		item(title='ef database update' cmd-line='/K dotnet ef database update')
		separator
		item(title='help' image=inherit cmd-line='/k dotnet -h')
		item(title='version' image=inherit cmd-line='/k dotnet --info')
	}
}
