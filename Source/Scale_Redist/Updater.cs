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

    // TODO: should ManualRegistration be an attribute?
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
}