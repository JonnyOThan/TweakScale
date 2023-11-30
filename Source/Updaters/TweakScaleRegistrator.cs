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
				if (type.IsGenericType) return;
				
				var rescalableInterface = type.GetInterfaces()
					.FirstOrDefault(i => typeof(IRescalable<>).IsAssignableFrom(i));

				if (rescalableInterface != null)
				{
					RegisterGenericRescalable(type, rescalableInterface.GetGenericArguments()[0]);
				}
			});
		}

		private static void RegisterGenericRescalable(Type updaterType, Type partModuleType)
		{
			var constructor = updaterType.GetConstructor(new[] { partModuleType });
			if (constructor == null)
			{
				Tools.LogError("Updater {0} for PartModule type {1} doesn't have an appropriate constructor", updaterType, partModuleType);
				return;
			}

			// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
			Func<PartModule, IRescalable> creator = partModule => (IRescalable)constructor.Invoke(new object[] { partModule });

			Tools.Log("Found an updater {0} for PartModule type {1}", updaterType, partModuleType);
			TweakScaleUpdater.RegisterUpdater(partModuleType, creator);
		}
	}

	static class TweakScaleUpdater
	{
		// Every kind of updater is registered here, and the correct kind of updater is created for each PartModule.
		static readonly Dictionary<Type, Func<PartModule, IRescalable>> Ctors = new Dictionary<Type, Func<PartModule, IRescalable>>();

		/// <summary>
		/// Registers an updater for partmodules of type <paramref name="partModuleType"/>.
		/// </summary>
		/// <param name="partModuleType">Type of the PartModule type to update.</param>
		/// <param name="creator">A function that creates an updater for this PartModule type.</param>
		static public void RegisterUpdater(Type partModuleType, Func<PartModule, IRescalable> creator)
		{
			// TODO: should there be a way to register updaters for parts as well?  could use this function and just check if Type == typeof(Part)
			
			if (!typeof(PartModule).IsAssignableFrom(partModuleType))
			{
				Tools.LogError("Tried to register an updater for type {0} but it doesn't inherit from PartModule", partModuleType);
				return;
			}
			if (Ctors.ContainsKey(partModuleType))
			{
				Tools.LogWarning("Updater for {0} is already registered, replacing it!", partModuleType);
			}
			Ctors[partModuleType] = creator;
		}

		// Creates an updater for each modules attached to destination part.
		public static IRescalable[] CreateUpdaters(Part part)
		{
			const int numHardcodedUpdaters = 2;
			List<IRescalable> updaters = new List<IRescalable>(part.modules.Count + numHardcodedUpdaters);

			foreach (var module in part.modules.modules)
			{
				if (module is IRescalable updater)
				{
					updaters.Add(updater);
				}
				else
				{
					for (Type moduleType = module.GetType(); moduleType != typeof(PartModule); moduleType = moduleType.BaseType)
					{
						if (Ctors.TryGetValue(moduleType, out var creator))
						{
							try
							{
								updaters.Add(creator(module));
							}
							catch (Exception ex)
							{
								Tools.LogException(ex, "Failed to create updater for module of type {0} (updater registered for type {1})", module.GetType(), moduleType);
							}

							break;
						}
					}
				}
			}

			updaters.Add(new TSGenericUpdater(part));
			updaters.Add(new EmitterUpdater(part)); // TODO: don't do this if there are no emitters
			// TODO: lights?

			return updaters.ToArray();
		}
	}
}
