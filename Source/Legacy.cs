namespace TweakScale
{
    /// <summary>
    /// Converts from Gaius' GoodspeedTweakScale to updated TweakScale.
    /// </summary>
    public class GoodspeedTweakScale : TweakScale
    {
        private bool _updated;

        protected override void Setup()
        {
            base.Setup();
            if (_updated)
                return;
            tweakName = (int)tweakScale;
            tweakScale = ScaleFactors[tweakName];
            _updated = true;
        }
    }
}
