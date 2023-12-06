// TODO: naming conventions are all over the place in here.  let's use tweakscale_ as a prefix but then pascal case the rest?  Should it be tweakScale_ ?  

# A note about MM passes:

TweakScale is installed in the TweakScale directory, but the dll is named Scale.dll.  So both of these names exist as passes and mod identifiers
There are various mods out there that use either one of these.  So we will use `FOR[TweakScale]` because 

1. that's the canonical name of the mod and
2. it comes alphabetically after Scale, so any other mod's patches in the before/after Scale pass will go first

As a general philosophy, TweakScale's patches should:

- apply to parts that don't already have tweakscale modules
- run AFTER most other mods set up their tweakscale configs
- set defaults or apply any hints
- not run so late that they can't then be modified (i.e. no `FINAL`)

So, if the other mods are using the legacy pass, `BEFORE[Scale]`, `AFTER[Scale]`, or `BEFORE[TweakScale]` they will go before `FOR[TweakScale]`.
in general I would suggest using `BEFORE[TweakScale]` if you want to provide tweakscale support for your own mod.  
This has the double benefit of not requiring a `:NEEDS[TweakScale]` marker - the `BEFORE[TweakScale]` pass will not be executed if TweakScale isn't installed.

Note that several existing mods (including RealismOverhaul) have their tweakscale support patches in FOR passes of their own.  This is a problem if tweakscale did all of its work in FOR[TweakScale].
In interest of maximum compatibility, TweakScale will set up its configuration fields (tweakscale_default_scale, tweakscale_node_size etc) in FOR[TweakScale] but then create the actual module in LAST[TweakScale] if it doesn't already exist

# Historical note

All the part support patches had been dumped into this folder, and as far as I can tell none of them have pass specifiers (should verify this).
These have now been moved into 000_Support so that the folder structure makes it clear that they're running first (legacy pass).
Most of these patches use a strange construction like this:

```
@PART[hydrogen-radial-25-1]
{
	%MODULE[TweakScale]
	{
		type = free
	}
}
```

I have to assume that the `%MODULE[TweakScale]` was purely to save on typing and not because it expected to be editing an existing module - because otherwise there would also be a `%` in front of `type`.  When the eventual cleanup pass is done, these should be fixed as well.  And many of them could just be deleted outright because they'd be handled by the automated systems.

Note that there might be some oddity here if the part already defined its own tweakscale module - we'd just add the `type` field when it might already exist.

At some point in the future these should probably be updated as described below.

# Organization of this directory

There are two main sections:

1. 000_Support folder
2. ###_patch.cfg

Patches that add support for specific parts should go in the 000_Support folder and have `:HAS[!MODULE[TweakScale]]:FOR[TweakScale]`.
This should provide maximum support and flexibility - if the other mod wants to provide their own support, these patches will be disabled as long as they do it early enough (see above).
And the naming of the folder ensures that they run before the main TweakScale patching mechanisms.
// TODO: maybe these should use LAST[TweakScale] ?  Move to something like 500_Support ?

The rest of the patches are executed in order by filename (so long as the patches inside are in the same pass specifier).  There may be rare cases where one of these patches (or something in the 000_Support folder) needs to use a different pass specifier.  That's permissible, but it should be avoided if possible for clarity.