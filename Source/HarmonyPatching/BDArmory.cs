using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.HarmonyPatching
{
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
				yield return method;
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
