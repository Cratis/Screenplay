// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static ImmutableArray<SemanticProducedEvent> CommandResults(SemanticCommandHandler? handler, ImmutableArray<SemanticProducedEvent> produced) =>
            handler is { ResultVersion: 2 } ? handler.PossibleEvents : produced;

        static SemanticProperty? PossibleEventProperty(PossibleEventSyntax entry, Dictionary<string, SemanticProperty> properties)
        {
            if (entry.Property is not null) return properties.GetValueOrDefault(entry.Property);
            var identifiers = properties.Values.Where(value => value.IsIdentifier).ToArray();
            return identifiers.Length == 1 ? identifiers[0] : null;
        }

        static bool IsPossibleEventDestination(SemanticProperty? property, bool oneOf)
        {
            if (property?.Type.IsOptional is not false) return false;
            if (oneOf) return property.Type.IsCollection && !property.IsGenerated;
            return !property.Type.IsCollection && (!property.IsGenerated || property.IsIdentifier);
        }

        SemanticCommandHandler? BindStagedPossibleEventsHandler(SemanticAddress address, CommandSyntax command, DeclarationScope scope, HandlerSyntax handler)
        {
            if (AllowPossibleEvents) return BindPossibleEventsHandler(address, command, handler);
            var subject = DiagnosticArgument.Declaration("command", [.. scope.Segments, command.Name]);
            Error(DiagnosticCodes.CommandHandlerRequiresAttachment, DescribedCommandAdmission.AttachmentRefusalMessage(command, null), handler.Location, DiagnosticArguments.Empty.With("subject", subject));
            return null;
        }

        SemanticCommandHandler? BindPossibleEventsContract(SemanticCommandHandler? handler, CommandSyntax command, Dictionary<string, SemanticProperty> properties, Dictionary<string, BoundEvent> events) =>
            handler is { ResultVersion: 2 } ? handler with { PossibleEvents = BindPossibleEvents(command, properties, events) } : handler;

        SemanticCommandHandler? BindPossibleEventsHandler(SemanticAddress address, CommandSyntax command, HandlerSyntax handler)
        {
            if (command.Produces.Any())
            {
                Error(DiagnosticCodes.InvalidPossibleEventSet, "A command cannot combine 'produces' with a handler 'may produce' set.", handler.PossibleEvents!.Location);
                return null;
            }

            var requirement = RequirePossibleEventsImplementation(address, handler);
            if (requirement is null) return null;
            if (requirement.AttachmentResolution == SemanticAttachmentResolution.Pending)
            {
                Information(DiagnosticCodes.DescribedCommandNotImplemented, $"Command '{command.Name}' declares possible events but its implementation requirement '{requirement.RequirementId}' is pending.", handler.Location);
            }
            else if (!DescribedCommandAdmission.IsResolvedBody(requirement))
            {
                RefusePossibleEventsBody(command, requirement, handler.Location, address);
                return null;
            }

            return new(requirement.RequirementId, 2);
        }

        SemanticImplementationRequirement? RequirePossibleEventsImplementation(SemanticAddress address, HandlerSyntax handler)
        {
            var file = handler.File;
            var code = handler.Code;
            if (file is not null || code is not null) return RequireImplementation(SemanticImplementationRole.CommandHandler, address, file, code, resultVersion: 2);
            if (handler.Implementation is not null) return RequirePendingImplementation(address, handler, resultVersion: 2);
            Error(DiagnosticCodes.InvalidPossibleEventSet, "A handler 'may produce' set requires an implementation declaration or a handler body.", handler.Location);
            return null;
        }

        void RefusePossibleEventsBody(CommandSyntax command, SemanticImplementationRequirement requirement, SourceLocation location, SemanticAddress owner) => Error(
            DiagnosticCodes.CommandHandlerRequiresAttachment,
            $"Command '{command.Name}' has an attached handler whose implementation requirement '{requirement.RequirementId}' must resolve to a non-empty, language-tagged body.",
            location,
            DiagnosticArguments.Empty.With("subject", DiagnosticArgument.Declaration("command", owner.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice or SemanticAddressPartKind.Declaration).Select(part => part.Key))));

        ImmutableArray<SemanticProducedEvent> BindPossibleEvents(CommandSyntax command, Dictionary<string, SemanticProperty> properties, Dictionary<string, BoundEvent> events)
        {
            var declaration = command.Handler!.PossibleEvents!;
            if (!declaration.Events.Any())
            {
                Error(DiagnosticCodes.InvalidPossibleEventSet, "A handler 'may produce' set must declare at least one event type.", declaration.Location);
            }

            var bound = ImmutableArray.CreateBuilder<SemanticProducedEvent>();
            var identities = new HashSet<SemanticId>();
            foreach (var entry in declaration.Events)
            {
                if (!events.TryGetValue(entry.Event, out var @event))
                {
                    Error(DiagnosticCodes.InvalidPossibleEventSet, $"Possible event '{entry.Event}' is not declared in command '{command.Name}' scope.", entry.Location);
                    continue;
                }
                if (!identities.Add(@event.Contract.Id))
                {
                    Error(DiagnosticCodes.InvalidPossibleEventSet, $"Possible event '{entry.Event}' is declared more than once; one destination rule is allowed per event type.", entry.Location);
                    continue;
                }

                var destination = BindPossibleEventDestination(command, entry, properties);
                var produced = new SemanticProducedEvent(@event.Contract.Id, null, destination, []) { Tags = BindTags(entry.Tags) };
                if (entry.Stream is { } route) produced = produced with { Route = BindRoute(command, route, properties, [produced]) };
                bound.Add(produced);
            }
            return bound.ToImmutable();
        }

        SemanticExpression? BindPossibleEventDestination(CommandSyntax command, PossibleEventSyntax entry, Dictionary<string, SemanticProperty> properties)
        {
            var property = PossibleEventProperty(entry, properties);
            if (!IsPossibleEventDestination(property, entry.OneOf))
            {
                var expected = entry.OneOf ? "a required, non-generated collection command property" : "a required scalar command property (only the generated identifier may be generated)";
                Error(DiagnosticCodes.InvalidPossibleEventSet, $"Possible event '{entry.Event}' on command '{command.Name}' requires {expected}; omitted destinations require exactly one command identifier.", entry.Location);
                return null;
            }

            return entry.OneOf ? new SemanticOneOfExpression(property!.Id) : SemanticExpression.Property(SemanticExpressionRootKind.Command, property!.Id);
        }
    }
}
