// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static class SpecificationStreamValidator
{
    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context)
    {
        if (!declarations.Slices.SelectMany(entry => entry.Slice.Specifications).Any(specification => specification.Given.Concat(specification.ThenEvents)
            .Concat(specification.WhenAppended is { } appended ? [appended] : []).Any(node => node.Stream is not null || node.NoStream is not null)))
        {
            return;
        }
        var catalog = new EventSourceCatalog(application);
        var values = new ResponseValueTypes(application);
        var producers = Producers(declarations).ToArray();
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var specification in slice.Specifications)
            {
                var command = specification.When is { } action
                    ? declarations.Resolve(action.CommandType, scope, owner => owner.Commands, item => item.Name)?.Node : null;
                foreach (var (node, required, expected) in specification.Given.Select(node => (node, true, false))
                    .Concat(specification.WhenAppended is { } appended ? [(appended, true, false)] : [])
                    .Concat(specification.ThenEvents.Select(node => (node, false, true))))
                {
                    if (node.Stream is null && node.NoStream is null) continue;
                    if (required && node.NoStream is { } noStream)
                    {
                        context.Error(DiagnosticCodes.InvalidSpecificationStream, "Expected 'stream Source.Stream', or 'no stream' on a then event.", noStream.Location);
                    }
                    TypeRefSyntax? identifier = null;
                    var @event = declarations.Event(node.EventType, scope);
                    var eventProducers = producers.Where(producer => ReferenceEquals(producer.Event, @event)).ToArray();
                    if (node.Stream is { } route)
                    {
                        var resolution = catalog.Resolve(route.EventSource, route.Stream);
                        if (resolution.Kind != EventSourceResolutionKind.Unique)
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, $"Stream '{route.EventSource}.{route.Stream}' is {resolution.Kind}; routing requires one physical source and stream.", route.Location);
                            continue;
                        }
                        var source = resolution.Sources[0];
                        var stream = resolution.Streams[0];
                        if ((stream.StreamId is null) != (route.StreamId is null))
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, stream.StreamId is null ? "An unkeyed stream cannot take a streamId mapping." : "This keyed stream requires a streamId mapping.", route.Location);
                        }
                        if (route.StreamId is { } mapping && (mapping.Source is (not LiteralExpressionSyntax { Value: not null }) or LiteralExpressionSyntax { Value: "" } || (stream.StreamId is { } target && !values.Compatible(mapping.Source, target))))
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, "A specification stream id needs a nonempty concrete scalar literal compatible with the stream's declared type.", mapping.Source.Location);
                        }
                        identifier = source.Identifier;
                        if (identifier is null)
                        {
                            var types = eventProducers.Select(producer => producer.Type).ToArray();
                            var distinct = types.OfType<TypeRefSyntax>().Select(type => (type.Name, type.IsOptional, type.IsCollection)).Distinct().ToArray();
                            if (types.Length == 0 || types.Any(type => type is null) || distinct.Length != 1)
                            {
                                context.Error(DiagnosticCodes.InvalidSpecificationStreamEventSource, $"Declare an identifier on source '{source.Name}'; the event's producers do not supply one unambiguous destination type.", route.Location);
                            }
                            else
                            {
                                identifier = types[0];
                            }
                        }
                        if (required && node.For is null)
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed given or when append event requires 'for <literal>'.", route.Location);
                        }
                        else if (node.For is { } identity && (identity is not LiteralExpressionSyntax { Value: not null } || (identifier is { } type &&
                            (type.IsCollection || type.IsOptional || !values.Compatible(identity, type)))))
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed event's for value must be a concrete literal compatible with the source's identifier type.", identity.Location);
                        }
                    }
                    if (!expected || command is null || eventProducers.Length == 0 || eventProducers.Any(producer => !ReferenceEquals(producer.Command, command))) continue;
                    var contradicts = Contradicts(node, command.Stream, catalog, application, values);
                    if (node.For is LiteralExpressionSyntax identityValue && eventProducers.All(producer => producer.Type is { } type &&
                        (identifier is not null ? declarations.Compatible(type, identifier) == false : !values.Compatible(identityValue, type))))
                    {
                        contradicts = true;
                    }
                    if (contradicts)
                    {
                        context.Error(DiagnosticCodes.SpecificationStreamContradictsCommand, "The expected event route contradicts its only producer, the command under test.", node.Stream?.Location ?? node.NoStream!.Location);
                    }
                }
            }
        }
    }

    static IEnumerable<Producer> Producers(ConsistencyDeclarations declarations)
    {
        foreach (var (slice, scope) in declarations.Slices)
        {
            foreach (var command in slice.Commands)
            {
                var productions = command.Produces.Where(produced => declarations.Productions.IsEventProduction(produced, slice)).ToArray();
                foreach (var produced in productions)
                {
                    if (declarations.Event(produced.Event, scope) is not { } @event) continue;
                    yield return new(@event, command, DestinationType(command, produced, productions, declarations));
                }
            }

            // Reaction and capture destinations may depend on upstream producer types. Until binding
            // admits routes, retain their unknown type rather than guessing an identifier fallback.
            foreach (var produced in slice.Reactions.SelectMany(reaction => reaction.Triggers).SelectMany(trigger => trigger.Produces ?? []))
            {
                if (declarations.Event(produced.Event, scope) is { } @event) yield return new(@event, null, null);
            }
            foreach (var capture in slice.Captures)
            {
                foreach (var appended in capture.Appends.Concat(capture.Children.SelectMany(child => child.Appends)).Concat(capture.Nested.SelectMany(nested => nested.Appends)))
                {
                    if (declarations.Event(appended.Event, scope) is { } @event) yield return new(@event, null, null);
                }
            }
        }
    }

    static bool Contradicts(SpecificationEventSyntax occurrence, CommandStreamSyntax? route, EventSourceCatalog catalog, ApplicationSyntax application, ResponseValueTypes values)
    {
        if (route?.PropertyCandidate is not null) route = null;
        if (occurrence.NoStream is not null) return route is not null;
        var expected = occurrence.Stream!;
        if (route is null || route.EventSource != expected.EventSource || route.Stream != expected.Stream) return true;
        var resolution = catalog.Resolve(expected.EventSource, expected.Stream);
        if (resolution.Kind != EventSourceResolutionKind.Unique || resolution.Streams[0].StreamId is not { } type) return false;
        var actualId = FormatStreamId(route.StreamId?.Source, type, application, values);
        var expectedId = FormatStreamId(expected.StreamId?.Source, type, application, values);

        return actualId is not null && expectedId is not null && actualId != expectedId;
    }

    // Only known portable scalar types prove equality. Paths and unavailable imported types defer
    // to execution; a UUID's authored case, hyphens and wrappers are not part of its stream identity.
    static string? FormatStreamId(ExpressionSyntax? expression, TypeRefSyntax type, ApplicationSyntax application, ResponseValueTypes values)
    {
        if (expression is not LiteralExpressionSyntax literal || !values.Compatible(literal, type)) return null;
        var concepts = application.Concepts.Where(concept => concept.Name == type.Name).ToArray();
        var primitive = concepts is [var concept] ? concept.Type : type.Name;
        if (concepts.Length > 1) return null;

        return (primitive, literal.Value) switch
        {
            ("String", string text) => text,
            ("Uuid", string text) when Guid.TryParse(text, out var uuid) => uuid.ToString("D", CultureInfo.InvariantCulture),
            ("Int", ExactNumber exact) => exact.CanonicalText,
            ("Int", double number) => number.ToString("0", CultureInfo.InvariantCulture),
            _ => null
        };
    }

    static TypeRefSyntax? DestinationType(CommandSyntax command, ProducesSyntax produced, ProducesSyntax[] productions, ConsistencyDeclarations declarations)
    {
        if (produced.For is PathExpressionSyntax path) return PathType(command, path.Path, declarations);
        if (produced.For is not null) return null;
        var identifier = command.Properties.FirstOrDefault(property => property.IsIdentifier)?.Name;
        if (productions.Any(sibling => sibling.For is not null && (sibling.For is not PathExpressionSyntax destination || destination.Path != identifier)) ||
            (productions.Any(sibling => sibling.InlineEvent is not null && sibling.For is null) && productions.Any(sibling => sibling.InlineEvent is null && sibling.For is null)))
        {
            return null;
        }
        if (produced.InlineEvent is null)
        {
            return productions.Any(sibling => sibling.For is not null || sibling.InlineEvent is not null) ? null : new("Uuid", false, false, produced.Location);
        }

        return command.Properties.Where(property => property.IsIdentifier && !property.Type.IsOptional && !property.Type.IsCollection).ToArray() is [var property] ? property.Type : null;
    }

    static TypeRefSyntax? PathType(CommandSyntax command, string path, ConsistencyDeclarations declarations)
    {
        var type = declarations.Property(command.Properties, path, out _)?.Type;
        if (type is null) return null;
        var segments = path.Split('.');
        for (var depth = 1; depth < segments.Length; depth++)
        {
            if (declarations.Property(command.Properties, string.Join('.', segments.Take(depth)), out _) is { } parent)
            {
                type = type with { IsCollection = type.IsCollection || parent.Type.IsCollection, IsOptional = type.IsOptional || parent.Type.IsOptional };
            }
        }

        return type;
    }

    sealed record Producer(EventSyntax Event, CommandSyntax? Command, TypeRefSyntax? Type);
}
