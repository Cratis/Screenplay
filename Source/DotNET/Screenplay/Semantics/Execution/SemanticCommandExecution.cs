// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Executes one planned command against a world, one ordered step at a time.
/// </summary>
/// <param name="plan">The capability-admitted plan.</param>
/// <param name="world">The world the command runs against.</param>
/// <param name="command">The planned command.</param>
/// <param name="request">The execution request.</param>
internal sealed partial class SemanticCommandExecution(
    SemanticExecutionPlan plan,
    SemanticWorld world,
    SemanticCommand command,
    SemanticExecutionRequest request,
    SemanticRuleBodyExecution? ruleBodies,
    SemanticReducerBodyExecution reducers,
    SemanticPolicyBodyExecution policies,
    SemanticCommandBodyExecution handlers,
    Func<SemanticBodyExecutionRun>? commitRun,
    CancellationToken cancellationToken)
{
    const string NeedsOccurrenceMessage = "A v2 command using $context needs an occurrence supplied by the execution request.";

    readonly ImmutableArray<SemanticFact>.Builder _facts = ImmutableArray.CreateBuilder<SemanticFact>();
    readonly Dictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>> _reads = new(StringComparer.Ordinal);
    readonly IReadOnlyDictionary<SemanticId, SemanticPrimitiveType> _dateTypes = SemanticDateComparisons.Participants(plan.Model.Application, command);
    Dictionary<SemanticId, SemanticValue> _commandValues = [];
    Dictionary<SemanticCommandRoute, SemanticEventRoute> _routes = new(ReferenceEqualityComparer.Instance);
    SemanticStateChangeDestination? _defaultDestination;
    SemanticDecisionDependencies _dependencies = SemanticDecisionDependencies.Empty;

    public SemanticDecision Decide()
    {
        if (Admit() is { } admission)
        {
            return SemanticDecision.Refused(admission);
        }

        if (Validate() is { } validation)
        {
            return SemanticDecision.Refused(validation);
        }

        if (PrepareProduction() is { } preparation)
        {
            return SemanticDecision.Refused(preparation);
        }

        if (command.Handler is { } handler)
        {
            if (Handle(handler.RequirementId) is { } refusal) return SemanticDecision.Refused(refusal);
        }
        else
        {
            foreach (var produced in command.Produces)
            {
                if (Produce(produced) is { } production) return SemanticDecision.Refused(production);
            }
        }

        var response = SemanticEvaluator.Response(command.Response, _commandValues);
        return new(world, _dependencies, commitWorld => SettleWithProvider(commitWorld, response));
    }

    static bool HasContextMapping(SemanticProducedEvent produced) =>
        produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression);

    static bool NeedsCallerIdentity(SemanticProducedEvent produced) =>
        produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression { Value: not SemanticEventContextValueKind.Occurred });

    SemanticRejected Contract(string message) =>
        new(world, SemanticRejectionCategory.Contract, null, message);

    SemanticExecutionResult? Admit()
    {
        if (Authorize() is { } unauthorized)
        {
            return unauthorized;
        }

        if (RefuseInvalidDateTimeInput() is { } invalidDateTime) return invalidDateTime;

        if (RefuseOpaqueValidation() is { } opaque)
        {
            return opaque;
        }

        return RefuseInvalidRequest() ?? RefuseAllocatedSourceType();
    }

    SemanticExecutionResult? Authorize()
    {
        var authorizationValues = request.Values.IsDefault ? [] : request.Values.Where(value => value is not null).ToArray();
        var artifact = command.Properties
            .Where(property => !property.IsGenerated)
            .Select(property => (property, value: authorizationValues.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value))
            .Where(pair => pair.value is not null)
            .ToDictionary(pair => pair.property.Name, pair => pair.value!, StringComparer.Ordinal);
        var subject = command.Properties.Where(property => property.IsIdentifier && !property.IsGenerated)
            .Select(property => authorizationValues.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value)
            .FirstOrDefault(value => value is not null);
        var authorization = SemanticPolicyEvaluation.Evaluate(command.Authorization, plan, request.Caller, artifact, subject, command.Properties.Where(property => !property.IsGenerated), request.RunsAs, policies, request.Tenant, request.Occurrence?.Occurred, cancellationToken: cancellationToken);
        return authorization.Outcome switch
        {
            SemanticPolicyOutcome.Unsupported => new SemanticUnsupported(
                world,
                SemanticExecutionCapability.Authorization,
                authorization.Details ?? $"Policy '{authorization.Policy}' has an opaque predicate and requires a target provider."),
            SemanticPolicyOutcome.Deny => new SemanticRejected(world, SemanticRejectionCategory.Unauthorized, null, "Caller is not authorized."),
            _ => null
        };
    }

    // Ordering between generated declarative validators and attached code is not portable.
    // Refuse to infer a validation outcome after authorization, before evaluating any validators.
    SemanticUnsupported? RefuseOpaqueValidation() =>
        SemanticEvaluator.UnsupportedValidation(plan, command, ruleBodies is not null) is { } rule
            ? new SemanticUnsupported(
                world,
                SemanticExecutionCapability.Command,
                $"{rule} has an opaque validation predicate and requires a target provider.")
            : null;

    SemanticRejected? RefuseInvalidDateTimeInput()
    {
        if (request.Values.IsDefault) return null;
        return SemanticDateComparisons.CheckInputs(plan.Model.Application, command, request.Values) is { } failure
            ? new SemanticRejected(world, SemanticRejectionCategory.Validation, null, failure.Message) { ValidationFailures = [failure] }
            : null;
    }

    SemanticRejected? RefuseInvalidRequest() =>
        SemanticEvaluator.ValidateRequest(plan, command, request.Values) is { } contractRejection ? Contract(contractRejection) : null;

    SemanticRejected? RefuseAllocatedSourceType()
    {
        // A generated identifier is never a fallback for a destination-less plain production.
        var generatedAllocation = command.Properties.Any(property => property.IsGenerated && property.IsIdentifier) && command.Produces.Any(produced => produced.Destination is null);
        _defaultDestination = generatedAllocation ? null : command.Destination;
        return AllocatedSourceTypeDiffers() ? Contract("Allocated event source type differs from the command identifier type.") : null;
    }

    bool AllocatedSourceTypeDiffers() =>
        plan.Model.SemanticVersion != SemanticVersion.V1 && _defaultDestination is null &&
        request.AllocatedEventSourceType is { } suppliedType &&
        command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type is { } identityType && suppliedType != identityType;

    SemanticExecutionResult? Validate()
    {
        var failures = SemanticEvaluator.ValidateRules(plan, command, request.Values).ToBuilder();
        if (ValidateBodies(failures) is { } unsupported) return unsupported;
        _commandValues = request.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value);

        // Preserve legacy failure collection, but never evaluate a protected requirement before its reads.
        if (!command.Reads.IsDefaultOrEmpty)
        {
            if (failures.Count > 0) return ValidationFailure(failures);
            _dependencies = SemanticDecisionDependencies.Capture(plan, command, _commandValues);
            if (ReadRequired() is { } readRefusal) return readRefusal;
        }

        AddUnmetRequirements(failures);
        if (failures.Count > 0) return ValidationFailure(failures);

        return NeedsOccurrenceUpFront() ? Contract(NeedsOccurrenceMessage) : null;
    }

    SemanticUnsupported? ValidateBodies(ImmutableArray<SemanticValidationFailure>.Builder failures)
    {
        if (ruleBodies is null) return null;
        foreach (var check in new SemanticRuleBodyValidation(plan.Model.Application, ruleBodies, request).Check(command, cancellationToken))
        {
            if (check.Status == SemanticRuleStatus.NotEvaluable) return new SemanticUnsupported(world, SemanticExecutionCapability.ImplementationBody, check.Message);
            if (check.Status == SemanticRuleStatus.Failed) failures.Add(new(check.Message, check.Rule.Severity));
        }

        return null;
    }

    SemanticRejected ValidationFailure(ImmutableArray<SemanticValidationFailure>.Builder failures) =>
        SemanticEvaluator.RejectWithMessage(world, SemanticRejectionCategory.Validation, null, failures[0].Message) with { ValidationFailures = failures.ToImmutable() };

    void AddUnmetRequirements(ImmutableArray<SemanticValidationFailure>.Builder failures)
    {
        foreach (var requirement in command.Requirements)
        {
            if (!SemanticConditionEvaluator.Evaluate(requirement.Condition, _commandValues, _dateTypes, _reads))
            {
                failures.Add(new(requirement.Message ?? "Command requirement was not met.", requirement.Severity));
            }
        }
    }

    bool NeedsOccurrenceUpFront() =>
        command.Handler is null && !plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V6) &&
        command.Produces.Any(HasContextMapping) && request.Occurrence is null;

    SemanticExecutionResult? PrepareProduction()
    {
        if (ResolveRoutes() is { } routeRejection)
        {
            return routeRejection;
        }

        return Generate();
    }

    SemanticRejected? ResolveRoutes()
    {
        _routes = new(ReferenceEqualityComparer.Instance);
        foreach (var declaredRoute in command.Produces.Select(produced => produced.Route).Append(command.Route).OfType<SemanticCommandRoute>())
        {
            var (source, stream) = SemanticEventRouting.Resolve(plan.Model.Application, declaredRoute.Source, declaredRoute.Stream);
            var scalar = declaredRoute.StreamId is null ? null : Evaluate(declaredRoute.StreamId);
            var parts = declaredRoute.StreamIdParts.Select(part => new SemanticFixtureRoutePart(part.Part, Evaluate(part.Value))).ToImmutableArray();
            if (!SemanticEventRouting.TryFormat(source, stream, scalar, parts, plan.Model.Application.Concepts, out var route, out var failure))
            {
                return Contract(SemanticStreamIdFormatter.FailureMessage(failure));
            }

            _routes[declaredRoute] = route!;
        }

        return null;
    }

    SemanticValue Evaluate(SemanticExpression expression, SemanticCommandOccurrence? occurrence = null) =>
        expression is SemanticReadPathExpression read
            ? _reads[read.Read].GetValueOrDefault(read.PropertyId, SemanticValue.Null)
            : SemanticEvaluator.Evaluate(expression, SemanticExpressionRootKind.Command, _commandValues, occurrence);

    bool IsSkipped(SemanticProducedEvent produced) =>
        (produced.When is not null && !SemanticConditionEvaluator.Evaluate(produced.When, _commandValues, dateTypes: _dateTypes)) ||
        (produced.Condition is not null && Evaluate(produced.Condition) is SemanticBooleanValue { Value: false });

    SemanticExecutionResult? Produce(SemanticProducedEvent produced, ImmutableArray<SemanticPropertyValue>? handlerValues = null)
    {
        if (IsSkipped(produced))
        {
            return null;
        }

        if (RefuseOccurrence(produced) is { } occurrenceRefusal)
        {
            return occurrenceRefusal;
        }

        var destinationExpression = produced.Destination ?? _defaultDestination?.Value;
        var destination = destinationExpression is null
            ? request.AllocatedIdentities.GetValueOrDefault(command.Id)
            : Evaluate(destinationExpression);
        if (destination is null)
        {
            return new SemanticUnsupported(
                world,
                SemanticExecutionCapability.IdentityAllocation,
                $"Command '{command.Name}' requires one deterministic allocated destination.");
        }

        if (plan.Model.SemanticVersion != SemanticVersion.V1 && destinationExpression is null &&
            _defaultDestination is null && request.AllocatedEventSourceType is null)
        {
            return Contract("A v2 allocated event source requires its declared scalar identity type.");
        }

        var values = handlerValues ?? [.. produced.Mappings
            .Select(mapping => new SemanticPropertyValue(mapping.TargetProperty, Evaluate(mapping.Source, request.Occurrence)))];
        if (handlerValues is null && HasContextMapping(produced) && ValidateContextMappings(produced, values) is { } mappingRefusal)
        {
            return mappingRefusal;
        }

        _facts.Add(CreateFact(produced, destinationExpression, destination, values));
        return null;
    }

    SemanticExecutionResult? RefuseOccurrence(SemanticProducedEvent produced)
    {
        if (command.Handler is not null) return null;
        if (request.Occurrence is null && HasContextMapping(produced))
        {
            return Contract(NeedsOccurrenceMessage);
        }

        return request.Occurrence is { IsTimeOnly: true } && NeedsCallerIdentity(produced)
            ? new SemanticUnsupported(world, SemanticExecutionCapability.Command, "The occurrence supplies time only; $context.causedBy needs a caller audit identity the reference does not model.")
            : null;
    }

    SemanticRejected? ValidateContextMappings(SemanticProducedEvent produced, ImmutableArray<SemanticPropertyValue> values)
    {
        var validator = new SemanticValueValidator(
            plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
            plan.Model.Application.Types.ToDictionary(type => type.Id));
        var properties = plan.Events[produced.EventContract].Properties.ToDictionary(property => property.Id);
        foreach (var mapped in produced.Mappings.Zip(values).Where(pair => pair.First.Source is SemanticEventContextExpression))
        {
            try
            {
                validator.Validate(mapped.Second.Value, properties[mapped.Second.TargetProperty].Type, "event occurrence mapping");
            }
            catch (InvalidSemanticContract exception)
            {
                return Contract(exception.Message);
            }
        }

        return null;
    }

    SemanticFact CreateFact(
        SemanticProducedEvent produced,
        SemanticExpression? destinationExpression,
        SemanticValue destination,
        ImmutableArray<SemanticPropertyValue> values) =>
        new(produced.EventContract, destination, values)
        {
            Context = plan.Model.SemanticVersion != SemanticVersion.V1
                ? new(new(SemanticEvaluator.DestinationType(command, destinationExpression, request.AllocatedEventSourceType), destination))
                : null,
            Tags = plan.Events[produced.EventContract].Tags.AddRange(produced.Tags),
            Occurred = request.Occurrence?.Occurred,
            ReactionOrigin = request.ReactionOrigin,
            Route = (produced.Route ?? command.Route) is { } effectiveRoute ? _routes[effectiveRoute] : null
        };

    SemanticExecutionResult SettleWithProvider(SemanticWorld commitWorld, SemanticExecutionResponse? response)
    {
        if (commitRun is null ||
            (!SemanticEvaluator.AllReducers(plan).Any(reducer => _facts.Any(fact => SemanticReducerBodyExecution.Reached(reducer, fact, plan))) &&
             !request.Queries.Any(query => policies.HasProvider && plan.Queries.TryGetValue(query.Query, out var declared) && SemanticPolicyBodyExecution.RequiredBy(plan, declared.Authorization))))
        {
            return Settle(commitWorld, response, reducers, policies);
        }
        using var run = commitRun();
        return Settle(commitWorld, response, run.Evaluator.ReducerBodies, run.Evaluator.PolicyBodies);
    }

    SemanticExecutionResult Settle(SemanticWorld world, SemanticExecutionResponse? response, SemanticReducerBodyExecution reducerExecution, SemanticPolicyBodyExecution policyExecution)
    {
        // A violation is an outcome, not a failure: the command is rejected and the world is unchanged.
        if (RejectReusedGeneratedSource(world) is { } reused) return reused;
        if (request.CollectRefusalDetails)
        {
            var violations = SemanticConstraintEnforcement.FindViolations(plan, world, _facts.ToImmutable()).ToArray();
            if (violations.Length > 0)
            {
                return SemanticEvaluator.RejectWithMessage(world, SemanticRejectionCategory.Constraint, violations[0].Name, SemanticConstraintEnforcement.MessageFor(violations[0])) with
                {
                    ConstraintFailures = [.. violations.Select(constraint => new SemanticConstraintFailure(constraint.Name, SemanticConstraintEnforcement.MessageFor(constraint)))]
                };
            }
        }
        else if (SemanticConstraintEnforcement.FindViolation(plan, world, _facts.ToImmutable()) is { } violated)
        {
            return SemanticEvaluator.RejectWithMessage(world, SemanticRejectionCategory.Constraint, violated.Name, SemanticConstraintEnforcement.MessageFor(violated));
        }

        if (SemanticEvaluator.ReachedReducer(plan, _facts.ToImmutable(), reducerExecution) is { } reachedReducer)
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, $"Reducer '{reachedReducer.Name}' has opaque transitions and requires a target provider.");
        }

        if (!SemanticEvaluator.TryProject(plan, world.Facts, new SemanticEvaluator.ProjectionState(world.ReadModels, world.PendingChildren), _facts.ToImmutable(), out var state, out var projectionFailure, reducerExecution, cancellationToken))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, projectionFailure!);
        }

        if (!SemanticEventPublication.TryPublish(plan, world.Facts, _facts.ToImmutable(), out var publications, out var publicationFailure))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, publicationFailure!);
        }

        var facts = _facts.ToImmutable().AddRange(publications);
        if (SemanticDeferredProjections.FindFailure(plan, facts) is { } deferredFailure)
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, deferredFailure);
        }

        var tentative = world.Commit(facts, state.ReadModels, state.PendingChildren);
        var execution = SemanticEvaluator.ExecuteQueries(plan, world, tentative, facts, request.Queries, request.Caller, reducerExecution, policyExecution, request.Tenant, request.Occurrence?.Occurred, cancellationToken);
        return execution is SemanticAccepted accepted ? accepted with { Response = response } : execution;
    }
}
