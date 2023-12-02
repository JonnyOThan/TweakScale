using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale
{
	[RescalableSceneFilter(RescalableSceneFilter.FlightOnly)]
	class EmitterUpdater : IRescalablePart
	{
		readonly Part _part;
		readonly TweakScale _tweakScaleModule;

		public EmitterUpdater(Part part)
		{
			_part = part;
			_tweakScaleModule = part.FindModuleImplementing<TweakScale>();
		}

		public void OnRescale(ScalingFactor factor)
		{
			
		}
	}
}

