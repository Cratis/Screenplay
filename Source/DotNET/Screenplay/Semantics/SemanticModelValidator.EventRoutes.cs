// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateEventRoutesVersion(SemanticApplication application, SemanticVersion version)
    {
        var uses = SemanticEventRouting.Uses(application);
        if (UsesProductionRoutesOrObserverFilters(application) && !version.IsAtLeast(SemanticVersion.V10)) throw new InvalidSemanticContract("Production routes and observer filters require ESM v10.");
        if (version == SemanticVersion.V8 && !uses)
        {
            throw new InvalidSemanticContract("An event routes model must declare an event source, command route or specification route.");
        }
        if (!version.IsAtLeast(SemanticVersion.V8) && uses)
        {
            throw new InvalidSemanticContract("Event sources and routes require ESM v8.");
        }
    }

    static bool UsesProductionRoutesOrObserverFilters(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).Any(slice =>
            slice.Commands.Any(command => command.Produces.Any(produced => produced.Route is not null)) ||
            slice.Reactions.Any(reaction => reaction.From is not null) || slice.Reducers.Any(reducer => reducer.From is not null));

    static void ValidateV10Use(SemanticApplication application, SemanticVersion version, bool usesExactNumberLiterals)
    {
        if (version != SemanticVersion.V10 || usesExactNumberLiterals || UsesProductionRoutesOrObserverFilters(application)) return;
        var usesReactionIdentity = application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .Any(slice => slice.Reactions.Any(reaction => reaction.RunsAs is not null));
        if (!usesReactionIdentity) throw new InvalidSemanticContract("An ESM v10 model must use a reaction system identity, production route, observer filter or exact Double-mode number literal lowering.");
    }

    private sealed partial class ValidationContext
    {
        readonly SemanticApplication _eventRoutesApplication;

        static HashSet<SemanticId> RoutedInputs(SemanticCommand command) =>
            [.. command.Produces.Select(produced => produced.Route).Append(command.Route).OfType<SemanticCommandRoute>()
                .SelectMany(route => route.StreamIdParts.Select(part => part.Value).Concat(route.StreamId is { } scalar ? [scalar] : []))
                .OfType<SemanticResolvedExpression>().Select(expression => expression.Target)];

        void RegisterEventSources(SemanticApplication application)
        {
            RequireObjects(application.EventSources, nameof(application.EventSources), "event source");
            RejectDuplicateNames(application.EventSources.Select(source => source.Name), "event source");
            RejectDuplicateNames(application.EventSources.Select(source => source.SourceKind), "stored event source");
            foreach (var source in application.EventSources)
            {
                Register(source.Id, source.Name, "event source");
                if (string.IsNullOrWhiteSpace(source.SourceKind) || source.SourceKind == "Default")
                {
                    throw new InvalidSemanticContract("A stored event source name must be non-empty and cannot be Default.");
                }
                RequireObjects(source.Streams, nameof(source.Streams), "event stream");
                RejectDuplicateNames(source.Streams.Select(stream => stream.Name), "event stream");
                RejectDuplicateNames(source.Streams.Select(stream => stream.StreamKind), "stored event stream");
                foreach (var stream in source.Streams)
                {
                    Register(stream.Id, stream.Name, "event stream");
                    if (string.IsNullOrWhiteSpace(stream.StreamKind)) throw new InvalidSemanticContract("A stored stream name cannot be empty.");
                    RequireObjects(stream.StreamIdParts, nameof(stream.StreamIdParts), "stream identity part");
                    if ((stream.StreamIdType is not null && !stream.StreamIdParts.IsEmpty) || stream.StreamIdParts.Length == 1)
                    {
                        throw new InvalidSemanticContract("A stream identity must be unkeyed, scalar or composed of at least two parts, never mixed.");
                    }
                    RejectDuplicateNames(stream.StreamIdParts.Select(part => part.Name), "stream identity part");
                    if (stream.StreamIdParts.Any(part => string.IsNullOrWhiteSpace(part.Name))) throw new InvalidSemanticContract("A stream identity part name cannot be empty.");
                }
            }
        }

        void ValidateEventSources()
        {
            foreach (var source in _eventRoutesApplication.EventSources)
            {
                if (source.IdentifierType is { } identifier)
                {
                    ValidateTypeReference(identifier);
                    if (identifier.IsOptional || identifier.IsCollection || identifier.Kind == SemanticTypeReferenceKind.CompositeType)
                    {
                        throw new InvalidSemanticContract("An event source identifier must be a required scalar type.");
                    }
                }
                foreach (var stream in source.Streams)
                {
                    if (stream.StreamIdType is { } type) ValidateStreamIdType(type);
                    foreach (var part in stream.StreamIdParts) ValidateStreamIdType(part.Type);
                }
            }
        }

        void ValidateStreamIdType(SemanticTypeReference type)
        {
            ValidateTypeReference(type);
            _ = SemanticEventRouting.ScalarKind(type, _eventRoutesApplication.Concepts);
        }

        void ValidateCommandRoute(SemanticCommand command)
        {
            foreach (var produced in command.Produces.Where(produced => produced.Route is not null))
            {
                var productionRoute = produced.Route!;
                ValidateBoundRoute(command, productionRoute);
                var (productionSource, _) = SemanticEventRouting.Resolve(_eventRoutesApplication, productionRoute.Source, productionRoute.Stream);
                if (productionSource.IdentifierType is { } type && ProducedEventSourceType(command, produced) != type)
                {
                    throw new InvalidSemanticContract("A production destination must have its override source's identifier type.");
                }
            }
            if (command.Route is not { } route) return;
            ValidateBoundRoute(command, route);
            var (source, stream) = SemanticEventRouting.Resolve(_eventRoutesApplication, route.Source, route.Stream);
            RequireObjects(route.StreamIdParts, nameof(route.StreamIdParts), "command stream identity part");
            ValidateRouteShape(stream, route.StreamId is not null, [.. route.StreamIdParts.Select(part => part.Part)]);
            if (source.IdentifierType is { } identifier &&
                (command.Properties.Any(property => property.IsIdentifier && property.Type != identifier) ||
                (command.Destination is { } destination && destination.Type != identifier) ||
                command.Produces.Any(produced => produced.Route is null && ProducedEventSourceType(command, produced) is { } type && type != identifier)))
            {
                throw new InvalidSemanticContract("A routed command identifier and production destinations must have the source's identifier type.");
            }
        }

        void ValidateBoundRoute(SemanticCommand command, SemanticCommandRoute route)
        {
            var (_, stream) = SemanticEventRouting.Resolve(_eventRoutesApplication, route.Source, route.Stream);
            RequireObjects(route.StreamIdParts, nameof(route.StreamIdParts), "production stream identity part");
            ValidateRouteShape(stream, route.StreamId is not null, [.. route.StreamIdParts.Select(part => part.Part)]);
            if (route.StreamId is { } expression) ValidateRouteMapping(command, expression, stream.StreamIdType!);
            for (var index = 0; index < route.StreamIdParts.Length; index++)
            {
                ValidateRouteMapping(command, route.StreamIdParts[index].Value, stream.StreamIdParts[index].Type);
            }
        }

        void ValidateObserverFilter(SemanticObserverFilter? filter)
        {
            if (filter is null) return;
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V10)) throw new InvalidSemanticContract("Observer filters require ESM v10.");
            var source = _eventRoutesApplication.EventSources.SingleOrDefault(source => source.Id == filter.Source) ??
                throw new InvalidSemanticContract("An observer filter requires a declared event source.");
            if (filter.Stream is { } stream && !source.Streams.Any(value => value.Id == stream))
            {
                throw new InvalidSemanticContract("An observer filter stream must belong to its event source.");
            }
        }

        void ValidateRouteMapping(SemanticCommand command, SemanticExpression expression, SemanticTypeReference type)
        {
            if (expression is SemanticValueExpression { Kind: SemanticExpressionKind.Value } literal)
            {
                ValidateRouteLiteral(type, literal.Value);
                return;
            }
            if (expression is not SemanticResolvedExpression { Kind: SemanticExpressionKind.Resolved, Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } reference ||
                command.Properties.SingleOrDefault(property => property.Id == reference.Target) is not { IsGenerated: false, Type.IsOptional: false, Type.IsCollection: false } property || property.Type != type)
            {
                throw new InvalidSemanticContract("A stream identity mapping must read a direct required, non-generated scalar property of its command with the declared type, or a literal.");
            }
        }

        void ValidateRouteLiteral(SemanticTypeReference type, SemanticValue value)
        {
            _valueValidator.ValidateVariant(value);
            var stream = new SemanticEventStream(default, "literal", "literal") { StreamIdType = type };
            var source = new SemanticEventSource(default, "literal", "literal", []);
            if (!SemanticEventRouting.TryFormat(source, stream, value, [], _eventRoutesApplication.Concepts, out _, out _))
            {
                throw new InvalidSemanticContract("A stream identity literal must be a valid portable scalar value of its declared type.");
            }
        }

        void ValidateFixtureRoute(SemanticFixtureRoute? route, bool unrouted, bool then, SemanticEventSourceIdentity? eventSource, SemanticId eventContract)
        {
            if (unrouted && (!then || route is not null)) throw new InvalidSemanticContract("unrouted is true-only, then-only and exclusive with route.");
            if (route is null) return;
            if (!then && eventSource is null) throw new InvalidSemanticContract("A routed history or append fixture requires an event source identity.");
            var (source, stream) = SemanticEventRouting.Resolve(_eventRoutesApplication, route.Source, route.Stream);
            RequireObjects(route.StreamIdParts, nameof(route.StreamIdParts), "fixture stream identity part");
            ValidateRouteShape(stream, route.StreamId is not null, [.. route.StreamIdParts.Select(part => part.Part)]);
            if (route.StreamId is { } scalar) _valueValidator.ValidateVariant(scalar);
            foreach (var part in route.StreamIdParts) _valueValidator.ValidateVariant(part.Value);
            if (!SemanticEventRouting.TryFormat(source, stream, route.StreamId, route.StreamIdParts, _eventRoutesApplication.Concepts, out _, out _))
            {
                throw new InvalidSemanticContract("A fixture route must carry valid portable stream identity literals.");
            }

            // Even a then fixture without 'for' must resolve the source type rather than guess.
            _ = FixtureEventSourceType(route, eventContract);
        }

        SemanticTypeReference FixtureEventSourceType(SemanticFixtureRoute? route, SemanticId eventContract) => route is null
            ? DeclaredEventSourceType(_commands.Values, _reactions, _captures.Values, eventContract)
            : SemanticEventRouting.FixtureSourceType(_eventRoutesApplication, route, eventContract, out _) ??
                throw new InvalidSemanticContract("A routed fixture requires an identifier on its source or one unambiguous producer destination type.");

        void ValidateRouteShape(SemanticEventStream stream, bool scalar, ImmutableArray<string> parts)
        {
            if (stream.StreamIdType is not null)
            {
                if (!scalar || !parts.IsEmpty) throw new InvalidSemanticContract("A scalar stream requires exactly one scalar stream identity mapping.");
            }
            else if (!stream.StreamIdParts.IsEmpty)
            {
                if (scalar || !parts.SequenceEqual(stream.StreamIdParts.Select(part => part.Name), StringComparer.Ordinal))
                {
                    throw new InvalidSemanticContract("Composite stream identity mappings must cover all declared parts exactly once in declaration order.");
                }
            }
            else if (scalar || !parts.IsEmpty)
            {
                throw new InvalidSemanticContract("An unkeyed stream cannot carry stream identity mappings.");
            }
        }
    }
}
