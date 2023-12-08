This mod has one purpose: to protect you from losing data when TweakScale is not installed or failed to load.

Unlike most mods, TweakScale has the potential to really mess up your game if you uninstall it mid-save.  It's a similar problem as uninstalling a part mod - any vessels that were in flight that used those parts will be deleted, and you cannot open craft files that used parts from that mod.  However the game will warn you in those cases, and unless you ignore the warnings your data is safe.  The game *won't* warn you when you remove TweakScale because the parts still exist - but it will just ignore all the information about how the parts have been scaled.  For example: if you have a space station with a giant scaled up fuel tank, and then you uninstall tweakscale, now it's a tiny fuel tank.  And once the game is saved, the scale data is lost forever.

TweakScaleSafetyNet does nothing when TweakScale is installed and working properly.  If it detects that TweakScale is not installed or failed to load, then it does 3 things:

1. Shows a dialog box on startup with a shorter form of the above warning.  You can choose to hide this warning permanently.

2. Each time you load a save file from the main menu, it scans the save for vessels in flight that have scaled parts.  If there are any, it will warn you and create a backup copy of the save.

3. Each time you load a craft file in the editor, it scans the craft for scaled parts.  If there are any, it will warn you and create a backup copy of the craft.

