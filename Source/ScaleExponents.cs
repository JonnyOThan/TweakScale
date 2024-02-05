using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TweakScale
{
	public class ScaleExponents
	{
		public struct ScalingMode
		{
			// if ValueList is non-null, it is the list of values to use for a non-free-scale part (and ExponentValue should be 0)
			// If ValueList is null, then linearScale^ExponentValue is the multiplier that should be used
			public readonly double[] ValueList;
			public readonly double ExponentValue;
			public readonly string Name;
			public bool UseRelativeScaling;

			public ScalingMode(string name, string valueString, bool useRelativeScaling)
			{
				Name = name;
				UseRelativeScaling = useRelativeScaling;

				ExponentValue = double.NaN;
				if (valueString.Contains(','))
				{
					ValueList = Tools.ConvertString(valueString, (double[])null);
					ExponentValue = 0;
				}
				else
				{
					ValueList = null;
					if (!double.TryParse(valueString, out ExponentValue))
					{
						Tools.LogWarning("Invalid exponent {0} for field {1}", valueString, name);
					}
				}
			}
		}

		private readonly string _id;
		private readonly string _name;
		public readonly Dictionary<string, ScalingMode> _exponents;
		private readonly List<string> _ignores;
		private readonly Dictionary<string, ScaleExponents> _children;

		private static Dictionary<string, ScaleExponents> _globalList;

		private const string ExponentConfigName = "TWEAKSCALEEXPONENTS";

		public ScaleExponents GetChild(string name)
		{
			if (_children.TryGetValue(name, out var child))
			{
				return child;
			}
			return null;
		}

		private static bool IsExponentBlock(ConfigNode node)
		{
			return node.name == ExponentConfigName || node.name == "MODULE";
		}

		public static void ModuleManagerPostLoad()
		{
			// Load all TWEAKSCALEEXPONENTS that are globally defined.
			var tmp = GameDatabase.Instance.root.GetConfigs(ExponentConfigName)
				.Select(a => new ScaleExponents(a.config));

			_globalList = tmp
				.GroupBy(a => a._id)
				.Select(a => a.Aggregate(Merge))
				.ToDictionary(a => a._id, a => a);
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
				if (value.name.Equals("-ignore"))
				{
					_ignores.Add(value.value);
				}
				else
				{
					bool useRelativeScaling = value.name.StartsWith("!");
					string name = useRelativeScaling ? value.name.Substring(1) : value.name;
					_exponents[name] = new ScalingMode(name, value.value, useRelativeScaling);
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
		static private void Rescale(MemberUpdater current, MemberUpdater baseValue, ScalingMode scalingMode, ScalingFactor factor, string parentName, StringBuilder info)
		{
			// value-list scaling
			if (scalingMode.ValueList != null)
			{
				if (factor.index == -1)
				{
					Tools.LogWarning("Value list used for freescale part exponent field {0}.{1}", parentName, scalingMode.Name);
					return;
				}
				if (scalingMode.ValueList.Length <= factor.index)
				{
					Tools.LogWarning("Too few values given for {0}.{1}. Expected at least {2}, got {3}", parentName, scalingMode.Name, factor.index + 1, scalingMode.ValueList.Length);
					return;
				}

				double newValue = scalingMode.ValueList[factor.index];

				// if the field itself is a list, we set all the elements
				if (current.Value is IList currentValues)
				{
					for (int i = 0; i < currentValues.Count; ++i)
					{
						currentValues[i] = newValue;
					}
				}
				else
				{
					current.Set(newValue);
				}

				if (info != null)
				{
					info.AppendFormat("\n{1}: {2:0.##}", parentName, current.DisplayName, newValue);
				}
			}
			// polynomial scaling
			else if (scalingMode.ExponentValue != 0)
			{
				double absoluteScalar = Math.Pow(factor.absolute.linear, scalingMode.ExponentValue);
				double multiplyBy = scalingMode.UseRelativeScaling ? Math.Pow(factor.relative.linear, scalingMode.ExponentValue) : absoluteScalar;

				// field is a list - multiply each value
				if (current.Value is IList currentValues && baseValue.Value is IList baseValues)
				{
					for (int i = 0; i < currentValues.Count && i < baseValues.Count; ++i)
					{
						if (currentValues[i] is float)
						{
							currentValues[i] = (float)baseValues[i] * multiplyBy;
						}
						else if (currentValues[i] is double)
						{
							currentValues[i] = (double)baseValues[i] * multiplyBy;
						}
						else if (currentValues[i] is Vector3)
						{
							currentValues[i] = (Vector3)baseValues[i] * (float)multiplyBy;
						}
					}
				}
				// single field
				else
				{
					BuildInfoLine(current, baseValue, scalingMode, factor, parentName, info, absoluteScalar);

					current.Scale(multiplyBy, baseValue);
				}
			}
		}

		private static void BuildInfoLine(MemberUpdater current, MemberUpdater baseValue, ScalingMode scalingMode, ScalingFactor factor, string parentName, StringBuilder info, double absoluteScalar)
		{
			if (info == null || factor.absolute.linear == 1) return;

			if (current.ObjectType == typeof(PartResource) && current.Name != "maxAmount") return; // TODO: more general way to exclude certain fields?

			try
			{
				if (TryGetUnscaledValue(baseValue, scalingMode, factor, out double unscaledValue))
				{
					if (unscaledValue > 0)
					{
						// Should this get moved into the MemberUpdate.Scale method?
						info.AppendFormat("\n{1}: {2:0.##} x {3:0.##} = {4:0.##}", parentName, current.DisplayName, unscaledValue, absoluteScalar, unscaledValue * absoluteScalar);
					}
				}
				else
				{
					info.AppendFormat("\n{0}: x {1:0.##}", current.DisplayName, absoluteScalar);
				}
			}
			catch(Exception e)
			{
				Tools.LogException(e);
			}
		}

		private static bool TryGetUnscaledValue(MemberUpdater baseValue, ScalingMode scalingMode, ScalingFactor factor, out double unscaledValue)
		{
			unscaledValue = 0;
			try
			{
				if (scalingMode.UseRelativeScaling)
				{
					// relative = absolute / current
					// current = absolute / relative
					double oldScaleFactor = factor.absolute.linear / factor.relative.linear;
					double oldMultiplier = Math.Pow(oldScaleFactor, scalingMode.ExponentValue);
					unscaledValue = Convert.ToDouble(baseValue.Value) / oldMultiplier;
				}
				else
				{
					unscaledValue = Convert.ToDouble(baseValue.Value);
				}
			}
			catch(FormatException)
			{
				return false;
			}
			catch(InvalidCastException)
			{
				return false;
			}

			return true;
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
		private void UpdateFields(object obj, object baseObj, ScalingFactor factor, Part part, string parentName, StringBuilder info)
		{
			if (obj == null)
				return;

			if (ShouldIgnore(part))
				return;

			if (obj is IEnumerable enumerable)
			{
				UpdateEnumerable(enumerable, (IEnumerable)baseObj, factor, parentName, info, part);
				return;
			}

			foreach (var nameExponentKV in _exponents)
			{
				var value = MemberUpdater.Create(obj, nameExponentKV.Key);
				if (value == null)
				{
					continue;
				}

				var baseValue = nameExponentKV.Value.UseRelativeScaling ? value : MemberUpdater.Create(baseObj, nameExponentKV.Key);
				Rescale(value, baseValue, nameExponentKV.Value, factor, parentName, info);
			}

			foreach (var child in _children)
			{
				var childName = child.Key;
				var childObjField = MemberUpdater.Create(obj, childName);
				if (childObjField == null || child.Value == null)
					continue;
				var baseChildObjField = MemberUpdater.Create(baseObj, childName);
				child.Value.UpdateFields(childObjField.Value, (baseChildObjField ?? childObjField).Value, factor, part, childName, info);
			}
		}

		/// <summary>
		/// For IEnumerables (arrays, lists, etc), we want to update the items, not the list.
		/// </summary>
		/// <param name="obj">The list whose items we want to update.</param>
		/// <param name="prefabObj">The corresponding list in the prefab.</param>
		/// <param name="factor">The scaling factor.</param>
		/// <param name="part">The part the object is on.</param>
		private void UpdateEnumerable(IEnumerable obj, IEnumerable prefabObj, ScalingFactor factor, string parentName, StringBuilder info, Part part = null)
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
				UpdateFields(item.Current, item.Prefab, factor, part, parentName, info);
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

		public static void UpdateObject(Part part, Part prefabObj, Dictionary<string, ScaleExponents> exponents, ScalingFactor factor, StringBuilder info)
		{
			if (exponents.ContainsKey("Part"))
			{
				exponents["Part"].UpdateFields(part, prefabObj, factor, part, "Part", info);
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
						var moduleName = ((PartModule)m.Current).moduleName;
						if (e.Key != moduleName)
						{
							e.Value.UpdateFields(m.Current, m.Prefab, factor, part, moduleName, info);
						}
					}
				}
			}

			foreach (var modExp in modulesAndExponents)
			{
				var moduleName = ((PartModule)modExp.Current).moduleName;
				modExp.Exponents.UpdateFields(modExp.Current, modExp.Prefab, factor, part, moduleName, info);
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
			if (exponents.TryGetValue("TweakScale", out var moduleExponents))
			{
				if (moduleExponents._exponents.TryGetValue("MassScale", out var massExponent))
				{
					if (massExponent.ValueList != null)
					{
						Tools.LogWarning("getMassExponent not yet implemented for this kind of config");
						return 0;
					}

					return (float)massExponent.ExponentValue;
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
					if (costExponent.ValueList != null)
					{
						Tools.LogWarning("getCostExponent not yet implemented for this kind of config");
						return 0;
					}
					return (float)costExponent.ExponentValue;
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

			string massExponent = Exponents["Part"]._exponents["mass"].ExponentValue.ToString();
			if (!Exponents.ContainsKey("TweakScale"))
			{
				ConfigNode node = new ConfigNode();
				node.name = "TweakScale";
				node.id = "TweakScale";
				node.AddValue("DryCost", massExponent);
				node.AddValue("MassScale", massExponent);
				Exponents.Add("TweakScale", new ScaleExponents(node));
			}
			else
			{
				if (!Exponents["TweakScale"]._exponents.ContainsKey("DryCost"))
				{
					Exponents["TweakScale"]._exponents.Add("DryCost", new ScalingMode("DryCost", massExponent, false));
				}

				// move mass exponent into TweakScale module
				Exponents["TweakScale"]._exponents["MassScale"] = new ScalingMode("MassScale", massExponent, false);
			}
			Exponents["Part"]._exponents.Remove("mass");
		}

		public static Dictionary<string, ScaleExponents> CreateExponentsForModule(ConfigNode node, Dictionary<string, ScaleExponents> parent)
		{
			var scaleExponents = node.nodes
				.OfType<ConfigNode>()
				.Where(IsExponentBlock)
				.Select(a => new ScaleExponents(a));

			var local = new Dictionary<string, ScaleExponents>();
			foreach (var child in scaleExponents)
			{
				if (local.ContainsKey(child._id))
				{
					Tools.LogError("Duplicated exponents for key {0}", child._id);
				}
				else
				{
					local.Add(child._id, child);
				}
			}

			ScaleExponents.treatMassAndCost(local);

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
