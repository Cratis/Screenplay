// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Provides the stable document keys assigned when discovering a model root.
/// </summary>
public static class ModelRootDocumentKeys
{
    /// <summary>
    /// Gets the deterministic bootstrap key for a portable document path.
    /// </summary>
    /// <param name="path">The portable path relative to the model root.</param>
    /// <returns>The opaque discovery key, prefixed with <c>mcp:</c>.</returns>
    /// <remarks>
    /// Hashes <see cref="PortablePlayPath.Value"/>, which uses Unicode NFC and forward slashes.
    /// Canonically equivalent Unicode paths therefore receive the same key. Persisted keys remain authoritative
    /// when a root opens; moving a document does not require recomputing its persisted key.
    /// </remarks>
    public static string For(PortablePlayPath path) => $"mcp:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path.Value)))}";
}
