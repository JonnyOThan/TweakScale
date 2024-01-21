using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TweakScale.SafetyNet
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	class SafetyNetAddon : MonoBehaviour
	{
		public static bool TweakScaleLoaded { get; private set; }
		public static bool TweakScaleInstalled { get; private set; }

		public static string StateDescription { get; private set; }
		public static string DiagnosticMessage { get; private set; }
		
		void Start()
		{
			DetermineState();

			Debug.Log($"[TweakScaleSafetyNet]: TweakScale is {StateDescription}.");
			config = LoadConfig();
			showStartupDialog = config.GetValue(nameof(showStartupDialog), showStartupDialog);

			if (TweakScaleLoaded)
			{
				// if tweakscale is correctly installed, make sure the showStartupDialog flag is set to true,
				// so you get another warning the next time tweakscale goes missing
				if (!showStartupDialog)
				{
					config.SetValue(nameof(showStartupDialog), true);
					config.save();
				}

				ShutDown();
				return;
			}

			if (!showStartupDialog)
			{
				ShutDown();
				return;
			}

			GameObject.DontDestroyOnLoad(gameObject);

			var dialogMessage =
				"TweakScale is " + StateDescription + ".\n\n" +
				DiagnosticMessage +
				"When TweakScale is not loaded, playing saved games and resaving craft files that had used TweakScale can remove data in unrecoverable ways.\n\n" +
				"TweakScaleSafetyNet will try to check saved games and craft files when they are loaded and warn you if they might be affected.  To remove these checks permanently, uninstall TweakScaleSafetyNet.\n\n" +
				"Proceed with caution.";

			var multiOptionDialog = new MultiOptionDialog("SafetyNetDialog", dialogMessage, "TSSafetyNet Warning", HighLogic.UISkin, 450,
				new DialogGUIButton("Proceed", OnProceed),
				new DialogGUIButton("Don't show this again", OnHideForever));

			dialog = PopupDialog.SpawnPopupDialog(multiOptionDialog, true, HighLogic.UISkin);
		}

		void ShutDown()
		{
			if (dialog)
			{
				dialog.Dismiss();
				dialog = null;
			}
			GameObject.Destroy(this.gameObject);
		}

		private void OnHideForever()
		{
			showStartupDialog = false;
			config.SetValue(nameof(showStartupDialog), showStartupDialog);
			config.save();
			ShutDown();
		}

		private void OnProceed()
		{
			ShutDown();
		}

		void DetermineState()
		{
			var tweakScaleAssemblyInfo = AssemblyLoader.availableAssemblies.FirstOrDefault(assemblyInfo => assemblyInfo.name == "Scale");
			TweakScaleInstalled = tweakScaleAssemblyInfo != null;

			var tweakScaleLoadedAssmebly = AssemblyLoader.loadedAssemblies.FirstOrDefault(la => la.name == "Scale");
			if (tweakScaleLoadedAssmebly == null || tweakScaleLoadedAssmebly.assembly == null)
			{
				DiagnosticMessage = "";

				// check harmony
				var harmonyInfo = AssemblyLoader.availableAssemblies.FirstOrDefault(assemblyInfo => assemblyInfo.name == "0Harmony");
				var requiredHarmonyVersion = new Version(2, 2);
				if (harmonyInfo == null)
				{
					DiagnosticMessage = "Harmony is required and not installed.";
				}
				else if (harmonyInfo.assemblyVersion < requiredHarmonyVersion)
				{
					DiagnosticMessage = $"Harmony is the wrong version.  Expected {requiredHarmonyVersion}, but {harmonyInfo.assemblyVersion} is installed.";
				}
				else if (!AssemblyLoader.loadedAssemblies.Contains("0Harmony"))
				{
					DiagnosticMessage = "Harmony is installed but failed to load.";
				}

				// check scale_redist
				var scaleRedistInfo = AssemblyLoader.availableAssemblies.FirstOrDefault(assemblyInfo => assemblyInfo.name == "999_Scale_Redist");
				if (scaleRedistInfo == null)
				{
					DiagnosticMessage += "\n999_Scale_Redist.dll is required and not installed.";
				}
				else if (scaleRedistInfo.assemblyVersion.Major != TweakScale.VersionInfo.MAJOR)
				{
					DiagnosticMessage += $"\n999_Scale_Redist.dll is the wrong version.  Expected {TweakScale.VersionInfo.STRING}, but {scaleRedistInfo.assemblyVersion} is installed.";
				}
				else if (!AssemblyLoader.loadedAssemblies.Contains("Scale_Redist"))
				{
					DiagnosticMessage += "\n999_Scale_Redist is installed but failed to load.";
				}

				// check ModuleManager
				if (!AssemblyLoader.availableAssemblies.Any(assemblyInfo => assemblyInfo.name.StartsWith("ModuleManager.")))
				{
					DiagnosticMessage += "\nModuleManager is required and not installed.";
				}

				if (DiagnosticMessage == "")
				{
					DiagnosticMessage = "Failed to load.  Usually this is caused by missing dependencies.";
				}
			}
			else
			{
				try
				{
					// is this really going to do anything different?  I"m pretty sure the assemblyloader would have called this already
					// TODO: check what happens when you run ts/l without scale_redist
					// also look at how KSPCF or MM handle the error messages on the loading screen 
					tweakScaleLoadedAssmebly.assembly.GetTypes();
					TweakScaleLoaded = true;
				}
				catch (Exception e)
				{
					// TODO: if this *does* work, probably need to reach into inner exceptions to find out why
					Debug.LogException(e);
					DiagnosticMessage = "Encountered exception when accessing types from TweakScale: " + e.Message;
				}
			}

			if (DiagnosticMessage != null)
			{
				DiagnosticMessage = DiagnosticMessage +
				  "\nPlease see KSP.log for details.  Include the KSP.log file in any requests for support.\n\n";
			}
			else
			{
				DiagnosticMessage = "";
			}

			StateDescription = TweakScaleInstalled
				? TweakScaleLoaded
					? "installed and loaded"
					: "installed but not loaded"
				: "not installed";
		}

		PopupDialog dialog;
		KSP.IO.PluginConfiguration config;

		bool showStartupDialog = true;

		static KSP.IO.PluginConfiguration LoadConfig()
		{
			var cfg = new KSP.IO.PluginConfiguration(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.xml"));
			try
			{
				cfg.load();
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}

			return cfg;
		}
	}
}
