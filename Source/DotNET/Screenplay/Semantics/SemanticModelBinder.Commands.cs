// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticCommand BindCommand(
            SemanticAddress slice,
            CommandSyntax command,
            Dictionary<string, BoundEvent> events)
        {
            if (command.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Command '{command.Name}' description is authoring metadata.", command.Location);
            }

            foreach (var reads in command.Reads ?? [])
            {
                Error(
                    DiagnosticCodes.PreservedLegacySemanticSyntax,
                    $"Command '{command.Name}' reads '{reads.ReadModel}' with legacy semantics that cannot imply decision consistency.",
                    reads.Location);
            }

            if (command.Concurrency is not null)
            {
                Error(
                    DiagnosticCodes.PreservedLegacySemanticSyntax,
                    $"Command '{command.Name}' concurrency metadata keeps its legacy meaning and cannot bind to ESM v1.",
                    command.Concurrency.Location);
            }

            if (command.Handler is not null)
            {
                RequireImplementation(SemanticImplementationRole.CommandHandler, SemanticAddress.ForCommand(slice, command.Name), command.Handler.File, command.Handler.Code);
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' handler requires a constrained implementation attachment.", command.Handler.Location);
            }

            var address = SemanticAddress.ForCommand(slice, command.Name);
            var id = Resolve(address, command.Location);
            var properties = command.Properties.Select(property => BindProperty(address, property, property.IsIdentifier)).ToImmutableArray();
            var propertiesByName = properties.ToDictionary(_ => _.Name, StringComparer.Ordinal);
            var validations = BindValidations(address, command, propertiesByName);
            var requirements = command.Validations.OfType<DeclarativeValidateSyntax>()
                .SelectMany(_ => _.Requirements ?? [])
                .Select(requirement => (requirement, condition: BindCondition(requirement.Condition, propertiesByName), validMessage: ValidateStringKey(requirement.Message, requirement.Location)))
                .Where(_ => _.condition is not null)
                .Select(_ => new SemanticRequirement(_.condition!, _.requirement.Message) { Severity = Severity(_.requirement.Severity) })
                .ToImmutableArray();
            var produced = command.Produces
                .Select(value => BindProducedEvent(command, value, propertiesByName, events))
                .Where(_ => _ is not null)
                .Select(_ => _!)
                .ToImmutableArray();
            var typedDestination = command.Produces.Any(value => value.For is PathExpressionSyntax source &&
                events.TryGetValue(value.Event, out var @event) && !@event.Properties.ContainsKey(source.Path));
            if (typedDestination) UsesV2 = true;
            var defaultDestination = typedDestination || UsesV2
                ? produced.Select(value => value.Destination).OfType<SemanticResolvedExpression>()
                    .Select(value => properties.Single(property => property.Id == value.Target)).FirstOrDefault()
                : null;
            return new(id, command.Name, properties, validations, produced)
            {
                Requirements = requirements,
                Destination = defaultDestination is null ? null : new(defaultDestination.Type, SemanticExpression.Property(SemanticExpressionRootKind.Command, defaultDestination.Id))
            };
        }

        ImmutableArray<SemanticValidationRule> BindValidations(
            SemanticAddress address,
            CommandSyntax command,
            Dictionary<string, SemanticProperty> properties)
        {
            var validations = ImmutableArray.CreateBuilder<SemanticValidationRule>();
            foreach (var validation in command.Validations)
            {
                if (validation is not DeclarativeValidateSyntax declarative)
                {
                    if (validation is CodeValidateSyntax code)
                    {
                        RequireImplementation(SemanticImplementationRole.CommandValidation, address, null, code.Code);
                    }

                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' code validation requires a constrained implementation attachment (#139).", validation.Location);
                    continue;
                }

                foreach (var rule in declarative.Rules)
                {
                    if (rule.Property.Contains('.', StringComparison.Ordinal))
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Validation rule on '{rule.Property}' is not admitted: ESM v1 validates command properties, not nested paths - declare the rule on the nested value's concept instead.", rule.Location);
                        continue;
                    }

                    if (!properties.TryGetValue(rule.Property, out var property))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Validation rule property '{rule.Property}' is unresolved on command '{command.Name}'.", rule.Location);
                        continue;
                    }

                    if (BindValidationRule(rule, property.Id, CommandValidationSubject(rule.Property, property.Type), address) is { } bound)
                    {
                        validations.Add(bound);
                    }
                }
            }

            return validations.ToImmutable();
        }

        SemanticProducedEvent? BindProducedEvent(
            CommandSyntax command,
            ProducesSyntax produced,
            Dictionary<string, SemanticProperty> commandProperties,
            Dictionary<string, BoundEvent> events)
        {
            if (!events.TryGetValue(produced.Event, out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Produced event '{produced.Event}' is not declared in slice '{command.Name}'.", produced.Location);
                return null;
            }

            var when = produced.When is null ? null : BindCondition(produced.When, commandProperties);
            var tags = BindTags(produced.Tags);

            var destination = produced.For is null
                ? null
                : BindPropertyExpression(produced.For, commandProperties, "produced event destination");
            var mappings = ImmutableArray.CreateBuilder<SemanticPropertyMapping>();
            foreach (var mapping in produced.Mappings)
            {
                if (!@event.Properties.TryGetValue(mapping.Property, out var target))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Produced event mapping target '{mapping.Property}' is unresolved on '{@event.Syntax.Name}'.", mapping.Location);
                    continue;
                }

                var source = mapping.Source is ContextExpressionSyntax context
                    ? BindOccurrence(context, target.Type)
                    : BindExpression(mapping.Source, commandProperties, SemanticExpressionRootKind.Command, "produced event mapping");
                if (source is not null)
                {
                    mappings.Add(new(target.Id, source));
                }
            }

            return new(@event.Contract.Id, null, destination, mappings.ToImmutable()) { When = when, Tags = tags };
        }

        SemanticEventContextExpression? BindOccurrence(ContextExpressionSyntax context, SemanticTypeReference target)
        {
            // Decision: 0001. Chronicle EventContext carries Occurred and CausedBy, not command tenant or claims.
            var kind = context.Path switch
            {
                "occurred" => SemanticEventContextValueKind.Occurred,
                "identity.id" or "causedBy.subject" => SemanticEventContextValueKind.CausedBySubject,
                "identity.name" or "causedBy.name" => SemanticEventContextValueKind.CausedByName,
                "identity.userName" or "causedBy.userName" => SemanticEventContextValueKind.CausedByUserName,
                _ => SemanticEventContextValueKind.Unknown
            };
            if (kind == SemanticEventContextValueKind.Unknown)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"$context.{context.Path} has no scalar counterpart on Chronicle EventContext (tenant is not namespace; causation, roles and claims are collections or unbounded).", context.Location);
                return null;
            }

            UsesV2 = true;
            return new SemanticEventContextExpression(kind, target);
        }
    }
}
