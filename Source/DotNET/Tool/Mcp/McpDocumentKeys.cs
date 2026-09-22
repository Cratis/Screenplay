// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpDocumentKeys
{
    // Bootstrap once; subsequent moves retain this opaque key through workspace serialization.
    internal static string For(string path) => $"mcp:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)))}";
}
