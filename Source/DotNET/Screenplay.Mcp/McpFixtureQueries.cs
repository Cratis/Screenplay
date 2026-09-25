// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

static class McpFixtureQueries
{
    internal static object Values(McpSnapshot snapshot, string? specification, string? role, string? property, string? value, int offset = 0, int limit = 50, string? scope = null, string? document = null) => new
    {
        snapshot.Compilation.Success,
        snapshot.SourceRevision,
        snapshot.Compilation.Diagnostics,
        coverage = "Specification property assignments and explicit destinations only. Values are syntax, not evaluated expressions. Types resolve only direct fields of an unambiguous local declaration; nested paths, imports and implicit view shapes have no inferred type.",
        page = McpReadPage<McpFixtureValue>.Create(
            McpFixtureOccurrences.All(snapshot.Index).SelectMany(occurrence => Values(snapshot.Index, occurrence)),
            item => (specification is null || item.Specification.Address == specification) &&
                (role is null || item.Role == role) && (property is null || item.Property == property) &&
                (value is null || Convert.ToString(item.Value, CultureInfo.InvariantCulture) == value) &&
                (scope is null || item.Specification.Address.StartsWith(scope + ".", StringComparison.Ordinal)) &&
                (document is null || item.Location.Path == document),
            offset,
            limit)
    };

    internal static object AssertionGaps(McpSnapshot snapshot, int offset = 0, int limit = 50, string? scope = null, string? document = null) => new
    {
        snapshot.Compilation.Success,
        snapshot.SourceRevision,
        snapshot.Compilation.Diagnostics,
        coverage = "Authored assertions only: then denied, events, errors, read models or queries. A gap is not proof of missing runtime test coverage.",
        page = McpReadPage<McpFixtureAssertionGap>.Create(
            snapshot.Index.Declarations
            .Where(declaration => declaration.Syntax is SliceSyntax)
            .Select(declaration => Gap(declaration)),
            gap => gap.SpecificationsWithAssertions == 0 &&
                (scope is null || gap.Slice.Address == scope || gap.Slice.Address.StartsWith(scope + ".", StringComparison.Ordinal)) &&
                (document is null || gap.Slice.Location.Path == document),
            offset,
            limit)
    };

    static McpFixtureAssertionGap Gap(McpDeclaration declaration)
    {
        var specifications = ((SliceSyntax)declaration.Syntax).Specifications.ToArray();

        return new(declaration.Owner, specifications.Length, specifications.Count(HasAssertions));
    }

    static bool HasAssertions(SpecificationSyntax specification) => specification.ThenDenied is not null || specification.ThenEvents.Any() || specification.ThenErrors.Any() ||
        (specification.ThenReadModels?.Any() ?? false) || specification.ThenQueries.Any();

    static IEnumerable<McpFixtureValue> Values(McpSyntaxIndex index, McpFixtureOccurrence occurrence)
    {
        var candidates = index.Resolve(occurrence.Reference).Select(declaration => declaration.Owner).ToArray();
        foreach (var mapping in occurrence.Values)
        {
            yield return new(
                occurrence.Specification,
                occurrence.Role,
                occurrence.Ordinal,
                occurrence.Reference.Name,
                candidates,
                mapping.Property,
                McpFixtureTypes.For(index, occurrence, mapping.Property),
                mapping.Source.GetType().Name,
                Value(mapping.Source),
                mapping.Location);
        }

        if (occurrence.For is { } destination)
        {
            yield return new(
                occurrence.Specification,
                $"{occurrence.Role}Destination",
                occurrence.Ordinal,
                occurrence.Reference.Name,
                candidates,
                "for",
                null,
                destination.GetType().Name,
                Value(destination),
                destination.Location);
        }
    }

    static object? Value(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => literal.Value,
        ListExpressionSyntax list => list.Items.Select(Value).ToArray(),
        ObjectExpressionSyntax obj => obj.Members.GroupBy(member => member.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => Value(group.Last().Value), StringComparer.Ordinal),
        PathExpressionSyntax path => path.Path,
        RawExpressionSyntax raw => raw.Text,
        ContextExpressionSyntax context => $"$context.{context.Path}",
        EnvironmentExpressionSyntax environment => $"$env.{environment.Name}",
        StringsExpressionSyntax strings => $"$strings.{strings.Key}",
        SourceItemExpressionSyntax source => $"$.{source.Path}",
        _ => null
    };
}
