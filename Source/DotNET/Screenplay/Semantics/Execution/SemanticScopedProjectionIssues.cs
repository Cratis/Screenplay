// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Finds the scoped projection constructs the reference evaluator does not execute, so plan creation fails closed on them.
/// </summary>
static class SemanticScopedProjectionIssues
{
    /// <summary>
    /// Gets the plan issues of one projection.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <returns>The issues; empty for a flat projection.</returns>
    internal static IEnumerable<SemanticPlanIssue> For(SemanticProjection projection)
    {
        if (projection.Scope is not { } scope)
        {
            yield break;
        }

        // Chronicle's engine wires a projection-level 'remove via join' as a pull from a child collection at the root path
        // (ProjectionFactory.SetupRemovedWithJoin, ProjectionFactory.cs:297-308), while its documentation promises a delete.
        if (!scope.JoinRemovals.IsEmpty)
        {
            yield return Block(projection, "A 'remove via join' on a projection's own level has no verified meaning: Chronicle's engine wires it as a child removal (ProjectionFactory.cs:297-308).");
        }

        // An 'all' subscription reaches events other blocks own through their own key resolvers (ProjectionFactory.cs:616-631);
        // only the combination with from, join and every mappings on the projection's own level is modeled.
        if (scope.Every is { SubscribesToAllEvents: true } &&
            (!scope.Removals.IsEmpty || !scope.Children.IsEmpty || !scope.Nested.IsEmpty))
        {
            yield return Block(projection, "An 'all' block beside removals, children or nested objects has no verified reference semantics.");
        }

        foreach (var issue in Walk(projection, scope, false))
        {
            yield return issue;
        }
    }

    static IEnumerable<SemanticPlanIssue> Walk(SemanticProjection projection, SemanticProjectionScope scope, bool nested)
    {
        // Chronicle's engine wires only from, removals, every and further nested objects inside a nested object
        // (ProjectionFactory.SetupNestedSubscriptions, ProjectionFactory.cs:415-470); anything else there is dropped.
        if (nested && (!scope.Joins.IsEmpty || !scope.Children.IsEmpty || !scope.JoinRemovals.IsEmpty))
        {
            yield return Block(projection, "A join, children block or 'remove via join' inside a nested object is not wired by Chronicle's engine (ProjectionFactory.cs:415-470).");
        }

        if (Mappings(scope).Any(_ => _.Source is SemanticProjectionEventContextValue) ||
            scope.From.Any(_ => UsesEventContext(_.Key) || UsesEventContext(_.ParentKey)) ||
            scope.Removals.Any(_ => UsesEventContext(_.Key) || UsesEventContext(_.ParentKey)) ||
            scope.JoinRemovals.Any(_ => UsesEventContext(_.Key)))
        {
            yield return new(
                projection.Id,
                SemanticPlanIssueKind.UnsupportedEventContext,
                "An event-context value other than the event source identity needs occurrence context that ESM v1 facts do not carry.");
        }

        foreach (var issue in scope.Children.SelectMany(_ => Walk(projection, _.Scope, false)).Concat(scope.Nested.SelectMany(_ => Walk(projection, _.Scope, true))))
        {
            yield return issue;
        }
    }

    static IEnumerable<SemanticProjectionMapping> Mappings(SemanticProjectionScope scope) =>
        scope.From.SelectMany(_ => _.Mappings)
            .Concat(scope.Joins.SelectMany(_ => _.Mappings))
            .Concat(scope.Every?.Mappings ?? []);

    static bool UsesEventContext(SemanticProjectionKey? key) => key switch
    {
        SemanticProjectionValueKey value => value.Value is SemanticProjectionEventContextValue,
        SemanticProjectionCompositeKey composite => composite.Parts.Any(_ => _.Value is SemanticProjectionEventContextValue),
        _ => false
    };

    static SemanticPlanIssue Block(SemanticProjection projection, string details) =>
        new(projection.Id, SemanticPlanIssueKind.UnsupportedProjectionBlock, $"Projection '{projection.Name}': {details}");
}
