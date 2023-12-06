using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale
{
	[RescalableSceneFilter(RescalableSceneFilter.EditorOnly)]
	internal class CrewManifestHandler : IRescalablePart
	{
		public CrewManifestHandler(Part part)
		{
			// TODO: find a way to not create this for parts that don't have crew capacity (potentially tricky since some parts can dynnamically change it)
			_part = part;
		}

		// note: the TSGenericUpdater should have run first and altered the CrewCapacity value via reflection
		// this updater is just responsible for handling the crew assignment when the capacity changes
		public void OnRescale(ScalingFactor factor)
		{
			// this test might not be correct if there are other modules that can change crew capacity
			if (_part.partInfo.partPrefab.CrewCapacity > 0)
			{
				UpdateCrewManifest(_part);
			}
		}

		Part _part;

		internal static void UpdateCrewManifest(Part part)
		{
			VesselCrewManifest vcm = ShipConstruction.ShipManifest;
			if (vcm == null) { return; }
			PartCrewManifest pcm = vcm.GetPartCrewManifest(part.craftID);
			if (pcm == null) { return; }

			int len = pcm.partCrew.Length;
			int newLen = Math.Min(part.CrewCapacity, part.partInfo.partPrefab.CrewCapacity);
			if (len == newLen) { return; }

			if (EditorLogic.fetch.editorScreen == EditorScreen.Crew)
				EditorLogic.fetch.SelectPanelParts();

			for (int i = 0; i < len; i++)
				pcm.RemoveCrewFromSeat(i);

			pcm.partCrew = new string[newLen];
			for (int i = 0; i < newLen; i++)
				pcm.partCrew[i] = string.Empty;

			ShipConstruction.ShipManifest.SetPartManifest(part.craftID, pcm);
		}
	}
}
