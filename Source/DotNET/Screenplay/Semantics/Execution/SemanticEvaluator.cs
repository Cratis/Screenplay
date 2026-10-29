// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Executes the minimum portable semantic plan against immutable in-memory world state.
/// </summary>
public sealed partial class SemanticEvaluator : ISemanticEvaluator
{
    readonly SemanticRuleBodyExecution? _ruleBodies;
    readonly ISemanticBodyExecutionProviderFactory? _providerFactory;
    readonly IEnumerable<SemanticImplementationBody> _bodies = [];

    /// <summary>Initializes an evaluator without an implementation provider.</summary>
    public SemanticEvaluator()
    {
    }

    /// <summary>Initializes an evaluator that creates and disposes providers for executions, decisions and commits needing reducers or query policies.</summary>
    /// <param name="factory">The host-owned run factory.</param>
    /// <param name="bodies">Resolved bodies for the application.</param>
    public SemanticEvaluator(ISemanticBodyExecutionProviderFactory factory, IEnumerable<SemanticImplementationBody> bodies)
    {
        _providerFactory = factory;
        _bodies = [.. bodies];
    }

    internal SemanticEvaluator(ISemanticBodyExecutionProvider provider, IEnumerable<SemanticImplementationBody> bodies)
    {
        _ruleBodies = new(provider, bodies);
        ReducerBodies = new(provider, _ruleBodies);
        PolicyBodies = new(provider, _ruleBodies);
    }

    internal SemanticReducerBodyExecution ReducerBodies { get; } = SemanticReducerBodyExecution.None;
    internal SemanticPolicyBodyExecution PolicyBodies { get; } = SemanticPolicyBodyExecution.None;

    /// <inheritdoc/>
    public SemanticExecutionResult Execute(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request) => ExecuteWithCancellation(plan, world, request, default);

    /// <inheritdoc/>
    public SemanticExecutionResult ExecuteWithCancellation(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticExecutionRequest request,
        CancellationToken cancellationToken)
    {
        if (_providerFactory is not null)
        {
            using var run = new SemanticBodyExecutionRun(_providerFactory, plan.Model.Application, _bodies, cancellationToken);
            return run.Evaluator.ExecuteWithCancellation(plan, world, request, cancellationToken);
        }

        if (request.IsReadOnly)
        {
            return ExecuteReadOnly(plan, world, request, cancellationToken);
        }

        return DecideWithCancellation(plan, world, request, cancellationToken).Commit(world);
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
    public SemanticExecutionResult EstablishWorld(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts)
    {
        if (_providerFactory is null) return EstablishWorld(plan, facts, true, CancellationToken.None);
        using var run = new SemanticBodyExecutionRun(_providerFactory, plan.Model.Application, _bodies, CancellationToken.None);
        return run.Evaluator.EstablishWorld(plan, facts, true, CancellationToken.None);
    }

    /// <summary>Decides without host cancellation.</summary>
    /// <param name="plan">The admitted plan.</param>
    /// <param name="world">The starting world.</param>
    /// <param name="request">The command request.</param>
    /// <returns>The command decision.</returns>
    public SemanticDecision Decide(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request) => DecideWithCancellation(plan, world, request, default);

    /// <summary>
    /// Decides a command against a world without settling it, so a host can commit against a later world.
    /// </summary>
    /// <remarks>
    /// <see cref="Execute"/> is <c>Decide(...).Commit(world)</c> at one world. A host that appends to a store extends the decided
    /// world with facts written in the meantime (<see cref="ExtendWorld"/>) and commits against that world. A read-only
    /// request is refused with a contract rejection; run it through <see cref="Execute"/>.
    /// </remarks>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="world">The world the decision reads.</param>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation for host-owned rule execution; cancellation maps to Unsupported.</param>
    /// <returns>The decision; a refused decision returns its refusal from <see cref="SemanticDecision.Commit"/> and has empty dependencies.</returns>
    public SemanticDecision DecideWithCancellation(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request, CancellationToken cancellationToken)
    {
        if (_providerFactory is null) return new SemanticDecisionExecution(plan, world, request, cancellationToken).Decide(_ruleBodies, ReducerBodies, PolicyBodies, null);
        using var run = new SemanticBodyExecutionRun(_providerFactory, plan.Model.Application, _bodies, cancellationToken);
        return new SemanticDecisionExecution(plan, world, request, cancellationToken).Decide(run.Evaluator._ruleBodies, run.Evaluator.ReducerBodies, run.Evaluator.PolicyBodies, () => new SemanticBodyExecutionRun(_providerFactory, plan.Model.Application, _bodies, cancellationToken));
    }

    /// <summary>
    /// Extends a world with facts that already hold in the log, such as facts another writer appended.
    /// </summary>
    /// <remarks>
    /// The facts are validated as <see cref="EstablishWorld(SemanticExecutionPlan, ImmutableArray{SemanticFact})"/> validates them,
    /// then only they are projected onto the world's read models and pending children. No constraint is checked and nothing is
    /// published, because the facts are already in the log. The world of <c>ExtendWorld(Establish(a), b)</c> equals the world of
    /// <c>Establish(a + b)</c>; the result is not equal as a whole, because its accepted facts hold only <paramref name="facts"/>.
    /// A reached reducer requires executable transition bodies and a provider. On failure the result carries the unchanged input world.
    /// </remarks>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="world">The world to extend.</param>
    /// <param name="facts">The facts to append, in occurrence order.</param>
    /// <returns>An accepted result holding the extended world, a contract rejection, or an unsupported projection.</returns>
    public SemanticExecutionResult ExtendWorld(SemanticExecutionPlan plan, SemanticWorld world, ImmutableArray<SemanticFact> facts)
    {
        if (_providerFactory is not null)
        {
            using var run = new SemanticBodyExecutionRun(_providerFactory, plan.Model.Application, _bodies, CancellationToken.None);
            return run.Evaluator.ExtendWorld(plan, world, facts);
        }
        if (ReducerBodies.InvalidIdentity(plan, facts) is { } keyFailure) return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, keyFailure);
        if (facts.IsDefault)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "Extension facts cannot be default.");
        }

        try
        {
            if (ValidateHistory(plan, facts, true) is { } rejection)
            {
                return rejection with { World = world };
            }

            if (ReachedByHistory(plan, facts, ReducerBodies) is { } reducer)
            {
                return new SemanticUnsupported(
                    world,
                    SemanticExecutionCapability.Projection,
                    $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.");
            }

            if (!TryProject(plan, world.Facts, new ProjectionState(world.ReadModels, world.PendingChildren), facts, out var state, out var failure, ReducerBodies, CancellationToken.None))
            {
                return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, failure!);
            }

            return new SemanticAccepted(world.Commit(facts, state.ReadModels, state.PendingChildren), facts, []);
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, exception.Message);
        }
    }

    internal static SemanticExecutionResult Append(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticFact fact,
        ImmutableArray<SemanticQueryRequest> queries,
        SemanticCaller? caller) => Append(plan, world, fact, queries, caller, SemanticReducerBodyExecution.None, CancellationToken.None);

    internal static SemanticExecutionResult Append(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticFact fact,
        ImmutableArray<SemanticQueryRequest> queries,
        SemanticCaller? caller,
        SemanticReducerBodyExecution reducers,
        CancellationToken cancellationToken,
        SemanticPolicyBodyExecution? policies = null)
    {
        var facts = ImmutableArray.Create(fact);
        if (SemanticConstraintEnforcement.FindViolation(plan, world, facts) is { } violated)
        {
            return RejectWithMessage(world, SemanticRejectionCategory.Constraint, violated.Name, SemanticConstraintEnforcement.MessageFor(violated));
        }

        if (ReachedReducer(plan, facts, reducers) is { } reachedReducer)
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, $"Reducer '{reachedReducer.Name}' has opaque transitions and requires a target provider.");
        }

        if (!TryProject(plan, world.Facts, new ProjectionState(world.ReadModels, world.PendingChildren), facts, out var state, out var failure, reducers, cancellationToken))
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

        return ExecuteQueries(plan, world, world.Commit(facts, state.ReadModels, state.PendingChildren), facts, queries, caller, reducers, policies, cancellationToken: cancellationToken);
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
        SemanticTextValue text => string.IsNullOrWhiteSpace(text.Value),
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
        SemanticCaller? caller,
        SemanticReducerBodyExecution reducers,
        SemanticPolicyBodyExecution? policies = null,
        string? tenant = null,
        DateTimeOffset? occurred = null,
        CancellationToken cancellationToken = default)
    {
        if (queries.IsDefault)
        {
            return new SemanticRejected(original, SemanticRejectionCategory.Contract, null, "Execution request query collection cannot be default.");
        }

        var run = new SemanticQueryRun(plan, original, tentative, caller, reducers, policies ?? SemanticPolicyBodyExecution.None, tenant, occurred, cancellationToken);
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
        SemanticCommandOccurrence? occurrence = null,
        SemanticFact? cause = null) => expression switch
        {
            SemanticEventContextExpression { Value: SemanticEventContextValueKind.EventSourceIdentity } when expectedRoot == SemanticExpressionRootKind.Event && cause is not null => cause.Destination,
            SemanticEventContextExpression context when occurrence is not null => context.Value switch
            {
                SemanticEventContextValueKind.Occurred => OccurredValue(occurrence.Occurred),
                SemanticEventContextValueKind.CausedBySubject => SemanticValue.Text(occurrence.Subject),
                SemanticEventContextValueKind.CausedByName => SemanticValue.Text(occurrence.Name),
                SemanticEventContextValueKind.CausedByUserName => SemanticValue.Text(occurrence.UserName),
                _ => throw new InvalidSemanticContract("An occurrence field is unsupported.")
            },
            SemanticValueExpression literal => literal.Value,
            SemanticResolvedExpression resolved when resolved.Root == expectedRoot && resolved.Source == SemanticExpressionSourceKind.Property && values.TryGetValue(resolved.Target, out var value) => value,
            _ => throw new InvalidSemanticContract("An execution expression is unresolved in its declared root scope.")
        };

    internal static SemanticValue OccurredValue(DateTimeOffset occurred) =>
        SemanticValue.Text(occurred.UtcDateTime.ToString("O", System.Globalization.CultureInfo.InvariantCulture));

    internal static SemanticRejected RejectWithMessage(SemanticWorld world, SemanticRejectionCategory category, string? code, string message) =>
        new(world, category, code, message) { MessageIsStringKey = message.StartsWith("$strings.", StringComparison.Ordinal) };

    internal static SemanticReducer? ReachedReducer(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts, SemanticReducerBodyExecution reducers) =>
        plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V6)
            ? plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features))
                .FirstOrDefault(reducer => !reducers.CanExecute(plan, reducer) && reducer.Transitions.Any(transition => facts.Any(fact => fact.EventContract == transition.EventContract &&
                     (reducer.From?.Matches(plan.Model.Application, fact.Route) != false))))
            : null;

    internal static SemanticReducer? ReducerFor(SemanticExecutionPlan plan, SemanticId readModel) =>
        plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features))
            .FirstOrDefault(reducer => reducer.ReadModel == readModel);

    internal static IEnumerable<SemanticReducer> AllReducers(SemanticExecutionPlan plan) =>
        plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features));

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

    SemanticExecutionResult ExecuteReadOnly(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request, CancellationToken cancellationToken)
    {
        if (request.Command.IsSet || !request.Values.IsEmpty || !request.AllocatedIdentities.IsEmpty)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "A read-only request cannot carry command values or allocated identities.");
        }

        return ExecuteQueries(plan, world, world, [], request.Queries, request.Caller, ReducerBodies, PolicyBodies, request.Tenant, request.Occurrence?.Occurred, cancellationToken);
    }
}
