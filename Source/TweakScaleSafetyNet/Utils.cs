using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.SafetyNet
{
	static class Utils
	{
		public const string BackupFolderName = "TSSafetyNet";

		internal static string GetSaveFilePath(string saveFolder, string saveName)
		{
			return Path.ChangeExtension(Path.Combine(saveFolder, saveName), ".sfs");
		}

		internal static string GetSaveFileFullPath(string saveFolder, string saveName)
		{
			return Path.Combine(GetSavesDirectory(), GetSaveFilePath(saveFolder, saveName));
		}

		internal static string GetSavesDirectory()
		{
			return KSPUtil.ApplicationRootPath + "saves";
		}
		internal static bool ModuleNodeIsScaled(ConfigNode moduleNode)
		{
			float currentScaleFactor = 1.0f;
			float legacyCurrentScale = -1.0f;
			float legacyDefaultScale = -1.0f;

			foreach (var field in moduleNode.values.values)
			{
				switch (field.name)
				{
					case "name":
						if (field.value != "TweakScale") return false;
						break;
					case "currentScaleFactor":
						float.TryParse(field.value, out currentScaleFactor);
						break;
					case "currentScale":
						float.TryParse(field.value, out legacyCurrentScale);
						break;
					case "defaultScale":
						float.TryParse(field.value, out legacyDefaultScale);
						break;
				}
			}

			if (currentScaleFactor > 0)
			{
				return currentScaleFactor != 1;
			}

			if (legacyCurrentScale > 0 && legacyDefaultScale > 0)
			{
				return legacyCurrentScale != legacyDefaultScale;
			}

			return false;
		}

		// this might be usable for craft files too
		internal static bool SavedVesselNodeIsScaled(ConfigNode vesselNode)
		{
			foreach (var partNode in vesselNode.nodes.nodes)
			{
				if (partNode.name != "PART") continue;

				foreach (var moduleNode in partNode.nodes.nodes)
				{
					if (moduleNode.name != "MODULE") continue;

					if (ModuleNodeIsScaled(moduleNode))
					{
						return true;
					}
				}
			}

			return false;
		}

		internal static List<string> FindScaledVesselsInSave(ConfigNode node)
		{
			List<string> result = new List<string>();

			var flightState = node.GetNode("GAME")?.GetNode("FLIGHTSTATE");

			if (flightState != null)
			{
				foreach (var vesselNode in flightState.nodes.nodes)
				{
					if (vesselNode.name != "VESSEL") continue;

					if (SavedVesselNodeIsScaled(vesselNode))
					{
						result.Add(vesselNode.GetValue("name"));
					}
				}
			}

			return result;
		}
	}
}
