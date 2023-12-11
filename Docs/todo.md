# priority stuff

- check if DryCost and MassScale errors are important
- engine exhaust (can we get rid of IUpdateable?

# Feature Parity

- [ ] audit and implement all stock parts
	part support seems done, but not sure if everything is using exactly the same settings as TS/L
- [ ] audit and fix mod support
- [ ] verify tech unlocks are correct (fuel tanks, etc)
- [ ] Check FSFuelSwitch interaction
- [ ] Check KIS support
- [ ] test with existing companion
- [ ] find a way to remove tweakscale modules from saved craft files when they're not scaled

# Bugs

- [ ] investigate part cost scaling on HECS2
		seems like this is being treated as "science" which becomes cheaper when it's bigger
		all of the probe cores seem to do this, which makes some sense, though the HECS2 also has a lot of battery space
- [ ] scaled engines have a weird inverse scale to their plumes, even when we're not trying to scale anything
		could this be coming from the power curve?  maybe out of range or something? - doesn't seem to be
		different types of particle systems are being scaled differently.
		Could be a bug in unity where it's inverse-scaling something when it shouldn't, because particle systems can be set to not inherit their parents scales
- [ ] investigate craft file from StormCircuit

# Backwards Compatibilty

- [ ] make sure we can load crafts saved with TS/L
	done some limited testing here, it's looking good
- [ ] make sure we can load *saves* with vessels in flight that used TS/L
- [ ] restore hotkey for toggling child attachment just in case people get mad


# Architecture

- [ ] remove explicit setups for stock parts that could be handled by automatic ones
	maybe? would other things break if these aren't set up early enough?
- [ ] Errors due to removing fields from TweakScale module:
		[WRN 18:23:24.910] [TweakScale] No valid member found for DryCost in TweakScale
		[WRN 18:23:24.911] [TweakScale] No valid member found for MassScale in TweakScale
- [ ] create a IRescalable attribute with virtual functions to customize registration and construction
	For example the CrewManifest handler
	Maybe this isn't a big deal..there aren't that many handlers

# New Candy

- [ ] handle stock exhaust particles
		seems like there's already some code to do this, but doesn't work on some engines?
		or the flame particles work, but not smoke
- [ ] implement waterfall support
- [ ] realplume support?
- [ ] docking port support (this is tricky because of node types - needs a custom handler probably)
- [ ] increase crew capacity when scaling up?
- [ ] scale gizmo in editors (hit 5 or a new button next to re-root, create scale gizmo on part)
- [ ] is there a reasonable way to show modified stats in the PAW? Kind of like how B9PS does it
	e.g. engine thrust, etc.
- [ ] maybe some tool to make new items inherit scale? 
		1. global toggle (like scale children) for "inherit scale on attachment" (maybe 3 states - off, absolute, stack (diameter))
		2. when hovering a new part, it will rescale itself based on what it's hovering over.  So if you try to attach a fl-t400 to a rockomax, it magically becomes 2.5m
- [ ] support localization

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
		tsarbon's plane wing changed from stack to free, and it worked.
		pretty sure this is mostly going to work, just need to test loading things that aren't IsFreeScale
- [ ] how exactly does stack_square work with resources?  do they get squared or cubed?
- [ ] check undo after scaling

# won't do

- maybe rename scale.dll to tweakscale.dll (or tweakscale-rescaled.dll - should match the eventual ckan identifier) and add a FOR[Scale] patch for backwards compatibility
		this might be an issue if any mods declare a direct dependency on scale.dll, but I couldn't find any on github
		maybe leave scale.dll where it is and add a placeholder tweakscale-rescaled.dll?  Or just accept that it won't be auto-detected by ckan (this may improve globally later anyway)
- remove concept of "force relative scale" - not really sure what this was even for
		Maybe not - this might be the only way to handle things that aren't in the prefab?
- toggle button or hotkey to disable step behavior on scale slider
	probably not too useful if numeric editing is added
- PAW button to propagate current absolute scale to children
- copy/paste scale values?  
		could be hotkeys for this stuff when in scale tool mode.  
		And a button in the PAW.  For stack sizes, maybe have both "copy absolute scale" and "copy stack size" - possibly swap when alt is held?

# Done

- [x] add 1.875m scaling option for fuel tanks etc
- [x] handle part inventories
- [x] check node altering from B9PS
- [x] exception thrown from patching when B9PS isn't installed.  This isn't a bug, but it looks scary.  need a better way to report this.
		[EXC 14:51:48.160] ArgumentException: Undefined target method for patch method static System.Boolean TweakScale.B9PS_AttachNodeMover_SetAttachNodePosition::Prefix(AttachNode ___attachNode, UnityEngine.Vector3 ___position)
- [x] chain scaling doesn't update the scale factor in the gui for child parts
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
- [x] fix TestFlightCore error1
- [x] fix scale slider dragging (due to hasty refresh?)  was this intentional?
		removing the refresh doesn't fix dragging but it does fix the flickering
- [x] scaled node sizes are not preserved after save/load (because we don't know what "baseline" is when part variants etc are involved)
		might need a dictionary of nodeID -> nodeSize, populated from the prefab and updated when variants are applied?  Could we do the same thing for position?  
- [x] node positions are broken on loading again
	maybe a module is trying to set the unscaled node info before the tweakscale one has copied the dictionary?
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
- [x] remove IUpdater? seems like it's only the particle emitter and that's broken
- [x] add attribute for handling partmodules by name (e.g. ModuleFuelTanks)
		should fix ERR 15:50:18.696] [TweakScale] Part updater TweakScale.ModuleFuelTanksUpdater doesn't have an appropriate constructor
- [x] make a way to dump relevant info of all parts in a way that can be compared, in order to verify configuration changes are safe
- [x] "updaters" should be called "handlers" because "update" connotes something that happens every frame.  Or Rescalable to match the interface name.  RescalableHandler?
- [x] make chain scaling a toggle in the PAW
- [x] write currentScale and defaultScale keys in OnSave in an attempt to provide interoperability
- [x] Make sure all patches are in the FOR[TweakScale] pass (and make sure that other mods are OK with this)
		This is definitely conceptually correct, but seems pretty dangerous in terms of compatibility and could cause more problems than it solves
		blanket patches might need to be in LAST[TweakScale], considering that some mods might add modules in FOR passes of their own
		for example: https://github.com/KSP-RO/RealismOverhaul/blob/32ab62ccbde3600b6c22c5bd78d1161ef1f5c08e/GameData/RealismOverhaul/REWORK/RO_NovaPunch_Misc.cfg#L25
		This will need some rethinking...
- [x] bring back scale interval (or not? analog seems fine, but need to fix the slider dragging or add numeric entry)
- [x] clicking >> after hitting the max interval screws up the slider
		this may be due to the workaround in ScaleType that mentions a bug - I tried remove the workaround and the behaviour was way worse
		Do we need to use harmony to patch the UI code?
- [x] dragging the slider with the mouse often gets interrupted
	not sure what caused this but it doesn't happen anymore
- [x] See if we need to include the TweakableEverything updaters
		this really doesn't seem to be necessary.  they're derived modules from stock which should get updated by normal exponents
		it really seems like these could just be cfg patches?
- [x] numeric entry in PAW
- [x] dragging the scale slider around doesn't update the resources in the PAW
- [x] validation system (unfortunately I think this is required if I have to break out the "risk to users" line)
- [x] format everything with tabs and add .editorconfig
- [x] Check B9PS mass changing interactions
- [x] (from kurgut): when using TS on some cryo tanks (and others mods I don't remember rn), the fuel volume gets messed up completely, there is workaround in VAB by copy pasta or whatever, but it's really annoying and barely playable.
- [x] fl-t400 tank has 2 "type" entries in its cfg
- [x] Check RealFuels support (tanks and engines)
- [x] investigate modular fuel tanks
	mass seems off (realfuels too)
- [x] put scale stuff in a PAW group?

======

How to handle nodes changing positions?

Basic approach: we need to know what the unscaled position and size of each attach node should be.  Then we can directly calculate the scaled versions using absolute scaling.

The TweakScale partmodule maintains a dictionary mapping attachnode id to position and size.  There are harmony patches in the stock and b9ps code that updates this dictionary as the variants are changed