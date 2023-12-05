# Installation Check

do we really need this for the initial release?  Maybe not.

Oh god no....
Tweakscale has the unique ability to fuck up your game if you used it and then remove it (or lose a dependency or have a bad install).
It should be possible to create another DLL that detects this case and can warn about possible problems (boy that sounds familiar)

The main problems with the existing tweakscale "watchdog" stuff are:
1. problems with unrelated mods will trigger the alert, when really it doesn't need to
2. it sounds really scary, but does an awful job at explaining what is wrong and how to fix it
3. it's not targeted enough.  It's one big warning at startup and you don't really know what might be affected

I will aim to improve these 3 issues:

- [ ] deploy the check dll in plugins, and have it copy itself to the root gamedata?
- [ ] check to make sure TweakScale is in the assemblyloader's list and the right version
- [ ] pop up a warning box explaining the situation and the causes (missing dependencies, tweakscale removed, etc) and solutions
		should also say "delete this dll to stop these warnings"
- [ ] If they continue, find a way to check loaded save files for vessels with scaled parts and pop up similar warnings
- [ ] Also check when loading craft files, ditto

Is there a way we can have a DLL delete itself entirely?

possible conditions/language:

1. Tweakscale is installed but failed to load because of .... (needs very detailed info and specific instructions to fix)
2. Tweakscale is installed but in an incorrect location (this might not actually cause any problems..?)
3. TweakScale had been installed but was removed.

Proposed dialogs:

1. at main menu: "[condition text].  When TweakScale is not loaded, playing saved games and resaving craft files that had used 
tweakscale can destroy data in unrecoverable ways.  Proceed with caution.  

TweakScaleValidator will try to check saved games and craft files when they are loaded and warn you if they might be affected.

(we could offer to scan saved games and craft files here....)

buttons: "OK", maybe: "Remove TweakscaleValidator"

OR:
To suppress this message forever and disable all safety checks, delete TSRValidator.dll from GameData"

2. when loading a save: "hey this save file has vessels in flight that used tweakscale, which is not currently installed or loaded.  Loading this save could destroy data in unrecoverable ways.  Proceed with caution.  A backup save file has been save to some/path.sfs.  To suppress this message forever, delete TweakScaleValidator.dll from GameData"

3. When loading a craft: "hey this craft file uses tweakscale, which is not currently installed or loaded.  Resaving this craft file could destroy data in unrecoverable ways.  Proceed with caution.  A backup has been saved to some/path.craft.  To suppress this message forever, delete TweakScaleValidator.dll from GameData"
and to be clear, all of these can check for actually scaled parts, not just "the thing was saved while I had tweakscale installed"