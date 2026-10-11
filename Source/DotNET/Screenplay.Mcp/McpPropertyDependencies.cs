// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpPropertyDependencies
{
    const string Coverage = "Authored command/current-event/read-model property links in explicit production, capture-append and projection mappings, case-insensitive AutoMap, declarative conditions, query filters, command read/query lookup/from/removal/parent keys and join on fields, screen/form bindings and specification/example values. Event-source captures resolve source-item paths against their declared input events. Inherited module/feature/command policy readers retain all caller candidates. Legacy command reads, nested paths, external source items and named opaque text are incomplete evidence, not executable or code resolution. Not projection-level keys or child identity expressions, command response/route sources, arbitrary expression identifiers, imports, profile settings, external registrations or external code contents.";

    internal static object Legacy(McpSnapshot snapshot, McpDeclaration target, JsonElement arguments)
    {
        var links = snapshot.Index.PropertyLinks;
        var page = McpPaging.Page(links.Incoming(target), arguments, snapshot.SourceRevision);
        var diagnostics = snapshot.Compilation.Diagnostics.ToArray();
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            diagnostics = diagnostics.Take(50).ToArray(),
            diagnosticCount = diagnostics.Length,
            nextDiagnosticsOffset = diagnostics.Length > 50 ? (int?)50 : null,
            declaration = Summary(target),
            complete = links.Complete(target),
            page.TotalCount,
            page.Offset,
            page.NextOffset,
            references = page.Items.Where(link => link.Resolution == "resolved").Select(Reference).ToArray(),
            ambiguous = page.Items.Where(link => link.Resolution == "ambiguous").Select(link => new { reference = Reference(link), candidates = link.Candidates.Select(Summary).ToArray() }).ToArray(),
            incomplete = page.Items.Where(link => link.Resolution == "incomplete").Select(link => new { reference = Reference(link), candidates = link.Candidates.Select(Summary).ToArray() }).ToArray(),
            wrongKind = Array.Empty<object>(),
            coverage = Coverage
        };
    }

    internal static object Read(McpSnapshot snapshot, McpDeclaration target, JsonElement arguments)
    {
        var direction = McpJson.OptionalString(arguments, "direction") ?? "incoming";
        var links = snapshot.Index.PropertyLinks;
        var selected = direction switch
        {
            "incoming" => links.Incoming(target),
            "outgoing" => links.Outgoing(target),
            _ => throw new McpFailure("Reference direction must be incoming or outgoing.", -32602)
        };
        if (McpJson.OptionalString(arguments, "document") is { } document) selected = selected.Where(link => link.Reference.Location.Path == document);
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            declaration = Summary(target),
            direction,
            descendants = McpJson.Boolean(arguments, "descendants"),
            complete = links.Complete(target),
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot),
            coverage = $"Direct indexed dependencies only, not transitive runtime impact. {Coverage}",
            page = McpPaging.Page(
                selected,
                link => new
                {
                    reference = Reference(link),
                    resolution = link.Resolution,
                    targets = link.Candidates.Select(Summary).ToArray()
                },
                arguments,
                snapshot.SourceRevision)
        };
    }

    static object Reference(WorkspacePropertyLink link) => new
    {
        link.Reference.Name,
        link.Reference.Kinds,
        link.Reference.Scope,
        link.Reference.Location,
        link.Reference.Role,
        link.Reference.Owner,
        otherEnd = link.OtherEnd is null ? null : Summary(link.OtherEnd)
    };

    static object Summary(McpDeclaration property) => new
    {
        property.Kind,
        property.Name,
        property.Address,
        property.Scope,
        property.Location,
        property.Locations,
        property.Description,
        property.IsImplicit,
        property.Case,
        property.Details
    };
}
