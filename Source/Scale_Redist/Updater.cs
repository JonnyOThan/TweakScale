using System;

namespace TweakScale
{
	/// <summary>
	/// Base class for all rescale listeners.  A PartModule may implement this interface to be notified when its part is scaled.
	/// </summary>
	public interface IRescalable
	{
		void OnRescale(ScalingFactor factor);
	}

	/// <summary>
	/// Handles rescaling a specific PartModule type.
	/// Mods may provide a class that implements IRescalable<typeparamref name="T"/> which will be automatically created for parts that have a PartModule of type T and a TweakScale module.
	/// This class must have a constructor that takes an instance of T (i.e. the PartModule).  The OnRescale function will be called when the part's scale is changed.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRescalable<T> : IRescalable where T : PartModule
	{
	}

	/// <summary>
	/// Handles rescaling a part.
	/// Mods may provide a class that implements IRescalablePart which will automatically be created for parts that have a TweakScale module.
	/// This class must have a constructor that takes a Part.  The OnRescale function will be called when the part's scale is changed.
	/// </summary>
	public interface IRescalablePart : IRescalable
	{
	}

	/// <summary>
	/// Can be added to any type that implements IRescalable.  Controls the ordering of updaters for a given part. 0 is default, lower priority numbers will go earlier 
	/// (i.e. updaters are sorted by priority in ascending order).  use negative numbers if you need to run before most other updaters, and positive numbers to run after most updaters
	/// </summary>
	public interface IRescalablePriority
	{
		int Priority { get; }
	}

#if false
	// sketching out what this might look like eventually...

	/// <summary>
	/// Base type for metadata about IRescalables
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class RescalableFilterAttribute : System.Attribute
	{
		public virtual bool ShouldAutoRegister() { return true; }
		public virtual bool ShouldCreateForPart(Part part) { return true; }
		public virtual bool ShouldCreateForPartModule(PartModule partModule) { return true; }
	}
#endif

	public enum RescalableSceneFilter
	{
		Both,
		EditorOnly,
		FlightOnly,
	}

	/// <summary>
	/// This attribute may be placed on a type that implements IRescalable (or any of its derived types), and it will restrict the scenes in which the updater will be created
	/// </summary>
	public class RescalableSceneFilterAttribute : System.Attribute
	{
		public RescalableSceneFilterAttribute(RescalableSceneFilter filter)
		{
			Filter = filter;
		}

		// I considered making this just a derived RescalableFilterAttribute but it's probably good for the database to keep things separate to speed up creation
		public readonly RescalableSceneFilter Filter;
	}

	/// <summary>
	/// This attribute may be placed on a type that implements IRescalable (or any of its derived types).  It will register the updater for part modules of the given name
	/// This is functionally identical to IRescalable<T>, but it does not require that you have a hard dependency on the assembly that defines the type.
	/// </summary>
	public class RescalablePartModuleHandlerAttribute : System.Attribute
	{
		public RescalablePartModuleHandlerAttribute(string partModuleName)
		{
			PartModuleName = partModuleName;
		}

		public readonly string PartModuleName;
	}
}