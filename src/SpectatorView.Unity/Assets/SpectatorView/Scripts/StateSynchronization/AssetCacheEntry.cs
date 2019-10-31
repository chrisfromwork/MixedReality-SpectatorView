﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    internal class AssetId
    {
        public static AssetId Empty { get; } = new AssetId(System.Guid.Empty, -1, -1, -1, string.Empty);

        public StringGuid Guid => guid;

        [SerializeField]
        private StringGuid guid;

        public long FileIdentifier => fileIdentifier;

        [SerializeField]
        private long fileIdentifier;

        [SerializeField]
        private int instanceID;

        [SerializeField]
        private int hashCode;

        public string Name => name;

        [SerializeField]
        private string name;

        public AssetId(StringGuid guid, long fileIdentifier, int instanceID, int hashCode, string name)
        {
            this.guid = guid;
            this.fileIdentifier = fileIdentifier;
            this.instanceID = instanceID;
            this.hashCode = hashCode;
            this.name = name;
        }

        public override bool Equals(object obj)
        {
            AssetId assetId = obj as AssetId;
            if (assetId == null)
            {
                return false;
            }

            return this == assetId;
        }

        public override int GetHashCode()
        {
            return FileIdentifier.GetHashCode() ^ Guid.ToString().GetHashCode();
        }

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

        public static bool operator !=(AssetId lhs, AssetId rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            return $"{guid} {fileIdentifier} {name}";
        }
    }

    internal class AssetCacheEntry<T> where T : class
    {
        [SerializeField]
        public AssetId AssetId;

        [SerializeField]
        public T Asset;
    }
}