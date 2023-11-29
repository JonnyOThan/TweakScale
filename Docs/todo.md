# Feature Parity

- [ ] audit and implement all stock parts
- [ ] audit and fix mod support
- [ ] make sure save/load works
- [ ] make sure subassemblies/merging works
- [ ] verify tech unlocks are correct (fuel tanks, etc)
- [ ] Check ModuleFuelTanks interaction (realfuels)
- [ ] Check FSFuelSwitch interaction
- [ ] Check B9PS mass changing interactions
- [ ] bring back scale interval
- [x] fix TestFlightCore error
- [x] add 1.875m scaling option for fuel tanks etc
- [x] handle part inventories

# Verification

- [ ] check stock twin boar (since it's an engine + fuel tank)
- [ ] check parachutes
- [ ] check part recovery costs (with kspcf)
- [ ] check cloning part subtreess
- [ ] check FS buoyancy module
- [ ] check parts that modify drag cubes

# Bugs

- [ ] investigate part cost scaling on HECS2
		seems like this is being treated as "science" which becomes cheaper when it's bigger
		all of the probe cores seem to do this, which makes some sense, though the HECS2 also has a lot of battery space
- [ ] fix part variant node handling (structural tubes from MH)
- [ ] check node altering from B9PS
- [ ] node positions on scaled parts get reverted after loading
- [x] check part variants (structural tubes from MH)
		Really this was tweakscale not respecting any part or mass modifiers

# Backwards Compatibilty

- [ ] make sure we can load crafts saved with TS/L
- [ ] make sure we can load *saves* with vessels in flight that used TS/L

# Architecture

- [ ] Make it possible to change between free scale to stack scale (there's a lot of stuff set to free that should be stack)
- [ ] Figure out what scale_redist is and how it's used by other mods (KSPIE, etc).  Do we need to name it 999_scale_redist?
		seems like other mods can register for callbacks when scale changes, and this DLL just contains the API for that
- [ ] Make sure all patches are in the FOR[TweakScale] pass (and make sure that other mods are OK with this)
- [ ] format everything with tabs and add .editorconfig
- [ ] remove explicit setups for stock parts that could be handled by automatic ones (and find a way to verify that they're the same)
- [ ] move crew, mft, testflight and antenna modifications into modular system
- [ ] figure out why it's doing 2 passes over updaters
- [ ] find out what mods if any are using IUpdater's OnUpdate call, and see if they need to be split into editor and flight versions
- [ ] Refactor Updaters into separate files
- [x] Make all scaling absolute, and store only the raw scale factor.  Scale presets should just be a visual editor-only concept


# New Candy

- [ ] handle stock exhaust particles
		seems like there's already some code to do this, but doesn't work on some engines?
- [ ] implement waterfall support
- [ ] docking port support
- [ ] make chain scaling a toggle in the PAW