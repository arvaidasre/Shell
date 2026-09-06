menu(mode="multiple" title=loc.development vis=key.shift() sep=sep.bottom image=icon.open_with)
{
	menu(mode="single" title=loc.editors image=icon.open_file_location)
	{
		item(title='Visual Studio Code' image=inherit cmd='code' args='"@sel.path"')
		separator
		item(type='file' mode="single" title=loc.windows_notepad image=inherit cmd='@sys.bin\notepad.exe' args='"@sel.path"')
	}

	menu(mode="multiple" title='dotnet' image=icon.run_with_powershell)
	{
		item(title=loc.dotnet_run cmd-line='/K dotnet run' image=inherit)
		item(title=loc.dotnet_watch cmd-line='/K dotnet watch')
		item(title=loc.dotnet_clean image=inherit cmd-line='/K dotnet clean')
		separator
		item(title=loc.dotnet_build_debug cmd-line='/K dotnet build')
		item(title=loc.dotnet_build_release cmd-line='/K dotnet build -c release /p:DebugType=None')

		menu(mode="multiple" sep=sep.both title=loc.publish image=icon.share)
		{
			$publish='dotnet publish -r win-x64 -c release --output publish /*/p:CopyOutputSymbolsToPublishDirectory=false*/'
			item(title=loc.publish_single_file sep=sep.bottom cmd-line='/K @publish --no-self-contained /p:PublishSingleFile=true')
			item(title=loc.framework_dependent cmd-line='/K @publish')
			item(title=loc.self_contained cmd-line='/K @publish --self-contained true')
			item(title=loc.single_file cmd-line='/K @publish /p:PublishSingleFile=true /p:PublishTrimmed=false')
		}

		item(title=loc.ef_migrations_add cmd-line='/K dotnet ef migrations add InitialCreate')
		item(title=loc.ef_database_update cmd-line='/K dotnet ef database update')
		separator
		item(title=loc.dotnet_help image=inherit cmd-line='/k dotnet -h')
		item(title=loc.dotnet_version image=inherit cmd-line='/k dotnet --info')
	}
}
