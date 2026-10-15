// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesConceptGeneration(SemanticApplication application) =>
        application.Concepts.Any(concept => concept.Generation is not null);

    internal static bool UsesGeneratedConceptRules(SemanticApplication application) => GeneratedConceptsWithRules(application).Any();

    internal static bool UsesPhase3Generation(SemanticApplication application) => application.Concepts.Any(concept =>
        SemanticGenerationContract.IsPhase3(concept.Generation) || concept.Validations.Any(rule => rule.Kind == SemanticValidationRuleKind.Checksum));

    static IEnumerable<SemanticConcept> GeneratedConceptsWithRules(SemanticApplication application)
    {
        var generatedTypes = application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Commands).SelectMany(command => command.Properties)
            .Where(property => property.IsGenerated).Select(property => property.Type.Target).ToHashSet();

        return application.Concepts.Where(concept => !concept.Validations.IsEmpty &&
            (concept.Generation is not null || generatedTypes.Contains(concept.Id)));
    }

    static void ValidateGenerationVersion(SemanticApplication application, SemanticVersion version, int assessmentYear)
    {
        ValidateFeatureVersion(
            UsesConceptGeneration(application),
            version,
            SemanticVersion.V15,
            "An ESM v15 model must declare a non-default generation strategy.",
            "Concept generation requires ESM v15 or later.");
        ValidateFeatureVersion(
            UsesPhase3Generation(application),
            version,
            SemanticVersion.V21,
            "An ESM v21 model must use phase-3 generation or checksum validation.",
            "Phase-3 generation and checksum validation require ESM v21 or later.");
        foreach (var concept in application.Concepts) SemanticGenerationContract.Validate(concept, assessmentYear);
        ValidateFeatureVersion(
            UsesGeneratedConceptRules(application),
            version,
            SemanticVersion.V20,
            "An ESM v20 model must use rules on a generated concept.",
            "Rules on generated concepts require ESM v20 or later.");
        foreach (var concept in GeneratedConceptsWithRules(application))
        {
            foreach (var result in RuleConsistency.Check(concept, assessmentYear))
            {
                if (result.Status == RuleConsistencyStatus.Refuted)
                {
                    throw new InvalidSemanticContract($"Generation strategy '{concept.Generation?.Kind ?? "uuid"}' for concept '{concept.Name}' violates a rule; witness {System.Text.Json.JsonSerializer.Serialize(result.Witness)}.");
                }
            }
        }
    }
}
