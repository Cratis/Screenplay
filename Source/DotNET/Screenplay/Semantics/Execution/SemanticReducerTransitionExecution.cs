// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticReducerTransitionExecution(ISemanticBodyExecutionProvider provider, SemanticExecutionPlan plan, SemanticReadModel model)
{
    internal static bool HasScalarKey(SemanticFact fact) => fact.Destination is SemanticTextValue or SemanticNumberValue or SemanticBooleanValue;

    internal string? Apply(SemanticReducerFact input, SemanticImplementationBody body, List<SemanticReadModelInstance> instances, CancellationToken cancellationToken)
    {
        var fact = input.Fact;
        if (!HasScalarKey(fact))
        {
            return Unsupported(body, "The event source key must be a non-null scalar identity.");
        }
        var current = instances.SingleOrDefault(instance => instance.ReadModel == model.Id && SemanticValueRules.AreEqual(instance.Key, fact.Destination));
        var result = Invoke(body, Context(fact, current, input.Position), cancellationToken);
        try
        {
            var next = NextState(plan, model, fact, result);
            if (current is not null) instances.Remove(current);
            if (next is not null) instances.Add(next);
            return null;
        }
        catch (InvalidSemanticContract exception)
        {
            return Unsupported(body, exception.Message);
        }
    }

    static SemanticReducerContext Context(SemanticFact fact, SemanticReadModelInstance? current, ulong position) => new()
    {
        State = current is null ? null : SemanticValue.Composite(current.Values),
        Event = SemanticValue.Composite(fact.Values),
        Key = SemanticValue.Text(SemanticValueRules.CaptureText(fact.Destination)),
        Occurred = fact.Occurred,
        SequenceNumber = position
    };

    static SemanticReadModelInstance? NextState(SemanticExecutionPlan plan, SemanticReadModel model, SemanticFact fact, SemanticReducerBodyResult? result)
    {
        if (result is null || result.Diagnostic is not null) throw new InvalidSemanticContract(result?.Diagnostic ?? "The provider returned no outcome.");
        if (result is { Outcome: SemanticReducerBodyOutcome.Delete, State: null }) return null;
        if (result is not { Outcome: SemanticReducerBodyOutcome.State, State: SemanticCompositeValue state }) throw new InvalidSemanticContract("The provider returned a malformed state or outcome.");
        return new(model.Id, fact.Destination, SemanticReducerState.Validate(plan, model, state, fact.Destination));
    }

    static string Unsupported(SemanticImplementationBody body, string diagnostic) =>
        $"Reducer transition (requirement {body.Requirement.RequirementId}): {SemanticRuleBodyExecution.Sanitize(diagnostic)}";

    SemanticReducerBodyResult? Invoke(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Task.Run(async () => await provider.ReduceAsync(body, context, cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        catch (Exception exception)
        {
            return SemanticReducerBodyResult.Unsupported($"Provider threw {exception.GetType().Name}.");
        }
    }
}
