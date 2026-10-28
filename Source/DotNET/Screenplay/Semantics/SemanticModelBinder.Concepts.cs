// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        Dictionary<SemanticId, SemanticConcept> _boundConcepts = [];

        SemanticConcept BindConcept(ConceptSyntax concept)
        {
            if (concept.Description is not null) Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Concept '{concept.Name}' description is authoring metadata.", concept.Location);
            if (concept.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Concept '{concept.Name}' file reference is realization provenance.", concept.File.Location);
            }

            var primitive = concept.IsEnum ? SemanticPrimitiveType.Text : Primitive(concept.Type);
            var generation = BindGeneration(concept);
            var validations = BindConceptValidations(concept, generation);
            return new(_concepts[concept.Name].Id, concept.Name, primitive, [.. concept.Values], validations)
            {
                Numbers = concept.ValueNumbers is { } numbers && SemanticModelValidator.HasNonPositionalEnumNumbers(numbers, concept.Values.Count()) ? [.. numbers] : null,
                Compliance = BindCompliance(concept),
                Generation = generation
            };
        }

        ImmutableArray<SemanticValidationRule> BindConceptValidations(ConceptSyntax concept, SemanticGeneration? generation, Action<SyntaxNode, SemanticValidationRule>? inspect = null)
        {
            var validations = ImmutableArray.CreateBuilder<SemanticValidationRule>();
            var codeValidationOrdinal = 0;
            foreach (var validation in concept.Validations ?? [])
            {
                if (validation is not DeclarativeValidateSyntax declarative)
                {
                    if (validation is CodeValidateSyntax code)
                    {
                        var member = $"code validation {codeValidationOrdinal++}";
                        var name = $"Concept '{concept.Name}' {member}";
                        var requirement = RequireImplementation(SemanticImplementationRole.ConceptValidation, SemanticAddress.ForConcept(_applicationIdentity, concept.Name), code.File, code.Code, member);
                        if (requirement is not null)
                        {
                            UsesV3 = true;
                            var bound = new SemanticValidationRule(default, SemanticValidationRuleKind.CodeValidation, null, null)
                            {
                                Name = name,
                                RequirementId = requirement.RequirementId
                            };
                            validations.Add(bound);
                            inspect?.Invoke(code, bound);
                            ValidateGenerationRule(concept, generation, bound, code.Location);
                        }
                    }

                    continue;
                }

                foreach (var requirement in declarative.Requirements ?? [])
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' require is not admitted: requirements guard commands, not concept values; declare a concept validation rule instead.", requirement.Location);
                }

                var subject = ConceptValidationSubject(concept);
                foreach (var rule in declarative.Rules)
                {
                    if (ImplementationInvariants.NamedRuleError(rule, false) is { } wrapperError)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, wrapperError, rule.Location);
                        continue;
                    }

                    var requirement = rule.Rule == ValidationRuleKind.Rule
                        ? RequireImplementation(SemanticImplementationRole.RulePredicate, SemanticAddress.ForConcept(_applicationIdentity, concept.Name), rule.File, rule.Code, $"{rule.Property}/{(rule.Value as PathExpressionSyntax)?.Path}")
                        : null;

                    if (rule.Property != ValidationRuleSyntax.ConceptValue)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' validation rule must constrain the concept's own value, not '{rule.Property}'.", rule.Location);
                        continue;
                    }

                    if (BindValidationRule(rule, default, subject, requirement?.RequirementId) is { } bound)
                    {
                        validations.Add(bound);
                        inspect?.Invoke(rule, bound);
                        ValidateGenerationRule(concept, generation, bound, rule.Location);
                    }
                }
            }

            return validations.ToImmutable();
        }

        SemanticCompositeType BindType(TypeSyntax type)
        {
            if (type.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Composite type '{type.Name}' description is authoring metadata.", type.Location);
            }

            if (type.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Composite type '{type.Name}' file reference is realization provenance.", type.File.Location);
            }

            var owner = _types[type.Name].Address;
            var properties = type.Properties.Select(property => BindProperty(owner, property, false)).ToImmutableArray();
            return new(_types[type.Name].Id, type.Name, properties);
        }
    }
}
