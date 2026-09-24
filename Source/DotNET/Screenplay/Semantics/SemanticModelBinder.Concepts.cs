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
                    if (validation is CodeValidateSyntax code)
                    {
                        RequireImplementation(SemanticImplementationRole.ConceptValidation, SemanticAddress.ForConcept(_applicationIdentity, concept.Name), null, code.Code);
                    }

                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' code validation requires a constrained implementation attachment (#139).", validation.Location);
                    continue;
                }

                foreach (var requirement in declarative.Requirements ?? [])
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' require is not admitted: requirements guard commands, not concept values; declare a concept validation rule instead.", requirement.Location);
                }

                var subject = ConceptValidationSubject(concept);
                foreach (var rule in declarative.Rules)
                {
                    if (rule.Property != ValidationRuleSyntax.ConceptValue)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Concept '{concept.Name}' validation rule must constrain the concept's own value, not '{rule.Property}'.", rule.Location);
                        continue;
                    }

                    if (BindValidationRule(rule, default, subject, SemanticAddress.ForConcept(_applicationIdentity, concept.Name)) is { } bound)
                    {
                        validations.Add(bound);
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
