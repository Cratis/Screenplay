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

    internal static string? UnsupportedValidation(SemanticExecutionPlan plan, SemanticCommand command, bool hasRuleProvider = false)
    {
        if (!hasRuleProvider && !command.CodeValidations.IsEmpty)
        {
            return $"Command '{command.Name}' code validation 0";
        }

        if (!hasRuleProvider && command.Validations.FirstOrDefault(rule => rule.Kind == SemanticValidationRuleKind.RulePredicate) is { } predicate)
        {
            var property = command.Properties.Single(value => value.Id == predicate.Property);
            return $"Rule '{predicate.Name}' on command '{command.Name}' property '{property.Name}'";
        }

        var predicates = new SemanticOpaqueValidation(plan.Model.Application);
        return command.Properties.Select(property => predicates.Find(property.Type, hasRuleProvider && !property.IsGenerated)).FirstOrDefault(name => name is not null);
    }

    internal static ImmutableArray<SemanticValidationFailure> ValidateRules(
        SemanticExecutionPlan plan,
        SemanticCommand command,
        ImmutableArray<SemanticPropertyValue> values)
    {
        var valuesByTarget = values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
        var dateTypes = SemanticDateComparisons.Participants(plan.Model.Application, command);
        var failures = ImmutableArray.CreateBuilder<SemanticValidationFailure>();
        foreach (var validation in command.Validations)
        {
            var value = valuesByTarget[validation.Property];
            var check = SemanticDateComparisons.Check(validation, value, dateTypes.GetValueOrDefault(validation.Property));
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
            if (!query.KeyParts.IsEmpty)
            {
                var values = SemanticKeyPartValues.Require(key, query.KeyParts.Select(part => part.KeyProperty));
                foreach (var part in query.KeyParts)
                {
                    validator.Validate(values.Single(value => value.TargetProperty == part.KeyProperty).Value, part.Type, $"query '{query.Name}' key part '{part.Name}'");
                }
            }
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
        var predicates = new SemanticOpaqueValidation(plan.Model.Application);
        var arguments = QueryConceptArguments(query, SemanticValue.Null, filters);
        return arguments.Select(argument => predicates.Find(argument.Type)).FirstOrDefault(name => name is not null);
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
        foreach (var part in query.KeyParts)
        {
            yield return (part.Type, (key as SemanticCompositeValue)?.Properties.FirstOrDefault(value => value.TargetProperty == part.KeyProperty)?.Value ?? SemanticValue.Null);
        }
        foreach (var filter in query.Filters.Where(filter => filters.Any(value => value.Filter == filter.Id)))
        {
            yield return (filter.Type, filters.Single(value => value.Filter == filter.Id).Value);
        }
    }

    static bool HasMalformedShape(SemanticCommand command, ImmutableArray<SemanticPropertyValue> values) =>
        values.IsDefault || values.Any(_ => _ is null) || values.Length != command.Properties.Count(property => !property.IsGenerated) ||
        values.Select(_ => _.TargetProperty).Distinct().Count() != values.Length;
}
