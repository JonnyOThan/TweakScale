using System.Collections.Generic;
using UnityEngine;
using Waterfall;

namespace TweakScale.Waterfall
{
	internal class ModuleWaterfallFXHandler : IRescalable<ModuleWaterfallFX>
	{
		ModuleWaterfallFX _module;
		List<Vector3> _unscaledMeshScale;
		List<Vector3> _unscaledPosition;
		float _scaleFactor = 1.0f;

		public ModuleWaterfallFXHandler(ModuleWaterfallFX module)
		{
			_module = module;
			UpdateScale();
		}

		public void OnRescale(ScalingFactor factor)
		{
			_scaleFactor = factor.absolute.linear;
			UpdateScale();
		}

		internal void EffectsInitialized()
		{
			// make sure we don't do this more than once....
			// this feels pretty hacky, may not work with the waterfall editor..
			if (_unscaledMeshScale == null && _module.FX.Count > 0)
			{
				_unscaledMeshScale = new List<Vector3>(_module.FX.Count);
				_unscaledPosition = new List<Vector3>(_module.FX.Count);

				for (int i = 0; i < _module.FX.Count; i++)
				{
					_unscaledMeshScale.Add(_module.FX[i].TemplateScaleOffset);
					_unscaledPosition.Add(_module.FX[i].TemplatePositionOffset);
				}
			}

			UpdateScale();
		}

		internal void UpdateScale()
		{
			if (_unscaledMeshScale == null) return;

			for (int i = 0; i < _unscaledMeshScale.Count; i++)
			{
				WaterfallEffect fx = _module.FX[i];

				// note that the position ends up driving the localPosition of the plume, so it's already been scaled by the model scale
				fx.ApplyTemplateOffsets(_unscaledPosition[i], fx.TemplateRotationOffset, _unscaledMeshScale[i] * _scaleFactor);
			}
		}
	}
}
