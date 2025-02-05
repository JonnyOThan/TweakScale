using HarmonyLib;

namespace TweakScale.HarmonyPatching
{
	[HarmonyPatch("UniversalStorage2.USFuelSwitch", "setupTankInPart")]
	internal static class UniversalStorage
	{
		static bool Prepare()
		{
			return AssemblyLoader.loadedAssemblies.Contains("Universal Storage 2");
		}

		static void Postfix(PartModule __instance)
		{
			var tweakScaleModule = __instance.part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			tweakScaleModule.ScalePartResources();
		}
	}
}
