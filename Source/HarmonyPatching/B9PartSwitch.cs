using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale.HarmonyPatching
{
	[HarmonyPatch]
	static class B9PS_HarmonyPatching
	{
		static bool Prepare()
		{
			return AssemblyLoader.loadedAssemblies.Contains("B9PartSwitch");
		}

		// ----- attachnodes

		// this gets called when loading a ship that has a partswitch already applied
		[HarmonyPrefix]
		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.AttachNodeMover", "SetAttachNodePosition")]
		public static bool AttachNodeMover_SetAttachNodePosition_Prefix(AttachNode ___attachNode, Vector3 ___position)
		{
			if (!HighLogic.LoadedSceneIsEditor) return true;
			var tweakScaleModule = ___attachNode.owner.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return true;

			tweakScaleModule.SetUnscaledAttachNodePosition(___attachNode.id, ___position);
			tweakScaleModule.MoveNode(___attachNode);

			return false;
		}

		// B9PS only sets the attachnode position, not originalPosition, and this seems to be necessary to keep everything working
		[HarmonyPrefix]
		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.AttachNodeMover", "SetAttachNodePositionAndMoveParts")]
		public static bool AttachNodeMover_SetAttachNodePositionAndMoveParts_Prefix(AttachNode ___attachNode, Vector3 ___position)
		{
			var tweakScaleModule = ___attachNode.owner.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return true;

			tweakScaleModule.SetUnscaledAttachNodePosition(___attachNode.id, ___position);
			tweakScaleModule.MoveNode(___attachNode);

			return false;
		}

		// the B9PS version of this function depends on originalPosition being the one from the prefab, but thanks to our patch above it's not.
		[HarmonyPrefix]
		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.AttachNodeMover", "UnsetAttachNodePositionAndMoveParts")]
		public static bool AttachNodeMover_UnsetAttachNodePositionAndMoveParts_Prefix(AttachNode ___attachNode, Vector3 ___position)
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

		// Don't have any evidence that this is needed, so disabling for now.  If we do need it, these patches should be cleaned up and probably use a transpiler.
#if false

		// ----- module data switching
		// https://github.com/JonnyOThan/TweakScale/issues/53

		[HarmonyPrefix]
		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.ModuleDataHandlerBasic", "Activate")]
		public static bool ModuleDataHandlerBasic_Activate_Prefix(PartModule ___module, ConfigNode ___dataNode, BaseEventDetails ___moduleDataChangedEventDetails)
		{
			var tweakScaleModule = ___module.part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return true;

			bool isEnabled = ___module.isEnabled;
			___module.Load(___dataNode);
			___module.isEnabled = isEnabled;

			// new
			tweakScaleModule.OnB9PSModuleDataChanged(___module);
			// end new

			___module.Events.Send("ModuleDataChanged", ___moduleDataChangedEventDetails);

			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.ModuleDataHandlerBasic", "Deactivate")]
		public static bool ModuleDataHandlerBasic_Deactivate_Prefix(PartModule ___module, ConfigNode ___originalNode, BaseEventDetails ___moduleDataChangedEventDetails)
		{
			var tweakScaleModule = ___module.part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return true;

			bool isEnabled = ___module.isEnabled;
			___module.Load(___originalNode);
			___module.isEnabled = isEnabled;

			// new
			tweakScaleModule.OnB9PSModuleDataChanged(___module);
			// end new

			___module.Events.Send("ModuleDataChanged", ___moduleDataChangedEventDetails);

			return false;
		}
#endif
	}
}
