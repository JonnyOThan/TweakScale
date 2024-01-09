using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.HarmonyPatching
{
	[HarmonyPatch("InterstellarFuelSwitch.InterstellarFuelSwitch", "SetupTankInPart")]
	static class InterstellarFuelSwitch
	{
		static bool Prepare()
		{
			return AssemblyLoader.loadedAssemblies.Contains("InterstellarFuelSwitch");
		}

		static void Postfix(PartModule __instance, double ___storedVolumeMultiplier)
		{
			var tweakScaleModule = __instance.part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			// IFS' behavior is pretty weird: 
			// https://github.com/sswelm/KSP-Interstellar-Extended/blob/8a55a180d0f940510ffc508df90e915c05dd81b8/FuelSwitch/InterstellarFuelSwitch.cs#L941
			// https://github.com/sswelm/KSP-Interstellar-Extended/issues/731
			// it seems to scale the cost *linearly* with scale factor, and its treatment of the base cost doesn't seem right
			// but we'll just try to do something that matches TS/L's behavior and if IFS ever wants to change we'll have to adapt

			// TODO: find a IFS tank type that has an intrinsic cost and make sure it works with that (antimatter maybe?)

			// IFS (tries to) understand scaling, so the part's resources at this point will have already been scaled by storedVolumeMultiplier
			TweakScale.GetPartResourceCosts(__instance.part.partInfo.partPrefab, out double prefabResourceCost, out double prefabResourceCapacityCost);
			TweakScale.GetPartResourceCosts(__instance.part, out double currentResourceCost, out double currentResourceCapacityCost);

			// dry cost comes from the prefab; capacity cost from the current resources
			tweakScaleModule.SetUnscaledCosts(__instance.part.partInfo.cost - prefabResourceCost, currentResourceCapacityCost / ___storedVolumeMultiplier);
			tweakScaleModule.CalculateCostAndMass();

			// TODO: the IFS module doesn't send an editor ship modified event after changing fuel tanks, which means that the cost shown in the UI will be wrong until something else refreshes it.
			// We could patch that for them...
		}
	}
}
