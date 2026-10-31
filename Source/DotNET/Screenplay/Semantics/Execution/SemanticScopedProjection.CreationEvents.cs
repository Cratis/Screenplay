// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    bool SharesEventWithParent(Level level) => level.Ancestors[^1].Scope.From.Any(from => from.EventContract == _fact.EventContract);

    // FindChildCreationEvent takes the first earlier event of another own type whose event source is the child key
    // (Chronicle KeyResolvers.cs:586-635). A failed historical resolution falls back without retaining its failure.
    Location? LocateViaCreationEvent(SemanticProjectionScope scope, Level level, SemanticValue identity)
    {
        if (!SharesEventWithParent(level))
        {
            return null;
        }

        var contracts = OwnEventContracts(scope).Where(contract => contract != _fact.EventContract).ToHashSet();
        var creation = observed.FirstOrDefault(fact => contracts.Contains(fact.EventContract) && SemanticValueRules.AreEqual(SourceOf(fact), identity));
        if (creation is null)
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
}
