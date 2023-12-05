using System;
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
					if (type.IsInterface) return;

					// since IRescalable<T> inherits from IRescalable, GetInterfaces() would report both interfaces
					// but we're only interested in the "most derived" interface type
					var rescalableInterfaces= type.GetInterfaces().Where(i => typeof(IRescalable).IsAssignableFrom(i));
					var leafInterfaces = rescalableInterfaces.Except(rescalableInterfaces.SelectMany(i => i.GetInterfaces()));
					var rescalableInterfaceType = leafInterfaces.FirstOrDefault();

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
		}

		static void RegisterPartModuleHandler(Type updaterType, Type partModuleType, RescalableSceneFilter sceneFilter)
		{
			var constructor = updaterType.GetConstructor(new[] { partModuleType });

			if (constructor == null)
			{
				Tools.LogError("Updater {0} for PartModule type {1} doesn't have an appropriate constructor", updaterType, partModuleType);
				return;
			}

			// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
			Func<PartModule, IRescalable> creator = partModule => (IRescalable)constructor.Invoke(new object[] { partModule });

			Tools.Log("Found an updater {0} for PartModule type {1}: sceneFilter: {2}", updaterType, partModuleType, sceneFilter);
			TweakScaleHandlerDatabase.RegisterPartModuleUpdater(partModuleType, creator, sceneFilter);
		}

		static Type GetPartModuleTypeByName(string name)
		{
			try
			{
				return AssemblyLoader.GetClassByName(typeof(PartModule), name);
			}
			catch(Exception ex)
			{
				Tools.LogException(ex, "Caught exception while trying to find a PartModule type named {0}", name);
			}

			return null;
		}

		private static void RegisterUpdaterType(Type updaterType, Type rescalableInterfaceType)
		{
			var sceneFilterAttribute = updaterType.GetCustomAttribute<RescalableSceneFilterAttribute>();
			RescalableSceneFilter sceneFilter = sceneFilterAttribute != null ? sceneFilterAttribute.Filter : RescalableSceneFilter.Both;

			// register a partmodule handler by name (could be multiple attributes on the same handler)
			var partModuleHandlerAttributes = updaterType.GetCustomAttributes<RescalablePartModuleHandlerAttribute>();
			if (partModuleHandlerAttributes.Any())
			{
				foreach (var attribute in partModuleHandlerAttributes)
				{
					Type partModuleType = GetPartModuleTypeByName(attribute.PartModuleName);
					if (partModuleType == null)
					{
						Tools.LogWarning("Found an updater {0} for PartModule named {1} but no matching PartModule type exists", updaterType, attribute.PartModuleName);
					}
					else
					{
						RegisterPartModuleHandler(updaterType, partModuleType, sceneFilter);
					}
				}
			}
			// generic partmodule updater (IRescalable<T> where T is the PartModule type)
			else if (rescalableInterfaceType.IsGenericType && typeof(IRescalable<>).IsAssignableFrom(rescalableInterfaceType.GetGenericTypeDefinition()))
			{
				var partModuleType = rescalableInterfaceType.GetGenericArguments()[0];
				RegisterPartModuleHandler(updaterType, partModuleType, sceneFilter);
				
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
				TweakScaleHandlerDatabase.RegisterPartUpdater(creator, sceneFilter);
			}
			// PartModule implementing IRescalable
			else if (typeof(PartModule).IsAssignableFrom(updaterType))
			{
				Tools.Log("Found a PartModule {0} that implements IRescalable: sceneFilter: {1}", updaterType, sceneFilter);

				Func<PartModule, IRescalable> creator = partModule => (partModule as IRescalable);
				TweakScaleHandlerDatabase.RegisterPartModuleUpdater(updaterType, creator, sceneFilter);
			}
			// someone implemented IRescalable directly?
			else
			{
				Tools.LogError("Found an IRescalable type {0} but don't know what to do with it", updaterType);
			}
		}

		static readonly Type[] RescalablePartConstructorArgumentTypes = new[] { typeof(Part) };
	}
}
