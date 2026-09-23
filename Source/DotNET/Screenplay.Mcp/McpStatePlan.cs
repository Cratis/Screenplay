// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Screenplay.Mcp;

sealed record McpStatePlan(byte[]? Before, byte[] After)
{
    internal static string Revision(byte[]? bytes) => bytes is null ? "absent" : Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    internal object Describe() => new
    {
        path = ".screenplay/identities.json",
        changed = !McpManagedFiles.Equal(Before, After),
        beforeExists = Before is not null,
        beforeBytes = Before?.Length ?? 0,
        afterBytes = After.Length,
        beforeRevision = Revision(Before),
        afterRevision = Revision(After)
    };
}
