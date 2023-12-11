using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TweakScale
{
	internal static class TweakScaleHandlerDatabase
	{
		// Every kind of handler is registered here, and the correct kind of handler is created for each PartModule.
		static readonly Dictionary<Type, Func<PartModule, IRescalable>> editorPartModuleHandlers = new Dictionary<Type, Func<PartModule, IRescalable>>();
		static readonly Dictionary<Type, Func<PartModule, IRescalable>> flightPartModuleHandlers = new Dictionary<Type, Func<PartModule, IRescalable>>();
		static readonly List<Func<Part, IRescalable>> editorPartHandlers = new List<Func<Part, IRescalable>>();
		static readonly List<Func<Part, IRescalable>> flightPartHandlers = new List<Func<Part, IRescalable>>();

		/// <summary>
		/// Registers a handler for partmodules of type <paramref name="partModuleType"/>.
		/// </summary>
		/// <param name="partModuleType">Type of the PartModule type to handle.</param>
		/// <param name="creator">A function that creates a handler for this PartModule type.</param>
		static public void RegisterPartModuleHandler(Type partModuleType, Func<PartModule, IRescalable> creator, RescalableSceneFilter sceneFilter)
		{
			if (!typeof(PartModule).IsAssignableFrom(partModuleType))
			{
				Tools.LogError("Tried to register a handler for type {0} but it doesn't inherit from PartModule", partModuleType);
				return;
			}
			
			if (sceneFilter != RescalableSceneFilter.EditorOnly)
			{
				AddPartModuleHandler(partModuleType, creator, flightPartModuleHandlers);
			}
			if (sceneFilter != RescalableSceneFilter.FlightOnly)
			{
				AddPartModuleHandler(partModuleType, creator, editorPartModuleHandlers);
			}
		}

		static void AddPartModuleHandler(Type partModuleType, Func<PartModule, IRescalable> creator, Dictionary<Type, Func<PartModule, IRescalable>> handlers)
		{
			if (handlers.ContainsKey(partModuleType))
			{
				// hmm, maybe we should keep both?
				Tools.LogWarning("Handlerr for PartModule {0} is already registered, replacing it!", partModuleType);
			}
			handlers[partModuleType] = creator;
		}

		static public void RegisterPartHandler(Func<Part, IRescalable> creator, RescalableSceneFilter sceneFilter)
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

		// Creates a handler for each modules attached to destination part.
		public static IRescalable[] CreateHandlers(Part part)
		{
			var (partModuleHandlers, partHandlers) = HighLogic.LoadedSceneIsEditor 
				? (editorPartModuleHandlers, editorPartHandlers) 
				: (flightPartModuleHandlers, flightPartHandlers);

			// make a guess about how many handlers we might need (this should be an upper bound so we don't need to resize the list)
			List<IRescalable> handlers = x_scratchHandlers;
			handlers.Capacity = Math.Max(handlers.Capacity, part.modules.Count + partHandlers.Count);

			foreach (var module in part.modules.modules)
			{
				// walk up the module hierarchy to see if there's a handler
				for (Type moduleType = module.GetType(); moduleType != typeof(PartModule); moduleType = moduleType.BaseType)
				{
					if (partModuleHandlers.TryGetValue(moduleType, out var creator))
					{
						try
						{
							var handler = creator(module);
							if (handler != null) handlers.Add(handler);
						}
						catch (Exception ex)
						{
							Tools.LogException(ex, "Failed to create handler for module of type {0} on part [{1}] (handler registered for type {2})", module.GetType(), part.partInfo.name, moduleType);
						}

						break;
					}
				}
			}

			foreach (var partHandlerCreator in partHandlers)
			{
				try
				{
					var handler = partHandlerCreator(part);
					if (handler != null) handlers.Add(handler);
				}
				catch (Exception ex)
				{
					Tools.LogException(ex, "Failed to create handler for part [{0}]", part.partInfo.name);
				}
			}

			if (handlers.Count == 0) return null;

			var result = handlers.ToArray();
			x_scratchHandlers.Clear();

			Array.Sort(result, (a, b) => GetRescalablePriority(a).CompareTo(GetRescalablePriority(b)));

			return result;
		}

		static int GetRescalablePriority(IRescalable rescalable) => rescalable is IRescalablePriority p ? p.Priority : 0;
	}
}
