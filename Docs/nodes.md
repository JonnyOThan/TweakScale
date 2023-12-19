
How to handle nodes changing positions?

Basic approach: we need to know what the unscaled position and size of each attach node should be.  Then we can directly calculate the scaled versions using absolute scaling.

The TweakScale partmodule maintains a dictionary mapping attachnode id to position and size.  There are harmony patches in the stock and b9ps code that updates this dictionary as the variants are changed



1. place a fuel tank
2. attach planetside base node on the bottom of the fuel tank (using the base node's top attach node)
3. increase scale to 200%
4. change top node config to rounded
5. change back to bare


attachnodes are getting double-scaled because b9ps already handles scale via some crazy system:
https://github.com/blowfishpro/B9PartSwitch/commit/a2f64cb160f9e16030981481925ea151cebcab2f

Note that b9ps has a tweakscale support patch that looks like this:

```
TWEAKSCALEEXPONENTS
{
    name = ModuleB9PartSwitch
    scale = 1
}
```

(would it make sense to remove this config?  TODO: Need to audit everything in B9PS that is using this scale factor.  
If it's just attach nodes and things that we can handle (mass, cost, ....resources?), probably want to remove it.  
If there is more complex logic then we should leave it and address bugs individually.
It might be possible to set up nested nodes in the TWEAKSCALEEXPONENTS structure to have TweakScale's generic patching
system directly modify the fields inside the B9PS modules)

There's also a bug where they use the prefab's rescaleFactor cubed:
https://github.com/blowfishpro/B9PartSwitch/blob/e282eb138340aa0ea2c2926cb48e9c6a1e408d93/B9PartSwitch/PartSwitch/AttachNodeModifierInfo.cs#L49

Note that for many modded parts, rescaleFactor is 1 but it defaults to 1.25

OnTweakScaleChanged ->
	ScalePart
		MoveNode
	CallUpdaters
		applies exponent to b9ps module's scale field

B9PS node moving logic:

```
        private void SetAttachNodePosition()
        {
            attachNode.position = position * linearScaleProvider.LinearScale;
        }

        private void UnsetAttachNodePosition()
        {
            attachNode.position = attachNode.originalPosition * linearScaleProvider.LinearScale;
        }
```

Note that unsetattachnodeposition is especially wrong; originalPosition will still be the scaled position
and SetAttachNodePosition isn't setting the originalPosition like tweakscale would (I'm pretty sure it's necessary)

Proposal: use harmony to patch these 3 functions:
	AttachNodeMover constructor: in a prefix, scale the position by the prefab's rescaleFactor squared so that we get the correct prefab position of the node
	SetAttachNode: in a postfix, set the node's originalPosition
	UnsetAttachNodePosition:
		Not sure about this one.  If we build a mapping of attachNodeId to "unscaled" position, could use it here
		or use the prefab's position, but it won't be correct if ModulePartVariants or other node-moving mods is also in effect
		There might not be any such parts though, already seems problematic if you're using B9PS AND something else for nodes.
		Or maybe add a IRescalable<ModuleB9PartSwitch> that can store some extra state?


========

PartLoader.ParsePart:
	rescaleFactor = 1.25 or value from cfg
	rescaleFactor gets set as the localScale on the "model" gameobject transform
	num2 = rescaleFactor
	if cfg has "scale" or "exportscale" values:
		num2 = value * rescaleFactor
		part.scaleFactor = value / rescaleFactor
	attachNode's position and originalPosition are scaled by num2
	num2 also scales the localPosition (but not scale) of fx nodes
