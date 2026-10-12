// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Executes the minimum portable semantic plan against immutable in-memory world state.
/// </summary>
public sealed partial class SemanticEvaluator : ISemanticEvaluator
{
    /// <inheritdoc/>
    public SemanticExecutionResult Execute(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticExecutionRequest request)
    {
        if (request.IsReadOnly)
        {
            return ExecuteReadOnly(plan, world, request);
        }

        if (!plan.Commands.TryGetValue(request.Command, out var command))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Command, $"Command '{request.Command}' is not in the execution plan.");
        }

        if (plan.UnadmittedArtifacts.Contains(command.Id))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Command, $"Command '{command.Name}' was not admitted by scoped execution-plan compilation.");
        }

        if (request.Queries.IsDefault)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "Execution request query collection cannot be default.");
        }

        return new SemanticCommandExecution(plan, world, command, request).Run();
    }

    /// <summary>
    /// Establishes reference world state from existing facts in occurrence order.
    /// </summary>
    /// <remarks>
    /// Unlike specification runs that do not inspect reducer state, this public operation fails closed when
    /// a supplied event affects a reducer-backed read model. Legacy ESM v1 facts may omit typed event sources; a routed fact matches its declared source identifier type, and an unrouted fact matches the declared producer destination type.
    /// </remarks>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="facts">The ordered existing facts; no read-model snapshots are required.</param>
    /// <returns>An accepted established world, a contract rejection, or an unsupported projection.</returns>
    public SemanticExecutionResult EstablishWorld(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts) =>
        EstablishWorld(plan, facts, true);

    internal static SemanticExecutionResult Append(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticFact fact,
        ImmutableArray<SemanticQueryRequest> queries,
        SemanticCaller? caller)
    {
        var facts = ImmutableArray.Create(fact);
        if (SemanticConstraintEnforcement.FindViolation(plan, world, facts) is { } violated)
        {
            return RejectWithMessage(world, SemanticRejectionCategory.Constraint, violated.Name, SemanticConstraintEnforcement.MessageFor(violated));
        }

        if (ReachedReducer(plan, facts) is { } reachedReducer)
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, $"Reducer '{reachedReducer.Name}' has opaque transitions and requires a target provider.");
        }

        if (!TryProject(plan, world.Facts, world.ReadModels, facts, out var readModels, out var failure))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, failure!);
        }

        if (!SemanticEventPublication.TryPublish(plan, world.Facts, facts, out var publications, out var publicationFailure))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, publicationFailure!);
        }

        facts = facts.AddRange(publications);
        if (SemanticDeferredProjections.FindFailure(plan, facts) is { } deferredFailure)
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, deferredFailure);
        }

        return ExecuteQueries(plan, world, world.Commit(facts, readModels), facts, queries, caller);
    }

    // Projects from supplied snapshots without validating facts. The public establishment operation intentionally
    // starts from empty state so it can validate the complete supplied history first.
    internal static bool Establish(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticReadModelInstance> current,
        ImmutableArray<SemanticFact> facts,
        out ImmutableArray<SemanticReadModelInstance> readModels,
        out string? failure) =>
        TryProject(plan, [], current, facts, out readModels, out failure);

    // Every variant answers explicitly: an unknown variant is not quietly "present", it is a malformed value.
    internal static bool IsEmpty(SemanticValue value) => value switch
    {
        SemanticNullValue => true,
        SemanticTextValue text => string.IsNullOrEmpty(text.Value),
        SemanticArrayValue array => array.Values.IsEmpty,
        SemanticNumberValue or SemanticBooleanValue or SemanticCompositeValue => false,
        _ => throw SemanticValueRules.Malformed()
    };

    internal static SemanticExecutionResult ExecuteQueries(
        SemanticExecutionPlan plan,
        SemanticWorld original,
        SemanticWorld tentative,
        ImmutableArray<SemanticFact> facts,
        ImmutableArray<SemanticQueryRequest> queries,
        SemanticCaller? caller)
    {
        if (queries.IsDefault)
        {
            return new SemanticRejected(original, SemanticRejectionCategory.Contract, null, "Execution request query collection cannot be default.");
        }

        var run = new SemanticQueryRun(plan, original, tentative, caller);
        var queryResults = ImmutableArray.CreateBuilder<SemanticQueryResult>();
        foreach (var queryRequest in queries)
        {
            if (run.Answer(queryRequest, out var result) is { } refusal)
            {
                return refusal;
            }

            queryResults.Add(result!);
        }

        return run.RefuseReducerBackedWorld() ?? new SemanticAccepted(tentative, facts, queryResults.ToImmutable());
    }

    internal static SemanticValue Evaluate(
        SemanticExpression expression,
        SemanticExpressionRootKind expectedRoot,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticCommandOccurrence? occurrence = null) => expression switch
        {
            SemanticEventContextExpression context when occurrence is not null => context.Value switch
            {
                SemanticEventContextValueKind.Occurred => SemanticValue.Text(occurrence.Occurred.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture)),
                SemanticEventContextValueKind.CausedBySubject => SemanticValue.Text(occurrence.Subject),
                SemanticEventContextValueKind.CausedByName => SemanticValue.Text(occurrence.Name),
                SemanticEventContextValueKind.CausedByUserName => SemanticValue.Text(occurrence.UserName),
                _ => throw new InvalidSemanticContract("An occurrence field is unsupported.")
            },
            SemanticValueExpression literal => literal.Value,
            SemanticResolvedExpression resolved when resolved.Root == expectedRoot && resolved.Source == SemanticExpressionSourceKind.Property && values.TryGetValue(resolved.Target, out var value) => value,
            _ => throw new InvalidSemanticContract("An execution expression is unresolved in its declared root scope.")
        };

    internal static SemanticRejected RejectWithMessage(SemanticWorld world, SemanticRejectionCategory category, string? code, string message) =>
        new(world, category, code, message) { MessageIsStringKey = message.StartsWith("$strings.", StringComparison.Ordinal) };

    internal static SemanticReducer? ReachedReducer(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts) =>
        plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V6)
            ? plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features))
                .FirstOrDefault(reducer => reducer.Transitions.Any(transition => facts.Any(fact => fact.EventContract == transition.EventContract &&
                     (reducer.From?.Matches(plan.Model.Application, fact.Route) != false))))
            : null;

    internal static SemanticReducer? ReducerFor(SemanticExecutionPlan plan, SemanticId readModel) =>
        plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features))
            .FirstOrDefault(reducer => reducer.ReadModel == readModel);

    internal static IEnumerable<SemanticReducer> Reducers(ImmutableArray<SemanticFeature> features) =>
        features.SelectMany(feature => feature.Slices.SelectMany(slice => slice.Reducers).Concat(Reducers(feature.Features)));

    internal static SemanticExecutionResponse? Response(SemanticCommandResponse? response, Dictionary<SemanticId, SemanticValue> values) => response switch
    {
        null => null,
        SemanticScalarCommandResponse scalar => new SemanticScalarExecutionResponse(values.GetValueOrDefault(scalar.Source, SemanticValue.Null)),
        SemanticRecordCommandResponse record => new SemanticRecordExecutionResponse([.. record.Fields.Select(field =>
            new SemanticExecutionResponseField(field.Name, values.GetValueOrDefault(field.Source, SemanticValue.Null)))]),
        _ => throw new InvalidSemanticContract("A command response variant is unsupported.")
    };

    internal static SemanticTypeReference DestinationType(SemanticCommand command, SemanticExpression? expression, SemanticTypeReference? allocatedType) =>
        expression is SemanticResolvedExpression resolved
            ? command.Properties.Single(property => property.Id == resolved.Target).Type
            : (command.Properties.Any(property => property.IsGenerated && property.IsIdentifier) ? allocatedType : command.Destination?.Type ?? allocatedType) ??
                throw new InvalidSemanticContract("A v2 fact requires a typed state-change destination.");

    static SemanticExecutionResult ExecuteReadOnly(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request)
    {
        if (request.Command.IsSet || !request.Values.IsEmpty || !request.AllocatedIdentities.IsEmpty)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "A read-only request cannot carry command values or allocated identities.");
        }

        return ExecuteQueries(plan, world, world, [], request.Queries, request.Caller);
    }
}
