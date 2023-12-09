using Expansions;
using Expansions.Missions.Runtime;
using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale.SafetyNet
{
	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	class CraftChecker : MonoBehaviour
	{
		CraftBrowserDialog craftBrowserDialog;
		bool hideBrowserInLateUpdate = false;

		void Start()
		{
			if (SafetyNetAddon.TweakScaleLoaded)
			{
				GameObject.Destroy(this.gameObject);
				return;
			}

			craftBrowserDialog = EditorLogic.fetch.craftBrowserDialog;

			if (craftBrowserDialog == null)
			{
				string profile = HighLogic.SaveFolder;
				if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Mode == Game.Modes.MISSION_BUILDER && ExpansionsLoader.IsExpansionInstalled("MakingHistory") && MissionSystem.missions.Count > 0)
				{
					profile = MissionSystem.missions[0].MissionInfo.ShipFolderPath;
				}

				craftBrowserDialog = EditorLogic.fetch.craftBrowserDialog = CraftBrowserDialog.Spawn(
					EditorDriver.editorFacility,
					profile,
					ShipToLoadSelected,
					EditorLogic.fetch.CraftBrowseCancelled,
					EditorLogic.fetch.ship != null && EditorLogic.fetch.ship.parts.Count > 0);

				// if we deactivate the dialog immediately, it will never run its Start logic.  The OnDisable code depends on the data set up in Start.
				hideBrowserInLateUpdate = true;
			}
			else
			{
				craftBrowserDialog.OnConfigNodeSelected = ShipToLoadSelected;
			}
		}
		
		void LateUpdate()
		{
			if (hideBrowserInLateUpdate)
			{
				craftBrowserDialog.Hide();
				hideBrowserInLateUpdate = false;
			}
		}

		private void ShipToLoadSelected(ConfigNode node, CraftBrowserDialog.LoadType loadType)
		{
			if (!Utils.SavedVesselNodeIsScaled(node))
			{
				EditorLogic.fetch.ShipToLoadSelected(node, loadType);
				return;
			}

			var message =
				"This craft file has scaled parts.  Re-saving it will permanently remove TweakScale's information which could affect part mass, cost, size, resources, etc.\n\n" +
				"TweakScale is " + SafetyNetAddon.StateDescription + ".  " +
				SafetyNetAddon.DiagnosticMessage +
				"If you continue, a backup will be saved to saves/" + HighLogic.SaveFolder + "/" + Utils.BackupFolderName + "/Ships.\n" +
				"To remove this check permanently, uninstall TweakScaleSafetyNet.\n" +
				"Proceed with caution."; ;

			var multiOptionDialog = new MultiOptionDialog("", message, "TSSafetyNet Warning", HighLogic.UISkin, 450,
				new DialogGUIButton("Proceed", () => ProceedWithCaution(node, loadType)),
				new DialogGUIButton("Cancel", OnCancel));

			PopupDialog.SpawnPopupDialog(multiOptionDialog, false, HighLogic.UISkin);
		}

		private void OnCancel()
		{
			EditorLogic.fetch.CraftBrowseCancelled();
		}

		private void ProceedWithCaution(ConfigNode node, CraftBrowserDialog.LoadType loadType)
		{
			var originalPath = craftBrowserDialog.selectedEntry.fullFilePath;
			var craftFileName = Path.GetFileName(originalPath);
			var backupDirectory = Path.Combine(Utils.GetSavesDirectory(), HighLogic.SaveFolder, Utils.BackupFolderName, "Ships");
			var backupPath = Path.Combine(backupDirectory, craftFileName);
			Directory.CreateDirectory(backupDirectory);
			File.Copy(originalPath, backupPath, true);

			EditorLogic.fetch.ShipToLoadSelected(node, loadType);
		}
	}
}
