// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Indexing;

// Built only after merged scaffolds and implicit read models have their final meaning.
// Prefixes implement nearest shared scope; suffixes implement explicit qualification.
sealed class AuthoringReferences
{
    readonly Dictionary<(string Name, string Scope), List<AuthoredDeclaration>> _prefixes = [];
    readonly Dictionary<(string Name, string Scope), List<AuthoredDeclaration>> _suffixes = [];
    readonly Dictionary<(string Kind, string Address), AuthoredDeclaration[]> _addresses;
    readonly ProductionInventory _productions;
    readonly HashSet<string> _imports;
    readonly ILookup<string, AuthoredDeclaration> _sources;
    readonly ILookup<string, AuthoredDeclaration> _streams;
    readonly ILookup<string, AuthoredDeclaration> _sourceValueTypes;
    readonly Dictionary<AuthoredReference, AuthoredDeclaration[]> _resolutions = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<(string Name, string Kinds, string Scope), AuthoredDeclaration[]> _names = [];
    readonly Dictionary<(string Name, string Kinds, string Scope), AuthoredDeclaration[]> _productionNames = [];
    readonly Lock _resolutionLock = new();
    readonly Dictionary<AuthoredDeclaration, List<ReferenceResolution>> _incoming = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<ReadOwner, List<AuthoredReference>> _outgoing = [];
    readonly Dictionary<string, List<AuthoredReference>> _outgoingByAddress = new(StringComparer.Ordinal);
    readonly List<ReferenceResolution> _resolvedReferences = [];

    internal AuthoringReferences(IEnumerable<AuthoredDeclaration> declarations, IEnumerable<AuthoredReference> references, ProductionInventory productions, IEnumerable<string> imports)
    {
        var declared = declarations.ToArray();
        _addresses = declared.GroupBy(declaration => (declaration.Kind, declaration.Address)).ToDictionary(group => group.Key, group => group.ToArray());
        _productions = productions;
        _imports = imports.ToHashSet(StringComparer.Ordinal);
        _sources = declared.Where(declaration => declaration.Kind == "EventSource" && declaration.Scope.Length == 0).ToLookup(declaration => declaration.Name, StringComparer.Ordinal);
        _streams = declared.Where(declaration => declaration.Kind == "EventStream" && declaration.Scope.Length == 1).ToLookup(declaration => declaration.Address, StringComparer.Ordinal);
        _sourceValueTypes = declared.Where(declaration => (declaration.Kind == "Concept" || declaration.Kind == "Type") && declaration.Scope.Length == 0).ToLookup(declaration => declaration.Name, StringComparer.Ordinal);
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
            var resolution = new ReferenceResolution(reference, candidates);
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

    internal IEnumerable<ReferenceResolution> ResolvedReferences => _resolvedReferences;

    internal int ResolutionCount { get; private set; }

    internal int CandidateInspectionCount { get; private set; }

    internal static string ScopeKey(IEnumerable<string> scope) => string.Concat(scope.Select(segment => $"{segment.Length}:{segment}"));

    internal AuthoredDeclaration[] Find(string address, string kind) => _addresses.GetValueOrDefault((kind, address)) ?? [];

    internal bool HasExactOwnershipCollision(string kind, string name, string[] scope) => _productions.HasExactOwnershipCollision(kind, name, scope);

    internal IEnumerable<ReferenceResolution> Incoming(AuthoredDeclaration declaration) => _incoming.GetValueOrDefault(declaration) ?? [];

    internal IEnumerable<AuthoredReference> Outgoing(ReadOwner owner) => _outgoing.GetValueOrDefault(owner) ?? [];

    internal IEnumerable<AuthoredReference> Outgoing(string ownerAddress) => _outgoingByAddress.GetValueOrDefault(ownerAddress) ?? [];

    internal AuthoredDeclaration[] Resolve(AuthoredReference reference)
    {
        if (_resolutions.TryGetValue(reference, out var resolved))
        {
            return resolved;
        }

        if (reference.UseProductionCandidates)
        {
            var key = (reference.Name, ScopeKey(reference.Kinds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)), ScopeKey(reference.Scope));
            lock (_resolutionLock)
            {
                if (!_productionNames.TryGetValue(key, out var candidates))
                {
                    ResolutionCount++;
                    candidates = _productions.ResolveReference(reference.Name, reference.Scope);
                    CandidateInspectionCount += candidates.Length;
                    _productionNames.Add(key, candidates);
                }

                return candidates;
            }
        }

        return ResolveCachedName(reference);
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

    AuthoredDeclaration[] ResolveCachedName(AuthoredReference reference)
    {
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

    AuthoredDeclaration[] ResolveName(AuthoredReference reference)
    {
        if (reference.Kinds.Contains("EventSource", StringComparer.Ordinal))
        {
            var sources = _sources[reference.Name].ToArray();
            return sources.Length > 0 ? sources : [.. _sourceValueTypes[reference.Name]];
        }
        if (reference.Kinds.Contains("EventStream", StringComparer.Ordinal)) return [.. _streams[reference.Name]];
        if (reference.Kinds.Contains("Event", StringComparer.Ordinal) && reference.Kinds.Contains("Trigger", StringComparer.Ordinal))
        {
            var events = ResolveName(reference with { Kinds = ["Event"] });
            if (events.Length > 0 || _imports.Contains(reference.Name)) return events;
            return ResolveName(reference with { Kinds = ["Trigger"] });
        }
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

    AuthoredDeclaration[] Candidates(List<AuthoredDeclaration>? declarations, AuthoredReference reference)
    {
        CandidateInspectionCount += declarations?.Count ?? 0;
        return declarations is null ? [] : [.. declarations.Where(declaration => reference.Kinds.Contains(declaration.Kind, StringComparer.Ordinal))];
    }
}
