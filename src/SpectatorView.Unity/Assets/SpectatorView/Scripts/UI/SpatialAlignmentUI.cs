using Microsoft.MixedReality.SpatialAlignment;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpatialAlignmentUI : MonoBehaviour
    {
        /// <summary>
        /// Text updated to display the state of spatial alignment.
        /// </summary>
        [Tooltip("Text updated to display the state of spatial alignment.")]
        [SerializeField]
        protected Text spatialAlignmentStateText;

        /// <summary>
        /// Text updated to display the device tracking state.
        /// </summary>
        [Tooltip("Text updated to display the device tracking state.")]
        [SerializeField]
        protected Text trackingStateText;

        /// <summary>
        /// Text updated to display the main camera'a local position.
        /// </summary>
        [Tooltip("Text updated to display the main camera's local position.")]
        [SerializeField]
        protected Text localCameraPositionText;

        /// <summary>
        /// Text updated to display the main camera's position in the shared coordinate space.
        /// </summary>
        [Tooltip("Text updated to display the main camera's position in the shared coordinate space.")]
        [SerializeField]
        protected Text cameraPositionText;

        private const string spatialAlignmentPrompt = "Spatial Alignment State:";
        private const string trackingStatePrompt = "Tracking State:";
        private const string localPositionPrompt = "Local Device Position:";
        private const string positionPrompt = "Device Position:";

        public void OnResetLocalizationClick()
        {
            SpectatorView.Instance.TryResetLocalizationAsync().FireAndForget();
        }

        private void Update()
        {
            string stateText = "None";
            if (SpatialCoordinateSystemManager.IsInitialized)
            {
                bool foundAllLocal = SpatialCoordinateSystemManager.Instance.AllLocalCoordinatesFound;
                bool foundAllPeer = SpatialCoordinateSystemManager.Instance.AllPeerCoordinatesFound;
                if (foundAllLocal && foundAllPeer)
                {
                    stateText = "All Coordinates Located";
                }
                else if (!foundAllPeer && foundAllLocal)
                {
                    stateText = "Unknown Peer Coordinate";
                }
                else if (!foundAllLocal && foundAllPeer)
                {
                    stateText = "Unknown Local Coordinate";
                }
                else
                {
                    stateText = "No Coordinates Located";
                }
            }

            if (spatialAlignmentStateText != null)
            {
                spatialAlignmentStateText.text = $"{spatialAlignmentPrompt} {stateText}";
            }

            if (trackingStatePrompt != null &&
                SpatialCoordinateSystemManager.IsInitialized)
            {
                trackingStateText.text = $"{trackingStatePrompt} {SpatialCoordinateSystemManager.Instance.TrackingState.ToString()}";
            }

            if (localCameraPositionText != null &&
                Camera.main != null)
            {
                localCameraPositionText.text = $"{localPositionPrompt} {Camera.main.transform.localPosition.ToString("G1")}";
            }

            if (localCameraPositionText != null &&
                Camera.main != null)
            {
                cameraPositionText.text = $"{positionPrompt} {Camera.main.transform.position.ToString("G1")}";
            }
        }
    }
}
