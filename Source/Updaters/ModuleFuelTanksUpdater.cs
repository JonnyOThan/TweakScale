using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale
{
	// this is unfortunate, because it really should be a IRescalable<ModuleFuelTanks>, but we don't want to have a hard dependency on ModularFuelTanks
	internal class ModuleFuelTanksUpdater : IRescalablePart
	{
		#region static reflection stuff

		static Type x_moduleFuelTanksType;
		static FieldInfo x_totalVolume_FieldInfo;

		static ModuleFuelTanksUpdater()
		{
			x_moduleFuelTanksType = AssemblyLoader.GetClassByName(typeof(PartModule), "ModuleFuelTanks");

			if (x_moduleFuelTanksType == null)
			{
				Tools.Log("ModuleFuelTanks not loaded");
				return;
			}

			x_totalVolume_FieldInfo = x_moduleFuelTanksType.GetField("totalVolume", BindingFlags.Public | BindingFlags.Instance);

			if (x_totalVolume_FieldInfo == null)
			{
				Tools.LogError("ModuleFuelTanks was loaded but could not find the 'totalVolume' field!");
				return;
			}
		}

		#endregion

		public ModuleFuelTanksUpdater(PartModule partModule)
		{
			// *ideally* this should never happen as long as we check before registering the updater
			if (x_totalVolume_FieldInfo == null) return;

			// TODO: this is what the old code did, but it's certainly possible to have more than one ModuleFuelTanks right?
			// is there a better way to get the module's prefab?  by index?
			var modulePrefab = partModule.part.partInfo.partPrefab.Modules["ModuleFuelTanks"];
			_prefabTotalVolume = (double)x_totalVolume_FieldInfo.GetValue(modulePrefab) * 0.001d;

			_mftModule = partModule;
			var tsModule = _mftModule.part.FindModuleImplementing<TweakScale>();
			tsModule.scaleMass = false; // TODO: need to investigate why we do this and see if it still makes sense
		}

		public void OnRescale(ScalingFactor factor)
		{
			// TODO: implement a way to avoid creating this when TestFlightCore isn't installed
			if (x_totalVolume_FieldInfo == null) return;
			
			var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
			data.Set<string>("volName", "Tankage");
			data.Set<double>("newTotalVolume", _prefabTotalVolume * factor.absolute.cubic);
			_mftModule.part.SendEvent("OnPartVolumeChanged", data, 0);
		}

		PartModule _mftModule;
		double _prefabTotalVolume;
	}
}
