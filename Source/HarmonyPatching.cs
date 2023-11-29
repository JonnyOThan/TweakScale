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
			var harmony = new Harmony("TweakScale");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		[HarmonyPatch(typeof(ModulePartVariants), nameof(ModulePartVariants.UpdateNode))]
		class ModulePartVariants_UpdateNode
		{
			public static void Prefix(Part part, ref AttachNode variantNode)
			{
				var tweakScaleModule = part.FindModuleImplementing<TweakScale>();
				if (tweakScaleModule != null && tweakScaleModule.IsRescaled)
				{
					// make a copy so we don't mess with the one that's in the variant module
					variantNode = AttachNode.Clone(variantNode);
					variantNode.position *= tweakScaleModule.currentScaleFactor;
					variantNode.originalPosition *= tweakScaleModule.currentScaleFactor;
				}
			}
		}
	}
}
