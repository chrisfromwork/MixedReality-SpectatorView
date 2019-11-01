// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Id used to identify Unity Assets declared in a StateSynchronization AssetCache
    /// </summary>
    [Serializable]
    public class AssetId
    {
        /// <summary>
        /// Default AssetId value
        /// </summary>
        public static AssetId Empty { get; } = new AssetId(System.Guid.Empty, -1, string.Empty);

        /// <summary>
        /// Asset guid
        /// </summary>
        public StringGuid Guid => guid;

        [SerializeField]
        private StringGuid guid;

        /// <summary>
        /// Asset file identifier
        /// </summary>
        public long FileIdentifier => fileIdentifier;

        [SerializeField]
        private long fileIdentifier;

        /// <summary>
        /// Asset name
        /// </summary>
        public string Name => name;

        [SerializeField]
        private string name;

        /// <summary>
        /// Constructs an asset id.
        /// </summary>
        /// <param name="guid">Asset guid</param>
        /// <param name="fileIdentifier">Asset file identifier</param>
        /// <param name="name">Asset name</param>
        public AssetId(StringGuid guid, long fileIdentifier, string name)
        {
            this.guid = guid;
            this.fileIdentifier = fileIdentifier;
            this.name = name;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            AssetId assetId = obj as AssetId;
            if (assetId == null)
            {
                return false;
            }

            return this == assetId;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return FileIdentifier.GetHashCode() ^ Guid.ToString().GetHashCode();
        }

        /// <inheritdoc />
        public static bool operator ==(AssetId lhs, AssetId rhs)
        {
            if ((object)lhs == null && (object)rhs == null)
            {
                return true;
            }
            else if ((object)lhs == null && (object)rhs != null ||
                (object)lhs != null && (object)rhs == null)
            {
                return false;
            }

            return (lhs.Guid.ToString() == rhs.Guid.ToString()) && (lhs.FileIdentifier == rhs.FileIdentifier) && (lhs.name == rhs.name);
        }

        /// <inheritdoc />
        public static bool operator !=(AssetId lhs, AssetId rhs)
        {
            return !(lhs == rhs);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{guid} {fileIdentifier} {name}";
        }
    }
}
