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

        if (ValidateRules(plan, command, request.Values) is { } validationRejection)
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Validation, null, validationRejection);
        }

        var commandValues = request.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
        var facts = ImmutableArray.CreateBuilder<SemanticFact>();
        foreach (var produced in command.Produces)
        {
            var destination = produced.Destination is null
                ? request.AllocatedIdentities.GetValueOrDefault(command.Id)
                : Evaluate(produced.Destination, SemanticExpressionRootKind.Command, commandValues);
            if (destination is null)
            {
                return new SemanticUnsupported(
                    world,
                    SemanticExecutionCapability.IdentityAllocation,
                    $"Command '{command.Name}' requires one deterministic allocated destination.");
            }

            var values = produced.Mappings
                .Select(mapping => new SemanticPropertyValue(
                    mapping.TargetProperty,
                    Evaluate(mapping.Source, SemanticExpressionRootKind.Command, commandValues)))
                .ToImmutableArray();
            facts.Add(new(produced.EventContract, destination, values));
        }

        if (!TryProject(plan, world.ReadModels, facts.ToImmutable(), out var readModels, out var projectionFailure))
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, projectionFailure!);
        }

        var tentative = world.Commit(facts.ToImmutable(), readModels);
        var queryResults = ImmutableArray.CreateBuilder<SemanticQueryResult>();
        foreach (var queryRequest in request.Queries)
        {
            if (!plan.Queries.TryGetValue(queryRequest.Query, out var query))
            {
                return new SemanticUnsupported(world, SemanticExecutionCapability.Query, $"Query '{queryRequest.Query}' is not in the execution plan.");
            }

            if (ValidateQueryKey(plan, query, queryRequest.Key) is { } queryRejection)
            {
                return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, queryRejection);
            }

            var results = tentative.ReadModels
                .Where(instance => instance.ReadModel == query.ReadModel)
                .Where(instance => instance.Values.Any(value =>
                    value.TargetProperty == query.KeyProperty && SemanticValueRules.AreEqual(value.Value, queryRequest.Key)))
                .ToImmutableArray();
            queryResults.Add(new(query.Id, queryRequest.Key, results));
        }

        return new SemanticAccepted(tentative, facts.ToImmutable(), queryResults.ToImmutable());
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

    static string? ValidateRules(
        SemanticExecutionPlan plan,
        SemanticCommand command,
        ImmutableArray<SemanticPropertyValue> values)
    {
        var valuesByTarget = values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
        foreach (var validation in command.Validations)
        {
            var value = valuesByTarget[validation.Property];
            if (validation.Kind == SemanticValidationRuleKind.NotEmpty && IsEmpty(value))
            {
                return validation.Message ?? "A required value is empty.";
            }
        }

        foreach (var property in command.Properties.Where(_ => _.Type.Kind == SemanticTypeReferenceKind.Concept))
        {
            var concept = plan.Model.Application.Concepts.Single(_ => _.Id == property.Type.Target);
            foreach (var validation in concept.Validations)
            {
                if (validation.Kind == SemanticValidationRuleKind.NotEmpty && IsEmpty(valuesByTarget[property.Id]))
                {
                    return validation.Message ?? "A required concept value is empty.";
                }
            }
        }

        return null;
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

    static bool IsEmpty(SemanticValue value) => value switch
    {
        SemanticNullValue => true,
        SemanticTextValue text => string.IsNullOrEmpty(text.Value),
        SemanticArrayValue array => array.Values.IsEmpty,
        _ => false
    };

    static bool TryProject(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticReadModelInstance> current,
        ImmutableArray<SemanticFact> facts,
        out ImmutableArray<SemanticReadModelInstance> readModels,
        out string? failure)
    {
        var instances = current.ToList();
        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        var validator = new SemanticValueValidator(concepts, types);
        foreach (var fact in facts)
        {
            foreach (var projection in plan.Projections.Values.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal))
            {
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
                            failure = $"Projection '{projection.Name}' did not establish required read-model property '{property.Name}'.";
                            readModels = current;
                            return false;
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
        }

        failure = null;
        readModels = [.. instances];
        return true;
    }

    static SemanticValue Evaluate(
        SemanticExpression expression,
        SemanticExpressionRootKind expectedRoot,
        Dictionary<SemanticId, SemanticValue> values) => expression switch
    {
        SemanticValueExpression literal => literal.Value,
        SemanticResolvedExpression resolved when resolved.Root == expectedRoot && resolved.Source == SemanticExpressionSourceKind.Property && values.TryGetValue(resolved.Target, out var value) => value,
        _ => throw new InvalidSemanticContract("An execution expression is unresolved in its declared root scope.")
    };
}
