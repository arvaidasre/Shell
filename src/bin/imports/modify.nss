// Minimal Win7 style - only hide junk, no grouping into submenus
modify(mode=mode.multiple
	where=this.id(id.restore_previous_versions,id.cast_to_device)
	vis=vis.remove)
