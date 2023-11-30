using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale
{
	/// <summary>
	/// This addon gathers all IRescalable types and stores them in the updater database
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	class TweakScaleRegistrator : MonoBehaviour
	{
		void Start()
		{
			AssemblyLoader.loadedAssemblies.TypeOperation(type =>
			{
				try
				{
					var rescalableInterfaceType = type.GetInterfaces()
						.FirstOrDefault(i => typeof(IRescalable).IsAssignableFrom(i));

					if (rescalableInterfaceType != null)
					{
						RegisterUpdaterType(type, rescalableInterfaceType);
					}
				}
				catch (Exception ex)
				{
					// we catch this here so that we don't skip all types from this assembly
					Tools.LogException(ex);
				}
			});

			// one-offs:
			ModularFuelTanksUpdater.Register();
		}

		private static void RegisterUpdaterType(Type updaterType, Type rescalableInterfaceType)
		{
			var sceneFilterAttribute = updaterType.GetCustomAttribute<RescalableSceneFilterAttribute>();
			RescalableSceneFilter sceneFilter = sceneFilterAttribute != null ? sceneFilterAttribute.Filter : RescalableSceneFilter.Both;

			// generic partmodule updater (IRescalable<T> where T is the PartModule type)
			if (typeof(IRescalable<>).IsAssignableFrom(rescalableInterfaceType))
			{
				var partModuleType = rescalableInterfaceType.GetGenericArguments()[0];
				var constructor = updaterType.GetConstructor(new[] { partModuleType });

				if (constructor == null)
				{
					Tools.LogError("Updater {0} for PartModule type {1} doesn't have an appropriate constructor", updaterType, partModuleType);
					return;
				}

				// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
				Func<PartModule, IRescalable> creator = partModule => (IRescalable)constructor.Invoke(new object[] { partModule });

				Tools.Log("Found an updater {0} for PartModule type {1}: sceneFilter: {2}", updaterType, partModuleType, sceneFilter);
				TweakScaleUpdater.RegisterPartModuleUpdater(partModuleType, creator, sceneFilter);
			}
			// part rescaler (IRescalablePart)
			else if (typeof(IRescalablePart).IsAssignableFrom(rescalableInterfaceType))
			{
				var constructor = updaterType.GetConstructor(RescalablePartConstructorArgumentTypes);

				if (constructor == null)
				{
					Tools.LogError("Part updater {0} doesn't have an appropriate constructor", updaterType);
					return;
				}

				// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
				Func<Part, IRescalable> creator = part => (IRescalable)constructor.Invoke(new object[] { part });

				Tools.Log("Found an updater {0} for Parts: sceneFilter: {1}", updaterType, sceneFilter);
				TweakScaleUpdater.RegisterPartUpdater(creator, sceneFilter);
			}
			// PartModule implementing IRescalable
			else if (typeof(PartModule).IsAssignableFrom(updaterType))
			{
				Tools.Log("Found a PartModule {0} that implements IRescalable: sceneFilter: {1}", updaterType, sceneFilter);

				Func<PartModule, IRescalable> creator = partModule => (partModule as IRescalable);
				TweakScaleUpdater.RegisterPartModuleUpdater(updaterType, creator, sceneFilter);
			}
			// someone implemented IRescalable directly?
			else
			{
				Tools.LogError("Found an IRescalable type {0} but don't know what to do with it", updaterType);
			}
		}

		static readonly Type[] RescalablePartConstructorArgumentTypes = new[] { typeof(Part) };
	}

	// TODO: move this to its own file, e.g. UpdaterDatabase or something
	internal static class TweakScaleUpdater
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

			// TODO: does the order here matter much?  there is that weird double-application in the update.
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
