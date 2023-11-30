using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale
{
	// this class is the workhorse of TweakScale.
	// When the part is rescaled, it applies the scaling exponents from the tweakscale module's ScaleType to all applicable members in the part and its modules, using reflection
	class TSGenericUpdater : IRescalablePart
	{
		private readonly Part _part;
		private readonly Part _basePart;
		private readonly TweakScale _ts;

		public TSGenericUpdater(Part part)
		{
			_part = part;
			_basePart = PartLoader.getPartInfoByName(part.partInfo.name).partPrefab;
			_ts = part.FindModuleImplementing<TweakScale>();
		}

		public void OnRescale(ScalingFactor factor)
		{
			ScaleExponents.UpdateObject(_part, _basePart, _ts.ScaleType.Exponents, factor);
		}
	}
}
