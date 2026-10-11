// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

// Admission reachability is deliberately broader than instance selection: both projection engines must be safe,
// including event publication's ordinary 'every' subscription and blocks that cannot yet resolve an instance.
static class SemanticDeferredProjections
{
    internal static string? FindFailure(SemanticExecutionPlan plan, IEnumerable<SemanticFact> facts) =>
        plan.Projections.Values.Where(projection => plan.UnadmittedArtifacts.Contains(projection.Id))
            .Select(projection => FailureFor(projection, facts)).FirstOrDefault(failure => failure is not null);

    internal static string? FailureFor(SemanticProjection projection, IEnumerable<SemanticFact> facts) =>
        facts.Any(fact => CanObserve(projection, fact))
            ? $"Projection '{projection.Name}' was not admitted by scoped execution-plan compilation."
            : null;

    static bool CanObserve(SemanticProjection projection, SemanticFact fact) =>
        projection.Transitions.Any(transition => transition.EventContract == fact.EventContract) ||
        (projection.Scope is { } scope && CanObserve(scope, fact.EventContract));

    static bool CanObserve(SemanticProjectionScope scope, SemanticId eventContract) =>
        scope.Every is not null ||
        scope.From.Any(from => from.EventContract == eventContract) ||
        scope.Joins.Any(join => join.EventContract == eventContract) ||
        scope.Removals.Any(removal => removal.EventContract == eventContract) ||
        scope.JoinRemovals.Any(removal => removal.EventContract == eventContract) ||
        scope.Children.Any(children => CanObserve(children.Scope, eventContract)) ||
        scope.Nested.Any(nested => CanObserve(nested.Scope, eventContract));
}
