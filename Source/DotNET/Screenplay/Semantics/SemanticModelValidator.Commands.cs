// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        readonly HashSet<string> _handlerRequirements = new(StringComparer.Ordinal);

        void ValidateCommandDestination(SemanticCommand command)
        {
            if (command.Destination is null)
            {
                return;
            }

            if (_semanticVersion == SemanticVersion.V1)
            {
                throw new InvalidSemanticContract("A command state-change destination requires ESM v2.");
            }

            var identity = command.Destination.Value is SemanticResolvedExpression resolved
                ? command.Properties.SingleOrDefault(property => property.Id == resolved.Target && resolved.Root == SemanticExpressionRootKind.Command)
                : null;
            if (identity is not { Type.IsCollection: false, Type.IsOptional: false } ||
                (!identity.IsIdentifier && !_semanticVersion.IsAtLeast(SemanticVersion.V30)) ||
                identity.Type != command.Destination.Type)
            {
                throw new InvalidSemanticContract("A command destination must reference a required scalar command identifier with the same type.");
            }
        }

        void ValidateCommand(SemanticCommand command)
        {
            ValidateCommandHandler(command);
            ValidateCommandDestination(command);
            ValidateProperties(command.Properties, true);
            ValidateDecisionReads(command);
            foreach (var identifier in command.Properties.Where(property => property.IsIdentifier)) ValidateComplianceIdentity(identifier.Type);
            ValidateResponse(command);
            ValidateCommandRoute(command);
            var properties = Properties([.. command.Properties.Where(property => !property.IsGenerated)]);
            WithDecisionReadPaths(() => ValidateCommandRequirements(command, properties));
            RequireCodeValidations(command);
            ValidateCommandValidations(command, properties);
            RejectDuplicateRequirementIds(
                command.CodeValidations.Select(block => block.RequirementId).Concat(command.Validations.Select(validation => validation.RequirementId)),
                $"Command '{command.Name}'");

            foreach (var produced in command.Produces)
            {
                ValidateProducedEvent(produced, command);
            }
            _validatedDecisionReads.Clear();
        }

        void ValidateCommandHandler(SemanticCommand command)
        {
            if (command.Handler is not { } handler) return;
            if (handler.ResultVersion is not (1 or 2)) throw new InvalidSemanticContract("Unknown described command handler result version.");
            if (handler.RequirementId != ImplementationRequirementIdentity.Create(command.Id, SemanticImplementationRole.CommandHandler, null))
            {
                throw new InvalidSemanticContract("A described command handler requirement identity must match its owner.");
            }

            if (!_handlerRequirements.Add(handler.RequirementId)) throw new InvalidSemanticContract("A described command handler requirement identity cannot be duplicated.");
            ValidateHandlerResult(command);
            if (InvalidV1Productions(command))
            {
                throw new InvalidSemanticContract("A described command requires unconditional productions in result version 1.");
            }
        }

        bool InvalidV1Productions(SemanticCommand command) =>
            command.Handler!.ResultVersion == 1 && (command.Produces.IsEmpty || command.Produces.Any(production => production.When is not null || production.Condition is not null));

        void RequireCodeValidations(SemanticCommand command)
        {
            if (command.CodeValidations.IsDefault || command.CodeValidations.Any(IsUnidentified) || RequiresNewerVersion(command))
            {
                throw new InvalidSemanticContract("Command code validation requires ESM v3 and a requirement identity.");
            }
        }

        bool RequiresNewerVersion(SemanticCommand command) =>
            command.CodeValidations.Length > 0 && !_semanticVersion.IsAtLeast(SemanticVersion.V3);

        bool IsUnidentified(SemanticCodeValidation? block) => block is null || string.IsNullOrWhiteSpace(block.RequirementId);

        void ValidateCommandValidations(SemanticCommand command, Dictionary<SemanticId, SemanticProperty> properties)
        {
            foreach (var validation in command.Validations)
            {
                if (!properties.TryGetValue(validation.Property, out var property))
                {
                    throw new InvalidSemanticContract($"Validation property '{validation.Property}' is unresolved.");
                }

                ValidateValidation(validation, false, property.Type);
            }
        }

        void ValidateCommandRequirements(SemanticCommand command, Dictionary<SemanticId, SemanticProperty> properties)
        {
            if (command.Requirements.IsDefault) throw new InvalidSemanticContract("Command requirements cannot be default.");
            foreach (var requirement in command.Requirements)
            {
                if (requirement is null || requirement.Message?.Length == 0) throw new InvalidSemanticContract("A requirement or its message is invalid.");
                ValidateEnum(requirement.Severity, (SemanticValidationSeverity)(-1), "requirement severity");
                ValidateCondition(requirement.Condition, properties, allowCommandComparisons: true);
            }
        }

        void ValidateHandlerResult(SemanticCommand command)
        {
            var handler = command.Handler!;
            if (handler.PossibleEvents.IsDefault) throw new InvalidSemanticContract("Handler possible events cannot be default.");
            if (handler.ResultVersion == 1)
            {
                ValidateV1PossibleEvents(handler);
                return;
            }

            if (!HasV2Shape(command)) throw new InvalidSemanticContract("Result contract v2 requires ESM v35, possible events and no legacy productions.");
            var identities = new HashSet<SemanticId>();
            foreach (var entry in handler.PossibleEvents) ValidatePossibleEvent(entry, command, identities);
        }

        void ValidateV1PossibleEvents(SemanticCommandHandler handler)
        {
            if (!handler.PossibleEvents.IsEmpty) throw new InvalidSemanticContract("Result contract v1 cannot declare possible events.");
        }

        bool HasV2Shape(SemanticCommand command) =>
            _semanticVersion.IsAtLeast(SemanticVersion.V35) && !command.Handler!.PossibleEvents.IsEmpty && command.Produces.IsEmpty;

        void ValidatePossibleEvent(SemanticProducedEvent entry, SemanticCommand command, HashSet<SemanticId> identities)
        {
            if (!HasDistinctEventIdentity(entry, identities)) throw new InvalidSemanticContract("Possible events require distinct known event contracts.");
            if (HasPossibleEventPayload(entry)) throw new InvalidSemanticContract("Possible events cannot declare mappings, conditions or reaction destination types.");
            ValidateProducedEvent(entry, command, true);
        }

        bool HasDistinctEventIdentity(SemanticProducedEvent? entry, HashSet<SemanticId> identities) => entry is not null && identities.Add(entry.EventContract);

        bool HasPossibleEventPayload(SemanticProducedEvent entry) =>
            entry.Mappings.IsDefault || !entry.Mappings.IsEmpty || entry.Condition is not null || entry.When is not null || entry.DestinationType is not null;

        void ValidatePossibleEventDestination(SemanticProducedEvent entry, SemanticCommand command)
        {
            if (entry.Destination is null)
            {
                ValidateOmittedPossibleDestination(entry, command);
                return;
            }

            var oneOf = entry.Destination is SemanticOneOfExpression;
            var target = PossibleDestinationProperty(entry.Destination);
            var property = command.Properties.SingleOrDefault(value => value.Id == target);
            if (!IsPossibleDestinationProperty(property, oneOf))
                throw new InvalidSemanticContract("A possible-event destination requires a compatible command input property.");
            var type = oneOf ? property!.Type with { IsCollection = false } : property!.Type;
            ValidatePossibleDestinationRoute(entry.Route ?? command.Route, command, type);
        }

        void ValidateOmittedPossibleDestination(SemanticProducedEvent entry, SemanticCommand command)
        {
            if (HasDefaultPossibleDestination(entry, command)) throw new InvalidSemanticContract("An omitted possible-event destination requires an unrouted event without a default identifier.");
        }

        bool HasDefaultPossibleDestination(SemanticProducedEvent entry, SemanticCommand command) =>
            entry.Route is not null || command.Destination is not null || command.Properties.Any(property => property.IsIdentifier);

        SemanticId PossibleDestinationProperty(SemanticExpression expression) => expression switch
        {
            SemanticOneOfExpression { Kind: SemanticExpressionKind.OneOf } choice => choice.CollectionProperty,
            SemanticResolvedExpression { Kind: SemanticExpressionKind.Resolved, Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } scalar => scalar.Target,
            _ => throw new InvalidSemanticContract("A possible-event destination must reference a command property.")
        };

        bool IsPossibleDestinationProperty(SemanticProperty? property, bool oneOf)
        {
            if (property is null) return false;
            return !property.Type.IsOptional && property.Type.IsCollection == oneOf && (!property.IsGenerated || (!oneOf && property.IsIdentifier));
        }

        void ValidatePossibleDestinationRoute(SemanticCommandRoute? route, SemanticCommand command, SemanticTypeReference type)
        {
            if (route is not null)
            {
                ValidateBoundRoute(command, route);
                var (source, _) = SemanticEventRouting.Resolve(_eventRoutesApplication, route.Source, route.Stream);
                if (HasIncompatibleRouteType(source, type))
                    throw new InvalidSemanticContract("A routed possible-event destination must match the route type.");
            }
            else if (HasIncompatibleCommandDestination(command, type))
            {
                throw new InvalidSemanticContract("A possible-event destination must match the command destination type.");
            }
        }

        bool HasIncompatibleRouteType(SemanticEventSource source, SemanticTypeReference type) => source.IdentifierType is { } identifier && type != identifier;

        bool HasIncompatibleCommandDestination(SemanticCommand command, SemanticTypeReference type) => command.Destination is not null && type != command.Destination.Type;

        void ValidateProducedEvent(SemanticProducedEvent produced, SemanticCommand command, bool possibleEvent = false)
        {
            if (produced.DestinationType is not null)
            {
                throw new InvalidSemanticContract("A command production cannot declare a reaction destination type.");
            }

            if (!_events.TryGetValue(produced.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Produced event contract '{produced.EventContract}' is unresolved.");
            }

            var sources = Properties(command.Properties);
            ValidateTags(produced.Tags);
            if (produced.When is not null) ValidateCondition(produced.When, Properties(command.Properties), allowCommandComparisons: true);
            if (produced.Condition is not null)
            {
                var conditionType = ResolveExpression(produced.Condition, SemanticExpressionRootKind.Command, sources);
                RequireBoolean(conditionType, "produced event condition");
            }

            ValidateEventDestination(produced, command, sources, possibleEvent);

            WithDecisionReadPaths(() => ValidateMappings(produced.Mappings, Properties(eventContract.Properties), SemanticExpressionRootKind.Command, sources));
            var mapped = produced.Mappings.Select(_ => _.TargetProperty).ToHashSet();
            if (command.Handler is null && eventContract.Properties.Any(_ => !_.Type.IsOptional && !mapped.Contains(_.Id)))
            {
                throw new InvalidSemanticContract("A produced event must map every required event property.");
            }
        }

        void ValidateEventDestination(SemanticProducedEvent produced, SemanticCommand command, Dictionary<SemanticId, SemanticProperty> sources, bool possibleEvent)
        {
            if (possibleEvent) ValidatePossibleEventDestination(produced, command);
            else if (produced.Destination is not null) ValidateProducedDestination(produced, command, sources);
        }

        void ValidateProducedDestination(SemanticProducedEvent produced, SemanticCommand command, Dictionary<SemanticId, SemanticProperty> sources)
        {
            var destinationType = ResolveExpression(produced.Destination!, SemanticExpressionRootKind.Command, sources);
            ValidateComplianceIdentity(destinationType);
            var destinationProperty = DestinationProperty(produced, sources);
            if (!IsScalarDestinationType(destinationType) || !IsIdentityDestination(produced, command, destinationProperty))
            {
                throw new InvalidSemanticContract("A produced event destination must resolve to one required scalar command identity.");
            }
        }

        bool IsIdentityDestination(SemanticProducedEvent produced, SemanticCommand command, SemanticProperty? destinationProperty) =>
            destinationProperty is not null && (destinationProperty.IsIdentifier || produced.Route is not null || HasGeneratedIdentifier(command) || _semanticVersion.IsAtLeast(SemanticVersion.V30));

        bool HasGeneratedIdentifier(SemanticCommand command) =>
            _semanticVersion.IsAtLeast(SemanticVersion.V7) && command.Properties.Any(value => value.IsGenerated && value.IsIdentifier);

        SemanticProperty? DestinationProperty(SemanticProducedEvent produced, Dictionary<SemanticId, SemanticProperty> sources) =>
            produced.Destination is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } resolved &&
            sources.TryGetValue(resolved.Target, out var property)
                ? property
                : null;

        bool IsScalarDestinationType(SemanticTypeReference? type) => type?.IsCollection is false && !type.IsOptional;
    }
}
