using System;
using System.Collections.Generic;

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

		// Creates an updater for each modules attached to destination part.
		public static IRescalable[] CreateUpdaters(Part part)
		{
			var (partModuleHandlers, partHandlers) = HighLogic.LoadedSceneIsEditor 
				? (editorPartModuleHandlers, editorPartHandlers) 
				: (flightPartModuleHandlers, flightPartHandlers);

			// make a guess about how many updaters we might need (this should be an upper bound so we don't need to resize the list)
			List<IRescalable> updaters = new List<IRescalable>(part.modules.Count + partHandlers.Count);

			foreach (var module in part.modules.modules)
			{
				// walk up the module hierarchy to see if there's a handler
				for (Type moduleType = module.GetType(); moduleType != typeof(PartModule); moduleType = moduleType.BaseType)
				{
					if (partModuleHandlers.TryGetValue(moduleType, out var creator))
					{
						// man I really hate using exceptions for flow control here, but since we don't really have a "factory" class or anything I'm not sure about a better way
						try
						{
							updaters.Add(creator(module));
						}
						catch (RescalableRemoveRegistrationException ex)
						{
							Tools.Log("PartModule updater requests to remove registration for type {0} on part [{1}] (updater registered for type {2}: {3}", module.GetType(), part.partInfo.name, moduleType, ex.Message);
							partModuleHandlers.Remove(moduleType);
						}
						catch (RescalableNotApplicableException ex)
						{
							Tools.Log("PartModule updater disabled itself for module of type {0} on part [{1}] (updater registered for type {2}): {3}", module.GetType(), part.partInfo.name, moduleType, ex.Message);
						}
						catch (Exception ex)
						{
							Tools.LogException(ex, "Failed to create updater for module of type {0} on part [{1}] (updater registered for type {2})", module.GetType(), part.partInfo.name, moduleType);
						}

						break;
					}
				}
			}

			// TODO: does the order here matter much?
			// should IRescalable have a priority value, and then we sort by that?
			for (int partHandlerIndex = 0; partHandlerIndex < partHandlers.Count; ++partHandlerIndex)
			{
				var partHandlerCreator = partHandlers[partHandlerIndex];

				try
				{
					updaters.Add(partHandlerCreator(part));
				}
				catch (RescalableRemoveRegistrationException ex)
				{
					Tools.Log("Part updater requests to remove registration on part [{0}]: {1}", part.partInfo.name, ex.Message);
					partHandlers.RemoveAt(partHandlerIndex);
					--partHandlerIndex; // repeat same index on next iteration
				}
				catch (RescalableNotApplicableException ex)
				{
					// ugh...wish there was a way to indicate which updater it was...do we need to register a name or something with each of these?
					Tools.Log("Part updater disabled itself for part [{0}]: {1}", part.partInfo.name, ex.Message);
				}
				catch (Exception ex)
				{
					Tools.LogException(ex, "Failed to create updater for part [{0}]", part.partInfo.name);
				}
			}

			return updaters.Count == 0 ? null : updaters.ToArray();
		}
	}
}
