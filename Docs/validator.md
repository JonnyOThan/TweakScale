# Installation Check

do we really need this for the initial release?  Not necessarily, but if I have to use the "risk to users" line this will be a really good thing to have working.

Oh god no....
Tweakscale has the unique ability to fuck up your game if you used it and then remove it (or lose a dependency or have a bad install).
It should be possible to create another DLL that detects this case and can warn about possible problems (boy that sounds familiar)

The main problems with the existing tweakscale "watchdog" stuff are:
1. problems with unrelated mods will trigger the alert, when really it doesn't need to
2. it sounds really scary, but does an awful job at explaining what is wrong and how to fix it
3. it's not targeted enough.  It's one big warning at startup and you don't really know what might be affected

The validator should be another mod folder in gamedata, so that users doing manual installs are likely to get it
It should be a *separate* mod in CKAN, and recommended by TS/R, so that users control whether it is installed and when to remove it

possible conditions/language:

1. Tweakscale is installed but failed to load because of .... (needs very detailed info and specific instructions to fix)
2. Tweakscale is installed but in an incorrect location (this might not actually cause any problems..?)
3. TweakScale is not installed.

Proposed dialogs:

1. at main menu: "[condition text].  When TweakScale is not loaded, playing saved games and resaving craft files that had used 
tweakscale can remove data in unrecoverable ways.  Proceed with caution.  

TweakScaleValidator will try to check saved games and craft files when they are loaded and warn you if they might be affected.

To remove these checks permanently, uninstall TweakScaleValidator.

buttons: "Proceed", "Don't show this again", maybe: "scan my saves now"
	note that whatever state is tracking "don't show this again" probably should be cleared if tweakscale is ever reinstalled/loaded correctly

2. when loading a save: "This save file has vessels in flight that have parts scaled by TweakScale.  [ConditionText].  Loading this save will permanently remove tweakscale's information which could affect vessel mass, cost, size, resources, etc.  Proceed with caution.  A backup save file has been saved to some/path[TweakScale].sfs.  To remove this check forever, uninstall TweakScaleValidator"
	(list of vessels that are affected)
	buttons: "Proceed", "Cancel"

3. When loading a craft: "This craft file has parts scaled by tweakscale.  [Condition Text].  Re-saving this craft file will permanently remove tweakscale's information which could affect part mass, cost, size, resources, etc.  Proceed with caution.  A backup of the original has been saved to someCraft[TweakScale].craft.  To remove this check forever, uninstall TweakScaleValidator"
	No options here needed, because now it's on the user

and to be clear, all of these can check for actually scaled parts, not just "the thing was saved while I had tweakscale installed"

## naming

TweakScaleValidator is...fine I guess.  But it's really more about protecting the user from bad effects.  Watchdog is the term used elsewhere but that doesn't do a great job either.
GuardDog maybe, but that might be a bit too close to WatchDog
Guardian? Sentinel?  SafetyNet?  DataGuard? DataProtector?  DataGuardian?
Also might want to obscure the folder name slightly so that people aren't as likely to reflexivly delete it when uninstalling tweakscale manually.
e.g. TSRGuardian