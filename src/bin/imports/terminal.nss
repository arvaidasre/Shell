menu(type='*' where=(sel.count or wnd.is_taskbar or wnd.is_edit) title="Terminal" sep=sep.top image=icon.run_with_powershell)
{
	$tip_run_admin=["\xE1A7 Hold SHIFT to run as administrator: " + this.title, tip.warning, 1.0]
	$has_admin=key.shift() or key.rbutton()
	
	item(title="Command Prompt" tip=tip_run_admin admin=has_admin image=inherit cmd-prompt=`/K TITLE Command Prompt &ver& PUSHD "@sel.dir"`)
	item(title="Windows PowerShell" admin=has_admin tip=tip_run_admin image=inherit cmd-ps=`-noexit -command Set-Location -Path '@sel.dir'`)
	item(where=package.exists("WindowsTerminal") title="Windows Terminal" tip=tip_run_admin admin=has_admin image='@package.path("WindowsTerminal")\WindowsTerminal.exe' cmd="wt.exe" arg=`-d "@sel.path\."`)
}
