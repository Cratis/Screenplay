// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpSourceRevision
{
    internal static string For(IEnumerable<WorkspaceDocument> documents)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData("Screenplay/McpSource/v1"u8);
        foreach (var document in documents.OrderBy(document => document.Path.Value, StringComparer.Ordinal))
        {
            Append(hash, Encoding.UTF8.GetBytes(document.Path.Value));
            Append(hash, document.Bytes.AsSpan());
        }

        return $"source:{Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()}";
    }

    static void Append(IncrementalHash hash, ReadOnlySpan<byte> bytes)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }
}
