// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly Dictionary<SemanticFixtureRoute, SpecificationEventSyntax> _routedFixtures = new(ReferenceEqualityComparer.Instance);

        SemanticCommandRoute? BindCommandRoute(CommandSyntax command, Dictionary<string, SemanticProperty> properties, ImmutableArray<SemanticProducedEvent> produced)
        {
            return command.Stream is { } route ? BindRoute(command, route, properties, [.. produced.Where(value => value.Route is null)], true) : null;
        }

        SemanticCommandRoute? BindRoute(CommandSyntax command, CommandStreamSyntax route, Dictionary<string, SemanticProperty> properties, ImmutableArray<SemanticProducedEvent> produced, bool commandRoute = false)
        {
            if (ResolveRoute(route.EventSource, route.Stream, route.Location) is not { } resolved) return null;
            var (source, stream) = resolved;
            if (source.IdentifierType is { } identifier)
            {
                if (commandRoute && properties.Values.SingleOrDefault(property => property.IsIdentifier) is { } property && property.Type != identifier)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Command '{command.Name}' identifier type must match its routed source's identifier type.", route.Location);
                }
                foreach (var production in produced)
                {
                    if (production.Destination is SemanticResolvedExpression destination && properties.Values.Single(property => property.Id == destination.Target).Type != identifier)
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Command '{command.Name}' production destination type must match its routed source's identifier type.", route.Location);
                    }
                }
            }
            if (!ValidateRouteShape(stream, route.StreamId, route.StreamIdParts, route.Location)) return null;
            var scalar = route.StreamId is null ? null : BindRouteExpression(route.StreamId.Source, stream.StreamIdType!, properties, command.Name);
            var parts = ImmutableArray.CreateBuilder<SemanticCommandRoutePart>();
            foreach (var declaration in stream.StreamIdParts)
            {
                var mapping = route.StreamIdParts.Single(part => string.Equals(part.Property, declaration.Name, StringComparison.Ordinal));
                if (BindRouteExpression(mapping.Source, declaration.Type, properties, command.Name) is { } expression)
                {
                    parts.Add(new(declaration.Name, expression));
                }
            }

            return new(source.Id, stream.Id) { StreamId = scalar, StreamIdParts = parts.ToImmutable() };
        }

        SemanticExpression? BindRouteExpression(ExpressionSyntax expression, SemanticTypeReference type, Dictionary<string, SemanticProperty> properties, string command)
        {
            if (expression is PathExpressionSyntax path)
            {
                if (path.Path.Contains('.', StringComparison.Ordinal))
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Stream id mapping '{path.Path}' on command '{command}' reads a property path; only direct command inputs are admitted by event routes.", path.Location);
                    return null;
                }
                if (!properties.TryGetValue(path.Path, out var property) || property.IsGenerated || property.Type.IsCollection || property.Type.IsOptional || property.Type != type)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Stream id mapping '{path.Path}' on command '{command}' must read a direct, required, non-collection, non-generated property of the declared stream id type; routing occurs before generation.", path.Location);
                    return null;
                }

                return SemanticExpression.Property(SemanticExpressionRootKind.Command, property.Id);
            }
            var literal = BindRouteValue(expression, type);

            return literal is null ? null : SemanticExpression.FromValue(literal);
        }

        SemanticValue? BindRouteValue(ExpressionSyntax expression, SemanticTypeReference type)
        {
            if (expression is not LiteralExpressionSyntax literal)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, "A fixture stream id requires a concrete scalar literal of its declared type.", expression.Location);
                return null;
            }
            var value = BindRouteLiteral(literal);
            var stream = new SemanticEventStream(default, "Literal", "Literal") { StreamIdType = type };
            var source = new SemanticEventSource(default, "Literal", "Literal", []);
            if (!SemanticEventRouting.TryFormat(source, stream, value, [], _routeConcepts, out _, out var failure))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, SemanticStreamIdFormatter.FailureMessage(failure), expression.Location);
                return null;
            }

            return value;
        }

        bool ValidateRouteShape(SemanticEventStream stream, PropertyMappingSyntax? scalar, IEnumerable<PropertyMappingSyntax> suppliedParts, SourceLocation location)
        {
            var parts = suppliedParts.ToArray();
            bool valid;
            if (stream.StreamIdType is not null)
            {
                valid = scalar is not null && parts.Length == 0;
            }
            else if (stream.StreamIdParts.IsEmpty)
            {
                valid = scalar is null && parts.Length == 0;
            }
            else
            {
                valid = scalar is null && parts.Length == stream.StreamIdParts.Length &&
                    stream.StreamIdParts.All(part => parts.Count(mapping => string.Equals(mapping.Property, part.Name, StringComparison.Ordinal)) == 1);
            }
            if (!valid)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Route to stream '{stream.Name}' must match its key shape and map each declared composite part exactly once.", location);
            }

            return valid;
        }

        SemanticSpecificationCommand BindRoutedSpecificationCommand(SpecificationCommandSyntax when, SemanticCommand command)
        {
            var bound = BindSpecificationCommand(when, command);
            var routes = command.Produces.Select(produced => produced.Route).Append(command.Route).OfType<SemanticCommandRoute>();
            var routedInputs = routes.SelectMany(route => route.StreamIdParts.Select(part => part.Value).Concat(route.StreamId is { } scalar ? [scalar] : []))
                .OfType<SemanticResolvedExpression>().Select(expression => expression.Target).ToHashSet();
            var literals = when.Values.Where(mapping => mapping.Source is LiteralExpressionSyntax)
                .GroupBy(mapping => mapping.Property, StringComparer.Ordinal).Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => (LiteralExpressionSyntax)group.Single().Source, StringComparer.Ordinal);

            return bound with
            {
                Values = [.. bound.Values.Select(value => routedInputs.Contains(value.TargetProperty) &&
                    literals.TryGetValue(command.Properties.Single(property => property.Id == value.TargetProperty).Name, out var literal)
                        ? value with { Value = BindRouteLiteral(literal) } : value)]
            };
        }

        SemanticFixtureRoute? BindFixtureRoute(SpecificationEventSyntax fixture)
        {
            if (fixture.Stream is not { } route || ResolveRoute(route.EventSource, route.Stream, route.Location) is not { } resolved) return null;
            var (source, stream) = resolved;
            if (!ValidateRouteShape(stream, route.StreamId, route.StreamIdParts, route.Location)) return null;
            var scalar = route.StreamId is null ? null : BindRouteValue(route.StreamId.Source, stream.StreamIdType!);
            var parts = ImmutableArray.CreateBuilder<SemanticFixtureRoutePart>();
            foreach (var declaration in stream.StreamIdParts)
            {
                var mapping = route.StreamIdParts.Single(part => string.Equals(part.Property, declaration.Name, StringComparison.Ordinal));
                if (BindRouteValue(mapping.Source, declaration.Type) is { } value) parts.Add(new(declaration.Name, value));
            }
            var bound = new SemanticFixtureRoute(source.Id, stream.Id) { StreamId = scalar, StreamIdParts = parts.ToImmutable() };
            _routedFixtures.Add(bound, fixture);

            return bound;
        }

        void ValidateRoutedExpectations(SemanticApplication application)
        {
            var slices = application.Modules.SelectMany(module => module.Features).SelectMany(Slices).ToArray();
            var commands = slices.SelectMany(slice => slice.Commands).ToArray();
            var otherProductions = slices.SelectMany(slice => slice.Reactions).SelectMany(reaction => reaction.Triggers)
                .SelectMany(trigger => trigger.Produces).Select(produced => produced.EventContract)
                .Concat(slices.SelectMany(slice => slice.Captures).SelectMany(capture => capture.Appends
                    .Concat(capture.Children.SelectMany(child => child.Appends)).Concat(capture.Nested.SelectMany(nested => nested.Appends)))
                    .Select(append => append.EventContract)).ToHashSet();
            foreach (var specification in slices.SelectMany(slice => slice.Specifications))
            {
                if (specification.When is not { } when)
                {
                    continue;
                }
                var command = commands.Single(value => value.Id == when.Command);
                foreach (var expected in specification.ThenEvents.Where(expected => expected.Route is not null || expected.Unrouted))
                {
                    if (!command.Produces.Any(produced => produced.EventContract == expected.EventContract) ||
                        commands.Any(value => value.Id != command.Id && value.Produces.Any(produced => produced.EventContract == expected.EventContract)) ||
                        otherProductions.Contains(expected.EventContract))
                    {
                        continue;
                    }
                    var contradicts = command.Produces.Where(produced => produced.EventContract == expected.EventContract).All(produced => Contradicts(produced.Route ?? command.Route));
                    bool Contradicts(SemanticCommandRoute? actual)
                    {
                        var differs = expected.Unrouted ? actual is not null :
                            actual is null || actual.Source != expected.Route!.Source || actual.Stream != expected.Route.Stream;
                        if (differs || expected.Route is not { } route || actual is null) return differs;
                        var (source, stream) = SemanticEventRouting.Resolve(application, route.Source, route.Stream);
                        var scalar = (actual.StreamId as SemanticValueExpression)?.Value;
                        var parts = actual.StreamIdParts.Where(part => part.Value is SemanticValueExpression)
                            .Select(part => new SemanticFixtureRoutePart(part.Part, ((SemanticValueExpression)part.Value).Value)).ToImmutableArray();
                        if (SemanticEventRouting.TryFormat(source, stream, scalar, parts, application.Concepts, out var actualRoute, out _) &&
                            SemanticEventRouting.TryFormat(source, stream, route.StreamId, route.StreamIdParts, application.Concepts, out var expectedRoute, out _))
                        {
                            differs = actualRoute != expectedRoute;
                        }
                        if (expected.EventSource is { } identity)
                        {
                            differs |= command.Produces.Where(produced => produced.EventContract == expected.EventContract)
                                .All(produced => SemanticModelValidator.ProducedEventSourceType(command, produced) is { } type && type != identity.Type);
                        }
                        return differs;
                    }
                    if (contradicts)
                    {
                        Error(
                            DiagnosticCodes.SpecificationStreamContradictsCommand,
                            "The expected event route contradicts its only producer, the command under test.",
                            expected.Route is { } fixtureRoute ? _routedFixtures[fixtureRoute].Location : syntax.Location);
                    }
                }
            }

            static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
        }

        // All commands, captures and reactions must be available before applying 0031's producer fallback.
        SemanticApplication BindRoutedFixtureSources(SemanticApplication application)
        {
            var bound = application with { Modules = [.. application.Modules.Select(module => module with { Features = [.. module.Features.Select(BindFeature)] })] };
            ValidateRoutedExpectations(bound);

            return bound;

            SemanticFeature BindFeature(SemanticFeature feature) => feature with
            {
                Features = [.. feature.Features.Select(BindFeature)],
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Specifications = [.. slice.Specifications.Select(specification => specification with
                    {
                        GivenEvents = [.. specification.GivenEvents.Select(fixture => fixture with { EventSource = BindSource(fixture.Route, fixture.EventContract, fixture.EventSource) })],
                        ThenEvents = [.. specification.ThenEvents.Select(fixture => fixture with { EventSource = BindSource(fixture.Route, fixture.EventContract, fixture.EventSource) })],
                        WhenAppended = specification.WhenAppended is { } append ? append with { EventSource = BindSource(append.Route, append.EventContract, append.EventSource) } : null
                    })]
                })]
            };

            SemanticEventSourceIdentity? BindSource(SemanticFixtureRoute? route, SemanticId eventContract, SemanticEventSourceIdentity? existing)
            {
                if (route is null) return existing;
                var fixtureSyntax = _routedFixtures[route];
                var type = SemanticEventRouting.FixtureSourceType(application, route, eventContract, out _);
                if (type is null)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Routed fixture for '{fixtureSyntax.EventType}' needs an identifier on its event source; its producers do not supply one unambiguous destination type.", fixtureSyntax.Location);
                    return null;
                }

                return fixtureSyntax.For is null ? null : BindEventSource(fixtureSyntax.For, type);
            }
        }
    }
}
