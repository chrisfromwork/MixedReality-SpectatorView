// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class SpatialAnchorsCoordinateLocalizationInitializer : SpatialLocalizationInitializer
    {
        /// <summary>
        /// Configuration for the Azure Spatial Anchors service.
        /// </summary>
        [SerializeField]
        [Tooltip("Configuration for the Azure Spatial Anchors service.")]
        private SpatialAnchorsConfiguration configuration = null;

        public override Guid PeerSpatialLocalizerId => SpatialAnchorsLocalizer.Id;

        public override async Task<bool> TryRunLocalizationAsync(SpatialCoordinateSystemParticipant participant)
        {
            return await TryRunLocalizationImplAsync(participant);
        }

        public override async Task<bool> TryResetLocalizationAsync(SpatialCoordinateSystemParticipant participant)
        {
            return await TryRunLocalizationImplAsync(participant);
        }

        private async Task<bool> TryRunLocalizationImplAsync(SpatialCoordinateSystemParticipant participant)
        {
            if (await SpatialCoordinateSystemManager.Instance.LocalizeAsync(participant.SocketEndpoint, SpatialAnchorsLocalizer.Id, configuration))
            {
                configuration.IsCoordinateCreator = true;
                return await SpatialCoordinateSystemManager.Instance.RunRemoteLocalizationAsync(participant.SocketEndpoint, SpatialAnchorsLocalizer.Id, configuration);
            }

            return false;
        }
    }
}
