// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticReactionLoop
{
    static bool SupportsBody(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticFact cause) =>
        reaction.Triggers.Length == 1 && trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == cause.EventContract && trigger.Produces.IsEmpty && trigger.Invokes.IsEmpty;

    static void ValidateBodyResult(SemanticReactionBodyResult result)
    {
        if (result.Diagnostic is not null) throw new InvalidSemanticContract(result.Diagnostic);
        if (result.Events.IsDefault && result.Commands.IsDefault) throw new InvalidSemanticContract("The provider returned no reaction result.");
        if (!result.Events.IsDefaultOrEmpty && !result.Commands.IsDefaultOrEmpty) throw new InvalidSemanticContract("The reaction body returned a mixed event and command batch.");
    }

    SemanticExecutionResult? FireBody(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticFact? cause, SemanticCommandOccurrence? occurrence)
    {
        if (!SemanticReactionBodyExecution.For(evaluator).HasProvider)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"Reaction '{reaction.Name}' has an opaque body and requires a target provider to run it.");
        }
        if (cause is null || !SupportsBody(reaction, trigger, cause))
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"Reaction '{reaction.Name}' combines its body with declarative effects or a non-event trigger; only a standalone event-trigger body runs (decision 0121).");
        }
        var context = new SemanticReactionContext(cause.EventContract, new(cause.Values), cause.Destination, null, null, null, occurrence?.Occurred, reaction.RunsAs);
        var result = SemanticReactionBodyExecution.For(evaluator).React(plan, reaction, context, SemanticReducerBodyExecution.TokenFor(evaluator));
        try
        {
            ValidateBodyResult(result);
            if (!result.Events.IsDefaultOrEmpty) return ProduceBodyEvents(reaction, result.Events, cause, occurrence);
            if (!result.Commands.IsDefaultOrEmpty) return InvokeBodyCommands(reaction, result.Commands, occurrence);
            return null;
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"Reaction '{reaction.Name}' body (requirement {trigger.RequirementId}): {SemanticRuleBodyExecution.Sanitize(exception.Message)}");
        }
    }

    SemanticExecutionResult? ProduceBodyEvents(SemanticReaction reaction, ImmutableArray<SemanticReactionEffect> effects, SemanticFact cause, SemanticCommandOccurrence? occurrence)
    {
        var facts = effects.Select(effect =>
        {
            if (effect is null || !plan.Events.TryGetValue(effect.Target, out var contract)) throw new InvalidSemanticContract("The reaction body returned an unknown event target.");
            var values = SemanticCommandPayloads.ValidateEvent(_validator, contract, effect.Payload, "reaction body");
            return new SemanticFact(effect.Target, cause.Destination, values)
            {
                Context = cause.Context,
                Tags = contract.Tags,
                Occurred = occurrence?.Occurred,
                ReactionOrigin = reaction.Id
            };
        }).ToImmutableArray();
        if (plan.Model.Application.ReactionProductionMode is not null)
        {
            var result = PrepareBatch(facts);
            return result is SemanticAccepted accepted ? AcceptCommand(accepted, occurrence?.Occurred) : result;
        }
        foreach (var fact in facts)
        {
            if (Append(fact, occurrence?.Occurred) is { } failure) return failure;
        }
        return null;
    }

    SemanticExecutionResult? InvokeBodyCommands(SemanticReaction reaction, ImmutableArray<SemanticReactionEffect> effects, SemanticCommandOccurrence? occurrence)
    {
        var commands = effects.Select(effect =>
        {
            if (effect is null || !plan.Commands.TryGetValue(effect.Target, out var command)) throw new InvalidSemanticContract("The reaction body returned an unknown command target.");
            return (Command: command, Values: SemanticCommandPayloads.ValidateCommand(_validator, command, effect.Payload, "reaction body"));
        }).ToImmutableArray();
        foreach (var (command, values) in commands)
        {
            var request = SemanticExecutionRequest.Create(command.Id, values, []) with { Occurrence = occurrence };
            var result = RunInvoked(reaction, command, request);
            if (result is not SemanticAccepted accepted) return result;
            if (plan.Runs(command.Id) && AcceptCommand(accepted, occurrence?.Occurred) is { } failure) return failure;
        }
        return null;
    }
}
