// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

static class McpFixtureOccurrences
{
    internal static string Role(SpecificationSyntax specification, SyntaxNode node, string fallback)
    {
        if (node is SpecificationEventSyntax)
        {
            if (specification.Given.Any(item => ReferenceEquals(item, node))) return "givenEvent";
            if (ReferenceEquals(specification.WhenAppended, node)) return "whenAppendedEvent";
            return "thenEvent";
        }

        if (node is SpecificationReadModelSyntax)
        {
            return (specification.GivenReadModels ?? []).Any(item => ReferenceEquals(item, node)) ? "givenReadModel" : "thenReadModel";
        }

        return fallback;
    }

    internal static IEnumerable<McpFixtureOccurrence> All(McpSyntaxIndex index) => index.Declarations
        .Where(declaration => declaration.Syntax is SpecificationSyntax)
        .SelectMany(For);

    static IEnumerable<McpFixtureOccurrence> For(McpDeclaration declaration)
    {
        var specification = (SpecificationSyntax)declaration.Syntax;
        var ordinal = 0;
        foreach (var item in specification.Given)
        {
            yield return Occurrence(item.EventType, "Event", "givenEvent", item, item.Values, item.For);
        }

        if (specification.WhenAppended is { } appended)
        {
            yield return Occurrence(appended.EventType, "Event", "whenAppendedEvent", appended, appended.Values, appended.For);
        }

        foreach (var item in specification.ThenEvents)
        {
            yield return Occurrence(item.EventType, "Event", "thenEvent", item, item.Values, item.For);
        }

        if (specification.When is { } when)
        {
            yield return Occurrence(when.CommandType, "Command", "whenCommand", when, when.Values, when.For);
        }

        foreach (var item in (specification.GivenReadModels ?? []).Concat(specification.ThenReadModels ?? []))
        {
            yield return Occurrence(item.Name, "ReadModel", Role(specification, item, string.Empty), item, item.Properties);
        }

        foreach (var query in specification.ThenQueries)
        {
            yield return Occurrence(query.Query, "Query", "queryArguments", query, query.Arguments);
            foreach (var result in query.Results)
            {
                yield return Occurrence(query.Query, "Query", "queryResult", result, result.Properties);
            }
        }

        McpFixtureOccurrence Occurrence(string name, string kind, string role, SyntaxNode node, IEnumerable<PropertyMappingSyntax> values, ExpressionSyntax? destination = null) =>
            new(declaration.Owner, role, new(name, [kind], declaration.Scope, node.Location, role, declaration.Owner), ordinal++, values, destination);
    }
}
