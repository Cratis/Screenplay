// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticRuleBodyValidation(SemanticApplication application, SemanticRuleBodyExecution bodies, SemanticExecutionRequest request)
{
    readonly Dictionary<SemanticId, SemanticConcept> _concepts = application.Concepts.ToDictionary(concept => concept.Id);
    readonly Dictionary<SemanticId, SemanticCompositeType> _types = application.Types.ToDictionary(type => type.Id);

    internal static bool IsPredicate(SemanticValidationRule rule) => rule.Kind == SemanticValidationRuleKind.RulePredicate;

    internal IEnumerable<SemanticRuleCheck> Check(SemanticCommand command)
    {
        var values = request.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        var artifact = SemanticValue.Composite(request.Values);
        foreach (var rule in command.Validations.Where(IsPredicate))
        {
            var property = command.Properties.Single(property => property.Id == rule.Property);
            yield return values.TryGetValue(rule.Property, out var value)
                ? bodies.Check(rule, Context(artifact, value, property.Name))
                : SemanticRuleBodyExecution.Unsupported(rule, "The rule property is unavailable before generation.");
        }

        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
        {
            foreach (var check in CheckConcepts(property.Type, values[property.Id])) yield return check;
        }
    }

    IEnumerable<SemanticRuleCheck> CheckConcepts(SemanticTypeReference type, SemanticValue value)
    {
        if (type.IsCollection)
        {
            if (value is SemanticArrayValue array)
            {
                foreach (var element in array.Values)
                {
                    foreach (var check in CheckConcepts(type with { IsCollection = false, IsOptional = false }, element)) yield return check;
                }
            }
            yield break;
        }

        if (type.Kind == SemanticTypeReferenceKind.Concept)
        {
            foreach (var rule in _concepts[type.Target].Validations.Where(IsPredicate))
            {
                yield return bodies.Check(rule, Context(value, value, string.Empty)) with { Concept = type.Target };
            }
        }
        else if (type.Kind == SemanticTypeReferenceKind.CompositeType && value is SemanticCompositeValue composite)
        {
            var properties = _types[type.Target].Properties.ToDictionary(property => property.Id);
            foreach (var property in composite.Properties)
            {
                foreach (var check in CheckConcepts(properties[property.TargetProperty].Type, property.Value)) yield return check;
            }
        }
    }

    SemanticRuleContext Context(SemanticValue artifact, SemanticValue value, string property) =>
        new(
            artifact,
            value,
            property,
            request.Tenant,
            request.Occurrence is { IsTimeOnly: false } occurrence ? new CausedBy(occurrence.Subject, occurrence.Name, occurrence.UserName) : null,
            request.Occurrence?.Occurred);
}
