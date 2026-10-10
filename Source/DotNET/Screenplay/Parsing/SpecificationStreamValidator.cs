// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static class SpecificationStreamValidator
{
    internal static string? CanonicalStreamId(ExpressionSyntax? expression, TypeRefSyntax type, ApplicationSyntax application, ResponseValueTypes values) => FormatStreamId(expression, type, application, values);

    internal static void Validate(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context, EffectiveSpecificationApplication expansion)
    {
        var rows = expansion.Specifications.Where(specification => specification.Case is not null).Select(specification => specification.Case!).ToArray();
        if (rows.Length == 0)
        {
            ValidateRoutes(application, declarations, context, expansion);
            return;
        }
        var sink = ParserContext.ForDiagnostics();
        ValidateRoutes(application, declarations, sink, expansion);
        var reported = new HashSet<SourceLocation>();
        foreach (var diagnostic in sink.Diagnostics.DistinctBy(diagnostic => (diagnostic.Code, diagnostic.Location, diagnostic.Message)))
        {
            var row = diagnostic.Code == DiagnosticCodes.InvalidSpecificationStreamRoute ? rows.FirstOrDefault(row => row.Values.Any(value => value.Source.Location == diagnostic.Location)) : null;
            if (row is null) context.Add(diagnostic);
            else if (reported.Add(diagnostic.Location)) context.Add(diagnostic with { Message = $"Case '{row.Name}': {diagnostic.Message}" });
        }
    }

    static void ValidateRoutes(ApplicationSyntax application, ConsistencyDeclarations declarations, ParserContext context, EffectiveSpecificationApplication expansion)
    {
        var catalog = new EventSourceCatalog(application);
        var values = new ResponseValueTypes(application);
        var producers = Producers(declarations).ToArray();
        foreach (var occurrence in SpecificationRouteOccurrence.In(declarations, expansion))
        {
            var node = occurrence.Node;
            var command = occurrence.Command;
            if (node.Stream is null && node.NoStream is null) continue;
            if (occurrence.Required && node.NoStream is { } noStream)
            {
                occurrence.ContextualError(context, DiagnosticCodes.InvalidSpecificationStream, "Expected 'stream Source.Stream', or 'no stream' on a then event.", noStream.Location);
            }
            TypeRefSyntax? identifier = null;
            var eventProducers = producers.Where(producer => ReferenceEquals(producer.Event, occurrence.Event)).ToArray();
            if (node.Stream is { } route)
            {
                var resolution = catalog.Resolve(route.EventSource, route.Stream);
                if (resolution.Kind != EventSourceResolutionKind.Unique)
                {
                    if (occurrence.ValidateRoute) context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, $"Stream '{route.EventSource}.{route.Stream}' is {resolution.Kind}; routing requires one physical source and stream.", route.Location);
                    continue;
                }
                var source = resolution.Sources[0];
                var stream = resolution.Streams[0];
                if (occurrence.ValidateRoute)
                {
                    if (stream.StreamIdParts.Any())
                    {
                        EventSourceValidator.ValidateParts(stream, route.StreamIdParts, route.StreamId is not null, route.Location, DiagnosticCodes.InvalidSpecificationStreamRoute, context, (mapping, target) => ValidateLiteral(mapping, target, application, values, context));
                    }
                    else if (route.StreamIdParts.Any())
                    {
                        context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, "A streamId part block requires a composite stream.", route.Location);
                    }
                    else
                    {
                        if ((stream.StreamId is null) != (route.StreamId is null))
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, stream.StreamId is null ? "An unkeyed stream cannot take a streamId mapping." : "This keyed stream requires a streamId mapping.", route.Location);
                        }
                        if (route.StreamId is { } mapping) ValidateLiteral(mapping, stream.StreamId, application, values, context);
                    }
                }
                identifier = source.Identifier;
                if (identifier is null)
                {
                    var types = eventProducers.Select(producer => producer.Type).ToArray();
                    var distinct = types.OfType<TypeRefSyntax>().Select(type => (type.Name, type.IsOptional, type.IsCollection)).Distinct().ToArray();
                    if (types.Length == 0 || types.Any(type => type is null) || distinct.Length != 1)
                    {
                        if (occurrence.ValidateRoute) context.Error(DiagnosticCodes.InvalidSpecificationStreamEventSource, $"Declare an identifier on source '{source.Name}'; the event's producers do not supply one unambiguous destination type.", route.Location);
                    }
                    else
                    {
                        identifier = types[0];
                    }
                }
                if (occurrence.Required && node.For is null)
                {
                    occurrence.ContextualError(context, DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed given or when append event requires 'for <literal>'.", route.Location);
                }
                else if (occurrence.ValidateFor && !occurrence.InheritedIdentityIsInvalid(catalog, values, eventProducers.Select(producer => producer.Type)) && node.For is { } identity && (identity is not LiteralExpressionSyntax { Value: not null } || (identifier is { } type &&
                    (type.IsCollection || type.IsOptional || !values.Compatible(identity, type)))))
                {
                    occurrence.ContextualError(context, DiagnosticCodes.InvalidSpecificationStreamEventSource, "A routed event's for value must be a concrete literal compatible with the source's identifier type.", identity.Location);
                }
            }
            if (!occurrence.Expected || command is null || eventProducers.Length == 0 || eventProducers.Any(producer => !ReferenceEquals(producer.Command, command))) continue;
            var contradicts = eventProducers.All(producer => Contradicts(node, producer.Route, catalog, application, values));
            if (node.For is LiteralExpressionSyntax identityValue && eventProducers.All(producer => producer.Type is { } type &&
                (identifier is not null ? declarations.Compatible(type, identifier) == false : !values.Compatible(identityValue, type))))
            {
                contradicts = true;
            }
            if (contradicts)
            {
                occurrence.ContextualError(context, DiagnosticCodes.SpecificationStreamContradictsCommand, "The expected event route contradicts its only producer, the command under test.", node.Stream?.Location ?? node.NoStream!.Location);
            }
        }
    }

    static void ValidateLiteral(PropertyMappingSyntax mapping, TypeRefSyntax? target, ApplicationSyntax application, ResponseValueTypes values, ParserContext context)
    {
        EventSourceValidator.FormatLiteral(mapping.Source, target, application, out var failure);
        if (failure != StreamIdFormatFailure.None)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, SemanticStreamIdFormatter.FailureMessage(failure), mapping.Source.Location);
        }
        else if (mapping.Source is not LiteralExpressionSyntax { Value: not null } || (target is not null && !values.Compatible(mapping.Source, target)))
        {
            context.Error(DiagnosticCodes.InvalidSpecificationStreamRoute, "A specification stream id needs a nonempty concrete scalar literal compatible with the stream's declared type.", mapping.Source.Location);
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
                    yield return new(@event, command, CommandDestinationTypes.DestinationType(command, produced, productions, declarations), produced.Stream ?? command.Stream);
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
        if (resolution.Kind != EventSourceResolutionKind.Unique) return false;
        var stream = resolution.Streams[0];
        if (stream.StreamIdParts.Any())
        {
            if (route.StreamId is not null || expected.StreamId is not null || !route.StreamIdParts.Any() || !expected.StreamIdParts.Any()) return false;
            foreach (var part in stream.StreamIdParts)
            {
                var actualParts = route.StreamIdParts.Where(mapping => mapping.Property == part.Name).ToArray();
                var expectedParts = expected.StreamIdParts.Where(mapping => mapping.Property == part.Name).ToArray();
                if (actualParts.Length != 1 || expectedParts.Length != 1) continue;
                var actualPart = FormatStreamId(actualParts[0].Source, part.Type, application, values);
                var expectedPart = FormatStreamId(expectedParts[0].Source, part.Type, application, values);
                if (actualPart is not null && expectedPart is not null && actualPart != expectedPart) return true;
            }
            return false;
        }
        if (route.StreamIdParts.Any() || expected.StreamIdParts.Any() || stream.StreamId is not { } type) return false;
        var actualId = FormatStreamId(route.StreamId?.Source, type, application, values);
        var expectedId = FormatStreamId(expected.StreamId?.Source, type, application, values);

        return actualId is not null && expectedId is not null && actualId != expectedId;
    }

    // Only known portable scalar types prove equality. Paths and unavailable imported types defer
    // to execution; a UUID's authored case, hyphens and wrappers are not part of its stream identity.
    static string? FormatStreamId(ExpressionSyntax? expression, TypeRefSyntax type, ApplicationSyntax application, ResponseValueTypes values)
    {
        if (expression is not LiteralExpressionSyntax literal || !values.Compatible(literal, type)) return null;
        if (EventSourceValidator.Primitive(type, application) is not ("String" or "Uuid" or "Int")) return null;

        return EventSourceValidator.FormatLiteral(literal, type, application, out _);
    }

    sealed record Producer(EventSyntax Event, CommandSyntax? Command, TypeRefSyntax? Type, CommandStreamSyntax? Route = null);
}
