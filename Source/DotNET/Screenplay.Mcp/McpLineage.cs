// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

// The trace-lineage report (decision 0099): one read-model property's lineage as a paged hop tree.
static class McpLineage
{
    const int BreakLimit = 50;

    const string Coverage = "Advisory lineage over the property-link index, one hop per member or authored clause: projection mappings and AutoMap (from, join, all), " +
        "command and reaction production mappings, capture appends, reaction invocations, screen action arguments, form inputs, queries and screen bindings. " +
        "Breaks are leaves: missing (a placeholder naming the absent clause), ambiguous (candidates listed, none followed), incomplete (nested paths, child or nested " +
        "projection scopes, raw expressions, legacy command reads), opaque (reducer, handler, trigger or performer code is never read) and truncated (500 hops, depth 24). " +
        "A member reached twice is repeated, not expanded again. Command properties are the input boundary. Not transitive runtime impact, specification coverage, " +
        "completeness rules, event-source identity lineage or historical generations.";

    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var trace = new McpLineageTrace(snapshot.Index);
        var subject = trace.Subject(McpJson.RequiredString(arguments, "address"));
        var hops = trace.Run(subject);
        var breaks = hops.Where(hop => hop.IsBreak).ToArray();

        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot),
            coverage = Coverage,
            subject = new { subject.Kind, subject.Address, subject.Location },
            hopCount = hops.Count,
            complete = breaks.Length == 0,
            truncated = trace.Truncated,
            breakCount = breaks.Length,
            breaks = breaks.Take(BreakLimit).Select(hop => hop.Summary()).ToArray(),
            breaksTruncated = breaks.Length > BreakLimit,
            page = McpPaging.BoundedSourcePage(hops.Select(hop => hop.Json()), arguments, snapshot.SourceRevision)
        };
    }
}
