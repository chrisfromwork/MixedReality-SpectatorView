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

        [SerializeField]
        private bool ApplyOffsetOnMarkerPlane;

        [SerializeField]
        private Vector2 MarkerPlaneOffset;

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

        private void Update()
        {
            var participant = GetSpatialCoordinateSystemParticipant(HolographicCameraDevice);
            if (HolographicCamera != null &&
                HolographicCamera.SharedSpatialCoordinateProxy != null &&
                cachedCompositionManager != null &&
                cachedCompositionManager.VideoCameraPose != null)
            {
                // Unneeded for now
                //var coordinatePosition = participant.PeerSpatialCoordinateWorldPosition;
                //var coordinateRotation = participant.PeerSpatialCoordinateWorldRotation;

                //var cameraPosition = cachedCompositionManager.VideoCameraPose.transform.position;
                //var cameraRotation = cachedCompositionManager.VideoCameraPose.transform.rotation;

                var appliedPosition = HolographicCamera.SharedSpatialCoordinateProxy.transform.position;
                var appliedRotation = HolographicCamera.SharedSpatialCoordinateProxy.transform.rotation;

                var sharedWorldPosition = appliedPosition;
                if (ApplyOffsetOnMarkerPlane)
                {
                    sharedWorldPosition += CalcTopLeftFromCenter(sharedWorldPosition, appliedRotation, MarkerPlaneOffset);
                }
                var sharedWorldRotation = Quaternion.Euler(CustomRotation) * appliedRotation;

                // This seems to be right but more testing is needed.
                SharedWorldRootTransform.SetPositionAndRotation(sharedWorldPosition, sharedWorldRotation);
            }
        }

        private static Vector3 CalcTopLeftFromCenter(Vector3 middlePosition, Quaternion orientation, Vector2 offset)
        {
            var originToMiddle = Matrix4x4.TRS(middlePosition, orientation, Vector3.one);
            return originToMiddle.MultiplyPoint(new Vector3(offset.x, offset.y, 0));
        }
    }
}
