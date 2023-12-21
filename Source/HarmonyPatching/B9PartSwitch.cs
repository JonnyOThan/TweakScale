using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale.HarmonyPatching
{
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
}
