﻿// Copyright (c) MicroElements. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MicroElements.FileStorage.Abstractions
{
    /// <summary>
    /// Key generator.
    /// <para>Uses for key generation for new entities.</para>
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    public interface IKeyGenerator<T> where T : class
    {
        /// <summary>
        /// Key strategy.
        /// </summary>
        KeyType KeyStrategy { get; }

        /// <summary>
        /// Generates new key for collection.
        /// </summary>
        /// <param name="collection">Document collection.</param>
        /// <param name="entity">Entity.</param>
        /// <returns>New key.</returns>
        [Obsolete]
        Key GetNextKey(IDocumentCollection<T> collection, T entity);

        /// <summary>
        /// Generates new key.
        /// </summary>
        /// <param name="dataStore">DataStore.</param>
        /// <param name="entity">Entity.</param>
        /// <returns>New key.</returns>
        Key GetNextKey(IDataStore dataStore, T entity);
    }
}
