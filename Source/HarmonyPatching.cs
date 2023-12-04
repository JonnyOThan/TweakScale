using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class TweakScaleHarmonyPatching : MonoBehaviour
	{
		void Awake()
		{
#if DEBUG
			Harmony.DEBUG = true;
#endif

			var harmony = new Harmony("TweakScale");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

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

		[HarmonyPatch("B9PartSwitch.PartSwitch.PartModifiers.AttachNodeMover", "SetAttachNodePositionAndMoveParts")]
		class B9PS_AttachNodeMover_SetAttachNodePositionAndMoveParts
		{
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
