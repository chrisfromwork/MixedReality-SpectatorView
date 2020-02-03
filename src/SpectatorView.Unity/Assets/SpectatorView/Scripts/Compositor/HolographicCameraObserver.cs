﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Component that connects to the HoloLens application on the holographic camera rig for synchronizing camera poses and receiving calibration data.
    /// </summary>
    public class HolographicCameraObserver : NetworkManager<HolographicCameraObserver>
    {
        public const string CameraCommand = "Camera";
        public const string CalibrationDataCommand = "CalibrationData";

        [SerializeField]
        [Tooltip("The CompositionManager used to perform composition of holograms and real-world video.")]
        private CompositionManager compositionManager = null;

        [SerializeField]
        [Tooltip("The DeviceInfoObserver used for the connection between the compositor and the device running the app being viewed.")]
        private DeviceInfoObserver appDeviceObserver = null;

        [SerializeField]
        [Tooltip("The port that the HolographicCamera listens for connections on.")]
        private int remotePort = 7502;

        protected override int RemotePort => remotePort;

        public GameObject SharedSpatialCoordinateProxy { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            RegisterCommandHandler(CameraCommand, HandleCameraCommand);
            RegisterCommandHandler(CalibrationDataCommand, HandleCalibrationDataCommand);
        }

        protected override void OnConnected(SocketEndpoint endpoint)
        {
            base.OnConnected(endpoint);

            compositionManager.ResetOnNewCameraConnection();
        }

        private void Update()
        {
            if (appDeviceObserver != null &&
                appDeviceObserver.ConnectedEndpoint != null &&
                SharedSpatialCoordinateProxy != null &&
                SpatialCoordinateSystemManager.IsInitialized &&
                SpatialCoordinateSystemManager.Instance.TryGetSpatialCoordinateSystemParticipant(appDeviceObserver.ConnectedEndpoint, out SpatialCoordinateSystemParticipant participant))
            {
                SharedSpatialCoordinateProxy.transform.position = participant.PeerSpatialCoordinateWorldPosition;
                SharedSpatialCoordinateProxy.transform.rotation = participant.PeerSpatialCoordinateWorldRotation;
            }
        }

        private void HandleCameraCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            float timestamp = reader.ReadSingle();
            Vector3 cameraPosition = reader.ReadVector3();
            Quaternion cameraRotation = reader.ReadQuaternion();

            compositionManager.AddCameraPose(cameraPosition, cameraRotation, timestamp);
        }

        private void HandleCalibrationDataCommand(SocketEndpoint endpoint, string command, BinaryReader reader, int remainingDataSize)
        {
            int calibrationDataPayloadLength = reader.ReadInt32();
            byte[] calibrationDataPayload = reader.ReadBytes(calibrationDataPayloadLength);

            CalculatedCameraCalibration calibration;
            if (CalculatedCameraCalibration.TryDeserialize(calibrationDataPayload, out calibration))
            {
                if (SharedSpatialCoordinateProxy == null)
                {
                    SharedSpatialCoordinateProxy = new GameObject("App HMD Shared Spatial Coordinate");
                    SharedSpatialCoordinateProxy.transform.SetParent(transform, worldPositionStays: true);
                    if (appDeviceObserver != null &&
                        appDeviceObserver.ConnectedEndpoint != null &&
                        SpatialCoordinateSystemManager.IsInitialized &&
                        SpatialCoordinateSystemManager.Instance.TryGetSpatialCoordinateSystemParticipant(appDeviceObserver.ConnectedEndpoint, out SpatialCoordinateSystemParticipant participant))
                    {
                        SharedSpatialCoordinateProxy.transform.position = participant.PeerSpatialCoordinateWorldPosition;
                        SharedSpatialCoordinateProxy.transform.rotation = participant.PeerSpatialCoordinateWorldRotation;
                    }
                }
                compositionManager.EnableHolographicCamera(SharedSpatialCoordinateProxy.transform, new CalibrationData(calibration.Intrinsics, calibration.Extrinsics));
            }
            else
            {
                Debug.LogError("Received a CalibrationData packet from the HoloLens that could not be understood.");
            }
        }
    }
}