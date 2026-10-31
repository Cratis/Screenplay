// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Resolves shared child events through their earlier creation event, following Chronicle's FindParentEvent.
/// </summary>
internal sealed partial class SemanticScopedProjection
{
    static IEnumerable<SemanticId> OwnEventContracts(SemanticProjectionScope scope) =>
        scope.From.Select(_ => _.EventContract)
            .Concat(scope.Joins.Select(_ => _.EventContract))
            .Concat(scope.Removals.Select(_ => _.EventContract))
            .Concat(scope.JoinRemovals.Select(_ => _.EventContract));

    bool SharesEventWithParent(Level level)
    {
        // Avoid a capturing predicate on the ordinary, non-shared child path.
        foreach (var from in level.Ancestors[^1].Scope.From)
        {
            if (from.EventContract == _fact.EventContract)
            {
                return true;
            }
        }

        return false;
    }

    bool IsCreationContract(HashSet<SemanticId> contracts, SemanticId contract) =>
        contract != _fact.EventContract && contracts.Contains(contract);

    SemanticFact? FindCreationEvent(SemanticProjectionScope scope, SemanticValue identity)
    {
        var contracts = OwnedContractCache.Get(scope);
        // Preserve arrival order and structural semantic source equality.
        // An index would need a matching SemanticValue comparer, not record equality.
        for (var index = 0; index < observed.Count; index++)
        {
            var fact = observed[index];
            if (IsCreationContract(contracts, fact.EventContract) && SemanticValueRules.AreEqual(SourceOf(fact), identity))
            {
                return fact;
            }
        }

        return null;
    }

    // FindChildCreationEvent takes the first earlier event of another own type whose event source is the child key
    // (Chronicle KeyResolvers.cs:586-635). A failed historical resolution falls back without retaining its failure.
    Location? LocateViaCreationEvent(SemanticProjectionScope scope, Level level, SemanticValue identity)
    {
        if (!SharesEventWithParent(level))
        {
            return null;
        }

        if (FindCreationEvent(scope, identity) is not { } creation)
        {
            return null;
        }

        var from = scope.From.FirstOrDefault(candidate => candidate.EventContract == creation.EventContract);
        var key = from?.Key;
        var parentKey = from?.ParentKey;
        if (from is null)
        {
            var removal = scope.Removals.FirstOrDefault(candidate => candidate.EventContract == creation.EventContract);
            key = removal?.Key;
            parentKey = removal?.ParentKey;
        }

        if (key is null)
        {
            return null;
        }

        var current = _fact;
        var failure = _failure;
        try
        {
            _fact = creation;
            if (Key(key) is not { } childIdentity || ParentIdentity(parentKey, level) is not { } parent)
            {
                return null;
            }

            return LocateChild(level, childIdentity, parent);
        }
        finally
        {
            _fact = current;
            _failure = failure;
        }
    }

    // Scope records are immutable. Weak keys keep cached sets alive only as long as their scopes, across per-fact
    // projector instances and concurrent evaluator runs. This holder is reached only for a shared event.
    static class OwnedContractCache
    {
        static readonly ConditionalWeakTable<SemanticProjectionScope, HashSet<SemanticId>> _contracts = new();

        internal static HashSet<SemanticId> Get(SemanticProjectionScope scope) =>
            _contracts.GetValue(scope, static candidate => OwnEventContracts(candidate).ToHashSet());
    }
}
