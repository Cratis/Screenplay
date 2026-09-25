// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Executes the minimum portable semantic plan against immutable in-memory world state.
/// </summary>
public sealed class SemanticEvaluator : ISemanticEvaluator
{
    /// <inheritdoc/>
    public SemanticExecutionResult Execute(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticExecutionRequest request)
    {
        if (request.IsReadOnly)
        {
            if (request.Command.IsSet || !request.Values.IsEmpty || !request.AllocatedIdentities.IsEmpty)
            {
                return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "A read-only request cannot carry command values or allocated identities.");
            }

            return ExecuteQueries(plan, world, world, [], request.Queries, request.Caller);
        }

        if (!plan.Commands.TryGetValue(request.Command, out var command))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Command, $"Command '{request.Command}' is not in the execution plan.");
        }

        if (request.Queries.IsDefault)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "Execution request query collection cannot be default.");
        }

        var authorizationValues = request.Values.IsDefault ? [] : request.Values.Where(value => value is not null).ToArray();
        var artifact = command.Properties
            .Select(property => (property, value: authorizationValues.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value))
            .Where(pair => pair.value is not null)
            .ToDictionary(pair => pair.property.Name, pair => pair.value!, StringComparer.Ordinal);
        var subject = command.Properties.Where(property => property.IsIdentifier)
            .Select(property => authorizationValues.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value)
            .FirstOrDefault(value => value is not null);
        var authorization = SemanticPolicyEvaluation.Evaluate(command.Authorization, plan, request.Caller, artifact, subject, command.Properties);
        if (authorization.Outcome == SemanticPolicyOutcome.Unsupported)
        {
            return new SemanticUnsupported(
                world,
                SemanticExecutionCapability.Authorization,
                $"Policy '{authorization.Policy}' has an opaque predicate and requires a target provider.");
        }
        if (authorization.Outcome == SemanticPolicyOutcome.Deny)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Unauthorized, null, "Caller is not authorized.");
        }

        // Ordering between generated declarative validators and attached code is not portable.
        // Refuse to infer a validation outcome after authorization, before evaluating any validators.
        if (UnsupportedValidation(plan, command) is { } rule)
        {
            return new SemanticUnsupported(
                world,
                SemanticExecutionCapability.Command,
                $"{rule} has an opaque validation predicate and requires a target provider.");
        }

        if (ValidateRequest(plan, command, request.Values) is { } contractRejection)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, contractRejection);
        }

        if (plan.Model.SemanticVersion != SemanticVersion.V1 && command.Destination is null &&
            request.AllocatedEventSourceType is { } suppliedType &&
            command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type is { } identityType && suppliedType != identityType)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "Allocated event source type differs from the command identifier type.");
        }

        var failures = ValidateRules(plan, command, request.Values).ToBuilder();
        var commandValues = request.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
        foreach (var requirement in command.Requirements)
        {
            if (!SemanticConditionEvaluation.Evaluate(requirement.Condition, commandValues))
            {
                failures.Add(new(requirement.Message ?? "Command requirement was not met.", requirement.Severity));
            }
        }

        if (failures.Count > 0)
        {
            var message = failures[0].Message;
            return RejectWithMessage(world, SemanticRejectionCategory.Validation, null, message) with { ValidationFailures = failures.ToImmutable() };
        }

        if (command.Produces.Any(produced => produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression)) && request.Occurrence is null)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "A v2 command using $context needs an occurrence supplied by the execution request.");
        }

        var facts = ImmutableArray.CreateBuilder<SemanticFact>();
        foreach (var produced in command.Produces)
        {
            if ((produced.When is not null && !SemanticConditionEvaluation.Evaluate(produced.When, commandValues)) ||
                (produced.Condition is not null && Evaluate(produced.Condition, SemanticExpressionRootKind.Command, commandValues) is SemanticBooleanValue { Value: false }))
            {
                continue;
            }

            var destinationExpression = produced.Destination ?? command.Destination?.Value;
            var destination = destinationExpression is null
                ? request.AllocatedIdentities.GetValueOrDefault(command.Id)
                : Evaluate(destinationExpression, SemanticExpressionRootKind.Command, commandValues);
            if (destination is null)
            {
                return new SemanticUnsupported(
                    world,
                    SemanticExecutionCapability.IdentityAllocation,
                    $"Command '{command.Name}' requires one deterministic allocated destination.");
            }

            if (plan.Model.SemanticVersion != SemanticVersion.V1 && destinationExpression is null &&
                command.Destination is null && request.AllocatedEventSourceType is null)
            {
                return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "A v2 allocated event source requires its declared scalar identity type.");
            }

            var values = produced.Mappings
                .Select(mapping => new SemanticPropertyValue(
                    mapping.TargetProperty,
                    Evaluate(mapping.Source, SemanticExpressionRootKind.Command, commandValues, request.Occurrence)))
                .ToImmutableArray();
            if (produced.Mappings.Any(mapping => mapping.Source is SemanticEventContextExpression))
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
                        return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, exception.Message);
                    }
                }
            }

            facts.Add(new SemanticFact(produced.EventContract, destination, values)
            {
                Context = plan.Model.SemanticVersion != SemanticVersion.V1
                    ? new(new(DestinationType(command, destinationExpression, request.AllocatedEventSourceType), destination))
                    : null,
                Tags = plan.Events[produced.EventContract].Tags.AddRange(produced.Tags)
            });
        }

        // A violation is an outcome, not a failure: the command is rejected and the world is unchanged.
        if (SemanticConstraintEnforcement.FindViolation(plan, world, facts.ToImmutable()) is { } violated)
        {
            return RejectWithMessage(world, SemanticRejectionCategory.Constraint, violated.Name, SemanticConstraintEnforcement.MessageFor(violated));
        }

        if (!TryProject(plan, world.Facts, world.ReadModels, facts.ToImmutable(), out var readModels, out var projectionFailure))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, projectionFailure!);
        }

        var tentative = world.Commit(facts.ToImmutable(), readModels);
        return ExecuteQueries(plan, world, tentative, facts.ToImmutable(), request.Queries, request.Caller);
    }

    /// <summary>
    /// Establishes reference world state from existing facts in occurrence order.
    /// </summary>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="facts">The ordered existing facts; no read-model snapshots are required.</param>
    /// <returns>An accepted established world, a contract rejection, or an unsupported projection.</returns>
    public SemanticExecutionResult EstablishWorld(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts)
    {
        try
        {
            // Validate the entire history before any projection can observe an invalid occurrence.
            if (!facts.IsDefault && facts.Any(fact => fact is { Tags.IsDefault: true } or { Context.EventSource: null }))
            {
                return new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Contract, null, "Fact occurrence metadata is malformed.");
            }

            SemanticWorld.Create(facts, []);
            var validator = new SemanticValueValidator(
                plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
                plan.Model.Application.Types.ToDictionary(type => type.Id));
            foreach (var fact in facts)
            {
                if (!plan.Events.TryGetValue(fact.EventContract, out var eventContract) ||
                    fact.Values.Length != eventContract.Properties.Length ||
                    fact.Values.Select(value => value.TargetProperty).Distinct().Count() != fact.Values.Length)
                {
                    return new SemanticRejected(
                        SemanticWorld.Empty,
                        SemanticRejectionCategory.Contract,
                        null,
                        $"Fact '{fact.EventContract}' does not match a known event contract and its exact property shape.");
                }

                var values = fact.Values.ToDictionary(value => value.TargetProperty);
                foreach (var property in eventContract.Properties)
                {
                    if (!values.TryGetValue(property.Id, out var value))
                    {
                        return new SemanticRejected(
                            SemanticWorld.Empty,
                            SemanticRejectionCategory.Contract,
                            null,
                            $"Fact '{fact.EventContract}' is missing event property '{property.Name}'.");
                    }

                    validator.Validate(value.Value, property.Type, $"event property '{property.Name}'");
                }

                validator.ValidateVariant(fact.Destination);
                if (fact.Context is { } context)
                {
                    validator.Validate(context.EventSource.Value, context.EventSource.Type, "event source identity");
                }
            }
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Contract, null, exception.Message);
        }

        var observedEvents = facts.Select(fact => fact.EventContract).ToHashSet();
        var reducer = plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features))
            .FirstOrDefault(candidate =>
                candidate.Transitions.Any(transition => observedEvents.Contains(transition.EventContract)) ||
                (observedEvents.Count > 0 && plan.Projections.Values.Any(projection => projection.ReadModel == candidate.ReadModel &&
                    projection.GetAffectedInstances().Any(affected => affected.EventContract is null || observedEvents.Contains(affected.EventContract.Value)))));
        if (reducer is not null)
        {
            return new SemanticUnsupported(
                SemanticWorld.Empty,
                SemanticExecutionCapability.Projection,
                $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.");
        }

        try
        {
            if (!TryProject(plan, [], [], facts, out var readModels, out var failure))
            {
                return new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.Projection, failure!);
            }

            return new SemanticAccepted(SemanticWorld.Create(facts, readModels), facts, []);
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.Projection, exception.Message);
        }
    }

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

        if (!TryProject(plan, world.Facts, world.ReadModels, facts, out var readModels, out var failure))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, failure!);
        }

        return ExecuteQueries(plan, world, world.Commit(facts, readModels), facts, queries, caller);
    }

    // Existing internal callers use this to project from supplied snapshots. The public establishment operation
    // intentionally starts from empty state so it can validate the complete supplied history first.
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

    static SemanticRejected RejectWithMessage(SemanticWorld world, SemanticRejectionCategory category, string? code, string message) =>
        new(world, category, code, message) { MessageIsStringKey = message.StartsWith("$strings.", StringComparison.Ordinal) };

    static SemanticExecutionResult ExecuteQueries(
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

        var queryResults = ImmutableArray.CreateBuilder<SemanticQueryResult>();
        foreach (var queryRequest in queries)
        {
            if (!plan.Queries.TryGetValue(queryRequest.Query, out var query))
            {
                return new SemanticUnsupported(original, SemanticExecutionCapability.Query, $"Query '{queryRequest.Query}' is not in the execution plan.");
            }

            if (ReducerFor(plan, query.ReadModel) is { } reducer)
            {
                return new SemanticUnsupported(
                    original,
                    SemanticExecutionCapability.Query,
                    $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.");
            }

            var authorization = SemanticPolicyEvaluation.Evaluate(
                query.Authorization,
                plan,
                caller,
                new Dictionary<string, SemanticValue>(StringComparer.Ordinal) { [query.Argument.Name] = queryRequest.Key },
                queryRequest.Key,
                [new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)]);
            if (authorization.Outcome == SemanticPolicyOutcome.Unsupported)
            {
                return new SemanticUnsupported(
                    original,
                    SemanticExecutionCapability.Authorization,
                    $"Policy '{authorization.Policy}' has an opaque predicate and requires a target provider.");
            }
            if (authorization.Outcome == SemanticPolicyOutcome.Deny)
            {
                return new SemanticRejected(original, SemanticRejectionCategory.Unauthorized, null, "Caller is not authorized.");
            }

            if (ValidateQueryKey(plan, query, queryRequest.Key) is { } queryRejection)
            {
                return new SemanticRejected(original, SemanticRejectionCategory.Contract, null, queryRejection);
            }

            var results = tentative.ReadModels
                .Where(instance => instance.ReadModel == query.ReadModel)
                .Where(instance => instance.Values.Any(value =>
                    value.TargetProperty == query.KeyProperty && SemanticValueRules.AreEqual(value.Value, queryRequest.Key)))
                .ToImmutableArray();
            queryResults.Add(new(query.Id, queryRequest.Key, results));
        }

        foreach (var instance in tentative.ReadModels)
        {
            if (ReducerFor(plan, instance.ReadModel) is { } reducer)
            {
                return new SemanticUnsupported(
                    original,
                    SemanticExecutionCapability.Projection,
                    $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.");
            }
        }

        return new SemanticAccepted(tentative, facts, queryResults.ToImmutable());
    }

    static SemanticReducer? ReducerFor(SemanticExecutionPlan plan, SemanticId readModel) =>
        plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features))
            .FirstOrDefault(reducer => reducer.ReadModel == readModel);

    static IEnumerable<SemanticReducer> Reducers(ImmutableArray<SemanticFeature> features) =>
        features.SelectMany(feature => feature.Slices.SelectMany(slice => slice.Reducers).Concat(Reducers(feature.Features)));

    static string? ValidateRequest(
        SemanticExecutionPlan plan,
        SemanticCommand command,
        ImmutableArray<SemanticPropertyValue> values)
    {
        if (values.IsDefault || values.Any(_ => _ is null) || values.Length != command.Properties.Length ||
            values.Select(_ => _.TargetProperty).Distinct().Count() != values.Length)
        {
            return $"Command '{command.Name}' values do not match its exact property shape.";
        }

        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        var validator = new SemanticValueValidator(concepts, types);
        var valuesByTarget = values.ToDictionary(_ => _.TargetProperty);
        foreach (var property in command.Properties)
        {
            if (!valuesByTarget.TryGetValue(property.Id, out var value))
            {
                return $"Command '{command.Name}' is missing property '{property.Name}'.";
            }

            try
            {
                validator.Validate(value.Value, property.Type, $"command property '{property.Name}'");
            }
            catch (InvalidSemanticContract exception)
            {
                return exception.Message;
            }
        }

        return null;
    }

    static string? UnsupportedValidation(SemanticExecutionPlan plan, SemanticCommand command)
    {
        if (!command.CodeValidations.IsEmpty)
        {
            return $"Command '{command.Name}' code validation 0";
        }

        if (command.Validations.FirstOrDefault(rule => rule.Kind == SemanticValidationRuleKind.RulePredicate) is { } predicate)
        {
            var property = command.Properties.Single(value => value.Id == predicate.Property);
            return $"Rule '{predicate.Name}' on command '{command.Name}' property '{property.Name}'";
        }

        var concepts = plan.Model.Application.Concepts.ToDictionary(concept => concept.Id);
        var types = plan.Model.Application.Types.ToDictionary(type => type.Id);
        foreach (var property in command.Properties)
        {
            if (PredicateInType(property.Type, concepts, types, []) is { } name)
            {
                return name;
            }
        }

        return null;
    }

    static string? PredicateInType(
        SemanticTypeReference type,
        Dictionary<SemanticId, SemanticConcept> concepts,
        Dictionary<SemanticId, SemanticCompositeType> types,
        HashSet<SemanticId> visited)
    {
        if (type.Kind == SemanticTypeReferenceKind.Concept)
        {
            var concept = concepts[type.Target];
            if (concept.Validations.FirstOrDefault(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation) is { } rule)
            {
                return rule.Kind == SemanticValidationRuleKind.CodeValidation
                    ? $"Concept '{concept.Name}' code validation {concept.Validations.Take(concept.Validations.IndexOf(rule)).Count(validation => validation.Kind == SemanticValidationRuleKind.CodeValidation)}"
                    : $"Rule '{rule.Name}' on concept '{concept.Name}'";
            }

            return null;
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && visited.Add(type.Target))
        {
            foreach (var property in types[type.Target].Properties)
            {
                if (PredicateInType(property.Type, concepts, types, visited) is { } name) return name;
            }
        }

        return null;
    }

    static ImmutableArray<SemanticValidationFailure> ValidateRules(
        SemanticExecutionPlan plan,
        SemanticCommand command,
        ImmutableArray<SemanticPropertyValue> values)
    {
        var valuesByTarget = values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
        var failures = ImmutableArray.CreateBuilder<SemanticValidationFailure>();
        foreach (var validation in command.Validations)
        {
            var value = valuesByTarget[validation.Property];
            if (!SemanticValidationRules.Satisfies(validation, value))
            {
                failures.Add(new(validation.Message ?? SemanticValidationRules.DefaultMessage(validation, value, "A required value is empty."), validation.Severity));
            }
        }

        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        foreach (var property in command.Properties)
        {
            ValidateConceptValues(concepts, types, property.Type, valuesByTarget[property.Id], failures);
        }

        return failures.ToImmutable();
    }

    // A concept's rules constrain every value of the concept - directly, as each element of a collection and
    // inside composite values - so they are applied wherever the command carries one.
    static void ValidateConceptValues(
        Dictionary<SemanticId, SemanticConcept> concepts,
        Dictionary<SemanticId, SemanticCompositeType> types,
        SemanticTypeReference type,
        SemanticValue value,
        ImmutableArray<SemanticValidationFailure>.Builder failures)
    {
        if (type.IsCollection)
        {
            if (value is SemanticArrayValue array)
            {
                var elementType = type with { IsCollection = false, IsOptional = false };
                foreach (var element in array.Values)
                {
                    ValidateConceptValues(concepts, types, elementType, element, failures);
                }
            }

            return;
        }

        switch (type.Kind)
        {
            case SemanticTypeReferenceKind.Concept:
                foreach (var validation in concepts[type.Target].Validations)
                {
                    if (!SemanticValidationRules.Satisfies(validation, value))
                    {
                        failures.Add(new(validation.Message ?? SemanticValidationRules.DefaultMessage(validation, value, "A required concept value is empty."), validation.Severity));
                    }
                }

                break;
            case SemanticTypeReferenceKind.CompositeType when value is SemanticCompositeValue composite:
                var properties = types[type.Target].Properties.ToDictionary(_ => _.Id);
                foreach (var property in composite.Properties)
                {
                    ValidateConceptValues(concepts, types, properties[property.TargetProperty].Type, property.Value, failures);
                }

                break;
        }
    }

    static string? ValidateQueryKey(
        SemanticExecutionPlan plan,
        SemanticKeyedQuery query,
        SemanticValue key)
    {
        try
        {
            var validator = new SemanticValueValidator(
                plan.Model.Application.Concepts.ToDictionary(_ => _.Id),
                plan.Model.Application.Types.ToDictionary(_ => _.Id));
            validator.Validate(key, query.Argument.Type, $"query '{query.Name}' key");
            return null;
        }
        catch (InvalidSemanticContract exception)
        {
            return exception.Message;
        }
    }

    static bool TryProject(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticFact> history,
        ImmutableArray<SemanticReadModelInstance> current,
        ImmutableArray<SemanticFact> facts,
        out ImmutableArray<SemanticReadModelInstance> readModels,
        out string? failure)
    {
        var instances = current.ToList();
        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        var validator = new SemanticValueValidator(concepts, types);
        var observed = history.ToList();

        foreach (var fact in facts)
        {
            foreach (var projection in plan.Projections.Values.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal))
            {
                // A scoped projection runs through the reference semantics of every Chronicle projection block.
                if (projection.Scope is not null)
                {
                    if (new SemanticScopedProjection(plan, projection, validator, observed).Apply(instances, fact) is { } scopedFailure)
                    {
                        failure = scopedFailure;
                        readModels = current;
                        return false;
                    }

                    continue;
                }

                foreach (var transition in projection.Transitions.Where(_ => _.EventContract == fact.EventContract))
                {
                    var eventValues = fact.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
                    var key = Evaluate(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, eventValues);
                    var existing = instances.SingleOrDefault(instance =>
                        instance.ReadModel == projection.ReadModel && SemanticValueRules.AreEqual(instance.Key, key));
                    var state = existing?.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value) ?? [];
                    foreach (var mapping in transition.Mappings)
                    {
                        state[mapping.TargetProperty] = Evaluate(mapping.Source, SemanticExpressionRootKind.Event, eventValues);
                    }

                    var readModel = plan.ReadModels[projection.ReadModel];
                    foreach (var property in readModel.Properties)
                    {
                        if (!state.TryGetValue(property.Id, out var value))
                        {
                            if (!property.Type.IsOptional)
                            {
                                failure = $"Projection '{projection.Name}' did not establish required read-model property '{property.Name}'.";
                                readModels = current;
                                return false;
                            }

                            value = SemanticValue.Null;
                            state[property.Id] = value;
                        }

                        try
                        {
                            validator.Validate(value, property.Type, $"read-model property '{property.Name}'");
                        }
                        catch (InvalidSemanticContract exception)
                        {
                            failure = exception.Message;
                            readModels = current;
                            return false;
                        }
                    }

                    var identifier = readModel.Properties.Single(_ => _.IsIdentifier);
                    if (!SemanticValueRules.AreEqual(state[identifier.Id], key))
                    {
                        failure = $"Projection '{projection.Name}' affected key disagrees with read-model identifier '{identifier.Name}'.";
                        readModels = current;
                        return false;
                    }

                    if (existing is not null)
                    {
                        instances.Remove(existing);
                    }

                    instances.Add(new(
                        readModel.Id,
                        key,
                        [.. readModel.Properties.Select(property => new SemanticPropertyValue(property.Id, state[property.Id]))]));
                }
            }

            observed.Add(fact);
        }

        failure = null;
        readModels = [.. instances];
        return true;
    }

    static SemanticTypeReference DestinationType(SemanticCommand command, SemanticExpression? expression, SemanticTypeReference? allocatedType) =>
        expression is SemanticResolvedExpression resolved
            ? command.Properties.Single(property => property.Id == resolved.Target).Type
            : command.Destination?.Type ?? allocatedType ??
                throw new InvalidSemanticContract("A v2 fact requires a typed state-change destination.");

    static SemanticValue Evaluate(
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
}
