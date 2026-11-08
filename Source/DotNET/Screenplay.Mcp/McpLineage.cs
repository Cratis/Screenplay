// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

// The trace-lineage report (decision 0099): one property's or screen field's lineage as a paged hop tree.
static class McpLineage
{
    internal const int BreakLimit = 50;

    internal const string Coverage = "Advisory lineage over the property-link index from a command, event or read-model property or a screen field, upstream (where the value comes from), " +
        "downstream (where it goes) or both, one hop per member or authored clause: projection mappings and AutoMap (from, join, all), " +
        "command and reaction production mappings, capture appends, reaction invocations, screen action arguments, form inputs, queries and screen bindings. " +
        "Breaks are leaves: missing (a placeholder naming the absent clause), ambiguous (candidates listed, none followed), incomplete (nested paths, child or nested " +
        "projection scopes, raw expressions, legacy command reads), opaque (reducer, handler, trigger or performer code is never read) and truncated (500 hops, depth 24). " +
        "A member reached twice is repeated, not expanded again. Command properties are the input boundary. Not transitive runtime impact, specification coverage, " +
        "completeness rules, event-source identity lineage or historical generations.";

    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var report = Trace(snapshot, McpJson.RequiredString(arguments, "address"), McpLineageDirections.Parse(McpJson.OptionalString(arguments, "direction")));

        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot),
            coverage = Coverage,
            subject = report.SubjectJson(),
            direction = McpLineageDirections.Name(report.Direction),
            hopCount = report.Hops.Count,
            complete = report.Breaks.Length == 0,
            truncated = report.Truncated,
            breakCount = report.Breaks.Length,
            breaks = report.Breaks.Take(BreakLimit).Select(hop => hop.Summary()).ToArray(),
            breaksTruncated = report.Breaks.Length > BreakLimit,
            page = McpPaging.BoundedSourcePage(report.Hops.Select(hop => hop.Json()), arguments, snapshot.SourceRevision)
        };
    }

    // The whole trace: the CLI prints every hop, MCP pages them.
    internal static McpLineageReport Trace(McpSnapshot snapshot, string address, McpLineageDirection direction)
    {
        var trace = new McpLineageTrace(snapshot.Index);
        var subject = trace.Subject(address);
        var hops = trace.Run(subject, direction);

        return new(subject, direction, hops, trace.Truncated);
    }
}

// A whole lineage trace: its subject, direction and every hop in order.
sealed record McpLineageReport(McpLineageSubject Subject, McpLineageDirection Direction, IReadOnlyList<McpLineageHop> Hops, bool Truncated)
{
    internal McpLineageHop[] Breaks { get; } = [.. Hops.Where(hop => hop.IsBreak)];

    internal object SubjectJson() => new { Subject.Kind, Subject.Address, Subject.Location };
}
