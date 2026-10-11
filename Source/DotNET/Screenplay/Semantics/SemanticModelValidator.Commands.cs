// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
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
            if (identity is not { IsIdentifier: true, Type.IsCollection: false, Type.IsOptional: false } ||
                identity.Type != command.Destination.Type)
            {
                throw new InvalidSemanticContract("A command destination must reference a required scalar command identifier with the same type.");
            }
        }

        void ValidateCommand(SemanticCommand command)
        {
            ValidateCommandDestination(command);
            ValidateProperties(command.Properties, true);
            ValidateResponse(command);
            ValidateCommandRoute(command);
            var properties = Properties([.. command.Properties.Where(property => !property.IsGenerated)]);
            ValidateCommandRequirements(command, properties);
            RequireCodeValidations(command);
            ValidateCommandValidations(command, properties);
            RejectDuplicateRequirementIds(
                command.CodeValidations.Select(block => block.RequirementId).Concat(command.Validations.Select(validation => validation.RequirementId)),
                $"Command '{command.Name}'");

            foreach (var produced in command.Produces)
            {
                ValidateProducedEvent(produced, command);
            }
        }

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
                ValidateCondition(requirement.Condition, properties);
            }
        }

        void ValidateProducedEvent(SemanticProducedEvent produced, SemanticCommand command)
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
            if (produced.When is not null) ValidateCondition(produced.When, Properties(command.Properties));
            if (produced.Condition is not null)
            {
                var conditionType = ResolveExpression(produced.Condition, SemanticExpressionRootKind.Command, sources);
                RequireBoolean(conditionType, "produced event condition");
            }

            if (produced.Destination is not null)
            {
                ValidateProducedDestination(produced, command, sources);
            }

            ValidateMappings(produced.Mappings, Properties(eventContract.Properties), SemanticExpressionRootKind.Command, sources);
            var mapped = produced.Mappings.Select(_ => _.TargetProperty).ToHashSet();
            if (eventContract.Properties.Any(_ => !_.Type.IsOptional && !mapped.Contains(_.Id)))
            {
                throw new InvalidSemanticContract("A produced event must map every required event property.");
            }
        }

        void ValidateProducedDestination(SemanticProducedEvent produced, SemanticCommand command, Dictionary<SemanticId, SemanticProperty> sources)
        {
            var destinationType = ResolveExpression(produced.Destination!, SemanticExpressionRootKind.Command, sources);
            var destinationProperty = DestinationProperty(produced, sources);
            if (!IsScalarDestinationType(destinationType) || !IsIdentityDestination(produced, command, destinationProperty))
            {
                throw new InvalidSemanticContract("A produced event destination must resolve to one required scalar command identity.");
            }
        }

        bool IsIdentityDestination(SemanticProducedEvent produced, SemanticCommand command, SemanticProperty? destinationProperty) =>
            destinationProperty is not null && (destinationProperty.IsIdentifier || produced.Route is not null || HasGeneratedIdentifier(command));

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
