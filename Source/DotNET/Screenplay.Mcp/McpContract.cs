// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Exposes the complete tool catalog without opening an MCP connection or workspace.
/// </summary>
public static class McpContract
{
    /// <summary>
    /// Describes all tools, including the optional visualization tool, with their argument schemas.
    /// </summary>
    /// <returns>The tool descriptions.</returns>
    public static JsonArray DescribeTools() => JsonSerializer.SerializeToNode(McpToolCatalog.Describe(visual: true), McpJson.Options)!.AsArray();
}
