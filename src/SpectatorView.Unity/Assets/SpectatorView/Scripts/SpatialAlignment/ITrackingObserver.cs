namespace Microsoft.MixedReality.SpectatorView
{
    public interface ITrackingObserver
    {
        /// <summary>
        /// Returns whether the primary camera in the scene has tracking
        /// </summary>
        bool IsTracking { get; }
    }
}
