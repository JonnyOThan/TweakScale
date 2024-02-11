using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.HarmonyPatching
{
	[HarmonyPatch]
	static class Part_SetHierarchyRoot
	{
		static bool Prepare()
		{
			return AssemblyLoader.loadedAssemblies.Contains("KSPCommunityFixes");
		}

		// get the reversed attachnode into the unscaled node list
		// Even though this is a stock function, we only need to do this patch if KSPCF is installed
		[HarmonyPatch(typeof(AttachNode), nameof(AttachNode.ReverseSrfNodeDirection))]
		[HarmonyAfter("KSPCommunityFixes")]
		[HarmonyPostfix]
		static void Postfix(AttachNode __instance)
		{
			Part part = __instance.owner;

			var tweakScaleModule = part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule == null) return;

			foreach (var attachNode in part.attachNodes)
			{
				if (attachNode.nodeType == AttachNode.NodeType.Surface && attachNode.attachedPart == part.parent && part.parent != null)
				{
					tweakScaleModule.SetUnscaledAttachNode(attachNode);
					tweakScaleModule.SetUnscaledAttachNodePosition(attachNode.id, attachNode.position / tweakScaleModule.currentScaleFactor);
					return;
				}
			}
		}
	}
}
