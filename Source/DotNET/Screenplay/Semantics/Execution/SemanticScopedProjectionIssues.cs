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
        if (projection.Target == SemanticProjectionTargetKind.Event)
        {
            foreach (var issue in SemanticEventPublication.IssuesFor(projection))
            {
                yield return issue;
            }

            yield break;
        }

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

        if (scope.From.SelectMany(_ => _.Mappings).Any(_ => UnsupportedMapping(_, !nested)) ||
            scope.Joins.Any(_ => UsesEventContext(_.Key) || _.Mappings.Any(mapping => UnsupportedMapping(mapping, false))) ||
            (scope.Every?.Mappings.Any(_ => UnsupportedMapping(_, !nested && scope.Joins.IsEmpty)) ?? false) ||
            scope.From.Any(_ => UsesEventContext(_.Key) || UsesEventContext(_.ParentKey)) ||
            scope.Removals.Any(_ => UsesEventContext(_.Key) || UsesEventContext(_.ParentKey)) ||
            scope.JoinRemovals.Any(_ => UsesEventContext(_.Key)))
        {
            yield return new(
                projection.Id,
                SemanticPlanIssueKind.UnsupportedEventContext,
                "Event context is supported only for event source identity and occurred property assignments in from, every and children scopes; occurred is unsupported in keys, joins, arithmetic, every mappings with joins and nested objects.");
        }

        foreach (var issue in scope.Children.SelectMany(_ => Walk(projection, _.Scope, nested)).Concat(scope.Nested.SelectMany(_ => Walk(projection, _.Scope, true))))
        {
            yield return issue;
        }
    }

    static bool UnsupportedMapping(SemanticProjectionMapping mapping, bool allowOccurred) =>
        UnsupportedContext(mapping.Factor) ||
        (UnsupportedContext(mapping.Source) && !(allowOccurred && mapping.Operation == SemanticProjectionOperation.Set &&
            mapping.Source is SemanticProjectionEventContextValue context && string.Equals(context.Path, "occurred", StringComparison.Ordinal) && mapping.Factor is null));

    static bool UsesEventContext(SemanticProjectionKey? key) => key switch
    {
        SemanticProjectionValueKey value => UnsupportedContext(value.Value),
        SemanticProjectionCompositeKey composite => composite.Parts.Any(_ => UnsupportedContext(_.Value)),
        SemanticProjectionPartsKey parts => parts.Parts.Any(part => UnsupportedContext(part.Value)),
        _ => false
    };

    static bool UnsupportedContext(SemanticProjectionValue? value) =>
        value is SemanticProjectionEventContextValue { Path: not "eventSourceId" };

    static SemanticPlanIssue Block(SemanticProjection projection, string details) =>
        new(projection.Id, SemanticPlanIssueKind.UnsupportedProjectionBlock, $"Projection '{projection.Name}': {details}");
}
