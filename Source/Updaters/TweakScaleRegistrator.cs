using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TweakScale
{
	[KSPAddon(KSPAddon.Startup.Instantly, true)]
	class TweakScaleRegistrator : RescalableRegistratorAddon
	{
		override public void OnStart()
		{
			var genericRescalable = Tools.GetAllTypes()
				.Where(IsGenericRescalable)
				.ToArray();

			foreach (var gen in genericRescalable)
			{
				var t = gen.GetInterfaces()
					.First(a => a.IsGenericType &&
					a.GetGenericTypeDefinition() == typeof(IRescalable<>));

				RegisterGenericRescalable(gen, t.GetGenericArguments()[0]);
			}
		}

		private static void RegisterGenericRescalable(Type resc, Type arg)
		{
			var c = resc.GetConstructor(new[] { arg });
			if (c == null)
				return;
			Func<PartModule, IRescalable> creator = pm => (IRescalable)c.Invoke(new object[] { pm });

			TweakScaleUpdater.RegisterUpdater(arg, creator);
		}

		private static bool IsGenericRescalable(Type t)
		{
			return !t.IsGenericType && t.GetInterfaces()
				.Any(a => a.IsGenericType &&
				a.GetGenericTypeDefinition() == typeof(IRescalable<>));
		}
	}

	static class TweakScaleUpdater
	{
		// Every kind of updater is registered here, and the correct kind of updater is created for each PartModule.
		static readonly Dictionary<Type, Func<PartModule, IRescalable>> Ctors = new Dictionary<Type, Func<PartModule, IRescalable>>();

		/// <summary>
		/// Registers an updater for partmodules of type <paramref name="pm"/>.
		/// </summary>
		/// <param name="pm">Type of the PartModule type to update.</param>
		/// <param name="creator">A function that creates an updater for this PartModule type.</param>
		static public void RegisterUpdater(Type pm, Func<PartModule, IRescalable> creator)
		{
			Ctors[pm] = creator;
		}

		// Creates an updater for each modules attached to destination part.
		public static IEnumerable<IRescalable> CreateUpdaters(Part part)
		{
			var myUpdaters = part
				.Modules.Cast<PartModule>()
				.Select(CreateUpdater)
				.Where(updater => updater != null);
			foreach (var updater in myUpdaters)
			{
				yield return updater;
			}
			yield return new TSGenericUpdater(part);
			yield return new EmitterUpdater(part);
		}

		private static IRescalable CreateUpdater(PartModule module)
		{
			var updater = module as IRescalable;
			if (updater != null)
			{
				return updater;
			}
			return Ctors.ContainsKey(module.GetType()) ? Ctors[module.GetType()](module) : null;
		}
	}
}
