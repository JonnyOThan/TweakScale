using HarmonyLib;
using KSP.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TweakScale.HarmonyPatching

{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class TweakScaleHarmonyPatching : MonoBehaviour
	{
		void Awake()
		{
#if DEBUG
			// Harmony.DEBUG = true;
#endif

			var harmony = new Harmony("TweakScale");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}
