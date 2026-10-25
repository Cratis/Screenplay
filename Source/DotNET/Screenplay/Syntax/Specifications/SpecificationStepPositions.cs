// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

internal static class SpecificationStepPositions
{
    // Only direct clauses count. Query results nested inside 'then query', mappings, parameters,
    // cases and example declarations are not steps. Use the authored node, before any synthesis.
    internal static int IndexOf(SpecificationSyntax authored, SourceLocation location) => Locations(authored)
        .Distinct().OrderBy(position => position.Line).ThenBy(position => position.Column)
        .TakeWhile(position => position != location).Count();

    static IEnumerable<SourceLocation> Locations(SpecificationSyntax specification)
    {
        var repeated = specification.Given.Cast<SyntaxNode>()
            .Concat(specification.GivenReadModels ?? []).Concat(specification.GivenCaptures).Concat(specification.GivenGenerated)
            .Concat(specification.GivenOperationFailures).Concat(specification.ThenEvents)
            .Concat(specification.ThenReadModels ?? []).Concat(specification.ThenQueries)
            .Concat(specification.ThenResults).Concat(specification.ThenAbsentReadModels)
            .Concat(specification.ThenErrors).Concat(specification.ThenOperations).Concat(specification.ThenCompensated).Concat(specification.ThenInvocations);
        SyntaxNode?[] singular = [specification.GivenCaller, specification.GivenCallerPersona, specification.GivenClock, specification.GivenTimeZone,
            specification.When, specification.WhenAppended, specification.WhenRedelivered, specification.WhenClock,
            specification.WhenTrigger, specification.WhenCapture, specification.WhenQuery, specification.ThenDenied,
            specification.ThenNoResult, specification.ThenReturns];
        return repeated.Concat(singular.OfType<SyntaxNode>()).Select(node => node.Location)
            .Concat(specification.DirectiveLocations.Where(entry => entry.Key == "then no events" || entry.Key == "then events in any order").Select(entry => entry.Value));
    }
}
