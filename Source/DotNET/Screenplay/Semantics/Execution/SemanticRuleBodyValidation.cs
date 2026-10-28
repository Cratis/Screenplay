// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contexts;

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticRuleBodyValidation(SemanticApplication application, SemanticRuleBodyExecution bodies, SemanticExecutionRequest request)
{
    readonly Dictionary<SemanticId, SemanticConcept> _concepts = application.Concepts.ToDictionary(concept => concept.Id);
    readonly Dictionary<SemanticId, SemanticCompositeType> _types = application.Types.ToDictionary(type => type.Id);

    internal static bool IsPredicate(SemanticValidationRule rule) => rule.Kind == SemanticValidationRuleKind.RulePredicate;

    internal IEnumerable<SemanticRuleCheck> Check(SemanticCommand command, CancellationToken cancellationToken)
    {
        var values = request.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        var artifact = SemanticValue.Composite(request.Values);
        var commandChecks = command.Validations.Where(IsPredicate).Select(rule =>
            values.TryGetValue(rule.Property, out var value)
                ? bodies.Check(rule, Context(artifact, value, command.Properties.Single(property => property.Id == rule.Property).Name), cancellationToken)
                : SemanticRuleBodyExecution.Unsupported(rule, "The rule property is unavailable before generation."));
        var conceptChecks = command.Properties.Where(property => !property.IsGenerated)
            .SelectMany(property => CheckConcepts(property.Type, values[property.Id], cancellationToken));
        return commandChecks.Concat(conceptChecks);
    }

    IEnumerable<SemanticRuleCheck> CheckConcepts(SemanticTypeReference type, SemanticValue value, CancellationToken cancellationToken)
    {
        if (type.IsCollection) return CheckCollection(type, value, cancellationToken);
        if (type.Kind == SemanticTypeReferenceKind.Concept)
        {
            return _concepts[type.Target].Validations.Where(IsPredicate)
                .Select(rule => bodies.Check(rule, Context(value, value, string.Empty), cancellationToken) with { Concept = type.Target });
        }

        return CheckComposite(type, value, cancellationToken);
    }

    IEnumerable<SemanticRuleCheck> CheckCollection(SemanticTypeReference type, SemanticValue value, CancellationToken cancellationToken) =>
        value is SemanticArrayValue array
            ? array.Values.SelectMany(element => CheckConcepts(type with { IsCollection = false, IsOptional = false }, element, cancellationToken))
            : [];

    IEnumerable<SemanticRuleCheck> CheckComposite(SemanticTypeReference type, SemanticValue value, CancellationToken cancellationToken)
    {
        if (type.Kind != SemanticTypeReferenceKind.CompositeType || value is not SemanticCompositeValue composite) return [];
        var properties = _types[type.Target].Properties.ToDictionary(property => property.Id);
        return composite.Properties.SelectMany(property => CheckConcepts(properties[property.TargetProperty].Type, property.Value, cancellationToken));
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
