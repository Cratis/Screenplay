// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticOpaqueValidation(SemanticApplication application)
{
    readonly Dictionary<SemanticId, SemanticConcept> _concepts = application.Concepts.ToDictionary(concept => concept.Id);
    readonly Dictionary<SemanticId, SemanticCompositeType> _types = application.Types.ToDictionary(type => type.Id);

    internal string? Find(SemanticTypeReference type, bool hasRuleProvider = false) => Find(type, [], hasRuleProvider);

    static string? InConcept(SemanticConcept concept, bool hasRuleProvider)
    {
        var rules = concept.Validations.Where(rule => rule.Kind == SemanticValidationRuleKind.CodeValidation);
        if (!hasRuleProvider) rules = concept.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.CodeValidation or SemanticValidationRuleKind.RulePredicate);
        if (rules.FirstOrDefault() is not { } rule) return null;

        return rule.Kind == SemanticValidationRuleKind.CodeValidation
            ? $"Concept '{concept.Name}' code validation {concept.Validations.Take(concept.Validations.IndexOf(rule)).Count(validation => validation.Kind == SemanticValidationRuleKind.CodeValidation)}"
            : $"Rule '{rule.Name}' on concept '{concept.Name}'";
    }

    string? Find(SemanticTypeReference type, HashSet<SemanticId> visited, bool hasRuleProvider)
    {
        if (type.Kind == SemanticTypeReferenceKind.Concept) return InConcept(_concepts[type.Target], hasRuleProvider);
        if (type.Kind != SemanticTypeReferenceKind.CompositeType || !visited.Add(type.Target)) return null;

        return _types[type.Target].Properties.Select(property => Find(property.Type, visited, hasRuleProvider)).FirstOrDefault(name => name is not null);
    }
}
