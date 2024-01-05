using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.HarmonyPatching
{
	// The FSFuelSwitch module doesn't really handle the normal convention that a part prefab's cost *includes* the cost of any RESOURCEs in the part
	// Some of the default Firespitter parts have RESOURCE blocks which effectively get ignored, so we should not offset their cost during scaling
	// So really we just need to call InitializePrefabCosts each time the tank type is changed, and things should work out
	[HarmonyPatch("Firespitter.customization.FSfuelSwitch", "assignResourcesToPart")]
	static class Firespitter
	{
		static bool Prepare()
		{
			return AssemblyLoader.loadedAssemblies.Contains("Firespitter");
		}

		static void Postfix(PartModule __instance)
		{
			var tweakScaleModule = __instance.part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			tweakScaleModule.InitializePrefabCosts();
			tweakScaleModule.ScalePartResources();
		}
	}

	// TODO: the firespitter module doesn't send an editor ship modified event after changing fuel tanks, which means that the cost shown in the UI will be wrong until something else refreshes it.
	// We could patch that for them...
}
