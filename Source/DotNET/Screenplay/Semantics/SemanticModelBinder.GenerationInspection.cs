// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    internal static (SemanticConcept? Concept, IReadOnlyDictionary<SyntaxNode, SemanticValidationRule>? Rules, bool Generates, string? UnknownReason) InspectConceptGeneration(
        ApplicationSyntax application,
        ConceptSyntax concept,
        IReadOnlyDictionary<string, string> sources)
    {
        if (application.SourceOptions.NumericMode == NumericMode.Exact)
        {
            return (null, null, false, "Exact numeric mode is a preview (#285) that the binder does not bind; rule proofs are not established.");
        }

        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("GenerationInspection"));
        var documents = SemanticDocumentSet.Create([.. sources.Select(source => SemanticSourceDocument.Create(catalog.ResolveDocument(source.Key), source.Key, source.Key, source.Value))], catalog);
        var expansion = SpecificationExamples.Expand(application);
        var context = new BindingContext("GenerationInspection", expansion.Application, documents, new Dictionary<SyntaxNode, EffectiveSpecificationStep>(), expansion, DateTimeOffset.UtcNow.Year);

        try
        {
            var (bound, rules, generates) = context.InspectConceptGeneration(concept);

            return (bound, rules, generates, null);
        }
        catch (InvalidSemanticContract exception)
        {
            return (null, null, false, $"Generation inspection could not bind the concept: {exception.Message}");
        }
    }

    private sealed partial class BindingContext
    {
        internal (SemanticConcept Concept, IReadOnlyDictionary<SyntaxNode, SemanticValidationRule> Rules, bool Generates) InspectConceptGeneration(ConceptSyntax concept)
        {
            RegisterTypeDeclarations();
            var rules = new Dictionary<SyntaxNode, SemanticValidationRule>(ReferenceEqualityComparer.Instance);
            var generation = BindGeneration(concept);
            var validations = BindConceptValidations(concept, generation, (source, rule) => rules.Add(source, rule));
            var primitive = concept.IsEnum ? SemanticPrimitiveType.Text : Primitive(concept.Type);
            var bound = new SemanticConcept(_concepts[concept.Name].Id, concept.Name, primitive, [.. concept.Values], validations) { Generation = generation };

            return (bound, rules, generation is not null || UsesImplicitUuid(concept));
        }
    }
}
