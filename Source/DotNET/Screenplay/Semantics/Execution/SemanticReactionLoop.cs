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
/// invoked command runs through the full command pipeline with its reaction's declared system identity, or no caller
/// when none is declared. The given caller is never forwarded. System claims are unknown; audit identity is unaffected.
/// An unhandled rejection or an unsupported capability anywhere ends the scenario.
/// </remarks>
/// <param name="evaluator">The evaluator invoked commands run through.</param>
/// <param name="plan">The capability-admitted plan.</param>
/// <param name="world">The world the loop starts from.</param>
internal sealed partial class SemanticReactionLoop(ISemanticEvaluator evaluator, SemanticExecutionPlan plan, SemanticWorld world)
{
    /// <summary>
    /// The most facts one scenario may append before its reactions are taken not to settle.
    /// </summary>
    internal const int MaximumFacts = 1_000;

    readonly Queue<(SemanticFact Fact, DateTimeOffset? Occurred)> _pending = new();
    readonly Dictionary<(SemanticId Command, SemanticId Property), Queue<SemanticValue>> _pinned = [];
    readonly List<SemanticFact> _facts = [];
    readonly List<SemanticRecordedInvocation> _invocations = [];
    readonly SemanticValueValidator _validator = new(
        plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
        plan.Model.Application.Types.ToDictionary(type => type.Id));

    SemanticAbilityAnswer? _abilityAnswer;

    /// <summary>
    /// Gets the world as the loop has left it.
    /// </summary>
    public SemanticWorld World { get; private set; } = world;

    /// <summary>
    /// Gets the IANA time zone a specification pins with <c>given time zone</c>; commands that reactions invoke read it as the scenario and context zone.
    /// </summary>
    public string? GivenTimeZone { get; init; }

    /// <summary>
    /// Gets the answers the invoked abilities give, stated by the specification (ESM v51).
    /// </summary>
    public ImmutableArray<SemanticAbilityAnswer> AbilityAnswers { get; init; } = [];

    /// <summary>
    /// Gets every fact appended through the loop, in order.
    /// </summary>
    public ImmutableArray<SemanticFact> Facts => [.. _facts];

    /// <summary>
    /// Gets every invocation a reaction made through the loop, in invocation order.
    /// </summary>
    public ImmutableArray<SemanticRecordedInvocation> Invocations => [.. _invocations];

    /// <summary>
    /// Pins the values the commands that reactions invoke receive for their generated properties, each invocation taking the
    /// next one pinned for its command and property.
    /// </summary>
    /// <param name="pinned">The pinned values in authored order.</param>
    public void Pin(ImmutableArray<SemanticSpecificationGeneratedValue> pinned)
    {
        if (pinned.IsDefaultOrEmpty) return;
        foreach (var value in pinned)
        {
            if (!_pinned.TryGetValue((value.Command, value.Property), out var queue)) _pinned[(value.Command, value.Property)] = queue = new();
            queue.Enqueue(value.Value);
        }
    }

    /// <summary>
    /// Takes facts an action already appended to the world, so the reactions to them run.
    /// </summary>
    /// <param name="facts">The appended facts.</param>
    /// <param name="occurred">When they occurred, or <see langword="null"/> when the scenario states no clock.</param>
    /// <returns>An unsupported result when the scenario appends too many facts; otherwise <see langword="null"/>.</returns>
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
    /// <returns>An unsupported result retaining the prior world when the batch exceeds the budget; otherwise <see langword="null"/>.</returns>
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
    /// <param name="occurred">When it occurred, or <see langword="null"/> when the scenario states no clock.</param>
    /// <returns>The rejected or unsupported result, or <see langword="null"/> when the fact was appended.</returns>
    public SemanticExecutionResult? Append(SemanticFact fact, DateTimeOffset? occurred)
    {
        if (_facts.Count >= MaximumFacts)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, $"The scenario appends more than {MaximumFacts} facts; its reactions do not settle.");
        }

        var result = SemanticEvaluator.Append(plan, World, fact, [], null, SemanticReducerBodyExecution.For(evaluator), SemanticReducerBodyExecution.TokenFor(evaluator));
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
    /// <returns>The rejected or unsupported result, or <see langword="null"/> when the reactions settled.</returns>
    public SemanticExecutionResult? Settle()
    {
        var captures = new List<(SemanticFact Fact, DateTimeOffset? Occurred)>();
        while (_pending.TryDequeue(out var item))
        {
            var values = item.Fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
            foreach (var reaction in plan.RunningReactions.Where(reaction => reaction.From?.Matches(plan.Model.Application, item.Fact.Route) != false))
            {
                foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == item.Fact.EventContract))
                {
                    if (Fire(reaction, trigger, values, SemanticExpressionRootKind.Event, item.Fact, item.Occurred) is { } failure)
                    {
                        return failure;
                    }
                }
            }
            captures.Add(item);
        }

        // All reactions, including their cascades, settle before delivering their facts to captures.
        foreach (var item in captures)
        {
            if (DeliverCaptures(item.Fact, item.Occurred, history: false) is { } failure) return failure;
        }

        return null;
    }

    /// <summary>
    /// Delivers an existing fact to only the named reaction, respecting its observer filter.
    /// </summary>
    /// <param name="reaction">The reaction receiving the fact.</param>
    /// <param name="fact">The existing fact, which is not appended again.</param>
    /// <param name="occurred">When the new delivery occurs.</param>
    /// <returns>A failure, or <see langword="null"/> when the delivery succeeds or is filtered out.</returns>
    public SemanticExecutionResult? Redeliver(SemanticReaction reaction, SemanticFact fact, DateTimeOffset? occurred)
    {
        if (reaction.From?.Matches(plan.Model.Application, fact.Route) == false) return null;
        var values = fact.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == fact.EventContract))
        {
            if (Fire(reaction, trigger, values, SemanticExpressionRootKind.Event, fact, occurred) is { } failure) return failure;
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
    /// <param name="occurred">When the occurrence happened, or <see langword="null"/> when the scenario states no clock.</param>
    /// <returns>The rejected or unsupported result, or <see langword="null"/> when the reaction ran or its 'where' excluded it.</returns>
    public SemanticExecutionResult? Fire(
        SemanticReaction reaction,
        SemanticReactionTrigger trigger,
        IReadOnlyDictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        DateTimeOffset? occurred)
    {
        if (trigger.Where is not null && !SemanticConditionEvaluator.Evaluate(trigger.Where, values))
        {
            return null;
        }

        var occurrence = Occurrence(cause, occurred);
        if (trigger.RequirementId is not null)
        {
            return FireBody(reaction, trigger, cause, occurrence);
        }
        var lookup = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        try
        {
            if (ProduceAll(reaction, trigger.Produces, lookup, root, cause, occurrence) is { } productionFailure) return productionFailure;

            foreach (var invocation in trigger.Invokes)
            {
                if (invocation.Ability is not null)
                {
                    if (InvokeAbility(reaction, invocation, lookup, root, cause, occurrence) is { } abilityFailure) return abilityFailure;
                    continue;
                }

                if (Invoke(reaction, invocation, lookup, root, cause, occurrence, out var handled) is { } failure)
                {
                    return failure;
                }
                if (handled) break;
            }
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticRejected(World, SemanticRejectionCategory.Contract, null, exception.Message);
        }
        catch (SemanticAbilityAnswerMissing missing)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Reaction, missing.Message);
        }

        return null;
    }

    // An ability runs outside the model: the specification supplies its answer ('given <System>.<Ability> returns|answers|fails');
    // without one the ability returns nothing, so a branch that reads what it returned cannot be proved and reports unsupported.
    SemanticExecutionResult? InvokeAbility(
        SemanticReaction reaction,
        SemanticInvocation invocation,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        SemanticCommandOccurrence? occurrence)
    {
        var inputs = invocation.Inputs
            .Select(input => new SemanticAbilityFieldValue(input.Input, SemanticEvaluator.Evaluate(input.Source, root, values, occurrence, cause)))
            .ToImmutableArray();
        _invocations.Add(new(default, [], true) { System = invocation.System, Ability = invocation.Ability, Inputs = inputs });
        var answer = AbilityAnswers.FirstOrDefault(candidate => candidate.System == invocation.System && string.Equals(candidate.Ability, invocation.Ability, StringComparison.Ordinal));
        var selector = answer?.Kind switch
        {
            SemanticAbilityAnswerKind.Outcome => answer.Outcome,
            SemanticAbilityAnswerKind.Failed => "failed",
            _ => "returned"
        };
        var branch = invocation.Branches.FirstOrDefault(candidate => string.Equals(candidate.Selector, selector, StringComparison.Ordinal));
        if (branch is null) return null;
        var previous = _abilityAnswer;
        _abilityAnswer = answer ?? new SemanticAbilityAnswer(invocation.System, invocation.Ability!, SemanticAbilityAnswerKind.Returned);
        try
        {
            return ProduceAll(reaction, branch.Produces, values, root, cause, occurrence);
        }
        finally
        {
            _abilityAnswer = previous;
        }
    }

    SemanticValue AbilityValue(SemanticInvocationValueExpression expression)
    {
        var answer = _abilityAnswer!;
        var value = expression.Source switch
        {
            "result" => answer.Values.FirstOrDefault(field => string.Equals(field.Name, expression.Member, StringComparison.Ordinal))?.Value,
            "outcome" => answer.Outcome is null ? null : SemanticValue.Text(answer.Outcome),
            _ => answer.Reason is null ? null : SemanticValue.Text(answer.Reason)
        };
        return value ?? throw new SemanticAbilityAnswerMissing($"The reaction reads '${expression.Source}{(expression.Member.Length == 0 ? string.Empty : "." + expression.Member)}' of an ability, which the specification gives no answer for ('given <System>.<Ability> returns|answers|fails').");
    }

    // A reaction-invoked command reads the triggering instant when the model uses the clock (decision 0107); other models keep the scenario clock.
    SemanticCommandOccurrence? Occurrence(SemanticFact? cause, DateTimeOffset? occurred)
    {
        var instant = SemanticClockUsage.Uses(plan.Model.Application) ? cause?.Occurred ?? occurred : occurred;
        return instant is { } at ? new SemanticCommandOccurrence(at, string.Empty, string.Empty, string.Empty) { IsTimeOnly = true } : null;
    }

    SemanticExecutionResult? Produce(
        SemanticReaction reaction,
        SemanticProducedEvent produced,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        SemanticCommandOccurrence? occurrence,
        SemanticInvocationRefusalEvaluation? refusal = null)
    {
        if (Materialize(reaction, produced, values, root, cause, occurrence, refusal, out var fact) is { } failure) return failure;
        return Append(fact!, occurrence?.Occurred);
    }

    SemanticExecutionResult? Materialize(
        SemanticReaction reaction,
        SemanticProducedEvent produced,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        SemanticCommandOccurrence? occurrence,
        SemanticInvocationRefusalEvaluation? refusal,
        out SemanticFact? fact)
    {
        fact = null;
        if (RequiresAuditIdentity(reaction, produced.Mappings) is { } unsupported)
        {
            return unsupported;
        }

        if (occurrence is null && produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: not SemanticEventContextValueKind.EventSourceIdentity }))
        {
            return new SemanticRejected(World, SemanticRejectionCategory.Contract, null, $"Reaction '{reaction.Name}' uses $context, which needs the scenario's 'given clock'.");
        }

        var eventContract = plan.Events[produced.EventContract];
        var mapped = produced.Mappings
            .Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, mapping.Source switch
            {
                SemanticRefusalExpression expression when refusal is not null => refusal.ValueOf(expression.Member),
                SemanticInvocationValueExpression invocationValue when _abilityAnswer is not null => AbilityValue(invocationValue),
                _ => SemanticEvaluator.Evaluate(mapping.Source, root, values, occurrence, cause)
            }))
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

        fact = new SemanticFact(produced.EventContract, destination, mapped)
        {
            Context = context,
            Tags = eventContract.Tags.AddRange(produced.Tags),
            Occurred = occurrence?.Occurred,
            ReactionOrigin = reaction.Id
        };
        return null;
    }

    SemanticExecutionResult? Invoke(
        SemanticReaction reaction,
        SemanticInvocation invocation,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        SemanticCommandOccurrence? occurrence,
        out bool handled)
    {
        handled = false;
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
                mappings.TryGetValue(property.Id, out var mapping) ? SemanticEvaluator.Evaluate(mapping.Source, root, values, occurrence, cause) : SemanticValue.Null))
            .ToImmutableArray();
        var request = SemanticExecutionRequest.Create(command.Id, commandValues, []) with
        {
            Occurrence = occurrence,
            ContextTimeZone = GivenTimeZone,
            ContextTimeZones = SemanticCalendar.Given(plan.Commands.Values, GivenTimeZone),
            ScenarioTimeZone = GivenTimeZone,
            CollectRefusalDetails = !invocation.OnRefused.IsDefaultOrEmpty
        };
        var result = RunInvoked(reaction, command, request);
        if (result is not SemanticAccepted accepted)
        {
            if (!invocation.OnRefused.IsDefaultOrEmpty && SemanticInvocationRefusalEvaluation.Classify(result) is { } refusal &&
                invocation.OnRefused.FirstOrDefault(refusal.Matches) is { } branch)
            {
                if (ProduceAll(reaction, branch.Produces, values, root, cause, occurrence, refusal) is { } failure) return failure;
                handled = true;
                return null;
            }

            return result;
        }

        // An invoked response is computed by the evaluator, but is not the initiating command's response.
        // A value the specification pinned with 'given generated' reaches generation; an unpinned one reports IdentityAllocation.
        // A command another slice owns is that slice's behavior: in an isolated run its outcome decides the reaction, its facts are not adopted.
        return plan.Runs(command.Id) ? AcceptCommand(accepted, occurrence?.Occurred) : null;
    }

    SemanticExecutionResult RunInvoked(SemanticReaction reaction, SemanticCommand command, SemanticExecutionRequest request)
    {
        var result = evaluator.Execute(plan, World, request with
        {
            ReactionOrigin = reaction.Id,
            RunsAs = reaction.RunsAs,
            GeneratedValues = TakePinned(command)
        });
        _invocations.Add(new(command.Id, request.Values, result is SemanticAccepted));
        return result;
    }

    SemanticUnsupported? RequiresAuditIdentity(SemanticReaction reaction, ImmutableArray<SemanticPropertyMapping> mappings) =>
        mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.CausedBySubject or SemanticEventContextValueKind.CausedByName or SemanticEventContextValueKind.CausedByUserName })
            ? new(World, SemanticExecutionCapability.Reaction, $"Reaction '{reaction.Name}' has no caller audit identity for $context.causedBy.")
            : null;

    ImmutableArray<SemanticPropertyValue> TakePinned(SemanticCommand command) =>
        [.. command.Properties.Where(property => property.IsGenerated && _pinned.TryGetValue((command.Id, property.Id), out var queue) && queue.Count > 0)
            .Select(property => new SemanticPropertyValue(property.Id, _pinned[(command.Id, property.Id)].Dequeue()))];
}
