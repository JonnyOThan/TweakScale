using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TweakScale
{
	internal static class TweakScaleHandlerDatabase
	{
		// Every kind of updater is registered here, and the correct kind of updater is created for each PartModule.
		static readonly Dictionary<Type, Func<PartModule, IRescalable>> editorPartModuleHandlers = new Dictionary<Type, Func<PartModule, IRescalable>>();
		static readonly Dictionary<Type, Func<PartModule, IRescalable>> flightPartModuleHandlers = new Dictionary<Type, Func<PartModule, IRescalable>>();
		static readonly List<Func<Part, IRescalable>> editorPartHandlers = new List<Func<Part, IRescalable>>();
		static readonly List<Func<Part, IRescalable>> flightPartHandlers = new List<Func<Part, IRescalable>>();

		/// <summary>
		/// Registers an updater for partmodules of type <paramref name="partModuleType"/>.
		/// </summary>
		/// <param name="partModuleType">Type of the PartModule type to update.</param>
		/// <param name="creator">A function that creates an updater for this PartModule type.</param>
		static public void RegisterPartModuleUpdater(Type partModuleType, Func<PartModule, IRescalable> creator, RescalableSceneFilter sceneFilter)
		{
			if (!typeof(PartModule).IsAssignableFrom(partModuleType))
			{
				Tools.LogError("Tried to register an updater for type {0} but it doesn't inherit from PartModule", partModuleType);
				return;
			}
			
			if (sceneFilter != RescalableSceneFilter.EditorOnly)
			{
				AddPartModuleUpdater(partModuleType, creator, flightPartModuleHandlers);
			}
			if (sceneFilter != RescalableSceneFilter.FlightOnly)
			{
				AddPartModuleUpdater(partModuleType, creator, editorPartModuleHandlers);
			}
		}

		static void AddPartModuleUpdater(Type partModuleType, Func<PartModule, IRescalable> creator, Dictionary<Type, Func<PartModule, IRescalable>> handlers)
		{
			if (handlers.ContainsKey(partModuleType))
			{
				// hmm, maybe we should keep both?
				Tools.LogWarning("Updater for PartModule {0} is already registered, replacing it!", partModuleType);
			}
			handlers[partModuleType] = creator;
		}

		static public void RegisterPartUpdater(Func<Part, IRescalable> creator, RescalableSceneFilter sceneFilter)
		{
			if (sceneFilter != RescalableSceneFilter.EditorOnly)
			{
				flightPartHandlers.Add(creator);
			}
			if (sceneFilter != RescalableSceneFilter.FlightOnly)
			{
				editorPartHandlers.Add(creator);
			}
		}

		// reusing a list to reduce memory allocations
		static List<IRescalable> x_scratchHandlers = new List<IRescalable>();

		// Creates an updater for each modules attached to destination part.
		public static IRescalable[] CreateUpdaters(Part part)
		{
			var (partModuleHandlers, partHandlers) = HighLogic.LoadedSceneIsEditor 
				? (editorPartModuleHandlers, editorPartHandlers) 
				: (flightPartModuleHandlers, flightPartHandlers);

			// make a guess about how many updaters we might need (this should be an upper bound so we don't need to resize the list)
			List<IRescalable> updaters = x_scratchHandlers;
			updaters.Capacity = Math.Max(updaters.Capacity, part.modules.Count + partHandlers.Count);

			foreach (var module in part.modules.modules)
			{
				// walk up the module hierarchy to see if there's a handler
				for (Type moduleType = module.GetType(); moduleType != typeof(PartModule); moduleType = moduleType.BaseType)
				{
					if (partModuleHandlers.TryGetValue(moduleType, out var creator))
					{
						try
						{
							var updater = creator(module);
							if (updater != null) updaters.Add(updater);
						}
						catch (Exception ex)
						{
							Tools.LogException(ex, "Failed to create updater for module of type {0} on part [{1}] (updater registered for type {2})", module.GetType(), part.partInfo.name, moduleType);
						}

						break;
					}
				}
			}

			foreach (var partHandlerCreator in partHandlers)
			{
				try
				{
					var updater = partHandlerCreator(part);
					if (updater != null) updaters.Add(updater);
				}
				catch (Exception ex)
				{
					Tools.LogException(ex, "Failed to create updater for part [{0}]", part.partInfo.name);
				}
			}

			if (updaters.Count == 0) return null;

			var result = updaters.ToArray();
			x_scratchHandlers.Clear();

			Array.Sort(result, (a, b) => GetRescalablePriority(a).CompareTo(GetRescalablePriority(b)));

			return result;
		}

		static int GetRescalablePriority(IRescalable rescalable) => rescalable is IRescalablePriority p ? p.Priority : 0;
	}
}
