// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

public sealed partial class SemanticEvaluator
{
    internal static string? ValidateRequest(
        SemanticExecutionPlan plan,
        SemanticCommand command,
        ImmutableArray<SemanticPropertyValue> values)
    {
        if (HasMalformedShape(command, values))
        {
            return $"Command '{command.Name}' values do not match its exact property shape.";
        }

        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        var validator = new SemanticValueValidator(concepts, types);
        var valuesByTarget = values.ToDictionary(_ => _.TargetProperty);
        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
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

    internal static string? UnsupportedValidation(SemanticExecutionPlan plan, SemanticCommand command)
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
        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
        {
            if (PredicateInType(property.Type, concepts, types, []) is { } name)
            {
                return name;
            }
        }

        return null;
    }

    internal static ImmutableArray<SemanticValidationFailure> ValidateRules(
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
        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
        {
            ValidateConceptValues(concepts, types, property.Type, valuesByTarget[property.Id], failures);
        }

        return failures.ToImmutable();
    }

    // A concept's rules constrain every value of the concept - directly, as each element of a collection and
    // inside composite values - so they are applied wherever a command or query carries one.
    internal static void ValidateConceptValues(
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

    internal static string? ValidateQueryKey(
        SemanticExecutionPlan plan,
        SemanticKeyedQuery query,
        SemanticValue key)
    {
        try
        {
            var validator = new SemanticValueValidator(
                plan.Model.Application.Concepts.ToDictionary(_ => _.Id),
                plan.Model.Application.Types.ToDictionary(_ => _.Id));
            if (query.Argument is not null) validator.Validate(key, query.Argument.Type, $"query '{query.Name}' key");
            return null;
        }
        catch (InvalidSemanticContract exception)
        {
            return exception.Message;
        }
    }

    internal static string? ValidateQueryFilters(SemanticExecutionPlan plan, SemanticKeyedQuery query, ImmutableArray<SemanticQueryFilterValue> filters)
    {
        try
        {
            var validator = new SemanticValueValidator(plan.Model.Application.Concepts.ToDictionary(concept => concept.Id), plan.Model.Application.Types.ToDictionary(type => type.Id));
            SemanticQueryFilterRules.ValidateValues(query, filters, validator.Validate);
            return null;
        }
        catch (InvalidSemanticContract exception)
        {
            return exception.Message;
        }
    }

    internal static string? UnsupportedQueryValidation(SemanticExecutionPlan plan, SemanticKeyedQuery query, ImmutableArray<SemanticQueryFilterValue> filters)
    {
        var concepts = plan.Model.Application.Concepts.ToDictionary(concept => concept.Id);
        var types = plan.Model.Application.Types.ToDictionary(type => type.Id);
        var arguments = QueryConceptArguments(query, SemanticValue.Null, filters);
        return arguments.Select(argument => PredicateInType(argument.Type, concepts, types, [])).FirstOrDefault(name => name is not null);
    }

    internal static ImmutableArray<SemanticValidationFailure> ValidateQueryConcepts(
        SemanticExecutionPlan plan,
        SemanticKeyedQuery query,
        SemanticValue key,
        ImmutableArray<SemanticQueryFilterValue> filters)
    {
        var concepts = plan.Model.Application.Concepts.ToDictionary(concept => concept.Id);
        var types = plan.Model.Application.Types.ToDictionary(type => type.Id);
        var failures = ImmutableArray.CreateBuilder<SemanticValidationFailure>();
        foreach (var argument in QueryConceptArguments(query, key, filters))
        {
            ValidateConceptValues(concepts, types, argument.Type, argument.Value, failures);
        }

        return failures.ToImmutable();
    }

    static IEnumerable<(SemanticTypeReference Type, SemanticValue Value)> QueryConceptArguments(
        SemanticKeyedQuery query,
        SemanticValue key,
        ImmutableArray<SemanticQueryFilterValue> filters)
    {
        if (query.Argument is { } argument) yield return (argument.Type, key);
        foreach (var filter in query.Filters.Where(filter => filters.Any(value => value.Filter == filter.Id)))
        {
            yield return (filter.Type, filters.Single(value => value.Filter == filter.Id).Value);
        }
    }

    static bool HasMalformedShape(SemanticCommand command, ImmutableArray<SemanticPropertyValue> values) =>
        values.IsDefault || values.Any(_ => _ is null) || values.Length != command.Properties.Count(property => !property.IsGenerated) ||
        values.Select(_ => _.TargetProperty).Distinct().Count() != values.Length;

    static string? PredicateInType(
        SemanticTypeReference type,
        Dictionary<SemanticId, SemanticConcept> concepts,
        Dictionary<SemanticId, SemanticCompositeType> types,
        HashSet<SemanticId> visited)
    {
        if (type.Kind == SemanticTypeReferenceKind.Concept)
        {
            return PredicateInConcept(concepts[type.Target]);
        }

        if (type.Kind == SemanticTypeReferenceKind.CompositeType && visited.Add(type.Target))
        {
            return PredicateInComposite(types[type.Target], concepts, types, visited);
        }

        return null;
    }

    static string? PredicateInConcept(SemanticConcept concept)
    {
        if (concept.Validations.FirstOrDefault(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation) is not { } rule)
        {
            return null;
        }

        return rule.Kind == SemanticValidationRuleKind.CodeValidation
            ? $"Concept '{concept.Name}' code validation {concept.Validations.Take(concept.Validations.IndexOf(rule)).Count(validation => validation.Kind == SemanticValidationRuleKind.CodeValidation)}"
            : $"Rule '{rule.Name}' on concept '{concept.Name}'";
    }

    static string? PredicateInComposite(
        SemanticCompositeType composite,
        Dictionary<SemanticId, SemanticConcept> concepts,
        Dictionary<SemanticId, SemanticCompositeType> types,
        HashSet<SemanticId> visited)
    {
        foreach (var property in composite.Properties)
        {
            if (PredicateInType(property.Type, concepts, types, visited) is { } name) return name;
        }

        return null;
    }
}
