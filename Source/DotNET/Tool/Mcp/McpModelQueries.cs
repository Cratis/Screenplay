// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpModelQueries
{
    internal static object Describe(McpSnapshot snapshot, int fileCount, JsonElement arguments)
    {
        var declarations = snapshot.Index.Declarations.ToArray();
        var view = McpJson.OptionalString(arguments, "view") ?? "summary";
        if (view == "summary")
        {
            return new
            {
                snapshot.Compilation.Success,
                snapshot.SourceRevision,
                fileCount,
                declarationCount = declarations.Length,
                moduleCount = declarations.Count(declaration => declaration.Kind == "Module"),
                featureCount = declarations.Count(declaration => declaration.Kind == "Feature"),
                sliceCount = declarations.Count(declaration => declaration.Kind == "Slice"),
                referenceCount = snapshot.Index.References.Count(),
                diagnostics = DiagnosticSummary(snapshot),
                navigation = "Use view=children with parent address, search-declarations, declaration-details, and find-references for bounded navigation."
            };
        }

        var parent = McpJson.OptionalString(arguments, "parent") ?? string.Empty;
        var candidates = declarations.Where(declaration => string.Join('.', declaration.Scope) == parent);
        if (view == "children")
        {
            candidates = candidates.Where(declaration => declaration.Kind == "Module" || declaration.Kind == "Feature" || declaration.Kind == "Slice");
        }
        else if (view != "declarations")
        {
            throw new McpFailure("Application view must be summary, children, or declarations.", -32602);
        }

        return Result(snapshot, candidates, arguments, declarations.Length);
    }

    internal static object Find(McpSnapshot snapshot, JsonElement arguments)
    {
        var name = McpJson.RequiredString(arguments, "name");
        var includeContent = McpJson.Boolean(arguments, "includeContent");
        var matches = Filter(snapshot.Index.Declarations, arguments).Where(declaration => declaration.Name == name || declaration.Address == name);
        var page = McpPaging.Page(matches, declaration => includeContent ? declaration : McpReadResults.Summary(declaration), arguments, snapshot.SourceRevision);
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            diagnostics = DiagnosticSummary(snapshot),
            page.TotalCount,
            page.Offset,
            page.NextOffset,
            matches = page.Items
        };
    }

    internal static object Search(McpSnapshot snapshot, JsonElement arguments)
    {
        var declarations = snapshot.Index.Declarations.ToArray();
        var candidates = declarations.AsEnumerable();
        var query = McpJson.OptionalString(arguments, "name");
        var match = McpJson.OptionalString(arguments, "match") ?? "exact";
        if (query is not null)
        {
            candidates = candidates.Where(declaration => Matches(declaration, query, match));
        }
        else if (match != "exact" && match != "prefix" && match != "contains")
        {
            throw new McpFailure("Match must be exact, prefix, or contains.", -32602);
        }

        return Result(snapshot, candidates, arguments, declarations.Length);
    }

    internal static object Diagnostics(McpSnapshot snapshot, int fileCount, JsonElement arguments)
    {
        var diagnostics = snapshot.Compilation.Diagnostics;
        if (McpJson.OptionalString(arguments, "document") is { } document)
        {
            diagnostics = diagnostics.Where(diagnostic => diagnostic.Location.Path == document);
        }

        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            fileCount,
            summary = DiagnosticSummary(snapshot),
            page = McpPaging.Page(diagnostics, arguments, snapshot.SourceRevision)
        };
    }

    internal static object DiagnosticSummary(McpSnapshot snapshot)
    {
        var diagnostics = snapshot.Compilation.Diagnostics.ToArray();
        return new
        {
            total = diagnostics.Length,
            errors = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            warnings = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning),
            information = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Information)
        };
    }

    internal static IEnumerable<McpDeclaration> Filter(IEnumerable<McpDeclaration> declarations, JsonElement arguments)
    {
        if (McpJson.OptionalString(arguments, "kind") is { } kind)
        {
            declarations = declarations.Where(declaration => declaration.Kind == kind);
        }

        if (McpJson.OptionalString(arguments, "document") is { } document)
        {
            declarations = declarations.Where(declaration => declaration.Locations.Any(location => location.Path == document));
        }

        if (McpJson.OptionalString(arguments, "scope") is { } scope)
        {
            var segments = scope.Length == 0 ? [] : scope.Split('.');
            var descendants = McpJson.Boolean(arguments, "descendants", true);
            declarations = declarations.Where(declaration => declaration.Scope.Take(segments.Length).SequenceEqual(segments, StringComparer.Ordinal) &&
                (descendants || declaration.Scope.Length == segments.Length));
        }

        return declarations;
    }

    static object Result(McpSnapshot snapshot, IEnumerable<McpDeclaration> candidates, JsonElement arguments, int population)
    {
        var filtered = Filter(candidates, arguments).OrderBy(declaration => declaration.Address, StringComparer.Ordinal).ThenBy(declaration => declaration.Kind, StringComparer.Ordinal);
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            population,
            diagnostics = DiagnosticSummary(snapshot),
            page = McpPaging.Page(filtered, McpReadResults.Summary, arguments, snapshot.SourceRevision)
        };
    }

    static bool Matches(McpDeclaration declaration, string query, string match) => match switch
    {
        "exact" => declaration.Name == query || declaration.Address == query,
        "prefix" => declaration.Name.StartsWith(query, StringComparison.Ordinal) || declaration.Address.StartsWith(query, StringComparison.Ordinal),
        "contains" => declaration.Name.Contains(query, StringComparison.Ordinal) || declaration.Address.Contains(query, StringComparison.Ordinal),
        _ => throw new McpFailure("Match must be exact, prefix, or contains.", -32602)
    };
}
