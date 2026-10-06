// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Selects source diagnostics using the same physical declaration and dependency index as MCP navigation.
/// </summary>
static class ScopedDiagnostics
{
    /// <summary>
    /// Compiles the complete source set and selects a named scope and its direct dependent declarations.
    /// </summary>
    /// <param name="sources">All source documents, keyed by application-relative path.</param>
    /// <param name="scope">A case-sensitive dotted module, feature or slice address.</param>
    /// <returns>The selection, or null when the scope does not exist.</returns>
    internal static ScopedDiagnosticResult? Select(IReadOnlyDictionary<string, string> sources, string scope) =>
        Select(new McpSnapshot(sources), scope);

    internal static ScopedDiagnosticResult? Select(McpSnapshot snapshot, string scope)
    {
        var declarations = snapshot.Index.Declarations.ToArray();
        if (string.IsNullOrWhiteSpace(scope) || !declarations.Any(declaration =>
            (declaration.Kind == "Module" || declaration.Kind == "Feature" || declaration.Kind == "Slice") && declaration.Address == scope))
        {
            return null;
        }

        var selected = declarations.Where(declaration => declaration.Address == scope || Within(declaration.Scope, scope)).ToHashSet();

        // Inspect only the original set: inclusion is direct, never a transitive closure.
        var names = selected.Select(declaration => declaration.Name).ToHashSet(StringComparer.Ordinal);
        var unresolved = snapshot.Index.ResolvedReferences.Where(edge => edge.Candidates.Length == 0).ToArray();
        var possiblyAffected = unresolved.Where(edge => !names.Contains(edge.Reference.Name.Split('.')[^1]) &&
            !selected.Any(declaration => declaration.Owner == edge.Reference.Owner)).ToArray();

        // A current snapshot cannot prove the former target of a removed or renamed name.
        // Include unresolved event consumers conservatively, and disclose other uncertainty separately.
        var dependents = snapshot.Index.ResolvedReferences
            .Where(edge => edge.Candidates.Any(selected.Contains) || (edge.Candidates.Length == 0 &&
                (names.Contains(edge.Reference.Name.Split('.')[^1]) || edge.Reference.Kinds.Contains("Event", StringComparer.Ordinal))))
            .Select(edge => edge.Reference.Owner)
            .OfType<McpReadOwner>().ToHashSet();
        var dependentDeclarations = declarations.Where(declaration => dependents.Contains(declaration.Owner) && !selected.Contains(declaration)).ToArray();
        var affected = dependentDeclarations.Select(declaration => string.Join('.', declaration.Scope))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var scopeCount = selected.Count;
        selected.UnionWith(dependentDeclarations);

        var lines = snapshot.Sources.ToDictionary(source => source.Key, source => source.Value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'), StringComparer.Ordinal);
        var ranges = declarations.SelectMany(declaration => declaration.Locations.Select(location => Range(declaration, location, lines)))
            .Where(range => range is not null).OfType<DeclarationRange>().ToArray();
        var diagnostics = snapshot.Compilation.Diagnostics.Where(diagnostic =>
        {
            var owner = ranges.Where(range => range.Contains(diagnostic.Location))
                .OrderByDescending(range => range.Start.Line).ThenByDescending(range => range.Start.Column).FirstOrDefault();
            if (owner is not null)
            {
                return selected.Contains(owner.Declaration);
            }

            // Unlocated application diagnostics cannot safely be excluded from any scope.
            if (diagnostic.Location.Path is null || diagnostic.Location.Line < 1)
            {
                return true;
            }

            var placement = snapshot.Placements.FirstOrDefault(document => document.Path == diagnostic.Location.Path && document.IsPlacementResolved);
            return placement is not null && (Within(placement.Placement.Scope, scope) ||
                selected.Any(declaration => declaration.Locations.Any(location => location.Path == placement.Path) &&
                    declaration.Kind == "Slice"));
        }).ToImmutableArray();

        return new(scope, scopeCount, dependentDeclarations.Length, diagnostics, [.. affected], possiblyAffected.Length, McpReferenceKinds.Coverage);
    }

    static bool Within(IEnumerable<string> segments, string scope) => string.Join('.', segments) is var address &&
        (address == scope || address.StartsWith(scope + ".", StringComparison.Ordinal));

    static DeclarationRange? Range(McpDeclaration declaration, SourceLocation location, Dictionary<string, string[]> sources)
    {
        if (location.Path is null || !sources.TryGetValue(location.Path, out var lines))
        {
            return null;
        }
        if (location.Line < 1 || location.Line > lines.Length)
        {
            return null;
        }
        var header = lines[location.Line - 1];

        // Import scaffolds have synthetic header locations, not physical declaration ranges.
        if ((declaration.Kind == "Module" || declaration.Kind == "Feature") &&
            !header.TrimStart().StartsWith(declaration.Kind.ToLowerInvariant() + " ", StringComparison.Ordinal))
        {
            return null;
        }

        var indent = header.TakeWhile(char.IsWhiteSpace).Count();
        var end = location.Line;
        while (end < lines.Length)
        {
            var line = lines[end];
            if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//", StringComparison.Ordinal) &&
                line.TakeWhile(char.IsWhiteSpace).Count() <= indent)
            {
                break;
            }
            end++;
        }

        return new(declaration, location, end);
    }

    sealed record DeclarationRange(McpDeclaration Declaration, SourceLocation Start, int EndLine)
    {
        internal bool Contains(SourceLocation location) => location.Path == Start.Path && location.Line >= Start.Line && location.Line <= EndLine;
    }
}
