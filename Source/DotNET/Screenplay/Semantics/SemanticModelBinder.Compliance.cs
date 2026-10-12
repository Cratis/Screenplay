// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    internal static ImmutableArray<Diagnostic> ComplianceAdmissionDiagnostics(ApplicationSyntax syntax)
    {
        var slices = ScreenplayValidator.ScopedSlices(syntax).ToArray();
        var context = ParserContext.ForDiagnostics();
        IdentifierComplianceValidator.Validate(syntax, new ConsistencyDeclarations(syntax, slices), context);
        foreach (var concept in syntax.Concepts.Where(concept => concept.IsEnum && concept.Attributes.Any()))
        {
            context.Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Enum concept '{concept.Name}' cannot carry compliance in ESM v13.", concept.Location);
        }
        foreach (var declaration in slices.SelectMany(entry => EventDeclarations.In(entry.Slice)).Distinct())
        {
            if (declaration.Properties.Any(property => property.IsSubject) && declaration.Properties.Any(property => RequiresDefaultSubject(property.Type.Name, [])))
            {
                context.Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Event '{declaration.Name}' holds personal data or a subject-scoped secret and has an explicit subject mark; subject lineage is not admitted by ESM v13.", declaration.Location);
            }
        }

        return [.. context.Diagnostics.Select(diagnostic => diagnostic with { Code = DiagnosticCodes.UnsupportedSemanticSyntax })];

        bool RequiresDefaultSubject(string name, HashSet<string> visited)
        {
            if (!visited.Add(name)) return false;
            if (syntax.Concepts.SingleOrDefault(concept => concept.Name == name) is { } concept)
            {
                return concept.Attributes.Any(attribute => attribute.Name == ConceptAttributeSyntax.Pii || (attribute.Name == ConceptAttributeSyntax.Sensitive && attribute.Scope == "subject"));
            }

            return (syntax.Types ?? []).SingleOrDefault(type => type.Name == name)?.Properties.Any(property => RequiresDefaultSubject(property.Type.Name, visited)) == true;
        }
    }

    private sealed partial class BindingContext
    {
        SemanticConceptCompliance? BindCompliance(ConceptSyntax concept)
        {
            var personal = concept.Attributes.SingleOrDefault(attribute => attribute.Name == ConceptAttributeSyntax.Pii);
            var secret = concept.Attributes.SingleOrDefault(attribute => attribute.Name == ConceptAttributeSyntax.Sensitive);
            foreach (var attribute in concept.Attributes.Where(attribute => attribute.Reason is not null))
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Concept '{concept.Name}' compliance reason is report-only authoring metadata.", attribute.Location);
            }

            return personal is null && secret is null ? null : new(
                personal is null ? null : new(personal.SpecialCategory, personal.Criminal),
                secret is null ? null : new(personal is null ? secret.Scope : null));
        }

        void ValidateComplianceAdmission()
        {
            foreach (var diagnostic in ComplianceAdmissionDiagnostics(syntax)) Error(diagnostic.Code, diagnostic.Message, diagnostic.Location);
        }
    }
}
