# Bugs

- [ ] UniversalStorage2's "added cost" readout is wrong
- [ ] some fuel switch mods (US2, basically anything that has to manually rescale resources) don't refresh the tweakscale stats in the PAW

# New Candy

# Verification (do this last, except to generate new bugs)

- [ ] check stock twin boar (since it's an engine + fuel tank)
- [ ] check parts that modify drag cubes
- [ ] find all TODOs and make sure there are issues tracked if necessary
- [ ] Make sure switching a part's scale type doesn't break it
		tsarbon's plane wing changed from stack to free, and it worked.
		pretty sure this is mostly going to work, just need to test loading things that aren't IsFreeScale
- [ ] make sure we can load crafts saved with TS/L
	done some limited testing here, it's looking good
- [ ] make sure we can load *saves* with vessels in flight that used TS/L
	part support seems done, but not sure if everything is using exactly the same settings as TS/L
- [ ] test with existing companion
- [ ] should explosionPotential actually be scaled?
- [ ] do we need to respect min/max mass and cost?

# Mod Compatibility

- [ ] audit and fix mod support
- [ ] check WBI Fuel Switch (WBIResourceSwitcher, WBIModuleSwitcher) - pathfinder shuttle wings
- [ ] Check KIS support

# Stretch

- [ ] can the "show keybinding" option be moved to the main settings or something?
- [ ] better stats formatting - group by section?
- [ ] find a way to remove tweakscale modules from saved craft files when they're not scaled
- [ ] Better stats in GetInfo text (explain how different properties will scale)
- [ ] Mastodon engine changes node sizes based on variant....
- [ ] move waterfall support into main mod?
- [ ] simplefuelswitch doesn't update stats when changing tank type on rescaled part
- [ ] search github for other direct usages of old tweakscale fields
	- InnerLock https://github.com/whale2/InnerLock/blob/a50f3ad65f7e0231dc8098fb9144cd1eabb56649/InnerLock/LockMechanism.cs#L410
	- IR surface sampler: (never seems to be released?) https://github.com/DMagic1/IR-Surface-Sampler/blob/1565748adfe424b6a24d989550757326fa590550/ModuleIRSurfaceSampler.cs#L276
	- RP-0 (KCT?): https://github.com/TheBigLee/RP-0/blob/4c92c116757363d173b9ca6b0ba07a24ee5e54dd/Source/KerbalConstructionTime/Utilities/Utilities.cs#L565
- [ ] save current tweakscale version in module, to aid in addressing versioning issues?  this should have an enum or something rather than just the tweakscale assembly version directly
- [ ] ConfigurableContainers doesn't get the right tank volume when swapping tanks on a scaled part
	- but this happens in TS/L too: https://github.com/allista/ConfigurableContainers/issues/44  
	- since CC tries to understand scaling directly, it should probably be fixed there...but this bug has been sitting open for over a year with solid repro steps and CC has not been touched in nearly 2 years

# Stuff moved to github issues

- scaling parts with struts attached between them doesn't work (but offset mode does)
	note this also happens in TS/L
- subtrees don't reset scale properly after pressing ctrl to cancel a match-node-size action (but dragging the part off DOES for some reason)
- added cost is misleading because of accounting for resource differences
- stats window should show base cost/mass and final
- TSSafetyNet needs to actually populate the load failure reason
- revisit science scaling - don't allow scaling up most experiments; provide 1.25m and 0.625m science jr
- support localization
- docking port support (this is tricky because of node types - needs a custom handler probably)
- SM-18 and SM-25 service modules don't work well (flickering) with match node scale - has something to do with shrouds
- increase crew capacity when scaling up?
- smart tanks match diameter feature doesn't work when attaching to scaled parts
- verify tech unlocks are correct (fuel tanks, etc)
- Check rescalefactor warnings (see bottom)

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
- create a IRescalable attribute with virtual functions to customize registration and construction
	For example the CrewManifest handler
	Maybe this isn't a big deal..there aren't that many handlers
	this is probably moot now that you can have a static Create method

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
- [x] restore hotkey for toggling child attachment just in case people get mad
- [x] maybe some tool to make new items inherit scale? 
		1. global toggle (like scale children) for "inherit scale on attachment" (maybe 3 states - off, absolute, stack (diameter))
		2. when hovering a new part, it will rescale itself based on what it's hovering over.  So if you try to attach a fl-t400 to a rockomax, it magically becomes 2.5m
- [x] is there a reasonable way to show modified stats in the PAW? Kind of like how B9PS does it
	e.g. engine thrust, etc.
- [x] override GetInfo to provide scaletype etc in right-click menu in parts box
- [x] scale doesn't update in selected part's PAW if you have it open
- [x] need to revert to previous size if toggle is disabled mid-placement
- [x] analyze actual node size to better support adapter parts
- [x] parent part shouldn't actually need to have tweakscale module
- [x] reliant doesn't scale properly (but swivel does?)
- [X] merge HotKeyManager and TweakScaleEditorLogic
- [X] save state in config.xml
- [x] scale gizmo in editors (hit 5 or a new button next to re-root, create scale gizmo on part)
- [x] part can "jump" when rescaled and lose attachment - is there a way to offset this?
		e.g. try to attach a fl-t400 to a 2.5m tank
- [x] when chain scaling, are we scaling children twice?
	they get moved twice, but that seems unavoidable and the order doesn't matter
- [x] doesn't scale attachnode sizes
- [x] maybe shift should also be a temporary *enable* as well?  For Scale Children too?
- [x] find a different modifier key for temp toggle/disable
- [x] make sure half-sizes work properly, might need to provide extra info in tweakscale module
- [x] gizmo: clamp min/max sizes
- [x] use angle snap and shift to control snapping to step intervals
- [x] on-screen message showing current scale (like fairings)
- [x] support arrow keys
- [x] Undo after using scaling gizmo seems to break things
- [x] parts will flicker when matching size after being flipped over
- [x] scale mode icon is slightly bigger
- [x] Show some summary info in the scale PAW group text (current gui scale, total scale factor?)
- [x] put hotkeys for child scale / match node size in PAW
- [x] reliant is free-scale because it only have 6 terms in the node_stack_top field.  see if we can provide a good default there (&node_stack_top[6] = 1 maybe?)
- [x] stats: show old and new values
- [x] stats: hide things that don't make sense (crew capacity)
- [x] show resource names (and maybe just max amount?)
- [x] make sure K and M keybinds respect input locks / text fields
- [x] add a screen message for match node size
- [x] add scale children keybind to scaling mode screenmessage
- [x] add display format to scale type (alongside suffix) so that we don't get 3 digits of precision on percentage scalars
- [x] exception thrown when building stat string scaling up solar panels (field is a floatcurve)
- [x] pressing space on held part when orientation is already default should reset scale and/or allow scale gizmo to be used on unattached parts
- [x] Option for disabling keybind messages
- [x] stats section didn't collapse when turning off (seems to be specific to non-default construction modes?)
- [x] command pod inventory stopped working - reduced to 0 slots and then it would not increase again
		part action window just doesn't seem to respond correctly to changes in slots or volume
- [x] implement waterfall support
- [x] Devise a way to share version info across assemblies
- [x] Errors due to removing fields from TweakScale module:
		[WRN 18:23:24.910] [TweakScale] No valid member found for DryCost in TweakScale
		[WRN 18:23:24.911] [TweakScale] No valid member found for MassScale in TweakScale
- [x] show mass and cost in stats
- [x] realplume support?
- [x] move testflight to a separate handler dll?  maybe allow for rescalables to provide their own create function that could return null?
- [x] TD-25 decoupler shows "1.25" at default scale
- [x] Handle ModuleEngines exhaust
- [x] handle ModuleEnginesFX
- [x] error spew about attach nodes when starting flight mode
- [x] [WRN 20:41:19.040] [TweakScale] treatMassAndCost: TweakScale/MassScale exponent already exists!
- [x] investigate part cost scaling on HECS2
		seems like this is being treated as "science" which becomes cheaper when it's bigger
		all of the probe cores seem to do this, which makes some sense, though the HECS2 also has a lot of battery space
- [x] Drag cubes revert after launching the craft
- [x] investigate craft file from StormCircuit (attachnodes on structural tubes)
- [x] Loading TS/L craft has wrong mass and cost
- [x] structural tubes broke again
- [x] BDA tweakscale accessor broke
- [x] config preferences not saved in 3.0.4 - check tsarbon's log
- [x] remove SmokeScreen dll and just replace with a harmony patch in the main mod
- [x] ModuleAblator / ModuleHeatShield defaults?
- [x] what happend to smallHardpoint
- [x] check engine plate configs - stack? stack_square?
- [x] test engine plate nodes & lengths
- [x] stock heatshield0 has multiple configs applied
- [x] remove explicit setups for stock parts that could be handled by automatic ones
	maybe? would other things break if these aren't set up early enough?
- [x] node size resets when selecting tube variant on scaled part
- [x] holding alt to force node attachment then releasing will leave symmetry parts as scaled but not the hovered part
- [x] FAR: https://github.com/ferram4/Ferram-Aerospace-Research/blob/787a30bc9deab0bde87591f0cc973ec3b0dd2de9/FerramAerospaceResearch/LEGACYferram4/FARWingAerodynamicModel.cs#L1491
- [x] KCT: https://github.com/ts826848/KCT/blob/5df691d8b410d6f1abd635a7e767f88557fcba0d/Kerbal_Construction_Time/KCT_Utilities.cs#L837
	this doesn't seem to be called anywhere
- [x] Make stack_square treat resources properly (square instead of cube)
- [x] FSFuelSwitch doesn't handle cost scaling correctly (probably because of ignoreResourcesForCost)
- [x] surface-attached parts saved with TS/L don't seem to be at the right locations
- [x] dry cost of stock tanks seems to be scaled wrong
- [x] fuel tanks are triggering mass/cost updates because they use the stock variant module
- [x] maybe increase the max size for freescale type?  stack can go from 1.25m -> 20m, a factor of 16 increase, but freescale is limited to 400%
- [x] SimpleFuelSwitch cost scales are wrong
- [x] SimpleFuelSwitch: changing tank type when scaled doesn't adjust resources properly
- [x] simplefuelswitch resource capacity is too high on launch
- [x] stats don't seem to be updating correctly when dragging the slider
- [x] FSBuoyancy doesn't work correctly
		actually this isn't TOO bad...but the buoyancy slider range starts getting messed up
- [x] B9PS resource switching has wrong costs
- [x] IFS cost scaling doesn't work (might need harmony patch?)
- [x] ConfigurableContainers mass and cost are broken (but also seem to be in TS/L)
- [x] check universal storage fuel switch

# Finished Verification

- [x] check parachutes
- [x] make sure save/load works
- [x] check undo after scaling
- [x] audit squadexpansion parts to make sure node sizes are correct (adapters etc)
- [x] audit and implement all stock parts
- [x] check FS buoyancy module
- [x] check scale mode on non-supported parts
- [x] make sure subassemblies/merging works
- [x] make sure scaling command parts w/ kerbals works properly re: mass
- [x] check scaling with struts and fuel lines connected to affected parts
- [x] overheard on discord: "When you copy and paste tweakscaled tanks from FFT their volume resets to normal, but their size does not"
- [x] Check FSFuelSwitch interaction
- [x] check cloning part subtrees
- [x] how exactly does stack_square work with resources?  do they get squared or cubed?
	they seem to get cubed - this is probably bad.
- [x] check simple fuel switch
- [x] check interstellarfuelswitch
		weird behavior, but at least it matches TS/L...
- [x] check part recovery costs (with kspcf)
- [x] check SystemHeat

# Fuel Switch test cases

FL-T400

							  Cost	Mass
- Stock							X    X
- CryoTanks (B9PS)				X    X
- Firespitter					X    X
- InterstellarFuelSwitch		X    X
- SimpleFuelSwitch				X    X
- ModularFuelTanks				X    X
- ConfigurableContainers		X    X
- UniversalStorage2



