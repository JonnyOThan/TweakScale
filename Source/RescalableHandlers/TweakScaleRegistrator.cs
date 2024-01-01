using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TweakScale
{
	/// <summary>
	/// This addon gathers all IRescalable types and stores them in the handler database
	/// </summary>
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	class TweakScaleRegistrator : MonoBehaviour
	{
		void Start()
		{
			// TODO: should this use GetModulesImplementingInterface instead?
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
						RegisterHandlerType(type, rescalableInterfaceType);
					}
				}
				catch (Exception ex)
				{
					// we catch this here so that we don't skip all types from this assembly
					Tools.LogException(ex);
				}
			});
		}

		static void RegisterPartModuleHandler(Type handlerType, Type partModuleType, RescalableSceneFilter sceneFilter)
		{
			var createMethod = handlerType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public, null, new Type[] { partModuleType }, null);
			var constructor = handlerType.GetConstructor(new[] { partModuleType });
			Func<PartModule, IRescalable> creator = null;

			if (createMethod != null)
			{
				// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
				creator = partModule => (IRescalable)createMethod.Invoke(null, new object[] { partModule });
			}
			else if (constructor != null)
			{
				// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
				creator = partModule => (IRescalable)constructor.Invoke(new object[] { partModule });
			}
			else
			{
				Tools.LogError("handler {0} for PartModule type {1} doesn't have an appropriate constructor or Create method", handlerType, partModuleType);
				return;
			}

			Tools.Log("Found a handler {0} for PartModule type {1}: sceneFilter: {2}", handlerType, partModuleType, sceneFilter);
			TweakScaleHandlerDatabase.RegisterPartModuleHandler(partModuleType, creator, sceneFilter);
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

		private static void RegisterHandlerType(Type handlerType, Type rescalableInterfaceType)
		{
			var sceneFilterAttribute = handlerType.GetCustomAttribute<RescalableSceneFilterAttribute>();
			RescalableSceneFilter sceneFilter = sceneFilterAttribute != null ? sceneFilterAttribute.Filter : RescalableSceneFilter.Both;

			// register a partmodule handler by name (could be multiple attributes on the same handler)
			var partModuleHandlerAttributes = handlerType.GetCustomAttributes<RescalablePartModuleHandlerAttribute>();
			if (partModuleHandlerAttributes.Any())
			{
				foreach (var attribute in partModuleHandlerAttributes)
				{
					Type partModuleType = GetPartModuleTypeByName(attribute.PartModuleName);
					if (partModuleType == null)
					{
						Tools.LogWarning("Found a handler {0} for PartModule named {1} but no matching PartModule type exists", handlerType, attribute.PartModuleName);
					}
					else
					{
						RegisterPartModuleHandler(handlerType, partModuleType, sceneFilter);
					}
				}
			}
			// PartModule implementing IRescalable
			// note this is higher priority than IRescalable<T> because of FAR: https://github.com/ferram4/Ferram-Aerospace-Research/blob/787a30bc9deab0bde87591f0cc973ec3b0dd2de9/FerramAerospaceResearch/FARPartGeometry/GeometryPartModule.cs#L56
			else if (typeof(PartModule).IsAssignableFrom(handlerType))
			{
				Tools.Log("Found a PartModule {0} that implements IRescalable: sceneFilter: {1}", handlerType, sceneFilter);

				Func<PartModule, IRescalable> creator = partModule => (partModule as IRescalable);
				TweakScaleHandlerDatabase.RegisterPartModuleHandler(handlerType, creator, sceneFilter);
			}
			// generic partmodule handler (IRescalable<T> where T is the PartModule type)
			else if (rescalableInterfaceType.IsGenericType && typeof(IRescalable<>).IsAssignableFrom(rescalableInterfaceType.GetGenericTypeDefinition()))
			{
				var partModuleType = rescalableInterfaceType.GetGenericArguments()[0];
				RegisterPartModuleHandler(handlerType, partModuleType, sceneFilter);
				
			}
			// part rescaler (IRescalablePart)
			else if (typeof(IRescalablePart).IsAssignableFrom(rescalableInterfaceType))
			{
				var createMethod = handlerType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public, null, RescalablePartConstructorArgumentTypes, null);
				var constructor = handlerType.GetConstructor(RescalablePartConstructorArgumentTypes);
				Func<Part, IRescalable> creator = null;

				if (createMethod != null)
				{
					// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
					creator = part => (IRescalable)createMethod.Invoke(null, new object[] { part });
				}
				else if (constructor != null)
				{
					// TODO: can we use a bound delegate here to reduce allocation and reflection overhead?
					creator = part => (IRescalable)constructor.Invoke(new object[] { part });
				}
				else
				{
					Tools.LogError("Part handler {0} doesn't have an appropriate constructor or Create method", handlerType);
					return;
				}

				Tools.Log("Found a handler {0} for Parts: sceneFilter: {1}", handlerType, sceneFilter);
				TweakScaleHandlerDatabase.RegisterPartHandler(creator, sceneFilter);
			}
			// someone implemented IRescalable directly?
			else
			{
				Tools.LogError("Found an IRescalable type {0} but don't know what to do with it", handlerType);
			}
		}

		static readonly Type[] RescalablePartConstructorArgumentTypes = new[] { typeof(Part) };
	}
}
