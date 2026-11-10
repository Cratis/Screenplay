// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Cratis.Screenplay.Agents;

/// <summary>
/// Writes the capability manifest as deterministic ASCII-escaped UTF-8 JSON.
/// </summary>
public static class AgentCapabilityManifestJson
{
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Default
    };

    /// <summary>
    /// Writes a manifest with LF line endings and exactly one trailing newline, without a BOM.
    /// </summary>
    /// <param name="manifest">The emitted manifest.</param>
    /// <returns>The manifest bytes. Escaping is encoding, not prompt-injection protection.</returns>
    public static byte[] Write(AgentCapabilityManifest manifest) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(manifest, _options).ReplaceLineEndings("\n") + "\n");
}
