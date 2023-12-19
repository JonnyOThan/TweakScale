using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale.SmokeScreen
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class TweakScaleWaterfallHarmonyPatching : MonoBehaviour
	{
		void Awake()
		{
			var harmony = new Harmony("TweakScale_SmokeScreen");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		// ----- ModelMultiShurikenPersistFX

		// this is really really unfortunate.  The SmokeScreen code has builtin TweakScale support, but it does it by reaching into the TweakScale module's Fields and grabbing the currentScale and defaultScale items.
		// https://github.com/sarbian/SmokeScreen/blob/abdeaf1ba3ec6bbb8a72bb88555e323a4a671ab9/ModelMultiShurikenPersistFX.cs#L669
		// since those don't exist in this fork, it ends up scaling everything to zero.
		// a better approach would be to use IRescalablePart or handle the OnPartScaleChanged event
		[HarmonyPatch(typeof(ModelMultiShurikenPersistFX), nameof(ModelMultiShurikenPersistFX.OnInitialize))]
		class ModelMultiShurikenPersistFX_OnInitialize
		{
			public static void Postfix(ModelMultiShurikenPersistFX __instance)
			{
				var tweakScaleModule = __instance.hostPart.FindModuleImplementing<TweakScale>();
				if (tweakScaleModule != null)
				{
					__instance.specialScale = tweakScaleModule.currentScaleFactor;
				}
			}
		}
	}
}
