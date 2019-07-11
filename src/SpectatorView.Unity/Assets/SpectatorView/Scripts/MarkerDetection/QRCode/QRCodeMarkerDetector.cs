﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Enable this preprocessor directive (QRCODESTRACKER_BINARY_AVAILABLE) in your player settings as needed.
#if QRCODESTRACKER_BINARY_AVAILABLE && WINDOWS_UWP && UNITY_WSA
#define ENABLE_QRCODES
#endif

using UnityEngine;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

#if ENABLE_QRCODES
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Preview;
#endif

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// QR code detector that implements <see cref="Microsoft.MixedReality.Toolkit.Extensions.MarkerDetection.IVariableSizeMarkerDetector"/>
    /// </summary>
    public class QRCodeMarkerDetector : MonoBehaviour,
        IMarkerDetector
    {
        [Tooltip("Check to enable debug logging")]
        [SerializeField]
        private bool debugLogging = false;

#if ENABLE_QRCODES
        private QRCodesManager _qrCodesManager;
        private Dictionary<Guid, SpatialCoordinateSystem> _markerCoordinateSystems = new Dictionary<Guid, SpatialCoordinateSystem>();
        private bool _processMarkers = false;
#endif

        private object _contentLock = new object();
        private Dictionary<Guid, int> _markerIds = new Dictionary<Guid, int>();
        private Dictionary<int, float> _markerSizes = new Dictionary<int, float>();
        private Dictionary<int, List<Marker>> _markerObservations = new Dictionary<int, List<Marker>>();
        private readonly string _qrCodeNamePrefix = "sv";

#if ENABLE_QRCODES
        private bool _tracking = false;
#endif

#pragma warning disable 67
        /// <inheritdoc />
        public event MarkersUpdatedHandler MarkersUpdated;
#pragma warning restore 67

        /// <inheritdoc />
        public void SetMarkerSize(float markerSize){}

        /// <inheritdoc />
        public MarkerPositionBehavior MarkerPositionBehavior { get; set; }

        /// <inheritdoc />
        public void StartDetecting()
        {
#if ENABLE_QRCODES
            _tracking = true;
#else
            Debug.LogError("Current platform does not support qr code marker detector");
#endif
        }

        /// <inheritdoc />
        public void StopDetecting()
        {
#if ENABLE_QRCODES
            _tracking = false;
            _processMarkers = false;
#else
            Debug.LogError("Current platform does not support qr code marker detector");
#endif
        }

        /// <inheritdoc />
        public bool TryGetMarkerSize(int markerId, out float size)
        {
            lock(_contentLock)
            {
                if (_markerSizes.TryGetValue(markerId, out size))
                {
                    return true;
                }
            }

            size = 0.0f;
            return false;
        }

#if ENABLE_QRCODES
        protected async void OnEnable()
        {
            if (_qrCodesManager == null)
            {
                _qrCodesManager = QRCodesManager.FindOrCreateQRCodesManager(gameObject);
            }

            if (_qrCodesManager != null)
            {
                _qrCodesManager.QRCodeAdded += QRCodeAdded;
                _qrCodesManager.QRCodeRemoved += QRCodeRemoved;
                _qrCodesManager.QRCodeUpdated += QRCodeUpdated;
                await StartTrackingAsync();
            }
        }

        protected void OnDisable()
        {
            if (_qrCodesManager != null)
            {
                StopTracking();
                _qrCodesManager.QRCodeAdded -= QRCodeAdded;
                _qrCodesManager.QRCodeRemoved -= QRCodeRemoved;
                _qrCodesManager.QRCodeUpdated -= QRCodeUpdated;
            }
        }

        private async Task StartTrackingAsync()
        {
            var result = await _qrCodesManager.StartQRTrackingAsync();
            DebugLog("Started qr tracker: " + result.ToString());
        }

        private void StopTracking()
        {
            _qrCodesManager.StopQRTracking();
            DebugLog("Stopped qr tracker");
        }

        protected void Update()
        {
            if (_tracking &&
                _processMarkers)
            {
                ProcessMarkerUpdates();
            }
        }

        private void QRCodeAdded(object sender, QRCodeEventArgs<QRCodesTrackerPlugin.QRCode> e)
        {
            if (TryGetMarkerId(e.Data.Code, out var markerId))
            {
                lock (_contentLock)
                {
                    _markerIds[e.Data.Id] = markerId;
                    _markerSizes[markerId] = e.Data.PhysicalSizeMeters;
                    _processMarkers = true;
                }
            }
        }

        private void QRCodeUpdated(object sender, QRCodeEventArgs<QRCodesTrackerPlugin.QRCode> e)
        {
            if (TryGetMarkerId(e.Data.Code, out var markerId))
            {
                lock (_contentLock)
                {
                    _markerIds[e.Data.Id] = markerId;
                    _markerSizes[markerId] = e.Data.PhysicalSizeMeters;
                    _processMarkers = true;
                }
            }
        }

        private void QRCodeRemoved(object sender, QRCodeEventArgs<QRCodesTrackerPlugin.QRCode> e)
        {
            lock (_contentLock)
            {
                if (_markerIds.TryGetValue(e.Data.Id, out var markerId))
                {
                    _markerSizes.Remove(markerId);
                }

                _markerIds.Remove(e.Data.Id);
                _markerCoordinateSystems.Remove(e.Data.Id);
                _processMarkers = true;
            }
        }

        private void ProcessMarkerUpdates()
        {
            bool locatedAllMarkers = true;
            var markerDictionary = new Dictionary<int, Marker>();
            lock (_contentLock)
            {
                foreach (var markerPair in _markerIds)
                {
                    if (!_markerCoordinateSystems.ContainsKey(markerPair.Key))
                    {
                        var coordinateSystem = SpatialGraphInteropPreview.CreateCoordinateSystemForNode(markerPair.Key);
                        if (coordinateSystem != null)
                        {
                            _markerCoordinateSystems[markerPair.Key] = coordinateSystem;
                        }
                    }
                }

                foreach (var coordinatePair in _markerCoordinateSystems)
                {
                    if (!_markerIds.TryGetValue(coordinatePair.Key, out var markerId))
                    {
                        DebugLog($"Failed to locate marker:{coordinatePair.Key}, {markerId}");
                        locatedAllMarkers = false;
                        continue;
                    }

                    if (_qrCodesManager.TryGetLocationForQRCode(coordinatePair.Key, out var location))
                    {
                        var translation = location.GetColumn(3);
                        // The obtained QRCode orientation will reflect a positive y axis down the QRCode.
                        // Spectator view marker detectors should return a positive y axis up the marker,
                        // so, we rotate the marker orientation 180 degrees around its z axis.
                        var rotation = Quaternion.LookRotation(location.GetColumn(2), location.GetColumn(1)) * Quaternion.Euler(0, 0, 180);

                        if (_markerSizes.TryGetValue(markerId, out var size))
                        {
                            var transform = Matrix4x4.TRS(translation, rotation, Vector3.one);
                            var offset = -1.0f * size / 2.0f;
                            var markerCenter = transform.MultiplyPoint(new Vector3(offset, offset, 0));
                            var marker = new Marker(markerId, markerCenter, rotation);
                            markerDictionary[markerId] = marker;
                        }
                    }
                }
            }

            if (markerDictionary.Count > 0 || locatedAllMarkers)
            {
                MarkersUpdated?.Invoke(markerDictionary);
            }

            // Stop processing markers once all markers have been located
            _processMarkers = !locatedAllMarkers;
        }
#endif // ENABLE_QRCODES

        private bool TryGetMarkerId(string qrCode, out int markerId)
        {
            markerId = -1;
            if (qrCode != null &&
                qrCode.Trim().StartsWith(_qrCodeNamePrefix))
            {
                var qrCodeId = qrCode.Trim().Replace(_qrCodeNamePrefix, "");
                if (Int32.TryParse(qrCodeId, out markerId))
                {
                    return true;
                }
            }

            DebugLog("Unable to obtain markerId for QR code: " + qrCode);
            markerId = -1;
            return false;
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"QRCodeMarkerDetector: {message}");
            }
        }
    }
}
