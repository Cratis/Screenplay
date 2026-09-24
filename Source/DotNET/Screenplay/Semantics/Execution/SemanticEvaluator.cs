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

            return ExecuteQueries(plan, world, world, [], request.Queries);
        }

        if (!plan.Commands.TryGetValue(request.Command, out var command))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Command, $"Command '{request.Command}' is not in the execution plan.");
        }

        if (request.Queries.IsDefault)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "Execution request query collection cannot be default.");
        }

        if (ValidateRequest(plan, command, request.Values) is { } contractRejection)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, contractRejection);
        }

        if (plan.Model.SemanticVersion == SemanticVersion.V2 && command.Destination is null &&
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

            if (plan.Model.SemanticVersion == SemanticVersion.V2 && destinationExpression is null &&
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
                Context = plan.Model.SemanticVersion == SemanticVersion.V2
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
        return ExecuteQueries(plan, world, tentative, facts.ToImmutable(), request.Queries);
    }

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
        ImmutableArray<SemanticQueryRequest> queries)
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

        return new SemanticAccepted(tentative, facts, queryResults.ToImmutable());
    }

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
