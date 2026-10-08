// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Parsing;

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
    /// <returns>The selection, or null when the scope does not exist or is ambiguous.</returns>
    internal static ScopedDiagnosticResult? Select(IReadOnlyDictionary<string, string> sources, string scope) =>
        Select(new McpSnapshot(sources), scope);

    internal static ScopedDiagnosticResult? Select(McpSnapshot snapshot, string scope) => Select(snapshot, scope, out _);

    internal static ScopedDiagnosticResult? Select(McpSnapshot snapshot, string scope, out string? scopeError)
        => Select(snapshot, scope, [], out scopeError);

    internal static ScopedDiagnosticResult? Select(McpSnapshot snapshot, string scope, IEnumerable<Diagnostic> additional, out string? scopeError)
    {
        var declarations = snapshot.Index.Declarations.ToArray();
        var matches = declarations.Where(declaration =>
            (declaration.Kind == "Module" || declaration.Kind == "Feature" || declaration.Kind == "Slice") && declaration.Address == scope).ToArray();
        scopeError = matches.Length switch
        {
            0 => $"Unknown scope '{scope}'. Expected a module, feature or slice address.",
            > 1 => $"Ambiguous scope '{scope}'. Expected exactly one module, feature or slice.",
            _ => null
        };
        if (string.IsNullOrWhiteSpace(scope) || scopeError is not null)
        {
            return null;
        }

        var anchor = matches[0];
        var selected = declarations.Where(declaration => declaration == anchor || declaration.Hierarchy.Contains(anchor.Owner)).ToHashSet();

        // Inspect only the original set: inclusion is direct, never a transitive closure.
        // An unresolved reference counts as a dependent only when a selected declaration of a kind it can
        // target carries its name; module, feature and slice names are never reference targets.
        var targets = selected.Where(declaration => declaration.Kind is not ("Module" or "Feature" or "Slice"))
            .Select(declaration => (declaration.Kind, declaration.Name)).ToHashSet();
        var unresolved = snapshot.Index.ResolvedReferences.Where(edge => edge.Candidates.Length == 0).ToArray();
        var dependents = snapshot.Index.ResolvedReferences
            .Where(edge => edge.Candidates.Any(selected.Contains) || (edge.Candidates.Length == 0 &&
                edge.Reference.Kinds.Any(kind => targets.Contains((kind, edge.Reference.Name.Split('.')[^1])))))
            .Select(edge => edge.Reference.Owner)
            .OfType<McpReadOwner>().ToHashSet();
        var dependentDeclarations = declarations.Where(declaration => dependents.Contains(declaration.Owner) && !selected.Contains(declaration)).ToArray();
        var affected = dependentDeclarations.Select(declaration => string.Join('.', declaration.Scope))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var scopeCount = selected.Count;
        selected.UnionWith(dependentDeclarations);

        // A current snapshot cannot prove the former target of a removed or renamed name.
        // Unattributable event references are uncertainty, not evidence of direct scope impact.
        var selectedOwners = selected.Select(declaration => declaration.Owner).ToHashSet();
        var outside = unresolved.Where(edge => edge.Reference.Owner is null || !selectedOwners.Contains(edge.Reference.Owner)).ToArray();
        var unresolvedEvents = outside.Where(edge => edge.Reference.Kinds.Contains("Event", StringComparer.Ordinal)).ToArray();
        var unresolvedEventScopes = unresolvedEvents.Select(edge => string.Join('.', edge.Reference.Scope))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        var possiblyAffected = outside.Except(unresolvedEvents).Count();

        var lines = snapshot.Sources.ToDictionary(source => source.Key, source => SourceLineSplitter.Split(source.Value, path: source.Key), StringComparer.Ordinal);
        var ranges = declarations.SelectMany(declaration => declaration.Locations.Select(location => Range(declaration, location, lines, snapshot.Languages)))
            .Where(range => range is not null).OfType<DeclarationRange>().ToArray();
        var diagnostics = snapshot.Compilation.Diagnostics.Concat(additional).Where(diagnostic =>
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

        return new(
            scope,
            scopeCount,
            dependentDeclarations.Length,
            diagnostics,
            [.. affected],
            new(unresolvedEvents.Length, unresolvedEventScopes),
            possiblyAffected,
            McpReferenceKinds.Coverage);
    }

    static bool Within(IEnumerable<string> segments, string scope) => string.Join('.', segments) is var address &&
        (address == scope || address.StartsWith(scope + ".", StringComparison.Ordinal));

    static DeclarationRange? Range(McpDeclaration declaration, SourceLocation location, Dictionary<string, IReadOnlyList<SourceLine>> sources, IScreenplayLanguageRegistry languages)
    {
        if (location.Path is null || !sources.TryGetValue(location.Path, out var lines))
        {
            return null;
        }
        if (location.Line < 1 || location.Line > lines.Count)
        {
            return null;
        }
        var header = lines[location.Line - 1];

        // Import scaffolds have synthetic header locations, not physical declaration ranges.
        if ((declaration.Kind == "Module" || declaration.Kind == "Feature") &&
            !header.Content.StartsWith(declaration.Kind.ToLowerInvariant() + " ", StringComparison.Ordinal))
        {
            return null;
        }

        var end = location.Line;
        var inFence = false;
        while (end < lines.Count)
        {
            var line = lines[end];
            if (inFence)
            {
                // The parser consumes raw body and closing lines regardless of indentation.
                if (CodeBlockParser.IsClosingFence(line)) inFence = false;
            }
            else
            {
                if (!line.IsBlank && line.Indent <= header.Indent)
                {
                    break;
                }

                inFence = CodeBlockParser.IsOpeningFence(line, languages);
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
