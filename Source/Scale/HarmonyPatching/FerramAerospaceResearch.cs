using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.HarmonyPatching
{
	[HarmonyPatch("ferram4.FARWingAerodynamicModel", "OnRescale")]
	static class FerramAerospaceResearch
	{
		static bool Prepare()
		{
			return AssemblyLoader.loadedAssemblies.Contains("FerramAerospaceResearch");
		}

		// FAR does its own scale-dependent mass adjustments.
		// it accomplishes this by trying to fetch the scaled mass in its OnRescale handler, then subtracting that from the desired mass in its GetModuleMass
		// https://github.com/dkavolis/Ferram-Aerospace-Research/blob/c769cbd3d23ec9fd22538c1eb8b57fd2fcd025c6/FerramAerospaceResearch/LEGACYferram4/FARWingAerodynamicModel.cs#L190
		// This doesn't work for us because we use a more sophisticaed mass calculation than (MassScale * prefabMass); taking other modifiers etc. into account
		// Further, we haven't actually calculated the true mass scale until all the handlers have run
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var newInstructions = instructions.ToList();

			var PartModule_get_Fields_method = AccessTools.PropertyGetter(typeof(PartModule), nameof(PartModule.Fields));
			//var BaseFieldList_GetValue_method = AccessTools.Method(typeof(BaseFieldList<BaseField, KSPField>), nameof(BaseFieldList.GetValue), new Type[] {typeof(string)}, new Type[] {});
			var BaseFieldList_GetValue_method = typeof(BaseFieldList<BaseField, KSPField>).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
				.Where(m => m.Name == "GetValue" && !m.IsGenericMethod)
				.Single();

			bool found = false;

			for (int i = 0; i + 3 < newInstructions.Count; ++i)
			{
				if (newInstructions[i].Calls(PartModule_get_Fields_method) &&
					newInstructions[i + 1].opcode == OpCodes.Ldstr && (string)newInstructions[i + 1].operand == "MassScale" &&
					newInstructions[i + 2].Calls(BaseFieldList_GetValue_method) &&
					newInstructions[i + 3].opcode == OpCodes.Unbox_Any)
				{
					found = true;

					newInstructions[i] = CodeInstruction.Call(typeof(TweakScale), nameof(TweakScale.HandleFARScaling));
					newInstructions.RemoveRange(i + 1, 3);
					Tools.Log("Successfully patched ferram4.FARWingAerodynamicModel.OnRescale method");

					break;
				}
			}

			if (!found)
			{
				Tools.LogError("Failed to patch ferram4.FARWingAerodynamicModel.OnRescale method");
			}

			return newInstructions;
		}
	}
}
