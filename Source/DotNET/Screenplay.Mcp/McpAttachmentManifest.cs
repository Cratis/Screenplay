// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp;

/// <summary>Hashes the complete, sorted attachment identity/content/resolution manifest, including the empty manifest.</summary>
static class McpAttachmentManifest
{
    internal static string Revision(IEnumerable<SemanticImplementationRequirement> requirements)
    {
        var entries = requirements.Select(requirement => new
        {
            requirement.RequirementId,
            requirement.ContentHash,
            attachmentResolution = requirement.AttachmentResolution.ToString()
        }).OrderBy(entry => entry.RequirementId, StringComparer.Ordinal)
            .ThenBy(entry => entry.ContentHash, StringComparer.Ordinal)
            .ThenBy(entry => entry.attachmentResolution, StringComparer.Ordinal);
        return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(entries, McpJson.Options)));
    }
}
