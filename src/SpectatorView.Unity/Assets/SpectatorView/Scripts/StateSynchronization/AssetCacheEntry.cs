// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Wrapper class for items stored in a StateSynchronization Asset Cache
    /// </summary>
    [Serializable]
    public class AssetCacheEntry
    {
        /// <summary>
        /// Unity asset id
        /// </summary>
        [SerializeField]
        public AssetId AssetId;

        /// <summary>
        /// Unity asset
        /// </summary>
        [SerializeField]
        public UnityEngine.Object Asset;
    }
}