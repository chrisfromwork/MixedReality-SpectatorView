using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class HoloLensTrackingObserver : MonoBehaviour, ITrackingObserver
    {
        public bool IsTracking
        {
            get
            {
#if UNITY_WSA
                return UnityEngine.XR.WSA.WorldManager.state == UnityEngine.XR.WSA.PositionalLocatorState.Active;
#elif UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
