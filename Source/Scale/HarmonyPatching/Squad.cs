using HarmonyLib;
using KSP.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale.HarmonyPatching
{
	// ----- ModulePartVariants

	// This module can move attachnodes, and we need a notification to store the unscaled version of the nodes

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
					tweakScaleModule.MoveNode(variantNode);
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

	// ----- Stock ModuleWheelDeployment

	[HarmonyPatch(typeof(ModuleWheels.ModuleWheelDeployment), nameof(ModuleWheels.ModuleWheelDeployment.CreateStandInCollider))]
	static class ModuleWheelDeployment_CreateStandInCollider
	{
		static void Postfix(ModuleWheels.ModuleWheelDeployment __instance, SphereCollider __result)
		{
			var tweakScaleModule = __instance.part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			// stock code takes the VehiclePhysics wheel's radius for the standin collider.
			// however this has already been scaled by part.rescaleFactor, and then this collider is attached to the model transform (which also is scaled)
			__result.radius /= tweakScaleModule.currentScaleFactor;
		}
	}

	// ----- Stock VesselCrewManifest

	// This is annoying, it gets called all the time when adding or removing parts from the vessel
	// the upshot of it is that RefreshCrewAssignment creates a new VesselCrewManifest from the craft config node, then populates it from the existing manifest
	// That creation uses the part prefab crew capacities, so any modification to an individual part's crew capacity will be lost
	// https://github.com/JonnyOThan/TweakScale/issues/32
	[HarmonyPatch(typeof(VesselCrewManifest), nameof(VesselCrewManifest.UpdatePartManifest))]
	static class VesselCrewManifest_UpdatePartManifest
	{
		static void Prefix(VesselCrewManifest __instance, uint id, PartCrewManifest referencePCM)
		{
			if (__instance.partLookup.TryGetValue(id, out PartCrewManifest newPartCrewManifest))
			{
				int oldLength = newPartCrewManifest.partCrew.Length;
				Array.Resize(ref newPartCrewManifest.partCrew, referencePCM.partCrew.Length);
				for (int i = oldLength; i < newPartCrewManifest.partCrew.Length; ++i)
				{
					newPartCrewManifest.partCrew[i] = "";
				}
			}
		}
	}


	// ----- Stock InternalModel.Initialize

	// The internal model can get created and destroyed many times throughout flight
	// rather than having an update function where we continually set the scale, just hook into the creation to set it

	// https://github.com/JonnyOThan/TweakScale/issues/45
	[HarmonyPatch(typeof(InternalModel), nameof(InternalModel.Initialize))]
	static class InternalModel_Initialize
	{
		static void Prefix(InternalModel __instance, Part p)
		{
			var tweakScaleModule = p.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			// TODO: do we need to care about saving the default local scale in case it's not identity?
			__instance.transform.localScale = tweakScaleModule.currentScaleFactor * Vector3.one;
		}
	}

	// ----- Stock CompoundPart

	// This means fuel lines and struts, which connect two parts
	// These patches handle making sure that they stay in the correct positions relative to the part when scaling

	[HarmonyPatch(typeof(CompoundPart), nameof(CompoundPart.UpdateWorldValues))]
	static class CompoundPart_UpdateWorldValues
	{
		static void Postfix(CompoundPart __instance)
		{
			__instance.wTgtPos /= __instance.target.rescaleFactor;
		}
	}

	[HarmonyPatch(typeof(CompoundPart), nameof(CompoundPart.UpdateTargetCoords))]
	static class CompoundPart_UpdateTargetCoords
	{
		static void Prefix(CompoundPart __instance, ref Vector3 __state)
		{
			__state = __instance.wTgtPos;
			if (__instance.target != null)
			{
				__instance.wTgtPos *= __instance.target.rescaleFactor;
			}
		}

		static void Postfix(CompoundPart __instance, Vector3 __state)
		{
			__instance.wTgtPos = __state;
		}
	}

	// ----- stock AttachNode

	// when reversing a surface attachment during a re-root operation, the stock code will move the srfAttachNode for the part
	// this needs to become the new unscaled version of the node.

	[HarmonyPatch(typeof(AttachNode), nameof(AttachNode.ReverseSrfNodeDirection))]
	static class AttachNode_ReverseSrfNodeDirection
	{
		static void Postfix(AttachNode __instance)
		{
			var tweakScaleModule = __instance.owner.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			tweakScaleModule.StoreUnscaledSrfAttachNode();
		}
	}

}
