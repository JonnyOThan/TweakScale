﻿using System;
using System.Reflection;

namespace TweakScale
{
	[RescalablePartModuleHandler("ModuleFuelTanks")]
	internal class ModuleFuelTanksHandler : IRescalablePart
	{
		#region static reflection stuff

		static Type x_moduleFuelTanksType;
		static FieldInfo x_totalVolume_FieldInfo;

		static ModuleFuelTanksHandler()
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

		public ModuleFuelTanksHandler(PartModule partModule)
		{
			// *ideally* this should never happen because this updater won't be created if no PartModule named ModuleFuelTanks exists.
			// but it's possible that the type changes in a way that we can't find it anymore, or that some other mod creates a ModuleFuelTanks (yikes)
			if (x_totalVolume_FieldInfo == null) return;

			// TODO: this is what the old code did, but it's certainly possible to have more than one ModuleFuelTanks right?
			// is there a better way to get the module's prefab?  by index?
			var modulePrefab = partModule.part.partInfo.partPrefab.Modules["ModuleFuelTanks"];
			_prefabTotalVolume = (double)x_totalVolume_FieldInfo.GetValue(modulePrefab) * 0.001d;

			_mftModule = partModule;
			var tsModule = _mftModule.part.FindModuleImplementing<TweakScale>();
			tsModule.scaleMass = false;
		}

		public void OnRescale(ScalingFactor factor)
		{
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
