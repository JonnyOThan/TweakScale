using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale.HarmonyPatching
{
	// ----- BDA GetTweakScaleMultiplier

	[HarmonyPatch("BDArmory.Extensions.PartExtensions", "GetTweakScaleMultiplier")]
	static class BDArmoryPlus_GetTweakScaleMultiplier
	{
		static bool Prepare()
		{
			return AccessTools.Method("BDArmory.Extensions.PartExtensions:GetTweakScaleMultiplier") != null;
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

	[HarmonyPatch("BDArmory.Core.Extension.PartExtensions", "GetSize")]
	static class BDArmoryContinued_GetSize
	{
		static bool Prepare()
		{
			return AccessTools.Method("BDArmory.Core.Extension.PartExtensions:GetSize") != null;
		}

		// https://github.com/PapaJoesSoup/BDArmory/blob/f95d84f20ddb4f75ffd48b22e4a9879c3820823e/BDArmory.Core/Extension/PartExtensions.cs#L293
		static bool Prefix(Part part, ref Vector3 __result)
		{
			var size = part.GetComponentInChildren<MeshFilter>().mesh.bounds.size;

			if (part.name.Contains("B9.Aero.Wing.Procedural"))
			{
				size = size * 0.1f;
			}

			float scaleMultiplier = 1f;
			var tweakScaleModule = part.FindModuleImplementing<TweakScale>();
			if (tweakScaleModule != null)
			{
				scaleMultiplier = tweakScaleModule.currentScaleFactor;
			}

			__result = size * scaleMultiplier;

			return false;
		}
	}
}
