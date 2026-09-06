menu(type='*' where=(sel.count or wnd.is_taskbar or wnd.is_edit) title=loc.terminal sep=sep.top image=icon.run_with_powershell)
{
	$tip_run_admin=["\xE1A7 " + loc.run_as_admin_tip + this.title, tip.warning, 1.0]
	$has_admin=key.shift() or key.rbutton()

	item(title=loc.command_prompt tip=tip_run_admin admin=has_admin image=inherit cmd-prompt=`/K TITLE Command Prompt &ver& PUSHD "@sel.dir"`)
	item(title="Windows PowerShell" admin=has_admin tip=tip_run_admin image=inherit cmd-ps=`-noexit -command Set-Location -Path '@sel.dir'`)
	item(where=package.exists("WindowsTerminal") title=loc.windows_terminal tip=tip_run_admin admin=has_admin image='@package.path("WindowsTerminal")\WindowsTerminal.exe' cmd="wt.exe" arg=`-d "@sel.path\."`)
}
