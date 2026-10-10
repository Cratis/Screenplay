// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Indexing;

// One physical candidate view per snapshot. Exact ownership and scoped references are
// different operations: a suffix reference must never validate an exact authoring key.
sealed class ProductionInventory
{
    readonly AuthoringProductionResolver _resolver;
    readonly Dictionary<AuthoringProductionDeclaration, AuthoredDeclaration> _physical = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<(string Kind, string Scope, string Name), AuthoredDeclaration[]> _collisions = [];
    readonly Dictionary<(string Reference, string Scope), AuthoredDeclaration[]> _references = [];

    internal ProductionInventory(AuthoredDeclaration[] declarations)
    {
        var exact = declarations.ToLookup(declaration => Key(declaration.Kind, declaration.Scope, declaration.Name));
        var inlineOwners = declarations.Where(declaration => declaration.Syntax is CommandSyntax)
            .SelectMany(owner => ((CommandSyntax)owner.Syntax).Produces
                .Select(production => (Node: (SyntaxNode?)production.InlineOperation ?? production.InlineEvent, Owner: owner))
                .Where(entry => entry.Node is not null))
            .ToLookup(entry => entry.Node, entry => entry.Owner, ReferenceEqualityComparer.Instance);
        foreach (var group in declarations.Where(declaration => declaration.Kind == "Event" || declaration.Kind == "Operation")
            .GroupBy(declaration => (Scope: AuthoringReferences.ScopeKey(declaration.Scope), declaration.Name)))
        {
            var occurrences = group.ToArray();
            var owners = occurrences.SelectMany(declaration => exact[Key("Slice", declaration.Scope[..^1], declaration.Scope[^1])]
                .Concat(inlineOwners[declaration.Syntax].SelectMany(owner => exact[Key(owner.Kind, owner.Scope, owner.Name)])))
                .Distinct(ReferenceEqualityComparer.Instance).Cast<AuthoredDeclaration>().ToArray();
            var ownerCollision = owners.GroupBy(owner => Key(owner.Kind, owner.Scope, owner.Name)).Any(owner => owner.Count() > 1);
            var generations = occurrences.All(declaration => declaration.Kind == "Event") && !ownerCollision &&
                occurrences.All(declaration => !inlineOwners[declaration.Syntax].Any()) && ValidGenerations(occurrences);
            if (ownerCollision || (occurrences.Length > 1 && !generations))
            {
                var evidence = occurrences.Length > 1 ? occurrences : [.. occurrences, .. owners];
                foreach (var kind in occurrences.Select(declaration => declaration.Kind).Distinct(StringComparer.Ordinal))
                    _collisions.Add((kind, group.Key.Scope, group.Key.Name), evidence);
            }

            foreach (var candidate in generations ? occurrences.OrderByDescending(declaration => ((EventSyntax)declaration.Syntax).Generation).Take(1) : occurrences)
            {
                var declaration = new AuthoringProductionDeclaration(
                    candidate.Kind == "Event" ? AuthoringProductionKind.Event : AuthoringProductionKind.Operation,
                    candidate.Name,
                    candidate.Scope,
                    candidate.Syntax);
                _physical.Add(declaration, candidate);
            }
        }
        _resolver = new(_physical.Keys);
    }

    internal bool HasExactOwnershipCollision(string kind, string name, string[] scope) => _collisions.ContainsKey(Key(kind, scope, name));

    internal AuthoredDeclaration[] ResolveReference(string reference, string[] scope)
    {
        var key = (reference, AuthoringReferences.ScopeKey(scope));
        if (_references.TryGetValue(key, out var cached)) return cached;
        var resolution = _resolver.Resolve(reference, scope);
        var candidates = resolution.Declaration is { } declaration ? [declaration] : resolution.Candidates;

        var targets = candidates.SelectMany(candidate =>
            _collisions.GetValueOrDefault(Key(candidate.Kind.ToString(), candidate.Scope, candidate.Name)) ?? [_physical[candidate]])
            .Distinct(ReferenceEqualityComparer.Instance).Cast<AuthoredDeclaration>().ToArray();
        _references.Add(key, targets);

        return targets;
    }

    static (string Kind, string Scope, string Name) Key(string kind, IEnumerable<string> scope, string name) => (kind, AuthoringReferences.ScopeKey(scope), name);

    static bool ValidGenerations(AuthoredDeclaration[] occurrences)
    {
        var events = occurrences.Select(declaration => (EventSyntax)declaration.Syntax).OrderBy(node => node.Generation).ToArray();

        return events.Select(node => node.Id ?? node.Name).Distinct(StringComparer.Ordinal).Count() == 1 &&
            events.Select((node, index) => node.Generation == (uint)index + 1).All(valid => valid);
    }
}
