# Feature Parity

- [ ] audit and implement all stock parts
	part support seems done, but not sure if everything is using exactly the same settings as TS/L
- [ ] audit and fix mod support
- [ ] verify tech unlocks are correct (fuel tanks, etc)
- [ ] Check ModuleFuelTanks interaction (realfuels)
- [ ] Check FSFuelSwitch interaction
- [ ] Check B9PS mass changing interactions
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

# Bugs

- [ ] investigate part cost scaling on HECS2
		seems like this is being treated as "science" which becomes cheaper when it's bigger
		all of the probe cores seem to do this, which makes some sense, though the HECS2 also has a lot of battery space
- [ ] check node altering from B9PS
- [ ] scaled node sizes are not preserved after save/load
- [ ] fix scale slider dragging (due to hasty refresh?)
- [ ] clicking >> after hitting the max interval screws up the slider
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

- [ ] remove concept of "force relative scale" - not really sure what this was even for
- [ ] Make it possible to change between free scale to stack scale (there's a lot of stuff set to free that should be stack)
	this may be done, but need to test it
- [ ] Figure out what scale_redist is and how it's used by other mods (KSPIE, etc).  Do we need to name it 999_scale_redist?
		seems like other mods can register for callbacks when scale changes, and this DLL just contains the API for that
- [ ] Make sure all patches are in the FOR[TweakScale] pass (and make sure that other mods are OK with this)
		blanket patches might need to be in LAST[TweakScale] ?
- [ ] format everything with tabs and add .editorconfig
- [ ] remove explicit setups for stock parts that could be handled by automatic ones (and find a way to verify that they're the same)
- [ ] add priority value to IRescalable (this should help with the double pass)
- [ ] maybe rename scale.dll to tweakscale.dll (or tweakscale-rescaled.dll - should match ckan identifier) and add a FOR[Scale] patch for backwards compatibility
- [ ] addon binder reports a missing Scale_Redist dll because of load order - not a big deal, but in the interest of reducing noise should probably be addressed
- [ ] remove IUpdater? seems like it's only the particle emitter and that's broken
- [ ] move scale chaining hotkey handling out of the partmodule and into something global
- [x] figure out why it's doing 2 passes over updaters
- [x] find out what mods if any are using IUpdater's OnUpdate call, and see if they need to be split into editor and flight versions
	this interface is internal, and it doesnt' look like there's any references to it on github
- [x] Refactor Updaters into separate files
- [x] Make all scaling absolute, and store only the raw scale factor.  Scale presets should just be a visual editor-only concept
- [x] move crew, mft, testflight and antenna modifications into modular system


# New Candy

- [ ] handle stock exhaust particles
		seems like there's already some code to do this, but doesn't work on some engines?
		or the flame particles work, but not smoke
- [ ] implement waterfall support
- [ ] realplume support?
- [ ] docking port support
- [ ] make chain scaling a toggle in the PAW


======

How to handle nodes changing positions?

Node positions are saved in the protovessel, so on loading there's nothing to be done.  But we don't have a good way to
figure out what the original node position was, so absolute scaling is difficult to use.

I can trap all the places where the stock game changes them, but what about mods?  B9PS?  Any others?

I don't really want to have to save off the last positions and poll to see if they changed every frame.