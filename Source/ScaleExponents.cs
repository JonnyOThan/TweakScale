using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TweakScale
{
	public class ScaleExponents
	{
		public struct ScalingMode
		{
			public readonly string Exponent;
			public bool UseRelativeScaling;

			public ScalingMode(string exponent, bool useRelativeScaling)
				: this()
			{
				Exponent = exponent;
				UseRelativeScaling = useRelativeScaling;
			}
		}

		private readonly string _id;
		private readonly string _name;
		public readonly Dictionary<string, ScalingMode> _exponents;
		private readonly List<string> _ignores;
		private readonly Dictionary<string, ScaleExponents> _children;

		private static Dictionary<string, ScaleExponents> _globalList;
		private static bool _globalListLoaded;

		private const string ExponentConfigName = "TWEAKSCALEEXPONENTS";

		private static bool IsExponentBlock(ConfigNode node)
		{
			return node.name == ExponentConfigName || node.name == "MODULE";
		}

		/// <summary>
		/// Load all TWEAKSCALEEXPONENTS that are globally defined.
		/// </summary>
		public static void LoadGlobalExponents()
		{
			// TODO: this should probably use the ModuleManager database callback, so that it handles reloads better
			if (_globalListLoaded)
				return;

			var tmp = GameDatabase.Instance.GetConfigs(ExponentConfigName)
				.Select(a => new ScaleExponents(a.config));

			_globalList = tmp
				.GroupBy(a => a._id)
				.Select(a => a.Aggregate(Merge))
				.ToDictionary(a => a._id, a => a);
			_globalListLoaded = true;
		}

		/// <summary>
		/// Creates modules copy of the ScaleExponents.
		/// </summary>
		/// <returns>A copy of the object on which the function is called.</returns>
		private ScaleExponents Clone()
		{
			return new ScaleExponents(this);
		}

		private ScaleExponents(ScaleExponents source)
		{
			_id = source._id;
			_exponents = source._exponents.Clone();
			_children = source
				._children
				.Select(a => new KeyValuePair<string, ScaleExponents>(a.Key, a.Value.Clone()))
				.ToDictionary(a => a.Key, a => a.Value);
			_ignores = new List<string>(source._ignores);
		}

		private ScaleExponents(ConfigNode node, ScaleExponents source = null)
		{
			_id = IsExponentBlock(node) ? node.GetValue("name") : node.name;
			_name = node.GetValue("name");
			if (_id == null)
			{
				_id = "";
			}

			if (IsExponentBlock(node))
			{
				if (string.IsNullOrEmpty(_id))
				{
					_id = "Part";
					_name = "Part";
				}
			}

			_exponents = new Dictionary<string, ScalingMode>();
			_children = new Dictionary<string, ScaleExponents>();
			_ignores = new List<string>();

			foreach (var value in node.values.OfType<ConfigNode.Value>().Where(a => a.name != "name"))
			{
				if (value.name.StartsWith("!"))
				{
					_exponents[value.name.Substring(1)] = new ScalingMode(value.value, true);
				}
				else if (value.name.Equals("-ignore"))
				{
					_ignores.Add(value.value);
				}
				else
				{
					_exponents[value.name] = new ScalingMode(value.value, false);
				}
			}

			foreach (var childNode in node.nodes.OfType<ConfigNode>())
			{
				_children[childNode.name] = new ScaleExponents(childNode);
			}

			if (source != null)
			{
				Merge(this, source);
			}
		}

		/// <summary>
		/// Merge two ScaleExponents. All the values in <paramref name="source"/> that are not already present in <paramref name="destination"/> will be added to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">The ScaleExponents to update.</param>
		/// <param name="source">The ScaleExponents to add to <paramref name="destination"/></param>
		/// <returns>The updated exponentValue of <paramref name="destination"/>. Note that this exponentValue is also changed, so using the return exponentValue is not necessary.</returns>
		public static ScaleExponents Merge(ScaleExponents destination, ScaleExponents source)
		{
			if (destination._id != source._id)
			{
				Tools.LogWarning("Wrong merge target! A name {0}, B name {1}", destination._id, source._id);
			}
			foreach (var value in source._exponents.Where(value => !destination._exponents.ContainsKey(value.Key)))
			{
				destination._exponents[value.Key] = value.Value;
			}
			foreach (var value in source._ignores.Where(value => !destination._ignores.Contains(value)))
			{
				destination._ignores.Add(value);
			}
			foreach (var child in source._children)
			{
				if (destination._children.ContainsKey(child.Key))
				{
					Merge(destination._children[child.Key], child.Value);
				}
				else
				{
					destination._children[child.Key] = child.Value.Clone();
				}
			}
			return destination;
		}

		/// <summary>
		/// Rescales destination exponentValue according to its associated exponent.
		/// </summary>
		/// <param name="current">The current exponentValue.</param>
		/// <param name="baseValue">The unscaled exponentValue, gotten from the prefab.</param>
		/// <param name="name">The name of the field.</param>
		/// <param name="scalingMode">Information on exactly how to scale this.</param>
		/// <param name="factor">The rescaling factor.</param>
		/// <returns>The rescaled exponentValue.</returns>
		static private void Rescale(MemberUpdater current, MemberUpdater baseValue, string name, ScalingMode scalingMode, ScalingFactor factor)
		{
			var exponentValue = scalingMode.Exponent;
			var exponent = double.NaN;
			double[] values = null;
			if (exponentValue.Contains(','))
			{
				if (factor.index == -1)
				{
					Tools.LogWarning("Value list used for freescale part exponent field {0}: {1}", name, exponentValue);
					return;
				}
				values = Tools.ConvertString(exponentValue, new double[] { });
				if (values.Length <= factor.index)
				{
					Tools.LogWarning("Too few values given for {0}. Expected at least {1}, got {2}: {3}", name, factor.index + 1, values.Length, exponentValue);
					return;
				}
			}
			else if (!double.TryParse(exponentValue, out exponent))
			{
				Tools.LogWarning("Invalid exponent {0} for field {1}", exponentValue, name);
			}

			double multiplyBy = 1;
			if (!double.IsNaN(exponent))
				multiplyBy = Math.Pow(scalingMode.UseRelativeScaling ? factor.relative.linear : factor.absolute.linear, exponent);

			if (current.MemberType.GetInterface("IList") != null)
			{
				var v = (IList)current.Value;
				var v2 = (IList)baseValue.Value;
				if (v == null)
				{
					Tools.LogWarning("current.Value == null!");
					return;
				}

				for (var i = 0; i < v.Count && i < v2.Count; ++i)
				{
					if (values != null)
					{
						v[i] = values[factor.index];
					}
					else if (!double.IsNaN(exponent) && (exponent != 0))
					{
						if (v[i] is float)
						{
							v[i] = (float)v2[i] * multiplyBy;
						}
						else if (v[i] is double)
						{
							v[i] = (double)v2[i] * multiplyBy;
						}
						else if (v[i] is Vector3)
						{
							v[i] = (Vector3)v2[i] * (float)multiplyBy;
						}
					}
				}
			}

			if (values != null)
			{
				if (current.MemberType == typeof(float))
				{
					current.Set((float)values[factor.index]);
				}
				else if (current.MemberType == typeof(float))
				{
					current.Set(values[factor.index]);
				}

			}
			else if (!double.IsNaN(exponent) && (exponent != 0))
			{
				current.Scale(multiplyBy, baseValue);
			}
		}

		private bool ShouldIgnore(Part part)
		{
			return _ignores.Any(v => part.Modules.Contains(v));
		}

		/// <summary>
		/// Rescale the field of <paramref name="obj"/> according to the exponents of the ScaleExponents and <paramref name="factor"/>.
		/// </summary>
		/// <param name="obj">The object to rescale.</param>
		/// <param name="baseObj">The corresponding object in the prefab.</param>
		/// <param name="factor">The new scale.</param>
		/// <param name="part">The part the object is on.</param>
		private void UpdateFields(object obj, object baseObj, ScalingFactor factor, Part part)
		{
			if ((object)obj == null)
				return;

			/*if (obj is PartModule && obj.GetType().Name != _id)
            {
                Tools.LogWf("This ScaleExponent is intended for {0}, not {1}", _id, obj.GetType().Name);
                return;
            }*/

			if (ShouldIgnore(part))
				return;

			var enumerable = obj as IEnumerable;
			if (enumerable != null)
			{
				UpdateEnumerable(enumerable, (IEnumerable)baseObj, factor, part);
				return;
			}

			foreach (var nameExponentKV in _exponents)
			{
				var value = MemberUpdater.Create(obj, nameExponentKV.Key);
				if (value == null)
				{
					continue;
				}

				var baseValue = nameExponentKV.Value.UseRelativeScaling ? null : MemberUpdater.Create(baseObj, nameExponentKV.Key);
				Rescale(value, baseValue ?? value, nameExponentKV.Key, nameExponentKV.Value, factor);
			}

			foreach (var child in _children)
			{
				var childName = child.Key;
				var childObjField = MemberUpdater.Create(obj, childName);
				if (childObjField == null || child.Value == null)
					continue;
				var baseChildObjField = MemberUpdater.Create(baseObj, childName);
				child.Value.UpdateFields(childObjField.Value, (baseChildObjField ?? childObjField).Value, factor, part);
			}
		}

		/// <summary>
		/// For IEnumerables (arrays, lists, etc), we want to update the items, not the list.
		/// </summary>
		/// <param name="obj">The list whose items we want to update.</param>
		/// <param name="prefabObj">The corresponding list in the prefab.</param>
		/// <param name="factor">The scaling factor.</param>
		/// <param name="part">The part the object is on.</param>
		private void UpdateEnumerable(IEnumerable obj, IEnumerable prefabObj, ScalingFactor factor, Part part = null)
		{
			var prefabObjects = prefabObj as object[] ?? prefabObj.Cast<object>().ToArray();
			var urrentObjects = obj as object[] ?? obj.Cast<object>().ToArray();

			if (prefabObj == null || urrentObjects.Length != prefabObjects.Length)
			{
				prefabObjects = ((object)null).Repeat().Take(urrentObjects.Length).ToArray();
			}

			foreach (var item in urrentObjects.Zip(prefabObjects, ModuleAndPrefab.Create))
			{
				if (!string.IsNullOrEmpty(_name) && _name != "*") // Operate on specific elements, not all.
				{
					var childName = item.Current.GetType().GetField("name");
					if (childName != null)
					{
						if (childName.FieldType != typeof(string) || (string)childName.GetValue(item.Current) != _name)
						{
							continue;
						}
					}
				}
				UpdateFields(item.Current, item.Prefab, factor, part);
			}
		}

		struct ModuleAndPrefab
		{
			public object Current { get; private set; }
			public object Prefab { get; private set; }

			private ModuleAndPrefab(object current, object prefab)
				: this()
			{
				Current = current;
				Prefab = prefab;
			}

			public static ModuleAndPrefab Create(object current, object prefab)
			{
				return new ModuleAndPrefab(current, prefab);
			}
		}

		struct ModulesAndExponents
		{
			public object Current { get; private set; }
			public object Prefab { get; private set; }
			public ScaleExponents Exponents { get; private set; }

			private ModulesAndExponents(ModuleAndPrefab modules, ScaleExponents exponents)
				: this()
			{
				Current = modules.Current;
				Prefab = modules.Prefab;
				Exponents = exponents;
			}

			public static ModulesAndExponents Create(ModuleAndPrefab modules, KeyValuePair<string, ScaleExponents> exponents)
			{
				return new ModulesAndExponents(modules, exponents.Value);
			}
		}

		public static void UpdateObject(Part part, Part prefabObj, Dictionary<string, ScaleExponents> exponents, ScalingFactor factor)
		{
			if (exponents.ContainsKey("Part"))
			{
				exponents["Part"].UpdateFields(part, prefabObj, factor, part);
			}

			// TODO: this will probably break terribly if anyone messes with modules at runtime
			var modulePairs = part.Modules.Zip(prefabObj.Modules, ModuleAndPrefab.Create);

			var modulesAndExponents = modulePairs.Join(exponents,
										modules => ((PartModule)modules.Current).moduleName,
										exps => exps.Key,
										ModulesAndExponents.Create).ToArray();

			// include derived classes
			foreach (var e in exponents)
			{
				Type type = GetType(e.Key);
				if (type == null)
				{
					continue;
				}
				foreach (var m in modulePairs)
				{
					if (m.Current.GetType().IsSubclassOf(type))
					{
						if (e.Key != ((PartModule)m.Current).moduleName)
						{
							e.Value.UpdateFields(m.Current, m.Prefab, factor, part);
						}
					}
				}
			}

			foreach (var modExp in modulesAndExponents)
			{
				modExp.Exponents.UpdateFields(modExp.Current, modExp.Prefab, factor, part);
			}
		}

		public static Type GetType(string typeName)
		{
			var type = Type.GetType(typeName);
			if (type != null) return type;
			foreach (var a in AssemblyLoader.loadedAssemblies)
			{
				try
				{
					type = a.assembly.GetType(typeName);
					if (type != null)
						return type;
				}
				catch { }
			}
			return null;
		}

		public static float getDryMassExponent(Dictionary<string, ScaleExponents> exponents)
		{
			if (exponents.TryGetValue("Part", out var partExponents))
			{
				if (partExponents._exponents.TryGetValue("mass", out var massExponent))
				{
					if (massExponent.Exponent.Contains(','))
					{
						Tools.LogWarning("getMassExponent not yet implemented for this kind of config");
						return 0;
					}
					if (!float.TryParse(massExponent.Exponent, out var exponent))
					{
						Tools.LogWarning("parsing error for mass exponent");
						return 0;
					}
					return exponent;
				}
			}

			return 0;
		}

		public static float getDryCostExponent(Dictionary<string, ScaleExponents> exponents)
		{
			if (exponents.TryGetValue("TweakScale", out var moduleExponents))
			{
				if (moduleExponents._exponents.TryGetValue("DryCost", out var costExponent))
				{
					if (costExponent.Exponent.Contains(','))
					{
						Tools.LogWarning("getCostExponent not yet implemented for this kind of config");
						return 0;
					}
					if (!float.TryParse(costExponent.Exponent, out var exponent))
					{
						Tools.LogWarning("parsing error for cost exponent");
						return 0;
					}

					return exponent;
				}
			}

			// if that failed, use the mass exponent instead
			return getDryMassExponent(exponents);
		}

		// if there is no dryCost exponent, use the mass exponent instead
		public static void treatMassAndCost(Dictionary<string, ScaleExponents> Exponents)
		{
			if (!Exponents.ContainsKey("Part"))
				return;

			if (!Exponents["Part"]._exponents.ContainsKey("mass"))
				return;

			string massExponent = Exponents["Part"]._exponents["mass"].Exponent;
			if (!Exponents.ContainsKey("TweakScale"))
			{
				ConfigNode node = new ConfigNode();
				node.name = "TweakScale";
				node.id = "TweakScale";
				node.AddValue("!DryCost", massExponent);
				node.AddValue("MassScale", massExponent);
				Exponents.Add("TweakScale", new ScaleExponents(node));
			}
			else
			{
				if (Exponents["TweakScale"]._exponents.ContainsKey("DryCost"))
				{
					// force relative scaling
					ScalingMode tmp = Exponents["TweakScale"]._exponents["DryCost"];
					tmp.UseRelativeScaling = true;
					Exponents["TweakScale"]._exponents["DryCost"] = tmp;
				}
				else
				{
					Exponents["TweakScale"]._exponents.Add("DryCost", new ScalingMode(massExponent, true));
				}

				// move mass exponent into TweakScale module
				if (Exponents["TweakScale"]._exponents.ContainsKey("MassScale"))
					Tools.LogWarning("treatMassAndCost: TweakScale/MassScale exponent already exists!");
				else
					Exponents["TweakScale"]._exponents.Add("MassScale", new ScalingMode(massExponent, false));
			}
			//Exponents["Part"]._exponents.Remove("mass");
		}

		public static Dictionary<string, ScaleExponents> CreateExponentsForModule(ConfigNode node, Dictionary<string, ScaleExponents> parent)
		{
			var local = node.nodes
				.OfType<ConfigNode>()
				.Where(IsExponentBlock)
				.Select(a => new ScaleExponents(a))
				.ToDictionary(a => a._id);

			foreach (var pExp in parent.Values)
			{
				if (local.ContainsKey(pExp._id))
				{
					Merge(local[pExp._id], pExp);
				}
				else
				{
					local[pExp._id] = pExp;
				}
			}

			foreach (var gExp in _globalList.Values)
			{
				if (local.ContainsKey(gExp._id))
				{
					Merge(local[gExp._id], gExp);
				}
				else
				{
					local[gExp._id] = gExp;
				}
			}

			return local;
		}
	}
}
