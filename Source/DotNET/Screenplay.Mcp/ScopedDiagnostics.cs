// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Selects source diagnostics using the same physical declaration and dependency index as MCP navigation.
/// </summary>
public static class ScopedDiagnostics
{
    /// <summary>
    /// Compiles the complete source set and selects a named scope and its direct dependent declarations.
    /// </summary>
    /// <param name="sources">All source documents, keyed by application-relative path.</param>
    /// <param name="scope">A case-sensitive dotted module, feature or slice address.</param>
    /// <returns>The selection, or null when the scope does not exist.</returns>
    public static ScopedDiagnosticResult? Select(IReadOnlyDictionary<string, string> sources, string scope) =>
        Select(new McpSnapshot(sources), sources, scope);

    internal static ScopedDiagnosticResult? Select(McpSnapshot snapshot, IReadOnlyDictionary<string, string> sources, string scope)
    {
        var declarations = snapshot.Index.Declarations.ToArray();
        if (string.IsNullOrWhiteSpace(scope) || !declarations.Any(declaration =>
            (declaration.Kind == "Module" || declaration.Kind == "Feature" || declaration.Kind == "Slice") && declaration.Address == scope))
        {
            return null;
        }

        var selected = declarations.Where(declaration => declaration.Address == scope || Within(declaration.Scope, scope)).ToHashSet();

        // Inspect only the original set: inclusion is direct, never a transitive closure.
        var dependents = snapshot.Index.ResolvedReferences
            .Where(edge => edge.Candidates.Any(selected.Contains))
            .Select(edge => edge.Reference.Owner)
            .OfType<McpReadOwner>().ToHashSet();
        var dependentDeclarations = declarations.Where(declaration => dependents.Contains(declaration.Owner) && !selected.Contains(declaration)).ToArray();
        var affected = dependentDeclarations.Select(declaration => string.Join('.', declaration.Scope))
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var scopeCount = selected.Count;
        selected.UnionWith(dependentDeclarations);

        var lines = sources.ToDictionary(source => source.Key, source => source.Value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'), StringComparer.Ordinal);
        var ranges = declarations.SelectMany(declaration => declaration.Locations.Select(location => Range(declaration, location, lines)))
            .Where(range => range is not null).OfType<DeclarationRange>().ToArray();
        var diagnostics = snapshot.Compilation.Diagnostics.Where(diagnostic =>
        {
            var owner = ranges.Where(range => range.Contains(diagnostic.Location))
                .OrderByDescending(range => range.Start.Line).ThenByDescending(range => range.Start.Column).FirstOrDefault();
            return owner is not null && selected.Contains(owner.Declaration);
        }).ToImmutableArray();

        return new(scope, scopeCount, dependentDeclarations.Length, diagnostics, [.. affected], McpReferenceKinds.Coverage);
    }

    static bool Within(string[] segments, string scope) => string.Join('.', segments) is var address &&
        (address == scope || address.StartsWith(scope + ".", StringComparison.Ordinal));

    static DeclarationRange? Range(McpDeclaration declaration, SourceLocation location, Dictionary<string, string[]> sources)
    {
        if (location.Path is null || !sources.TryGetValue(location.Path, out var lines)) return null;
        if (location.Line < 1 || location.Line > lines.Length) return null;
        var indent = lines[location.Line - 1].TakeWhile(char.IsWhiteSpace).Count();
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

/// <summary>
/// Reports the non-vacuity counts and direct impact of a scoped source check, not executable readiness.
/// </summary>
/// <param name="Scope">The requested scope.</param>
/// <param name="DeclarationCount">Declarations in the requested scope, including its header.</param>
/// <param name="DependentDeclarationCount">Additional directly dependent declarations.</param>
/// <param name="Diagnostics">Diagnostics belonging to selected declarations.</param>
/// <param name="AffectedScopes">Other scopes containing direct dependents; an empty address means application-level.</param>
/// <param name="DependencyCoverage">Limits of the explicit source reference index.</param>
public sealed record ScopedDiagnosticResult(
    string Scope,
    int DeclarationCount,
    int DependentDeclarationCount,
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<string> AffectedScopes,
    string DependencyCoverage);
