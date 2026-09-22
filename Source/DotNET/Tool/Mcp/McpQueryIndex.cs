// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp;

// Built only after merged scaffolds and implicit read models have their final meaning.
// Prefixes implement nearest shared scope; suffixes implement explicit qualification.
sealed class McpQueryIndex
{
    readonly Dictionary<(string Name, string Scope), List<McpDeclaration>> _prefixes = [];
    readonly Dictionary<(string Name, string Scope), List<McpDeclaration>> _suffixes = [];
    readonly Dictionary<(string Kind, string Address), McpDeclaration[]> _addresses;
    readonly Dictionary<McpReference, McpDeclaration[]> _resolutions = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<(string Name, string Kinds, string Scope), McpDeclaration[]> _names = [];
    readonly Lock _resolutionLock = new();
    readonly Dictionary<McpDeclaration, List<McpQueryIndexResolution>> _incoming = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<McpReadOwner, List<McpReference>> _outgoing = [];
    readonly Dictionary<string, List<McpReference>> _outgoingByAddress = new(StringComparer.Ordinal);
    readonly List<McpQueryIndexResolution> _resolvedReferences = [];

    internal McpQueryIndex(IEnumerable<McpDeclaration> declarations, IEnumerable<McpReference> references)
    {
        var declared = declarations.ToArray();
        _addresses = declared.GroupBy(declaration => (declaration.Kind, declaration.Address)).ToDictionary(group => group.Key, group => group.ToArray());
        foreach (var declaration in declared)
        {
            for (var depth = 0; depth <= declaration.Scope.Length; depth++)
            {
                Add(_prefixes, (declaration.Name, ScopeKey(declaration.Scope.Take(depth))), declaration);
                Add(_suffixes, (declaration.Name, ScopeKey(declaration.Scope.Skip(depth))), declaration);
            }
        }

        foreach (var reference in references)
        {
            var candidates = Resolve(reference);
            _resolutions.Add(reference, candidates);
            var resolution = new McpQueryIndexResolution(reference, candidates);
            _resolvedReferences.Add(resolution);
            foreach (var candidate in candidates)
            {
                Add(_incoming, candidate, resolution);
            }

            if (reference.Owner is not null)
            {
                Add(_outgoing, reference.Owner, reference);
                Add(_outgoingByAddress, reference.Owner.Address, reference);
            }
        }
    }

    internal IEnumerable<McpQueryIndexResolution> ResolvedReferences => _resolvedReferences;

    internal int ResolutionCount { get; private set; }

    internal int CandidateInspectionCount { get; private set; }

    internal static string ScopeKey(IEnumerable<string> scope) => string.Concat(scope.Select(segment => $"{segment.Length}:{segment}"));

    internal McpDeclaration[] Find(string address, string kind) => _addresses.GetValueOrDefault((kind, address)) ?? [];

    internal IEnumerable<McpQueryIndexResolution> Incoming(McpDeclaration declaration) => _incoming.GetValueOrDefault(declaration) ?? [];

    internal IEnumerable<McpReference> Outgoing(McpReadOwner owner) => _outgoing.GetValueOrDefault(owner) ?? [];

    internal IEnumerable<McpReference> Outgoing(string ownerAddress) => _outgoingByAddress.GetValueOrDefault(ownerAddress) ?? [];

    internal McpDeclaration[] Resolve(McpReference reference)
    {
        if (_resolutions.TryGetValue(reference, out var resolved))
        {
            return resolved;
        }

        // Fixture queries also construct equivalent references on demand. Cache those
        // by meaning, not object identity, and serialize cache misses across readers.
        var key = (reference.Name, ScopeKey(reference.Kinds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)), ScopeKey(reference.Scope));
        lock (_resolutionLock)
        {
            if (!_names.TryGetValue(key, out var candidates))
            {
                ResolutionCount++;
                candidates = ResolveName(reference);
                _names.Add(key, candidates);
            }

            return candidates;
        }
    }

    static void Add<TKey, TValue>(Dictionary<TKey, List<TValue>> dictionary, TKey key, TValue value)
        where TKey : notnull
    {
        if (!dictionary.TryGetValue(key, out var values))
        {
            values = [];
            dictionary.Add(key, values);
        }

        values.Add(value);
    }

    McpDeclaration[] ResolveName(McpReference reference)
    {
        var segments = reference.Name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return [];
        }

        if (segments.Length > 1)
        {
            return Candidates(_suffixes.GetValueOrDefault((segments[^1], ScopeKey(segments[..^1]))), reference);
        }

        for (var depth = reference.Scope.Length; depth >= 0; depth--)
        {
            var candidates = Candidates(_prefixes.GetValueOrDefault((segments[0], ScopeKey(reference.Scope.Take(depth)))), reference);
            if (candidates.Length > 0)
            {
                return candidates;
            }
        }

        return [];
    }

    McpDeclaration[] Candidates(List<McpDeclaration>? declarations, McpReference reference)
    {
        CandidateInspectionCount += declarations?.Count ?? 0;
        return declarations is null ? [] : [.. declarations.Where(declaration => reference.Kinds.Contains(declaration.Kind, StringComparer.Ordinal))];
    }
}
