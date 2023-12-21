using HarmonyLib;
using KSP.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TweakScale.HarmonyPatching

{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class TweakScaleHarmonyPatching : MonoBehaviour
	{
		void Awake()
		{
#if DEBUG
			// Harmony.DEBUG = true;
#endif

			var harmony = new Harmony("TweakScale");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		// ----- ModulePartVariants

		[HarmonyPatch(typeof(ModulePartVariants), nameof(ModulePartVariants.UpdateNode))]
		class ModulePartVariants_UpdateNode
		{
			public static void Prefix(Part part, ref AttachNode variantNode)
			{
				var tweakScaleModule = part.FindModuleImplementing<TweakScale>();
				if (tweakScaleModule != null)
				{
					tweakScaleModule.SetUnscaledAttachNode(variantNode);
					if (tweakScaleModule.IsRescaled)
					{
						// make a copy so we don't mess with the one that's in the variant module
						variantNode = AttachNode.Clone(variantNode);
						variantNode.position *= tweakScaleModule.currentScaleFactor;
						variantNode.originalPosition *= tweakScaleModule.currentScaleFactor;
					}
				}
			}
		}


		// ----- stock UIPartActionScaleEdit

		[HarmonyPatch(typeof(UIPartActionScaleEdit), nameof(UIPartActionScaleEdit.UpdateInterval))]
		class UIPartActionScaleEdit_UpdateInterval
		{
			static void SetValue(UIPartActionScaleEdit instance, float value, UIButtonToggle button)
			{
				// note we should not mess with changing the interval here, only the slider value
				instance.slider.value = value;
				instance.UpdateDisplay(value, button);
				instance.SetFieldValue(value);
			}

			public static bool Prefix(UIPartActionScaleEdit __instance, bool up, UIButtonToggle button)
			{
				// if we hit the up button while already on the top of the range
				if (up && __instance.intervalIndex == __instance.scaleControl.intervals.Length - 2)
				{
					float value = __instance.scaleControl.intervals.Last();
					SetValue(__instance, value, button);
					return false;
				}
				// hit the down button at the bottom of the range
				if (!up && __instance.intervalIndex == 0)
				{
					float value = __instance.scaleControl.intervals.First();
					SetValue(__instance, value, button);
					return false;
				}

				return true;
			}
		}

		[HarmonyPatch(typeof(UIPartActionScaleEdit), nameof(UIPartActionScaleEdit.FindInterval))]
		class UIPartActionScaleEdit_FindInterval
		{
			// The stock version of this function has an off-by-one error where it won't consider the final interval range
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				foreach (CodeInstruction instruction in instructions)
				{
					if (instruction.LoadsConstant(2))
					{
						instruction.opcode = OpCodes.Ldc_I4_1;
					}

					yield return instruction;
				}
			}
		}

		// ----- stock UIPartActionController

		[HarmonyPatch(typeof(UIPartActionController), nameof(UIPartActionController.Awake))]
		class UIPartActionController_Awake
		{
			static void Postfix(UIPartActionController __instance)
			{
				// needs to happen before UIPartActionController.SetupItemControls which is called in awake
				__instance.fieldPrefabs.Add(UIPartActionScaleEditNumeric.CreatePrefab());
			}
		}

		// ----- stock UIPartActionResourceEditor

		[HarmonyPatch(typeof(UIPartActionResourceEditor), nameof(UIPartActionResourceEditor.UpdateItem))]
		class UIPartActionResourceEditor_UpdateItem
		{
			static void Postfix(UIPartActionResourceEditor __instance)
			{
				__instance.resourceAmnt.text = KSPUtil.LocalizeNumber(__instance.resource.amount, "F1");
				__instance.resourceMax.text = KSPUtil.LocalizeNumber(__instance.resource.maxAmount, "F1");
			}
		}
	}
}
