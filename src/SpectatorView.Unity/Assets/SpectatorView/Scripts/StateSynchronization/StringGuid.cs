﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    [Serializable]
    public class StringGuid : IComparable
    {
        [SerializeField]
        private string m_storage;

        /// <inheritdoc />
        public static implicit operator StringGuid(Guid rhs)
        {
            return new StringGuid { m_storage = rhs.ToString("D") };
        }

        /// <inheritdoc />
        public static implicit operator Guid(StringGuid rhs)
        {
            if (rhs.m_storage == null) return Guid.Empty;
            try
            {
                return new Guid(rhs.m_storage);
            }
            catch (FormatException)
            {
                return System.Guid.Empty;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return (m_storage == null) ? System.Guid.Empty.ToString("D") : m_storage;
        }

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            var guid = obj as StringGuid;
            if (guid == null)
            {
                return 1;
            }

            return this.m_storage.CompareTo(guid.m_storage);
        }
    }
}