using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale
{
	/// <summary>
	/// This class updates mmpfxField and properties that are mentioned in TWEAKSCALEEXPONENTS blocks in .cfgs.
	/// It does this by looking up the mmpfxField or property by name through reflection, and scales the exponentValue stored in the base part (i.e. prefab).
	/// </summary>
	class TSGenericUpdater : IRescalable
	{
		private readonly Part _part;
		private readonly Part _basePart;
		private readonly TweakScale _ts;

		public TSGenericUpdater(Part part)
		{
			_part = part;
			_basePart = PartLoader.getPartInfoByName(part.partInfo.name).partPrefab;
			_ts = part.Modules.OfType<TweakScale>().First();
		}

		public void OnRescale(ScalingFactor factor)
		{
			ScaleExponents.UpdateObject(_part, _basePart, _ts.ScaleType.Exponents, factor);
		}
	}
}
