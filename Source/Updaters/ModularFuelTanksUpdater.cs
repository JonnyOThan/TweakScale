﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale
{
	// this is unfortunate, because it really should be a IRescalable<ModuleFuelTanks>, but we don't want to have a hard dependency on ModularFuelTanks
	internal class ModularFuelTanksUpdater : IRescalableManualRegistration
	{
		#region static reflection stuff

		static Type x_moduleFuelTanksType;
		static FieldInfo x_totalVolume_FieldInfo;

		static ModularFuelTanksUpdater()
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

		internal static void Register()
		{
			if (x_totalVolume_FieldInfo != null)
			{
				TweakScaleUpdater.RegisterPartModuleUpdater(x_moduleFuelTanksType, partModule => new ModularFuelTanksUpdater(partModule), RescalableSceneFilter.Both);
			}
		}

		#endregion

		public ModularFuelTanksUpdater(PartModule partModule)
		{
			// *ideally* this should never happen as long as we check before registering the updater
			if (x_totalVolume_FieldInfo == null)
			{
				throw new RescalableRemoveRegistrationException("ModuleFuelTanks not loaded");
			}

			_mftModule = partModule;
			var tsModule = _mftModule.part.FindModuleImplementing<TweakScale>();
			tsModule.scaleMass = false; // TODO: need to investigate why we do this and see if it still makes sense
		}

		public void OnRescale(ScalingFactor factor)
		{
			double oldVol = (double)x_totalVolume_FieldInfo.GetValue(_mftModule) * 0.001d;
			var data = new BaseEventDetails(BaseEventDetails.Sender.USER);
			data.Set<string>("volName", "Tankage");
			data.Set<double>("newTotalVolume", oldVol * factor.absolute.cubic); // TODO: should this use relative instead of absolute scale?  this is the original version, but it seems wrong
			_mftModule.part.SendEvent("OnPartVolumeChanged", data, 0);
		}

		PartModule _mftModule;
	}
}
