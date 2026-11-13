// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

// Parses and names the directions a lineage trace can take.
static class McpLineageDirections
{
    internal static readonly string[] Names = ["upstream", "downstream", "both"];

    internal static McpLineageDirection Parse(string? value) => value switch
    {
        null or "both" => McpLineageDirection.Both,
        "upstream" => McpLineageDirection.Upstream,
        "downstream" => McpLineageDirection.Downstream,
        _ => throw new McpFailure($"direction must be upstream, downstream or both; '{value}' is neither.")
    };

    internal static string Name(McpLineageDirection direction) => direction switch
    {
        McpLineageDirection.Upstream => "upstream",
        McpLineageDirection.Downstream => "downstream",
        _ => "both"
    };
}
