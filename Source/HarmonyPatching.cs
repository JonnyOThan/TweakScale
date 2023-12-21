using HarmonyLib;
using KSP.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TweakScale
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

		// ----- B9PS AttachNodes

		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.AttachNodeMover", "SetAttachNodePosition")]
		class B9PS_AttachNodeMover_SetAttachNodePosition
		{
			static bool Prepare()
			{
				return AssemblyLoader.loadedAssemblies.Contains("B9PartSwitch");
			}

			// this gets called when loading a ship that has a partswitch already applied
			public static bool Prefix(AttachNode ___attachNode, Vector3 ___position)
			{
				if (!HighLogic.LoadedSceneIsEditor) return true;
				var tweakScaleModule = ___attachNode.owner.FindModuleImplementing<TweakScale>();
				if (tweakScaleModule == null) return true;

				tweakScaleModule.SetUnscaledAttachNodePosition(___attachNode.id, ___position);

				return false;
			}
		}

		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.AttachNodeMover", "SetAttachNodePositionAndMoveParts")]
		class B9PS_AttachNodeMover_SetAttachNodePositionAndMoveParts
		{
			static bool Prepare()
			{
				return AssemblyLoader.loadedAssemblies.Contains("B9PartSwitch");
			}

			// B9PS only sets the attachnode position, not originalPosition, and this seems to be necessary to keep everything working
			public static bool Prefix(AttachNode ___attachNode, Vector3 ___position)
			{
				var tweakScaleModule = ___attachNode.owner.FindModuleImplementing<TweakScale>();
				if (tweakScaleModule == null) return true;

				tweakScaleModule.SetUnscaledAttachNodePosition(___attachNode.id, ___position);
				tweakScaleModule.MoveNode(___attachNode);

				return false;
			}
		}

		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.AttachNodeMover", "UnsetAttachNodePositionAndMoveParts")]
		class B9PS_AttachNodeMover_UnsetAttachNodePositionAndMoveParts
		{
			static bool Prepare()
			{
				return AssemblyLoader.loadedAssemblies.Contains("B9PartSwitch");
			}

			// the B9PS version of this function depends on originalPosition being the one from the prefab, but thanks to our patch above it's not.
			public static bool Prefix(AttachNode ___attachNode, Vector3 ___position)
			{
				var tweakScaleModule = ___attachNode.owner.FindModuleImplementing<TweakScale>();
				if (tweakScaleModule == null) return true;

				// we can't use the stored unscaled node position because we *just wrote to it* when activating this subtype
				// for now, assume the pure unmodified value is the one in the prefab...which it should be, unless someone has modified what the "base" node should be, e.g. ModulePartVariants - but it seems pretty suspect to have ModulePartVariants and b9ps on the same part.
				var prefabAttachNode = ___attachNode.owner.partInfo.partPrefab.FindAttachNode(___attachNode.id);

				tweakScaleModule.SetUnscaledAttachNode(prefabAttachNode);
				tweakScaleModule.MoveNode(___attachNode);

				return false;
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

		// ----- BDA GetTweakScaleMultiplier

		[HarmonyPatch]
		class BDArmory_GetTweakScaleMultiplier
		{
			static bool Prepare()
			{
				return AssemblyLoader.loadedAssemblies.Contains("BDArmory");
			}

			static IEnumerable<MethodBase> TargetMethods()
			{
				Type bdaPartExtensionsType = AccessTools.TypeByName("BDArmory.Extensions.PartExtensions");

				if (bdaPartExtensionsType != null)
				{
					var method = AccessTools.Method(bdaPartExtensionsType, "GetTweakScaleMultiplier");
					yield return method ;
				}
			}

			static bool Prefix(Part part, ref float __result)
			{
				var tweakScaleModule = part.FindModuleImplementing<TweakScale>();
				if (tweakScaleModule != null)
				{
					__result = tweakScaleModule.currentScaleFactor;
				}
				else
				{
					__result = 1.0f;
				}

				return false;
			}
		}
	}

	internal static class B9PartSwitch
	{
		static B9PartSwitch()
		{
			var b9psLoadedAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(la => la.name == "B9PartSwitch");

			if (b9psLoadedAssembly == null) return;

			b9psLoadedAssembly.TypeOperation(type =>
			{
				switch (type.Name)
				{
					case "ILinearScaleProvider":
						var LinearScale_propertyInfo = type.GetProperty("LinearScale");
						x_ILinearScaleProvider_get_LinearScale_MethodInfo = LinearScale_propertyInfo.GetGetMethod();
						break;
				}
			});
		}

		public static float ILinearScaleProvider_get_LinearScale(object iLinearScaleProvider)
		{
			return (float)x_ILinearScaleProvider_get_LinearScale_MethodInfo.Invoke(iLinearScaleProvider, x_emptyArgList);
		}

		static MethodInfo x_ILinearScaleProvider_get_LinearScale_MethodInfo;
		static readonly object[] x_emptyArgList = new object[0];
	}
}
