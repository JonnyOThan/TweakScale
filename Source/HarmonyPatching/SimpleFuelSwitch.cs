using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.HarmonyPatching
{
	// Unfortunately, SimpleFuelSwitch shares the set of available resources between all parts of the same type, so we can't just reach in there and scale the amounts
	// The regular scale handlers work fine for altering the resources in the part when it's scaled, but we still need to handle the case of changing the fuel type
	// on a part that is already scaled (or else it'll just apply the unscaled resource amount)
	[HarmonyPatch("SimpleFuelSwitch.ModuleSimpleFuelSwitch", "UpdateSelectedResources")]
	static class SimpleFuelSwitch
	{
		static bool Prepare()
		{
			return AssemblyLoader.loadedAssemblies.Contains("SimpleFuelSwitch");
		}

		public static void Postfix(PartModule __instance)
		{
			var tweakScaleModule = __instance.part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			tweakScaleModule.InitializePrefabCosts();
			tweakScaleModule.ScalePartResources();
		}
	}
}
