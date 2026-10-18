// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp;

// One step of a lineage trace before it is placed in the tree (decision 0099). A member step names the
// property whose own sources come next; a clause step carries the hops it leads to.
sealed record McpLineageStep(string Role, string Status, string? Kind, string? Address, SourceLocation Location)
{
    internal static readonly string[] BreakStatuses = ["missing", "ambiguous", "incomplete", "opaque", "truncated"];

    internal string? Mode { get; init; }
    internal string? Block { get; init; }
    internal string? Text { get; init; }
    internal string? Reason { get; init; }
    internal McpDeclaration[] Candidates { get; init; } = [];
    internal McpDeclaration? Member { get; init; }
    internal Func<IEnumerable<McpLineageStep>>? Next { get; init; }

    internal static McpLineageStep Break(string status, string role, string? kind, string? address, SourceLocation location, string reason) =>
        new(role, status, kind, address, location) { Reason = reason };

    internal static McpLineageStep Terminal(string role, SourceLocation location, string text) => new(role, "resolved", null, null, location) { Text = text };
}

// A placed hop: a step with its id, parent and depth in the trace.
sealed class McpLineageHop(int id, int? parent, int depth, string direction, McpLineageStep step)
{
    internal int Id => id;
    internal int? Parent => parent;
    internal int Depth => depth;
    internal string Direction => direction;
    internal McpLineageStep Step { get; set; } = step;
    internal int? RepeatOf { get; set; }
    internal int Branches { get; set; }
    internal bool IsBreak => McpLineageStep.BreakStatuses.Contains(Step.Status, StringComparer.Ordinal);

    internal object Json() => new
    {
        id,
        parent,
        depth,
        direction,
        role = Step.Role,
        status = Step.Status,
        kind = Step.Kind,
        address = Step.Address,
        location = Step.Location,
        mode = Step.Mode,
        block = Step.Block,
        text = Step.Text,
        reason = Step.Reason,
        candidates = Step.Candidates.Select(candidate => new { candidate.Kind, candidate.Address, candidate.Location }).ToArray(),
        repeatOf = RepeatOf,
        branches = Branches
    };

    internal object Summary() => new { id, status = Step.Status, role = Step.Role, kind = Step.Kind, address = Step.Address, reason = Step.Reason };
}
