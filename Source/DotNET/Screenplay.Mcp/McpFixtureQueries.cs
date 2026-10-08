// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Printing;
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
        coverage = "Effective specification property assignments and explicit destinations, and event routes with authored/example/override origins, including whenRedeliveredEvent locators. Values are syntax, not evaluated expressions. Types resolve only direct fields of an unambiguous local declaration; nested paths, imports and implicit view shapes have no inferred type.",
        page = McpReadPage<McpFixtureValue>.Create(
            McpFixtureOccurrences.All(snapshot.Index, snapshot.Compilation.Value).SelectMany(occurrence => Values(snapshot.Index, occurrence, snapshot.Compilation.Value)),
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
        coverage = "Authored assertions only: then returns (admitted as ESM v7), denied, events, errors, read models or queries. A gap is not proof of missing runtime test coverage.",
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

    static bool HasAssertions(SpecificationSyntax specification) => specification.ThenReturns is not null || specification.ThenDenied is not null || specification.ThenEvents.Any() || specification.ThenErrors.Any() ||
        (specification.ThenReadModels?.Any() ?? false) || specification.ThenAbsentReadModels.Any() || specification.ThenQueries.Any();

    static IEnumerable<McpFixtureValue> Values(McpSyntaxIndex index, McpFixtureOccurrence occurrence, ApplicationSyntax? application)
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
                mapping.Location,
                Origin(occurrence, mapping.Property)?.Origin.ToString().ToLowerInvariant() ?? "authored",
                occurrence.Step?.Example?.Name,
                Origin(occurrence, mapping.Property)?.OverriddenValue is { } replaced ? Value(replaced) : null);
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
                destination.Location,
                Origin(occurrence, "for")?.Origin.ToString().ToLowerInvariant() ?? "authored",
                occurrence.Step?.Example?.Name,
                Origin(occurrence, "for")?.OverriddenValue is { } replaced ? Value(replaced) : null);
        }

        var routeOrigin = occurrence.Step?.Route?.Origin.ToString().ToLowerInvariant() ?? "authored";
        var routeExample = occurrence.Step?.Example?.Name;
        var replacedRoute = occurrence.Step?.Route?.OverriddenValue is { } replacedRouteValue ? RouteText(replacedRouteValue, application) : null;
        if (occurrence.Stream is { } stream)
        {
            yield return new(occurrence.Specification, $"{occurrence.Role}Stream", occurrence.Ordinal, occurrence.Reference.Name, candidates, "stream", null, nameof(SpecificationStreamSyntax), $"{stream.EventSource}.{stream.Stream}", stream.ReferenceLocation, routeOrigin, routeExample, replacedRoute);
            if (stream.StreamId is { } streamId)
            {
                yield return new(occurrence.Specification, $"{occurrence.Role}StreamId", occurrence.Ordinal, occurrence.Reference.Name, candidates, "streamId", null, streamId.Source.GetType().Name, Value(streamId.Source), streamId.Location, routeOrigin, routeExample, null);
            }
            foreach (var part in stream.StreamIdParts)
            {
                yield return new(occurrence.Specification, $"{occurrence.Role}StreamIdPart", occurrence.Ordinal, occurrence.Reference.Name, candidates, part.Property, null, part.Source.GetType().Name, Value(part.Source), part.Location, routeOrigin, routeExample, null);
            }
        }

        if (occurrence.NoStream is { } noStream)
        {
            yield return new(occurrence.Specification, $"{occurrence.Role}NoStream", occurrence.Ordinal, occurrence.Reference.Name, candidates, "no stream", null, nameof(SpecificationNoStreamSyntax), true, noStream.Location, routeOrigin, routeExample, replacedRoute);
        }
    }

    static string RouteText(SyntaxNode route, ApplicationSyntax? application)
    {
        if (route is SpecificationNoStreamSyntax) return "no stream";
        if (route is not SpecificationStreamSyntax stream) return string.Empty;
        var text = $"{stream.EventSource}.{stream.Stream}";
        if (stream.StreamId is { } id) return $"{text} streamId = {ScreenplaySyntaxText.ResponseValue(id.Source)}";
        var parts = stream.StreamIdParts;
        var resolution = application is null ? null : new EventSourceCatalog(application).Resolve(stream.EventSource, stream.Stream);
        if (resolution?.Kind == EventSourceResolutionKind.Unique)
        {
            var declarations = resolution.Streams[0].StreamIdParts.ToArray();
            var authored = parts.ToArray();
            if (declarations.Length == authored.Length && declarations.All(declaration => authored.Count(part => part.Property == declaration.Name) == 1))
            {
                parts = [.. declarations.Select(declaration => authored.Single(part => part.Property == declaration.Name))];
            }
        }

        return parts.Any() ? text + " streamId " + string.Join(", ", parts.Select(part => $"{part.Property} = {ScreenplaySyntaxText.ResponseValue(part.Source)}")) : text;
    }

    static EffectiveSpecificationValue? Origin(McpFixtureOccurrence occurrence, string property) =>
        occurrence.Step?.Values.SingleOrDefault(value => value.Property == (occurrence.Role == "generatedValues" ? "generated " + property : property));

    static object? Value(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax { Value: ExactNumber } => throw new McpFailure("ExactNumberFixtureTransportUnsupported: exact fixture values require the deferred lossless value-tree DTO contract; no numeric payload was returned."),
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
