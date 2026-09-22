// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpDependencyQueries
{
    internal static object Legacy(McpSnapshot snapshot, JsonElement arguments)
    {
        var target = McpDeclarationDetails.Target(snapshot, arguments);
        var edges = snapshot.Index.Incoming(target).Select(edge => new McpReferenceEdge(edge.Reference, edge.Candidates));
        var page = McpPaging.Page(edges, arguments, snapshot.SourceRevision);
        var diagnostics = snapshot.Compilation.Diagnostics.ToArray();
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            diagnostics = diagnostics.Take(50).ToArray(),
            diagnosticCount = diagnostics.Length,
            nextDiagnosticsOffset = diagnostics.Length > 50 ? (int?)50 : null,
            declaration = McpReadResults.Summary(target),
            page.TotalCount,
            page.Offset,
            page.NextOffset,
            references = page.Items.Where(edge => edge.Targets.Length == 1).Select(edge => edge.Reference).ToArray(),
            ambiguous = page.Items.Where(edge => edge.Targets.Length > 1).Select(edge => new { reference = edge.Reference, candidates = edge.Targets.Select(McpReadResults.Summary).ToArray() }).ToArray(),
            coverage = McpReferenceKinds.Coverage
        };
    }

    internal static object Read(McpSnapshot snapshot, JsonElement arguments)
    {
        var target = McpDeclarationDetails.Target(snapshot, arguments);
        var direction = McpJson.OptionalString(arguments, "direction") ?? "incoming";
        var descendants = McpJson.Boolean(arguments, "descendants");
        var references = (direction, descendants) switch
        {
            ("incoming", false) => snapshot.Index.Incoming(target).Select(edge => edge.Reference),
            ("outgoing", false) => snapshot.Index.Outgoing(target.Owner),
            _ => snapshot.Index.References
        };
        if (direction == "outgoing")
        {
            references = references.Where(reference => reference.Owner is not null &&
                (reference.Owner.Address == target.Address || (descendants && reference.Owner.Address.StartsWith(target.Address + ".", StringComparison.Ordinal))));
        }
        else if (direction != "incoming")
        {
            throw new McpFailure("Reference direction must be incoming or outgoing.", -32602);
        }

        if (McpJson.OptionalString(arguments, "document") is { } document)
        {
            references = references.Where(reference => reference.Location.Path == document);
        }

        var edges = references.Select(reference => new McpReferenceEdge(reference, snapshot.Index.Resolve(reference)));
        if (direction == "incoming")
        {
            edges = edges.Where(edge => edge.Targets.Any(candidate => candidate == target ||
                (descendants && candidate.Address.StartsWith(target.Address + ".", StringComparison.Ordinal))));
        }

        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            declaration = McpReadResults.Summary(target),
            direction,
            descendants,
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot),
            coverage = $"Direct indexed dependencies only, not transitive runtime impact. {McpReferenceKinds.Coverage}",
            page = McpPaging.Page(
                edges,
                edge => new
                {
                    reference = edge.Reference,
                    resolution = edge.Targets.Length switch { 0 => "unresolved", 1 => "resolved", _ => "ambiguous" },
                    targets = edge.Targets.Select(McpReadResults.Summary).ToArray()
                },
                arguments,
                snapshot.SourceRevision)
        };
    }
}
