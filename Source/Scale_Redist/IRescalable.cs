using System;

namespace TweakScale
{
	/// <summary>
	/// Base class for all rescale handlers.  A PartModule may implement this interface to be notified when its part is scaled.  See TweakScaleRegistrator for how these get discovered and registered, and TweakScaleHandlerDatabase for how they are created
	/// </summary>
	public interface IRescalable
	{
		void OnRescale(ScalingFactor factor);
	}

	/// <summary>
	/// Handles rescaling a specific PartModule type.
	/// Mods may provide a class that implements IRescalable<typeparamref name="T"/> which will be automatically created for parts that have a PartModule of type T and a TweakScale module.
	/// The OnRescale function will be called when the part's scale is changed.
	/// This class must have one of:
	/// <list type="bullet">
	/// <item>A constructor that takes an instance of T (i.e. the PartModule).</item>
	/// <item>A <c>public static IRescalable Create(T)</c> method, which may return null if the handler should not be used for this instance of this part.</item>
	/// </list>  
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRescalable<T> : IRescalable where T : PartModule
	{
	}

	/// <summary>
	/// Handles rescaling a part.
	/// Mods may provide a class that implements IRescalablePart which will automatically be created for parts that have a TweakScale module.  The OnRescale function will be called when the part's scale is changed.
	/// This class must have one of:
	/// <list type="bullet">
	/// <item>A constructor that takes a Part</item>
	/// <item>A <c>public static IRescalable Create(Part)</c> method, which may return null if the handler should not be used for this instance of this part.</item>
	/// </list>  
	/// </summary>
	public interface IRescalablePart : IRescalable
	{
	}

	/// <summary>
	/// Can be added to any type that implements IRescalable.  Controls the ordering of handlers for a given part. 0 is default, lower priority numbers will go earlier 
	/// (i.e. handlers are sorted by priority in ascending order).  use negative numbers if you need to run before most other handlers, and positive numbers to run after most handlers
	/// </summary>
	public interface IRescalablePriority
	{
		int Priority { get; }
	}

	public enum RescalableSceneFilter
	{
		Both,
		EditorOnly,
		FlightOnly,
	}

	/// <summary>
	/// This attribute may be placed on a type that implements IRescalable (or any of its derived types), and it will restrict the scenes in which the handler will be created
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
	/// This attribute may be placed on a type that implements IRescalable.  It will register the handler for part modules of the given name.
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