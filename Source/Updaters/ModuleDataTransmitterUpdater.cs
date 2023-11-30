using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.Updaters
{
	internal class ModuleDataTransmitterUpdater : IRescalable<ModuleDataTransmitter>
	{
		public ModuleDataTransmitterUpdater(ModuleDataTransmitter dataTransmitter)
		{
			_dataTransmitter = dataTransmitter;
		}

		public void OnRescale(ScalingFactor factor)
		{
			_dataTransmitter.UpdatePowerText();
		}

		ModuleDataTransmitter _dataTransmitter;
	}
}
