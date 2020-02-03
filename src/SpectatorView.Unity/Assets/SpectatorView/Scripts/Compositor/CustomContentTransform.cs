using Microsoft.MixedReality.SpatialAlignment;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class CustomContentTransform : MonoBehaviour
    {
        [SerializeField]
        private Transform SharedWorldRootTransform;

        [SerializeField]
        private Vector3 CustomRotation;

        HolographicCameraObserver HolographicCamera
        {
            get
            {
                if (cachedHolographicCameraObserver == null)
                {
                    cachedHolographicCameraObserver = FindObjectOfType<HolographicCameraObserver>();
                }

                return cachedHolographicCameraObserver;
            }
        }
        private HolographicCameraObserver cachedHolographicCameraObserver;

        DeviceInfoObserver HolographicCameraDevice
        {
            get
            {
                if (cachedHolographicCameraDevice == null &&
                    HolographicCamera != null)
                {
                    cachedHolographicCameraDevice = HolographicCamera.GetComponent<DeviceInfoObserver>();
                }

                return cachedHolographicCameraDevice;
            }
        }
        private DeviceInfoObserver cachedHolographicCameraDevice;

        private CompositionManager cachedCompositionManager;

        protected SpatialCoordinateSystemParticipant GetSpatialCoordinateSystemParticipant(DeviceInfoObserver device)
        {
            if (device != null && device.ConnectedEndpoint != null && SpatialCoordinateSystemManager.IsInitialized)
            {
                if (SpatialCoordinateSystemManager.Instance.TryGetSpatialCoordinateSystemParticipant(device.ConnectedEndpoint, out SpatialCoordinateSystemParticipant participant))
                {
                    return participant;
                }
                else
                {
                    Debug.LogError("Expected to be able to find participant for an endpoint");
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        private void Start()
        {
            cachedCompositionManager = FindObjectOfType<CompositionManager>();
            if (cachedCompositionManager == null)
            {
                Debug.Log("Composition Manager wasn't found, disabling CustomContentTransform.");
                this.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            var participant = GetSpatialCoordinateSystemParticipant(HolographicCameraDevice);
            if (HolographicCamera != null &&
                HolographicCamera.SharedSpatialCoordinateProxy != null &&
                cachedCompositionManager != null &&
                cachedCompositionManager.VideoCameraPose != null)
            {
                var coordinatePosition = participant.PeerSpatialCoordinateWorldPosition;
                var coordinateRotation = participant.PeerSpatialCoordinateWorldRotation;

                var cameraPosition = cachedCompositionManager.VideoCameraPose.transform.position;
                var cameraRotation = cachedCompositionManager.VideoCameraPose.transform.rotation;

                var appliedPosition = HolographicCamera.SharedSpatialCoordinateProxy.transform.position;
                var appliedRotation = HolographicCamera.SharedSpatialCoordinateProxy.transform.rotation;

                var sharedWorldPosition = appliedPosition;
                var sharedWorldRotation = Quaternion.Euler(CustomRotation) * Quaternion.Euler(90, 0, 0) * appliedRotation;

                // This seems to be right but more testing is needed.
                SharedWorldRootTransform.SetPositionAndRotation(sharedWorldPosition, sharedWorldRotation);
            }
        }
    }
}
