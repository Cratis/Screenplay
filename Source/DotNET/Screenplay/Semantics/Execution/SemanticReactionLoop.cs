// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Runs reactions in the reference evaluator. Every accepted fact sets off the reactions to its event, and what those
/// append, or the commands they invoke, set off more, until nothing is left to react to.
/// </summary>
/// <remarks>
/// Facts are reacted to in the order they were appended. The reactions to one fact run in the order of their semantic
/// identities, and a reaction's triggers in authored order; each produces its events, then invokes its commands. An
/// invoked command runs through the full command pipeline with no caller: a reaction acts on its own authority, so a
/// command that requires a caller rejects it. A rejection or an unsupported capability anywhere ends the scenario.
/// </remarks>
/// <param name="evaluator">The evaluator invoked commands run through.</param>
/// <param name="plan">The capability-admitted plan.</param>
/// <param name="world">The world the loop starts from.</param>
internal sealed class SemanticReactionLoop(ISemanticEvaluator evaluator, SemanticExecutionPlan plan, SemanticWorld world)
{
    /// <summary>
    /// The most facts one scenario may append before its reactions are taken not to settle.
    /// </summary>
    internal const int MaximumFacts = 1_000;

    readonly Queue<(SemanticFact Fact, DateTimeOffset? Occurred)> _pending = new();
    readonly List<SemanticFact> _facts = [];
    readonly SemanticValueValidator _validator = new(
        plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
        plan.Model.Application.Types.ToDictionary(type => type.Id));

    /// <summary>
    /// Gets the world as the loop has left it.
    /// </summary>
    public SemanticWorld World { get; private set; } = world;

    /// <summary>
    /// Gets every fact appended through the loop, in order.
    /// </summary>
    public ImmutableArray<SemanticFact> Facts => [.. _facts];

    /// <summary>
    /// Takes facts an action already appended to the world, so the reactions to them run.
    /// </summary>
    /// <param name="facts">The appended facts.</param>
    /// <param name="occurred">When they occurred, or <c>null</c> when the scenario states no clock.</param>
    /// <returns>An unsupported result when the scenario appends too many facts; otherwise <c>null</c>.</returns>
    public SemanticExecutionResult? Observe(IEnumerable<SemanticFact> facts, DateTimeOffset? occurred)
    {
        foreach (var fact in facts)
        {
            if (_facts.Count >= MaximumFacts)
            {
                return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.");
            }

            _facts.Add(fact);
            _pending.Enqueue((fact, occurred));
        }

        return null;
    }

    /// <summary>
    /// Adopts a complete accepted command transaction only when it fits the remaining scenario fact budget.
    /// </summary>
    /// <param name="accepted">The tentative accepted command transaction.</param>
    /// <param name="occurred">When its facts occurred.</param>
    /// <returns>An unsupported result retaining the prior world when the batch exceeds the budget; otherwise <c>null</c>.</returns>
    public SemanticExecutionResult? AcceptCommand(SemanticAccepted accepted, DateTimeOffset? occurred)
    {
        if (_facts.Count + accepted.Facts.Length > MaximumFacts)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.");
        }

        World = accepted.World;
        return Observe(accepted.Facts, occurred);
    }

    /// <summary>
    /// Appends a fact - enforcing append constraints and projecting it - and takes it for the reactions to it.
    /// </summary>
    /// <param name="fact">The fact.</param>
    /// <param name="occurred">When it occurred, or <c>null</c> when the scenario states no clock.</param>
    /// <returns>The rejected or unsupported result, or <c>null</c> when the fact was appended.</returns>
    public SemanticExecutionResult? Append(SemanticFact fact, DateTimeOffset? occurred)
    {
        if (_facts.Count >= MaximumFacts)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.");
        }

        var result = SemanticEvaluator.Append(plan, World, fact, [], null);
        if (result is not SemanticAccepted accepted)
        {
            return result;
        }

        // What the append published (an outbound translation's public event) is observed with it.
        World = accepted.World;
        return Observe(accepted.Facts, occurred);
    }

    /// <summary>
    /// Runs the reactions to every fact taken, and to what they set off, until none remain.
    /// </summary>
    /// <returns>The rejected or unsupported result, or <c>null</c> when the reactions settled.</returns>
    public SemanticExecutionResult? Settle()
    {
        while (_pending.TryDequeue(out var item))
        {
            var values = item.Fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
            foreach (var reaction in plan.Reactions.Where(reaction => reaction.From?.Matches(plan.Model.Application, item.Fact.Route) != false))
            {
                foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == item.Fact.EventContract))
                {
                    if (Fire(reaction, trigger, values, SemanticExpressionRootKind.Event, item.Fact, item.Occurred) is { } failure)
                    {
                        return failure;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Runs one trigger of a reaction for an occurrence.
    /// </summary>
    /// <param name="reaction">The reaction.</param>
    /// <param name="trigger">The trigger that fired.</param>
    /// <param name="values">The values the occurrence carries.</param>
    /// <param name="root">Whether the values are an event's or an application trigger's.</param>
    /// <param name="cause">The fact that set the reaction off, when an event did.</param>
    /// <param name="occurred">When the occurrence happened, or <c>null</c> when the scenario states no clock.</param>
    /// <returns>The rejected or unsupported result, or <c>null</c> when the reaction ran or its 'where' excluded it.</returns>
    public SemanticExecutionResult? Fire(
        SemanticReaction reaction,
        SemanticReactionTrigger trigger,
        IReadOnlyDictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        DateTimeOffset? occurred)
    {
        if (trigger.Where is not null && !SemanticConditionEvaluation.Evaluate(trigger.Where, values))
        {
            return null;
        }

        if (trigger.RequirementId is not null)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"Reaction '{reaction.Name}' has an opaque body and requires a target provider to run it.");
        }

        var occurrence = occurred is { } at ? new SemanticCommandOccurrence(at, string.Empty, string.Empty, string.Empty) { IsTimeOnly = true } : null;
        var lookup = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        try
        {
            foreach (var produced in trigger.Produces)
            {
                if (Produce(reaction, produced, lookup, root, cause, occurrence) is { } failure)
                {
                    return failure;
                }
            }

            foreach (var invocation in trigger.Invokes)
            {
                if (Invoke(reaction, invocation, lookup, root, occurrence) is { } failure)
                {
                    return failure;
                }
            }
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticRejected(World, SemanticRejectionCategory.Contract, null, exception.Message);
        }

        return null;
    }

    SemanticExecutionResult? Produce(
        SemanticReaction reaction,
        SemanticProducedEvent produced,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        SemanticCommandOccurrence? occurrence)
    {
        if (RequiresAuditIdentity(reaction, produced.Mappings) is { } unsupported)
        {
            return unsupported;
        }

        if (occurrence is null && produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression))
        {
            return new SemanticRejected(World, SemanticRejectionCategory.Contract, null, $"Reaction '{reaction.Name}' uses $context, which needs the scenario's 'given clock'.");
        }

        var eventContract = plan.Events[produced.EventContract];
        var mapped = produced.Mappings
            .Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, SemanticEvaluator.Evaluate(mapping.Source, root, values, occurrence)))
            .ToImmutableArray();
        var properties = eventContract.Properties.ToDictionary(property => property.Id);
        foreach (var value in mapped)
        {
            _validator.Validate(value.Value, properties[value.TargetProperty].Type, $"event property '{properties[value.TargetProperty].Name}'");
        }

        SemanticValue destination;
        SemanticEventContext? context;
        if (produced.Destination is null)
        {
            destination = cause!.Destination;
            context = cause.Context;
        }
        else
        {
            destination = SemanticEvaluator.Evaluate(produced.Destination, root, values);
            _validator.Validate(destination, produced.DestinationType!, "reaction event source");
            context = new(new(produced.DestinationType!, destination));
        }

        var fact = new SemanticFact(produced.EventContract, destination, mapped)
        {
            Context = context,
            Tags = eventContract.Tags.AddRange(produced.Tags),
            Occurred = occurrence?.Occurred,
            ReactionOrigin = reaction.Id
        };
        return Append(fact, occurrence?.Occurred);
    }

    SemanticExecutionResult? Invoke(
        SemanticReaction reaction,
        SemanticInvocation invocation,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticCommandOccurrence? occurrence)
    {
        if (RequiresAuditIdentity(reaction, invocation.Mappings) is { } unsupported)
        {
            return unsupported;
        }

        var command = plan.Commands[invocation.Command];
        var mappings = invocation.Mappings.ToDictionary(mapping => mapping.TargetProperty);
        var commandValues = command.Properties
            .Where(property => !property.IsGenerated)
            .Select(property => new SemanticPropertyValue(
                property.Id,
                mappings.TryGetValue(property.Id, out var mapping) ? SemanticEvaluator.Evaluate(mapping.Source, root, values, occurrence) : SemanticValue.Null))
            .ToImmutableArray();
        var request = SemanticExecutionRequest.Create(command.Id, commandValues, []) with
        {
            Occurrence = occurrence,
            ReactionOrigin = reaction.Id
        };

        var result = evaluator.Execute(plan, World, request);
        if (result is not SemanticAccepted accepted)
        {
            return result;
        }

        // An invoked response is computed by the evaluator, but is not the initiating command's response.
        // Invocation has no generation fixture channel; reached generation reports IdentityAllocation.
        return AcceptCommand(accepted, occurrence?.Occurred);
    }

    SemanticUnsupported? RequiresAuditIdentity(SemanticReaction reaction, ImmutableArray<SemanticPropertyMapping> mappings) =>
        mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: not SemanticEventContextValueKind.Occurred })
            ? new(World, SemanticExecutionCapability.Reaction, $"Reaction '{reaction.Name}' has no caller audit identity for $context.causedBy.")
            : null;
}
