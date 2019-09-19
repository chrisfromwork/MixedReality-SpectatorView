using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class ARCoreTrackingObserver : MonoBehaviour, ITrackingObserver
    {
        public bool IsTracking
        {
            get
            {
#if UNITY_ANDROID
                return GoogleARCore.Session.Status == GoogleARCore.SessionStatus.Tracking;
#elif UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
