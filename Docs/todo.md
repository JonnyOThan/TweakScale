# priority stuff

- add attribute for scale handlers to bind to a module by name (ModuleFuelTanks)
- engine exhaust (can we get rid of IUpdateable?

# Feature Parity

- [ ] audit and implement all stock parts
	part support seems done, but not sure if everything is using exactly the same settings as TS/L
- [ ] audit and fix mod support
- [ ] verify tech unlocks are correct (fuel tanks, etc)
- [ ] Check ModuleFuelTanks interaction (realfuels)
- [ ] Check FSFuelSwitch interaction
- [ ] Check B9PS mass changing interactions
- [ ] check node altering from B9PS
- [ ] bring back scale interval (or not? analog seems fine, but need to fix the slider dragging or add numeric entry)
- [x] add 1.875m scaling option for fuel tanks etc
- [x] handle part inventories

# Verification (do this last, except to generate new bugs)

- [ ] check stock twin boar (since it's an engine + fuel tank)
- [ ] check parachutes
- [ ] check part recovery costs (with kspcf)
- [ ] check cloning part subtrees
- [ ] check FS buoyancy module
- [ ] check parts that modify drag cubes
- [ ] make sure save/load works
- [ ] make sure subassemblies/merging works
- [ ] make sure scaling command parts w/ kerbals works properly re: mass
- [ ] find all TODOs and make sure there are issues tracked if necessary
- [ ] Make sure switching a part's scale type doesn't break it
- [ ] how exactly does stack_square work with resources?  do they get squared or cubed?
- [ ] check undo after scaling

# Bugs

- [ ] investigate part cost scaling on HECS2
		seems like this is being treated as "science" which becomes cheaper when it's bigger
		all of the probe cores seem to do this, which makes some sense, though the HECS2 also has a lot of battery space
- [ ] scaled node sizes are not preserved after save/load (because we don't know what "baseline" is when part variants etc are involved)
		might need a dictionary of nodeID -> nodeSize, populated from the prefab and updated when variants are applied?  Could we do the same thing for position?  
- [ ] fix scale slider dragging (due to hasty refresh?)  was this intentional?
- [ ] clicking >> after hitting the max interval screws up the slider
- [ ] chain scaling doesn't update the scale factor in the gui for child parts
- [x] the builtin IRescalables don't seem to be handled properly, e.g.
	[ERR 23:56:14.016] [TweakScale] Found an IRescalable type TweakScale.CrewManifestUpdater but don't know what to do with it
- [x] fuel tank cost is going negative when scaled up
- [x] fix part variant node handling (structural tubes from MH)
- [x] attached parts will move after saving them once
		start a new craft, place a fuel tank, scale it up, attach something to it, save, load
		does not occur if you then reattach the part and save/load
- [x] changing variant and then altering scale will screw up nodes
- [x] check part variants (structural tubes from MH)
		Really this was tweakscale not respecting any part or mass modifiers
- [x] scaling up a fuel tank, saving it, then loading will increase its resources
		the resources etc are saved in the protovessel, so when we try to apply the scale again it's not based on an unscaled part
- [x] node positions on scaled parts get reverted after loading
- [x] fix TestFlightCore error


# Backwards Compatibilty

- [ ] make sure we can load crafts saved with TS/L
- [ ] make sure we can load *saves* with vessels in flight that used TS/L

# Architecture

- [ ] Make sure all patches are in the FOR[TweakScale] pass (and make sure that other mods are OK with this)
		blanket patches might need to be in LAST[TweakScale] ?
- [ ] format everything with tabs and add .editorconfig
- [ ] remove explicit setups for stock parts that could be handled by automatic ones (and find a way to verify that they're the same)
- [ ] remove IUpdater? seems like it's only the particle emitter and that's broken
- [ ] add attribute for handling partmodules by name (e.g. ModuleFuelTanks)
- [ ] change manual registration to an attribute
- [ ] See if we need to include the TweakableEverything updaters
		it really seems like these could just be cfg patches?
- [ ] Errors due to removing fields from TweakScale module:
		[WRN 18:23:24.910] [TweakScale] No valid member found for DryCost in TweakScale
		[WRN 18:23:24.911] [TweakScale] No valid member found for MassScale in TweakScale
- [x] figure out why it's doing 2 passes over updaters
- [x] find out what mods if any are using IUpdater's OnUpdate call, and see if they need to be split into editor and flight versions
	this interface is internal, and it doesnt' look like there's any references to it on github
- [x] Refactor Updaters into separate files
- [x] Make all scaling absolute, and store only the raw scale factor.  Scale presets should just be a visual editor-only concept
- [x] move crew, mft, testflight and antenna modifications into modular system
- [x] Make it possible to change between free scale to stack scale (there's a lot of stuff set to free that should be stack)
- [x] rename scale_redist to 999_scale_redist and get deployment set up
- [x] See if there's a way to get rid of the flow-controlling execptions (more attributes?  looking up functions by reflection?)
- [x] addon binder reports a missing Scale_Redist dll because of load order - not a big deal, but in the interest of reducing noise should probably be addressed
		actually, could this be solved with adding KSPAssembly on ScaleRedist and a KSPAssemblyDependency on TweakScale?
		KSPAssembly is a good idea anyway because we need to update the version number so that mods can differentiate
- [x] add priority value to IRescalable
- [x] move scale chaining hotkey handling out of the partmodule and into something global
		probably remove the entire hotkey system?
- [x] remove settings xml stuff (this is only used for chaing scaling setting)

# New Candy

- [ ] handle stock exhaust particles
		seems like there's already some code to do this, but doesn't work on some engines?
		or the flame particles work, but not smoke
- [ ] implement waterfall support
- [ ] realplume support?
- [ ] docking port support (this is tricky because of node types - needs a custom handler probably)
- [ ] numeric entry in PAW
- [ ] increase crew capacity when scaling up?
- [x] make chain scaling a toggle in the PAW

# won't do

- maybe rename scale.dll to tweakscale.dll (or tweakscale-rescaled.dll - should match the eventual ckan identifier) and add a FOR[Scale] patch for backwards compatibility
		this might be an issue if any mods declare a direct dependency on scale.dll, but I couldn't find any on github
		maybe leave scale.dll where it is and add a placeholder tweakscale-rescaled.dll?  Or just accept that it won't be auto-detected by ckan (this may improve globally later anyway)
- remove concept of "force relative scale" - not really sure what this was even for
		Maybe not - this might be the only way to handle things that aren't in the prefab?
- 
======

How to handle nodes changing positions?

Node positions are saved in the protovessel, so on loading there's nothing to be done.  But we don't have a good way to
figure out what the original node position was, so absolute scaling is difficult to use.

I can trap all the places where the stock game changes them, but what about mods?  B9PS?  Any others?

I don't really want to have to save off the last positions and poll to see if they changed every frame.

For now, using relative scaling seems to work..but still need to test b9ps.  But that can surely be handled with harmony