using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakScale
{
	public static class Tech
	{
		public static bool IsUnlocked(string techId)
		{
			if (ResearchAndDevelopment.Instance == null) return true;
			if (techId == "") return true;
			if (ResearchAndDevelopment.Instance.protoTechNodes.TryGetValue(techId, out var techNode))
			{
				return techNode.state == RDTech.State.Available;
			}

			return false;
		}
	}

	/// <summary>
	/// Configuration values for TweakScale.
	/// </summary>
	public class ScaleType
	{
		private float[] _scaleFactors = { };
		private readonly string[] _scaleNames = { };
		public readonly Dictionary<string, ScaleExponents> Exponents = new Dictionary<string, ScaleExponents>();

		public readonly bool IsFreeScale = true;
		public readonly string[] TechRequired = { };
		public float DefaultScale = -1;
		public float[] IncrementSlide = { };
		public string Suffix = null;
		public readonly string Name = null;

		public float[] AllScaleFactors
		{
			get
			{
				return _scaleFactors;
			}
		}

		public float[] GetUnlockedScaleFactors()
		{
			if (TechRequired.Length == 0)
				return _scaleFactors;
			var result = _scaleFactors.ZipFilter(TechRequired, Tech.IsUnlocked).ToArray();
			return result;
		}

		public string[] GetUnlockedScaleNames()
		{
			if (TechRequired.Length == 0)
				return _scaleNames;

			var result = _scaleNames.ZipFilter(TechRequired, Tech.IsUnlocked).ToArray();
			return result;
		}

		// config is a TweakScale module config OR SCALETYPE
		public ScaleType(ConfigNode scaleConfig)
		{
			if (scaleConfig != null)
			{
				Name = scaleConfig.GetValue("type");

				// copy values from global scale type
				if (Name != null)
				{
					if (globalScaleTypes.TryGetValue(Name, out ScaleType scaleType))
					{
						// search scaletype for values
						IsFreeScale = scaleType.IsFreeScale;
						DefaultScale = scaleType.DefaultScale;
						Suffix = scaleType.Suffix;
						_scaleFactors = scaleType._scaleFactors;
						_scaleNames = scaleType._scaleNames;
						TechRequired = scaleType.TechRequired;

						Exponents = scaleType.Exponents;
					}
				}
				// if we're loading the global scale types
				else if (globalScaleTypes == null)
				{
					Name = scaleConfig.GetValue("name");
				}
				else
				{
					Name = "";
				}

				// search config for overrides
				IsFreeScale = Tools.ConfigValue(scaleConfig, "freeScale", IsFreeScale);
				DefaultScale = Tools.ConfigValue(scaleConfig, "defaultScale", DefaultScale);
				Suffix = Tools.ConfigValue(scaleConfig, "suffix", Suffix);
				_scaleFactors = Tools.ConfigValue(scaleConfig, "scaleFactors", _scaleFactors);
				_scaleNames = Tools.ConfigValue(scaleConfig, "scaleNames", _scaleNames).Select(a => a.Trim()).ToArray();
				TechRequired = Tools.ConfigValue(scaleConfig, "techRequired", TechRequired).Select(a => a.Trim()).ToArray();

				Exponents = ScaleExponents.CreateExponentsForModule(scaleConfig, Exponents);
			}

			if (IsFreeScale && (_scaleFactors.Length > 1))
			{
				bool error = false;
				for (int i = 0; i < _scaleFactors.Length - 1; i++)
					if (_scaleFactors[i + 1] <= _scaleFactors[i])
						error = true;

				if (error)
				{
					Tools.LogWarning("scaleFactors must be in ascending order! \n{0}", this.ToString());
					_scaleFactors = new float[0];
				}
			}

			// fill in missing values
			if ((DefaultScale <= 0) || (_scaleFactors.Length == 0))
				RepairScaletype(scaleConfig);

			if (!IsFreeScale && (_scaleFactors.Length != _scaleNames.Length))
			{
				if (_scaleNames.Length != 0)
					Tools.LogWarning("Wrong number of scaleFactors compared to scaleNames in scaleType \"{0}\": {1} scaleFactors vs {2} scaleNames\n{3}", Name, _scaleFactors.Length, _scaleNames.Length, this.ToString());

				_scaleNames = new string[_scaleFactors.Length];
				for (int i = 0; i < _scaleFactors.Length; i++)
					_scaleNames[i] = _scaleFactors[i].ToString();
			}

			if (!IsFreeScale)
			{
				DefaultScale = Tools.Closest(DefaultScale, AllScaleFactors);
			}
			DefaultScale = Mathf.Clamp(DefaultScale, _scaleFactors.Min(), _scaleFactors.Max());

			if (IncrementSlide.Length == 0)
			{
				IncrementSlide = new float[_scaleFactors.Length - 1];
				for (var i = 0; i < _scaleFactors.Length - 1; i++)
					IncrementSlide[i] = (_scaleFactors[i + 1] - _scaleFactors[i]) / 50f;
			}

			var numTechs = TechRequired.Length;
			if ((numTechs > 0) && (numTechs != _scaleFactors.Length))
			{
				Tools.LogWarning("Wrong number of techRequired compared to scaleFactors in scaleType \"{0}\": {1} scaleFactors vs {2} techRequired", Name, _scaleFactors.Length, TechRequired.Length);
				if (numTechs < _scaleFactors.Length)
				{
					var lastTech = TechRequired[TechRequired.Length - 1];
					TechRequired = TechRequired.Concat(lastTech.Repeat()).Take(_scaleFactors.Length).ToArray();
				}
			}

			bool allowGrow = true, allowShrink = true;
			int resizeCount = _scaleFactors.Length;

			// if we don't allow shrinking, remove all the scale factors that are smaller than the default scale
			if (scaleConfig.TryGetValue(nameof(allowShrink), ref allowShrink) && !allowShrink)
			{
				int keepIndex = Array.FindLastIndex(_scaleFactors, factor => factor <= DefaultScale);
				if (keepIndex >= 0) // this should probably always succeed...
				{
					resizeCount = _scaleFactors.Length - keepIndex;
					_scaleFactors[keepIndex] = DefaultScale;
					Array.Copy(_scaleFactors, keepIndex, _scaleFactors, 0, resizeCount);
					Array.Copy(IncrementSlide, keepIndex, IncrementSlide, 0, resizeCount);
					if (!IsFreeScale)
					{
						Array.Copy(_scaleNames, keepIndex, _scaleNames, 0, resizeCount);
					}
					if (numTechs > 0)
					{
						Array.Copy(TechRequired, keepIndex, TechRequired, 0, resizeCount);
					}
				}
			}
			else if (scaleConfig.TryGetValue(nameof(allowGrow), ref allowGrow) && !allowGrow)
			{
				int removeIndex = Array.FindIndex(_scaleFactors, factor => factor > DefaultScale);

				if (removeIndex > 0)
				{
					resizeCount = removeIndex;
				}
			}

			if (resizeCount < _scaleFactors.Length)
			{
				Array.Resize(ref _scaleFactors, resizeCount);
				Array.Resize(ref IncrementSlide, resizeCount);
				if (!IsFreeScale)
				{
					Array.Resize(ref _scaleNames, resizeCount);
				}
				if (numTechs > 0)
				{
					Array.Resize(ref TechRequired, resizeCount);
				}
			}
		}

		private void RepairScaletype(ConfigNode scaleConfig)
		{
			if ((DefaultScale <= 0) && (_scaleFactors.Length == 0))
			{
				DefaultScale = 100;
				if (Suffix == null)
					Suffix = "%";
				if (IncrementSlide.Length == 0)
					IncrementSlide = new float[] { 1f, 1f, 1f, 2f, 5f };
			}
			if ((DefaultScale > 0) && (_scaleFactors.Length == 0))
			{
				_scaleFactors = new float[] { DefaultScale / 10f, DefaultScale / 4f, DefaultScale / 2f, DefaultScale, DefaultScale * 2f, DefaultScale * 4f };
			}
			else if ((DefaultScale <= 0) && (_scaleFactors.Length > 0))
			{
				DefaultScale = _scaleFactors[0];
			}
			else
			{
				// Legacy support: min/maxValue
				float minScale = -1;
				float maxScale = -1;
				if (scaleConfig != null)
				{
					minScale = Tools.ConfigValue(scaleConfig, "minScale", minScale);    // deprecated!
					maxScale = Tools.ConfigValue(scaleConfig, "maxScale", maxScale);    // deprecated!
				}

				if ((minScale > 0) && (maxScale > 0))
				{
					if (minScale > 0 && maxScale > 0)
					{
						if (DefaultScale > minScale && DefaultScale < maxScale)
							_scaleFactors = new float[] { minScale, DefaultScale, maxScale };
						else
							_scaleFactors = new float[] { minScale, maxScale };
					}
				}
			}
		}

		public override string ToString()
		{
			var result = "ScaleType {";
			result += "\n name = " + Name;
			result += "\n isFreeScale = " + IsFreeScale;
			result += "\n " + _scaleFactors.Length + " scaleFactors = ";
			foreach (var s in _scaleFactors)
				result += s + "  ";
			result += "\n " + _scaleNames.Length + " scaleNames = ";
			foreach (var s in _scaleNames)
				result += s + "  ";
			result += "\n " + IncrementSlide.Length + " incrementSlide = ";
			foreach (var s in IncrementSlide)
				result += s + "  ";
			result += "\n " + TechRequired.Length + " TechRequired = ";
			foreach (var s in TechRequired)
				result += s + "  ";
			result += "\n defaultScale = " + DefaultScale;
			//result += " scaleNodes = " + ScaleNodes + "\n";
			//result += "	minValue = " + MinValue + "\n";
			//result += "	maxValue = " + MaxValue + "\n";
			return result + "\n}";
		}

		static Dictionary<string, ScaleType> globalScaleTypes;

		public static void ModuleManagerPostLoad()
		{
			globalScaleTypes = null;
			var scaleTypeUrlConfigs = GameDatabase.Instance.root.GetConfigs("SCALETYPE");
			var scaleTypes = scaleTypeUrlConfigs.Select(urlConfig => new ScaleType(urlConfig.config));
			globalScaleTypes = scaleTypes.ToDictionary(scaleType => scaleType.Name);
		}
	}
}
