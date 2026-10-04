// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

// Built only after merged scaffolds and implicit read models have their final meaning.
// Prefixes implement nearest shared scope; suffixes implement explicit qualification.
sealed class McpQueryIndex
{
    readonly Dictionary<(string Name, string Scope), List<McpDeclaration>> _prefixes = [];
    readonly Dictionary<(string Name, string Scope), List<McpDeclaration>> _suffixes = [];
    readonly Dictionary<(string Kind, string Address), McpDeclaration[]> _addresses;
    readonly Dictionary<(string Kind, string Address), McpDeclaration[]> _productionCollisions = [];
    readonly Dictionary<(string Kind, string Scope, string Name, Cratis.Screenplay.Diagnostics.SourceLocation Location), McpDeclaration[]> _productionDeclarations;
    readonly Dictionary<McpReference, McpDeclaration[]> _resolutions = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<(string Name, string Kinds, string Scope), McpDeclaration[]> _names = [];
    readonly Dictionary<(string Name, string Kinds, string Scope), McpDeclaration[]> _productionNames = [];
    readonly Lock _resolutionLock = new();
    readonly Dictionary<McpDeclaration, List<McpQueryIndexResolution>> _incoming = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<McpReadOwner, List<McpReference>> _outgoing = [];
    readonly Dictionary<string, List<McpReference>> _outgoingByAddress = new(StringComparer.Ordinal);
    readonly List<McpQueryIndexResolution> _resolvedReferences = [];

    internal McpQueryIndex(IEnumerable<McpDeclaration> declarations, IEnumerable<McpReference> references)
    {
        var declared = declarations.ToArray();
        _addresses = declared.GroupBy(declaration => (declaration.Kind, declaration.Address)).ToDictionary(group => group.Key, group => group.ToArray());
        _productionDeclarations = declared.Where(declaration => declaration.Kind == "Event" || declaration.Kind == "Operation")
            .GroupBy(declaration => (declaration.Kind, Scope: ScopeKey(declaration.Scope), declaration.Name, declaration.Location))
            .ToDictionary(group => group.Key, group => group.ToArray());
        var inlineOwners = declared.Where(declaration => declaration.Syntax is CommandSyntax)
            .SelectMany(owner => ((CommandSyntax)owner.Syntax).Produces
                .Where(production => production.InlineOperation is not null)
                .Select(production => (Node: production.InlineOperation, Owner: owner)))
            .ToLookup(entry => entry.Node, entry => entry.Owner, ReferenceEqualityComparer.Instance);
        foreach (var (key, occurrences) in _addresses.Where(entry => entry.Key.Kind == "Event" || entry.Key.Kind == "Operation"))
        {
            var ownerKeys = occurrences.SelectMany(declaration => new[] { (Kind: "Slice", Address: string.Join('.', declaration.Scope)) }
                .Concat(inlineOwners[declaration.Syntax].Select(owner => (owner.Kind, owner.Address)))).Distinct();
            var owners = ownerKeys.SelectMany(owner => Find(owner.Address, owner.Kind)).ToArray();
            var ownerCollision = owners.GroupBy(owner => (owner.Kind, owner.Address)).Any(group => group.Count() > 1);
            var declarationCollision = key.Kind == "Operation" ? occurrences.Length > 1
                : occurrences.Select(declaration => ((EventSyntax)declaration.Syntax).Generation).Distinct().Count() != occurrences.Length;
            if (declarationCollision || ownerCollision)
            {
                // Multiple physical declarations already carry their source evidence. If only
                // one survived in an ambiguous owner, retain that owner's physical witnesses too.
                _productionCollisions[key] = occurrences.Length > 1 ? occurrences : [.. occurrences, .. owners];
            }
        }
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

        if (reference.ProductionResolution is not null)
        {
            var key = (reference.Name, ScopeKey(reference.Kinds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)), ScopeKey(reference.Scope));
            lock (_resolutionLock)
            {
                if (!_productionNames.TryGetValue(key, out var candidates))
                {
                    ResolutionCount++;
                    candidates = ResolveProduction(reference);
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

    McpDeclaration[] ResolveProduction(McpReference reference)
    {
        var production = reference.ProductionResolution!;
        var declarations = production.Declaration is { } declaration ? [declaration] : production.Candidates;
        if (production.Kind == AuthoringProductionKind.Unresolved)
        {
            // Merge can discard the only target along with a duplicated slice. An
            // unresolved assembled result must not erase that physical collision.
            var physical = ResolveCachedName(reference with { Kinds = ["Event", "Operation"] });

            return [.. physical.SelectMany(candidate => _productionCollisions.GetValueOrDefault((candidate.Kind, candidate.Address)) ?? [])
                .Distinct(ReferenceEqualityComparer.Instance).Cast<McpDeclaration>()];
        }

        return [.. declarations.SelectMany(candidate =>
        {
            var key = (candidate.Kind.ToString(), string.Join('.', candidate.Scope.Append(candidate.Name)));
            if (_productionCollisions.TryGetValue(key, out var collisions)) return collisions;

            // The assembled resolver selects kind and scope, including the current event
            // generation. Physical indexing must not undo that selection when it is unique.
            return _productionDeclarations.GetValueOrDefault((key.Item1, ScopeKey(candidate.Scope), candidate.Name, candidate.Node.Location)) ?? [];
        }).Distinct(ReferenceEqualityComparer.Instance).Cast<McpDeclaration>()];
    }

    McpDeclaration[] ResolveCachedName(McpReference reference)
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
