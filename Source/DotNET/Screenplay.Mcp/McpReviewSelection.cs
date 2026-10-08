// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

static class McpReviewSelection
{
    internal static McpDeclaration[] Declarations(McpSnapshot snapshot, JsonElement arguments)
    {
        var all = snapshot.Index.Declarations.ToArray();
        var scope = McpJson.OptionalString(arguments, "scope");
        if (scope is not null)
        {
            var candidates = all.Where(declaration => declaration.Address == scope && (declaration.Kind == "Module" || declaration.Kind == "Feature" || declaration.Kind == "Slice")).ToArray();
            if (candidates.Length != 1)
            {
                throw new McpFailure("Scope must name one unambiguous module, feature or slice address.", -32602);
            }
        }

        return [.. all.Where(declaration => scope is null || declaration.Address == scope || declaration.Hierarchy.Any(owner => owner.Address == scope))];
    }

    internal static bool Resolves(McpSyntaxIndex index, McpDeclaration from, string name, string kind, McpDeclaration target)
    {
        var candidates = index.Resolve(new(name, [kind], from.Scope, from.Location, "review", from.Owner));

        return candidates is [var candidate] && candidate.Address == target.Address && candidate.Kind == target.Kind;
    }

    internal static object Page(McpSnapshot snapshot, IEnumerable<object> items, JsonElement arguments, string coverage) => new
    {
        snapshot.Compilation.Success,
        snapshot.SourceRevision,
        diagnostics = McpModelQueries.DiagnosticSummary(snapshot),
        coverage,
        page = McpPaging.BoundedSourcePage(items, arguments, snapshot.SourceRevision)
    };
}
