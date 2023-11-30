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
    public interface IRescalable<T> : IRescalable
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
    /// A rescalable updater that will not be registered automatically - it must be manually registered with the updater database
    /// </summary>
    public interface IRescalableManualRegistration : IRescalable
    {
    }

    public enum RescalableSceneFilter
    {
        Both,
        EditorOnly,
        FlightOnly,
    }

    /// <summary>
    /// This attribute may be placed on a type that implements IRescalable (or any of its derived types), and it will restrict the scenes in which the updater will be created
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class RescalableSceneFilterAttribute : System.Attribute
    {
        public RescalableSceneFilterAttribute(RescalableSceneFilter filter)
        {
            Filter = filter;
        }

        public readonly RescalableSceneFilter Filter;
    }

    // TODO: maybe these should be replaced with an attribute that binds a delegate?  is that possible?  Or can the attribute have virtual functions?
    // e.g. the registrator checks the the attribute and calls the delegate before registering it or adding it to the part

    /// <summary>
    /// A rescalable type may throw this exception from their constructor to indicate that they should not be applied to this part.
    /// </summary>
    public class RescalableNotApplicableException : System.Exception
    {
        public RescalableNotApplicableException(string message) : base(message) { }
    }

    /// <summary>
    /// A rescalable type may throw this exception from their constructor to indicate that this handler should be unregistered (e.g. dependencies not satisfied, etc).
    /// This is permanent for the rest of the play session.
    /// </summary>
    public class RescalableRemoveRegistrationException : System.Exception
    {
		public RescalableRemoveRegistrationException(string message) : base(message) { }
	}
}