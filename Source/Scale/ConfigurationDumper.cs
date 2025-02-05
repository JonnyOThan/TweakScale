using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Reflection;

namespace TweakScale
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	internal class ConfigurationDumper : MonoBehaviour
	{
		void Awake()
		{
			GameEvents.OnPartLoaderLoaded.Add(Dump);
			GameObject.DontDestroyOnLoad(gameObject);
		}

		void OnDestroy()
		{
			GameEvents.OnPartLoaderLoaded.Remove(Dump);
		}

		void Dump()
		{
			// TODO: some setting somewhere to control if this runs?
			using (var writer = new StreamWriter(Path.Combine("Logs", "TweakScaleDump.txt")))
			{
				var tweakScaleAvailableParts = PartLoader.LoadedPartsList
					.Where(availablePart => availablePart.partPrefab.HasModuleImplementing<TweakScale>())
					.OrderBy(availablePart => availablePart.name);

				const int maxDepth = 2;

				foreach (var availablePart in tweakScaleAvailableParts)
				{
					var module = availablePart.partPrefab.FindModuleImplementing<TweakScale>();

					Tools.VisitRecursive(availablePart.name, module, (fieldName, value, depth) =>
					{
						Type valueType = value.GetType();

						if (HandleValueDirectly(valueType))
						{
							writer.WriteLine($"{Indent(depth)}{fieldName} = {value}");
						}
						else if (value is IList list)
						{
							var listContents = string.Join(", ", EnumerableToString(list));
							writer.WriteLine($"{Indent(depth)}{fieldName} = [{listContents}]");
						}
						else
						{
							writer.WriteLine($"{Indent(depth)}{valueType.Name} {fieldName}:");
						}
					},
					maxDepth,
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

					writer.WriteLine();
				}
			}
		}

		static IEnumerable<string> EnumerableToString(IEnumerable enumerable)
		{
			foreach (var obj in enumerable)
			{
				yield return obj.ToString();
			}
		}

		static readonly HashSet<Type> typesToTreatAsPrimitive = new HashSet<Type>()
		{
			typeof(string),
			typeof(Vector3),
		};

		static bool HandleValueDirectly(Type type)
		{
			return type.IsEnum || type.IsPrimitive || typesToTreatAsPrimitive.Contains(type);
		}

		static string Indent(int num)
		{
			return new string('\t', num);
		}
	}
}
