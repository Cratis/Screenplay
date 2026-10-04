// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Performs the action of an ESM v6 specification - a command, an append, the clock, a trigger or a capture record -
/// and runs the reactions it sets off until they settle.
/// </summary>
/// <remarks>
/// <c>given clock</c> fixes when the scenario happens: every command, append and reaction in it occurs at that instant,
/// except what a clock occurrence sets off, which occurs at the instant it fell due. The clock states a time and no
/// caller, so a mapping from <c>$context.causedBy</c> is unsupported in a scenario that states one.
/// </remarks>
internal static class SemanticScenario
{
    /// <summary>
    /// Performs the specification's action.
    /// </summary>
    /// <param name="evaluator">The evaluator commands run through.</param>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="world">The established world.</param>
    /// <param name="expected">The specification.</param>
    /// <param name="queries">The queries to answer once the reactions settle.</param>
    /// <param name="actionFacts">How many of the resulting facts the action itself appended, ahead of those reactions appended.</param>
    /// <returns>The accepted scenario with every new fact, or what rejected it or is unsupported.</returns>
    public static SemanticExecutionResult Perform(
        ISemanticEvaluator evaluator,
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticSpecification expected,
        ImmutableArray<SemanticQueryRequest> queries,
        out int actionFacts)
    {
        actionFacts = 0;
        DateTimeOffset? clock = expected.GivenClock is { } given ? Instant(given) : null;
        var occurrence = clock is { } at ? new SemanticCommandOccurrence(at, string.Empty, string.Empty, string.Empty) { IsTimeOnly = true } : null;

        var loop = new SemanticReactionLoop(evaluator, plan, world);
        SemanticExecutionResult? failure;
        try
        {
            switch (expected)
            {
                case { When: { } when }:
                    var executed = evaluator.Execute(plan, world, CommandRequest(when) with { Caller = expected.GivenCaller, Occurrence = occurrence });
                    if (executed is not SemanticAccepted accepted)
                    {
                        return executed;
                    }

                    loop = new(evaluator, plan, accepted.World);
                    actionFacts = accepted.Facts.Length;
                    failure = loop.Observe(accepted.Facts, clock);
                    break;
                case { WhenAppended: { } appended }:
                    actionFacts = 1;
                    failure = loop.Append(
                        new SemanticFact(appended.EventContract, appended.EventSource?.Value ?? SemanticValue.Null, appended.Values)
                        {
                            Context = appended.EventSource is null ? null : new(appended.EventSource),
                            Tags = plan.Events[appended.EventContract].Tags,
                            Occurred = clock
                        },
                        clock);
                    break;
                case { WhenClock: { } advance }:
                    failure = Advance(plan, loop, clock!.Value, Instant(advance));
                    break;
                case { WhenTrigger: { } fired }:
                    failure = Fire(plan, loop, fired, clock);
                    break;
                case { WhenCapture: { } presented }:
                    failure = Present(plan, loop, expected, presented, occurrence);
                    break;
                default:
                    failure = null;
                    break;
            }
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticRejected(loop.World, SemanticRejectionCategory.Contract, null, exception.Message);
        }

        failure ??= loop.Settle();
        return failure ?? SemanticEvaluator.ExecuteQueries(plan, loop.World, loop.World, loop.Facts, queries, expected.GivenCaller);
    }

    static SemanticExecutionRequest CommandRequest(SemanticSpecificationCommand when) =>
        SemanticExecutionRequest.Create(when.Command, when.Values, []) with
        {
            AllocatedIdentities = when.EventSource is null
                ? ImmutableDictionary.Create<SemanticId, SemanticValue>()
                : ImmutableDictionary<SemanticId, SemanticValue>.Empty.Add(when.Command, when.EventSource.Value),
            AllocatedEventSourceType = when.EventSource?.Type
        };

    static SemanticExecutionResult? Advance(SemanticExecutionPlan plan, SemanticReactionLoop loop, DateTimeOffset from, DateTimeOffset to)
    {
        if (!SemanticClock.TryFindDue(plan.Reactions, from, to, out var due))
        {
            return new SemanticUnsupported(loop.World, SemanticExecutionCapability.Reaction, $"Advancing the clock makes more than {SemanticClock.MaximumOccurrences} occurrences due.");
        }

        foreach (var (at, reaction, trigger) in due)
        {
            if ((loop.Fire(reaction, trigger, ImmutableDictionary<SemanticId, SemanticValue>.Empty, SemanticExpressionRootKind.Trigger, null, at) ?? loop.Settle()) is { } failure)
            {
                return failure;
            }
        }

        return null;
    }

    static SemanticExecutionResult? Fire(SemanticExecutionPlan plan, SemanticReactionLoop loop, SemanticSpecificationTrigger fired, DateTimeOffset? clock)
    {
        var values = fired.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        foreach (var reaction in plan.Reactions)
        {
            foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Kind == fired.Kind && trigger.Source == fired.Trigger))
            {
                if ((loop.Fire(reaction, trigger, values, SemanticExpressionRootKind.Trigger, null, clock) ?? loop.Settle()) is { } failure)
                {
                    return failure;
                }
            }
        }

        return null;
    }

    static SemanticExecutionResult? Present(
        SemanticExecutionPlan plan,
        SemanticReactionLoop loop,
        SemanticSpecification expected,
        SemanticSpecificationCapture presented,
        SemanticCommandOccurrence? occurrence)
    {
        var capture = plan.Captures[presented.Capture];
        var key = KeyOf(presented.Record, capture.Key);
        var previous = expected.GivenCaptures.LastOrDefault(given => given.Capture == presented.Capture &&
            SemanticValueRules.AreEqual(KeyOf(given.Record, capture.Key), key))?.Record;
        foreach (var fact in SemanticCaptureEvaluation.Evaluate(plan, capture, previous, presented.Record, occurrence))
        {
            if (loop.Append(fact, occurrence?.Occurred) is { } failure)
            {
                return failure;
            }
        }

        return null;
    }

    static SemanticValue KeyOf(SemanticCaptureRecord record, string key) =>
        record.Fields.SingleOrDefault(field => field.Name == key && field.Kind == SemanticCaptureFieldKind.Value)?.Value
        ?? throw new InvalidSemanticContract($"A capture record has no scalar key '{key}'.");

    static DateTimeOffset Instant(string value) =>
        DateTimeOffset.ParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
}
