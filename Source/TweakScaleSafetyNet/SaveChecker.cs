using KSP.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TweakScale.SafetyNet
{
	// note that currently, this only runs when loading saves from the main menu.
	// it's possible for someone to run into issues when loading a quicksave too
	[KSPAddon(KSPAddon.Startup.MainMenu, false)]
	class SaveChecker : MonoBehaviour
	{
		MainMenu mainMenu;

		void Start()
		{
			if (SafetyNetAddon.TweakScaleLoaded)
			{
				GameObject.Destroy(gameObject);
				return;
			}

			mainMenu = GameObject.FindObjectOfType<MainMenu>();

			// intercept the "load game" button click so we can control what the load game dialog does
			mainMenu.continueBtn.onTap = OnContinueTap;
		}

		private void OnContinueTap()
		{
			LoadGameDialog.Create(OnLoadGameDismiss, "saves", persistent: true, mainMenu.guiSkinDef.SkinDef);
			InputLockManager.SetControlLock(ControlTypes.MAIN_MENU, "loadGameDialog");
		}
		private void OnLoadGameDismiss(string saveFolder)
		{
			InputLockManager.RemoveControlLock("loadGameDialog");
			if (string.IsNullOrEmpty(saveFolder))
			{
				return;
			}
			ConfigNode configNode = GamePersistence.LoadSFSFile("persistent", saveFolder);
			if (configNode == null)
			{
				ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_485739", saveFolder), 5f, ScreenMessageStyle.UPPER_LEFT);
				return;
			}

			var scaledVessels = Utils.FindScaledVesselsInSave(configNode);
			if (scaledVessels.Count > 0)
			{
				ShowWarningDialog(scaledVessels, saveFolder);
			}
			else
			{
				mainMenu.OnLoadDialogFinished(saveFolder);
			}
		}

		string GetVesselListText(List<string> scaledVessels)
		{
			const int numToInclude = 5;
			int remaining = scaledVessels.Count - numToInclude;

			string suffix = remaining > 0
				? "And " + remaining + " more.\n"
				: "";

			return "Vessels in flight with scaled parts:\n" +
				string.Join("\n", scaledVessels.Take(numToInclude)) + "\n" +
				suffix;
		}

		void ShowWarningDialog(List<string> scaledVessels, string saveFolder)
		{
			var message =
				"Loading this save may permanently remove TweakScale's data which could affect vessel mass, cost, size, resources, etc.\n\n" +
				GetVesselListText(scaledVessels) + "\n" +
				"TweakScale is " + SafetyNetAddon.StateDescription + ".\n\n" +
				SafetyNetAddon.DiagnosticMessage +
				"If you continue, a backup will be saved to saves/" + saveFolder + "/" + Utils.BackupFolderName + ".\n" +
				"To remove this check permanently, uninstall TweakScaleSafetyNet.\n" + 
				"Proceed with caution.";

			var multiOptionDIalog = new MultiOptionDialog("TSSafetyNet-Save", message, "TSSafetyNet Warning", HighLogic.UISkin, 450,
				new DialogGUIButton<string>("Proceed", ProceedWithCaution, saveFolder),
				new DialogGUIButton("Cancel", () => { }));

			PopupDialog.SpawnPopupDialog(multiOptionDIalog, false, HighLogic.UISkin, true);
		}

		void ProceedWithCaution(string saveFolder)
		{
			var backupFileName = KSPUtil.SystemDateTime.DateTimeNow().ToString("yyyy-MM-dd_HH-mm-ss") + ".sfs";
			var saveGamePath = Utils.GetSaveFileFullPath(saveFolder, "persistent");
			var backupDirectory = Path.Combine(Utils.GetSavesDirectory(), saveFolder, Utils.BackupFolderName);
			var backupPath = Path.Combine(backupDirectory, backupFileName);
			Directory.CreateDirectory(backupDirectory);
			File.Copy(saveGamePath, backupPath, overwrite: false);

			mainMenu.OnLoadDialogFinished(saveFolder);
		}
	}
}
