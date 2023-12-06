using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale.Updaters
{
	internal class ModuleDataTransmitterHandler : IRescalable<ModuleDataTransmitter>
	{
		public ModuleDataTransmitterHandler(ModuleDataTransmitter dataTransmitter)
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
