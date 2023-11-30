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
	class EmitterUpdater : IRescalablePart, IUpdateable
	{
		private struct EmitterData
		{
			public readonly float MinSize, MaxSize, Shape1D;
			public readonly Vector2 Shape2D;
			public readonly Vector3 Shape3D, LocalVelocity, Force;

			public EmitterData(KSPParticleEmitter pe)
			{
				MinSize = pe.minSize;
				MaxSize = pe.maxSize;
				LocalVelocity = pe.localVelocity;
				Shape1D = pe.shape1D;
				Shape2D = pe.shape2D;
				Shape3D = pe.shape3D;
				Force = pe.force;
			}
		}

		readonly Part _part;
		readonly TweakScale _tweakScaleModule;

		bool _rescale = true;
		readonly Dictionary<KSPParticleEmitter, EmitterData> _scales = new Dictionary<KSPParticleEmitter, EmitterData>();

		public EmitterUpdater(Part part)
		{
			_part = part;
			_tweakScaleModule = part.FindModuleImplementing<TweakScale>();
		}

		public void OnRescale(ScalingFactor factor)
		{
			_rescale = true;
		}

		private void UpdateParticleEmitter(KSPParticleEmitter pe)
		{
			if (pe == null)
			{
				return;
			}
			var factor = _tweakScaleModule.currentScaleFactor;

			if (!_scales.ContainsKey(pe))
			{
				_scales[pe] = new EmitterData(pe);
			}
			var ed = _scales[pe];

			pe.minSize = ed.MinSize * factor;
			pe.maxSize = ed.MaxSize * factor;
			pe.shape1D = ed.Shape1D * factor;
			pe.shape2D = ed.Shape2D * factor;
			pe.shape3D = ed.Shape3D * factor;

			pe.force = ed.Force * factor;

			pe.localVelocity = ed.LocalVelocity * factor;
		}

		public void OnUpdate()
		{
			if (!_rescale)
				return;

			var fxn = _part.FindModulesImplementing<EffectBehaviour>();
			_rescale = fxn.Count != 0;
			foreach (var fx in fxn)
			{
				if (fx is ModelMultiParticleFX mmpfx)
				{
					var p = mmpfx.emitters;
					if (p == null)
						continue;
					foreach (var pe in p)
					{
						UpdateParticleEmitter(pe);
					}
					_rescale = false;
				}
				else if (fx is ModelParticleFX mpfx)
				{
					var pe = mpfx.emitter;
					UpdateParticleEmitter(pe);
					_rescale = false;
				}
			}
		}
	}
}

