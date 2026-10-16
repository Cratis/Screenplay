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
        foreach (var property in command.Properties)
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
            var property = command.Properties.Single(property => property.Id == validation.Property);
            var primitive = SemanticCommandDateComparisons.Primitive(plan.Model.Application, property.Type);
            var check = SemanticDateComparison.IsDate(primitive) && SemanticDateComparison.IsComparison(validation.Kind)
                ? new SemanticRuleCheck(
                    SemanticDateComparison.Satisfies(validation, value, primitive) ? SemanticRuleStatus.Satisfied : SemanticRuleStatus.Failed,
                    validation,
                    validation.Message ?? SemanticValidationRules.DefaultMessage(validation, value, "A required value is empty."))
                : SemanticValidationRules.Check(validation, value);
            if (check.Status == SemanticRuleStatus.Failed)
            {
                failures.Add(new(check.Message, validation.Severity));
            }
        }

        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
        {
            AddConceptFailures(plan.Model.Application, property.Type, valuesByTarget[property.Id], failures);
        }

        return failures.ToImmutable();
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
        var failures = ImmutableArray.CreateBuilder<SemanticValidationFailure>();
        foreach (var argument in QueryConceptArguments(query, key, filters))
        {
            AddConceptFailures(plan.Model.Application, argument.Type, argument.Value, failures);
        }

        return failures.ToImmutable();
    }

    static void AddConceptFailures(
        SemanticApplication application,
        SemanticTypeReference type,
        SemanticValue value,
        ImmutableArray<SemanticValidationFailure>.Builder failures) =>
        failures.AddRange(SemanticValidationRules.Check(application, type, value)
            .Where(check => check.Status == SemanticRuleStatus.Failed)
            .Select(check => new SemanticValidationFailure(check.Message, check.Rule.Severity)));

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
