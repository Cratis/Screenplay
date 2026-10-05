// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

// A candidate view, not a new set of resolution rules. Occurrences remain distinct even
// when their names and scopes agree; ReferenceResolver selects the winning scope.
internal sealed class ReferenceDeclarationIndex
{
    readonly Dictionary<(string Name, string Scope), List<Declaration>> _prefixes = [];
    readonly Dictionary<(string Name, string Scope), List<Declaration>> _suffixes = [];

    internal ReferenceDeclarationIndex(IEnumerable<Declaration> declarations)
    {
        foreach (var declaration in declarations)
        {
            for (var depth = 0; depth <= declaration.Scope.Depth; depth++)
            {
                Add(_prefixes, (declaration.Name, ScopeKey(declaration.Scope.Segments.Take(depth))), declaration);
                Add(_suffixes, (declaration.Name, ScopeKey(declaration.Scope.Segments.Skip(depth))), declaration);
            }
        }
    }

    internal static string ScopeKey(IEnumerable<string> segments) => string.Concat(segments.Select(segment => $"{segment.Length}:{segment}"));

    internal IReadOnlyList<Declaration> Qualified(string name, IReadOnlyList<string> qualifiers) =>
        _suffixes.GetValueOrDefault((name, ScopeKey(qualifiers))) ?? [];

    internal IReadOnlyList<Declaration> Visible(string name, DeclarationScope from, int depth) =>
        _prefixes.GetValueOrDefault((name, ScopeKey(from.Segments.Take(depth)))) ?? [];

    static void Add(Dictionary<(string Name, string Scope), List<Declaration>> index, (string Name, string Scope) key, Declaration declaration)
    {
        if (!index.TryGetValue(key, out var candidates)) index.Add(key, candidates = []);
        candidates.Add(declaration);
    }
}
