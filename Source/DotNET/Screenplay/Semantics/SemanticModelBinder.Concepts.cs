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
        SemanticConcept BindConcept(ConceptSyntax concept)
        {
            if (concept.File is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Concept '{concept.Name}' file reference is realization provenance.", concept.File.Location);
            }

            if (concept.Attributes.Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' compliance attributes require portable data-subject semantics.", concept.Location);
            }

            var primitive = concept.IsEnum ? SemanticPrimitiveType.Text : Primitive(concept.Type);
            var validations = BindConceptValidations(concept);
            return new(_concepts[concept.Name].Id, concept.Name, primitive, [.. concept.Values], validations);
        }

        ImmutableArray<SemanticValidationRule> BindConceptValidations(ConceptSyntax concept)
        {
            var validations = ImmutableArray.CreateBuilder<SemanticValidationRule>();
            foreach (var validation in concept.Validations ?? [])
            {
                if (validation is not DeclarativeValidateSyntax declarative)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' code validation requires a constrained implementation attachment.", validation.Location);
                    continue;
                }

                foreach (var requirement in declarative.Requirements ?? [])
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' requirement conditions are not admitted by the first ESM v1 vertical.", requirement.Location);
                }

                foreach (var rule in declarative.Rules)
                {
                    if (rule.Property != ValidationRuleSyntax.ConceptValue || rule.Rule != ValidationRuleKind.NotEmpty ||
                        rule.Value is not null || rule.File is not null || rule.Code is not null)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept validation rule '{rule.Rule}' is not admitted by the first ESM v1 vertical.", rule.Location);
                        continue;
                    }

                    validations.Add(new(default, SemanticValidationRuleKind.NotEmpty, null, rule.Message));
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
