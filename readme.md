# Support

You MUST include your ksp.log file with all support requests.  These links are for TweakScaleRescaled.  DO NOT ask for support on other TweakScale threads.

* KSP Forum: <https://forum.kerbalspaceprogram.com/topic/223692-1125-tweakscalerescaled-scale-your-parts-in-the-editor/>
* Discord: <https://discord.gg/tTWcqrBB3t>
* Github: <https://github.com/JonnyOThan/TweakScale/issues>
* Reddit: <https://www.reddit.com/r/KerbalSpaceProgram/comments/1957t3x/wip_tweakscale_rescaled/>

# New Feature Demos

<https://youtu.be/em_CyA90Sdo?list=PLDO10NRxfG06QzFcqaEFVjB6FurUaoer8>

# What is this?

TweakScale is a mod that allows you to rescale parts in the vessel editor.  All of their stats and properties are affected realistically - tanks contain more fuel, engines get stronger, solar panels produce more power, etc.  It's one of KSP's oldest mods and has been maintained by many different people over the years.

I've forked pellinor's version of TweakScale and updated it for modern KSP to provide a simpler and streamlined version.  I've paid special attention to the following areas:

## User experience

* Improvements to part action window in the editor
  * clicking interval buttons at the ends of the ranges works better
  * can now drag the slider smoothly
  * Toggle for scaling changes to affect the entire subtree
  * Toggle for matching node sizes when attaching parts
  * Numeric entry (click #) is now supported
  * Option to display the effects of scaling on the part's stats
* Scaling construction mode gizmo akin to offset and rotate (press 5 in the editor)
* New "match node size" feature to auto-scale parts when attaching nodes to each other
* All parts are supported with reasonable defaults unless they opt-out or match certain filters to trap known issues (AllTweak will still override this and enable *everything*)
* Scaling docking ports is supported
* Reduced dependencies to only ModuleManager and Harmony
* Optimized for better performance
  * better loading time to main menu
  * better switch-to-editor time
  * scaling a part is more responsive
  * less performance impact in flight scene
  * better save/load performance
* Less intrusive and more targeted validation system (TweakScaleSafetyNet)

## Robustness

* fixed part attachments and mass/cost scaling when variants or B9PartSwitch are involved
* scaling engine effects (stock, waterfall, realplume, smokescreen, etc) works correctly
* Craft files and saved games no longer break from changes in TweakScale configs
* More resilient to problems caused by missing dependencies, problems with other mods, and incorrect installations
* Copying part subtrees, subassemblies, load-merging all work properly
* Made it easier to add or customize TweakScale support for other mods
* Scaled parts work correctly with EVA construction
* Compatible with many fuel-switching mods:
  * B9PartSwitch
  * ModularFuelTanks
  * InterstellarFuelSwitch
  * SimpleFuelSwitch
  * Firespitter
  * Universal Storage 2
  * Configurable Containers

# Why not work to improve the current TweakScale?

The gist of it is: it would take far more energy than I have.  If I believed this option was possible, I would do it (because usually, this *is* the best option).

However, TweakScaleRescaled (this fork) is licensed CC-BY-NC-SA.  Its code may be used by anyone else provided they abide by the license terms.  Pull requests are also welcome from anyone who wants to contribute.

# Compatibilty

Backwards compatibility is a core goal of TweakScaleRescaled.  Existing craft files, saved games, configs, and mods that depend on Scale\_Redist should work seamlessly if you change to TweakScaleRescaled.

However, ongoing interoperability with other versions of TweakScale is not a goal.  I will make a reasonable effort to support it where it's easy.  In order to make the improvements necessary, data structures and assumptions have to be altered in ways that are difficult to reverse.

TweakScaleRescaled should *just work* with almost all stock or modded parts.  It includes the stock and mod-support patches that were written by the previous authors (updated as necessary).  There's also a new patching system based on heuristics that will apply to all other parts unless they opt out or are blocked by filters for things that are known to break.

Only KSP 1.12 is supported.  Other versions 1.8-1.11 *may* work but have not been tested and are not a priority.

# Dependencies

* ModuleManager
* Harmony
* KSP Community Fixes is recommended to address stock bugs that become more apparent when using TweakScale

# License Info

* TweakScaleRescaled is licensed CC-BY-NC-SA-4.0.
* © 2023 JonnyOThan
* © 2015-2018 pellinor
* © 2014 Gaius Godspeed and Biotronic

## History

1. TweakScale was [originally created by Gaius Goodspeed](https://forum.kerbalspaceprogram.com/topic/65819-0235-goodspeed-aerospace-parts-v201441b/) under CC-BY-NC-SA.
2. [Biotronic took over](https://forum.kerbalspaceprogram.com/topic/72554-090-tweakscale-rescale-everything-v150-2014-12-24-1040-utc/) and [relicensed it as WTFPL](https://forum.kerbalspaceprogram.com/topic/72554-090-tweakscale-rescale-everything-v150-2014-12-24-1040-utc/?do=findComment&comment=1204240)
3. [pellinor adopted it](https://forum.kerbalspaceprogram.com/topic/101540-14x-tweakscale-v2312apr-16/) and kept the WTFPL license.
4. TweakScaleRescaled is based off of pellinor's version, and is once again licensed as CC-BY-NC-SA.

# Download

* CKAN is recommended (NOTE: if you previously had TweakScale-Redist installed, you may need to deselect it and select TweakScaleRescaled-Redist instead)
* Github: <https://github.com/JonnyOThan/TweakScale/releases>
* Spacedock: <https://spacedock.info/mod/3514/TweakScale%20Rescaled>

# Installation Instructions

1. Remove any previous version of TweakScale and KSP Recall.
2. Install the contents of the GameData folder in the zip file into your GameData folder.
3. Install Harmony and ModuleManger - CKAN is recommended for those.
