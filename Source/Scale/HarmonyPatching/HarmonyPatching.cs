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

			try
			{
				var harmony = new Harmony("TweakScale");
				harmony.PatchAll(Assembly.GetExecutingAssembly());
			}
			catch(Exception e)
			{
				Tools.LogException(e, "Error in Harmony patching, exception details:");

				var message =
					"TweakScale's Harmony patching failed.  Please make sure your other mods are up to date.  It may be unsafe to load your saved games.\n\n" +
					"Include your KSP.log file in any requests for support.";
				var dialog = new MultiOptionDialog("TweakScale", message, "TweakScale Error", HighLogic.UISkin, 300, 
					new DialogGUIButton("Quit", Application.Quit));

				PopupDialog.SpawnPopupDialog(dialog, true, HighLogic.UISkin);
			}
		}
	}
}
