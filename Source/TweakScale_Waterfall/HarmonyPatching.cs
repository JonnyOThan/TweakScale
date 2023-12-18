﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TweakScale.RescalableHandlers;
using UnityEngine;
using Waterfall;

namespace TweakScale.Waterfall
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class TweakScaleWaterfallHarmonyPatching : MonoBehaviour
	{
		void Awake()
		{
			var harmony = new Harmony("TweakScale_Waterfall");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}

		// ----- ModulePartVariants

		[HarmonyPatch(typeof(ModuleWaterfallFX), "Initialize")]
		class ModuleWaterfallFX_Initialize
		{
			public static void Postfix(ModuleWaterfallFX __instance)
			{
				var tweakScaleModule = __instance.part.FindModuleImplementing<TweakScale>();
				tweakScaleModule.FindHandlerOfType<ModuleWaterfallFXHandler>().EffectsInitialized();
			}
		}
	}
}
