# Feature Parity

- [ ] audit and implement all stock parts
- [ ] audit and fix mod support
- [ ] make sure save/load works
- [ ] make sure subassemblies/merging works
- [x] fix TestFlightCore error
- [x] add 1.875m scaling option for fuel tanks etc
- [x] handle part inventories

# Verification

- [ ] check stock twin boar
- [ ] check parachutes
- [ ] check part recovery costs (with kspcf)
- [ ] check cloning part subtreess

# Bugs

- [ ] investigate part cost scaling on HECS2
		seems like this is being treated as "science" which becomes cheaper when it's bigger
		all of the probe cores seem to do this, which makes some sense, though the HECS2 also has a lot of battery space
- [ ] fix part variant node handling (structural tubes from MH)
- [ ] check node altering from B9PS
- [x] check part variants (structural tubes from MH)
		Really this was tweakscale not respecting any part or mass modifiers

# Backwards Compatibilty

- [ ] make sure we can load crafts saved with TS/L
- [ ] make sure we can load *saves* with vessels in flight that used TS/L

# Architecture

- [ ] Make it possible to change between free scale to stack scale (there's a lot of stuff set to free that should be stack)
- [ ] Make all scaling absolute, and store only the raw scale factor.  Scale presets should just be a visual editor-only concept
- [ ] Figure out what scale_redist is and how it's used by other mods (KSPIE, etc).  Do we need to name it 999_scale_redist?

# New Candy

- [ ] implement whole-vessel (or subtree) scaling
- [ ] handle stock exhaust particles
- [ ] implement waterfall support