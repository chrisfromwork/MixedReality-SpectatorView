﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public abstract class NetworkManager<TService> : CommandRegistry<TService>, INetworkManager where TService : Singleton<TService>
    {
#pragma warning disable 414 // The field is assigned but its value is never used
        [Tooltip("Default prefab for creating an INetworkConnectionManager.")]
        [SerializeField]
        private GameObject defaultConnectionManagerPrefab = null;
        private GameObject connectionManagerGameObject = null;
        protected INetworkConnectionManager connectionManager = null;
#pragma warning restore 414

        private float lastReceivedUpdate;

        private INetworkConnection currentConnection;

        /// <inheritdoc />
        public string ConnectedIPAddress => currentConnection?.Address;

        public IReadOnlyList<INetworkConnection> Connections => connectionManager == null ? new List<INetworkConnection>() : connectionManager.Connections;

        /// <inheritdoc />
        public bool IsConnected => connectionManager != null && connectionManager.Connections.Count > 0;

        /// <inheritdoc />
        public bool IsConnecting => connectionManager != null && connectionManager.IsConnecting && connectionManager.Connections.Count == 0;

        /// <inheritdoc />
        public TimeSpan TimeSinceLastUpdate => TimeSpan.FromSeconds(Time.time - lastReceivedUpdate);

        /// <summary>
        /// Gets the port used to connect to the remote device.
        /// </summary>
        protected abstract int RemotePort { get; }

        /// <summary>
        /// Starts a listening socket on the given port.
        /// </summary>
        /// <param name="port">The port to listen for new connections on.</param>
        protected void StartListening(int port)
        {
            if (connectionManager != null)
            {
                connectionManager.StartListening(port);
            }
            else
            {
                Debug.LogError($"Failed to start listening: {nameof(connectionManager)} was not assigned.");
            }
        }

        /// <inheritdoc />
        public void ConnectTo(string remoteAddress)
        {
            ConnectTo(remoteAddress, RemotePort);
        }

        /// <inheritdoc />
        public void ConnectTo(string ipAddress, int port)
        {
            if (connectionManager != null)
            {
                connectionManager.ConnectTo(ipAddress, port);
            }
            else
            {
                Debug.LogError($"Failed to start connecting: {nameof(connectionManager)} was not assigned.");
            }
        }

        /// <summary>
        /// Sends data to other connected devices
        /// </summary>
        /// <param name="data">payload to send to other devices</param>
        public void Broadcast(byte[] data)
        {
            if (currentConnection != null)
            {
                currentConnection.Send(data);
            }
        }

        /// <summary>
        /// Disconnects the network connection to the holographic camera rig.
        /// </summary>
        public void Disconnect()
        {
            if (connectionManager != null)
            {
                connectionManager.DisconnectAll();
            }
            else
            {
                Debug.LogError($"Failed to disconnect: {nameof(connectionManager)} was not assigned.");
            }
        }

        protected override void Awake()
        {
            base.Awake();
            CreateConnectionManager();

            if (connectionManager != null)
            {
                connectionManager.OnConnected += OnConnected;
                connectionManager.OnDisconnected += OnDisconnected;
                connectionManager.OnReceive += OnReceive;
            }
            else
            {
                Debug.LogError($"{nameof(connectionManager)} is required but was not assigned.");
            }
        }

        protected virtual void Start()
        {
            if (SpatialCoordinateSystemManager.IsInitialized)
            {
                SpatialCoordinateSystemManager.Instance.RegisterNetworkManager(this);
            }
            else
            {
                Debug.LogError("Attempted to register NetworkManager with the SpatialCoordinateSystemManager but no SpatialCoordinateSystemManager is initialized");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (connectionManager != null)
            {
                connectionManager.StopListening();
                connectionManager.DisconnectAll();

                connectionManager.OnConnected -= OnConnected;
                connectionManager.OnDisconnected -= OnDisconnected;
                connectionManager.OnReceive -= OnReceive;
            }

            if (connectionManagerGameObject != null)
            {
                connectionManager = null;
                Destroy(connectionManagerGameObject);
            }

            if (SpatialCoordinateSystemManager.IsInitialized)
            {
                SpatialCoordinateSystemManager.Instance.UnregisterNetworkManager(this);
            }
        }

        protected virtual void OnConnected(INetworkConnection connection)
        {
            currentConnection = connection;

            NotifyConnected(connection);
        }

        protected virtual void OnDisconnected(INetworkConnection connection)
        {
            if (currentConnection == connection)
            {
                currentConnection = null;
            }

            NotifyDisconnected(connection);
        }

        protected void OnReceive(IncomingMessage data)
        {
            lastReceivedUpdate = Time.time;

            using (MemoryStream stream = new MemoryStream(data.Data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                string command = reader.ReadString();

                NotifyCommand(data.Connection, command, reader, data.Size - (int)stream.Position);
            }
        }

        private void CreateConnectionManager()
        {
            var prefab = defaultConnectionManagerPrefab;
            if (NetworkConfigurationSettings.IsInitialized &&
                NetworkConfigurationSettings.Instance.OverrideConnectionManagerPrefab != null)
            {
                prefab = NetworkConfigurationSettings.Instance.OverrideConnectionManagerPrefab;
            }

            if (prefab == null)
            {
                throw new Exception("Network connection manager prefab wasn't specified. DeviceInfoBroadcaster will not work correctly.");
            }

            connectionManagerGameObject = Instantiate(prefab, this.transform);
            connectionManager = connectionManagerGameObject.GetComponentInChildren<INetworkConnectionManager>();

            if (connectionManager == null)
            {
                throw new Exception("INetworkConnectionManager wasn't found in instantiated prefab. DeviceInfoBroadcaster will not work correctly.");
            }
        }
    }
}