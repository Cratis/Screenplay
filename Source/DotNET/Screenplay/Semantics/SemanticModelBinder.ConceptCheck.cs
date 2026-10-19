// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    internal static (SemanticConcept? Concept, Diagnostic[] Diagnostics) InspectConceptDeclaration(ConceptSyntax concept, int year, bool assumeGenerated)
    {
        var application = new ApplicationSyntax([], [concept], [], [], SourceLocation.Start);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GenerationInspection"));

        // A private anchor document supplies binder attachment identities and offsets, never model text.
        var lines = (concept.Validations ?? []).Sum(validation => validation is DeclarativeValidateSyntax declarative ? declarative.Rules.Count() + (declarative.Requirements?.Count() ?? 0) : 1) + 2;
        var source = SemanticSourceDocument.Create(catalog.ResolveDocument("concept.play"), "concept.play", "concept.play", new string('\n', lines));
        var documents = SemanticDocumentSet.Create([source], catalog);
        var expansion = SpecificationExamples.Expand(application);
        var context = new BindingContext("GenerationInspection", application, documents, new Dictionary<SyntaxNode, EffectiveSpecificationStep>(), expansion, year);
        try
        {
            return (context.InspectConceptDeclaration(concept, assumeGenerated), [.. context.Diagnostics]);
        }
        catch (InvalidSemanticContract error)
        {
            return (null, [.. context.Diagnostics, Diagnostic.Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Generation inspection could not bind the concept: {error.Message}", concept.Location)]);
        }
    }

    internal static SemanticVersion RequiredConceptVersion(SemanticConcept concept, bool generates)
    {
        var application = new SemanticApplication(default, "ConceptCheck", [concept], [], []);
        if (SemanticModelValidator.UsesAcmeAdmissions(application)) return SemanticVersion.V29;
        if (SemanticModelValidator.UsesTextRules(application)) return SemanticVersion.V24;
        if (SemanticModelValidator.UsesPhase3Generation(application)) return SemanticVersion.V21;
        if (generates && !concept.Validations.IsEmpty) return SemanticVersion.V20;
        if (SemanticModelValidator.UsesConceptGeneration(application)) return SemanticVersion.V15;
        if (SemanticModelValidator.UsesConceptCompliance(application)) return SemanticVersion.V13;
        if (SemanticModelValidator.UsesEnumMemberNumbers(application)) return SemanticVersion.V12;
        if (concept.Validations.Any(rule => rule.Operand is SemanticNumberValue number && SemanticModelValidator.DiffersFromLegacyLowering(number.Value))) return SemanticVersion.V11;

        return concept.Validations.Any(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation) ? SemanticVersion.V3 : SemanticVersion.V1;
    }

    private sealed partial class BindingContext
    {
        bool _assumeConceptGenerated;

        internal SemanticConcept InspectConceptDeclaration(ConceptSyntax concept, bool assumeGenerated)
        {
            _assumeConceptGenerated = assumeGenerated;
            var (bound, _, _) = InspectConceptGeneration(concept);

            return bound with
            {
                Compliance = BindCompliance(concept),
                Numbers = concept.ValueNumbers is { } numbers && SemanticModelValidator.HasNonPositionalEnumNumbers(numbers, concept.Values.Count()) ? [.. numbers] : null
            };
        }
    }
}
