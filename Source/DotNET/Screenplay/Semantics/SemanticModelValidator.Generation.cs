// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Generation;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesConceptGeneration(SemanticApplication application) =>
        application.Concepts.Any(concept => concept.Generation is not null);

    internal static bool UsesGeneratedConceptRules(SemanticApplication application) => GeneratedConceptsWithRules(application).Any();

    static IEnumerable<SemanticConcept> GeneratedConceptsWithRules(SemanticApplication application)
    {
        var generatedTypes = application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Commands).SelectMany(command => command.Properties)
            .Where(property => property.IsGenerated).Select(property => property.Type.Target).ToHashSet();

        return application.Concepts.Where(concept => !concept.Validations.IsEmpty &&
            (concept.Generation is not null || generatedTypes.Contains(concept.Id)));
    }

    static void ValidateGenerationVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            UsesConceptGeneration(application),
            version,
            SemanticVersion.V15,
            "An ESM v15 model must declare a non-default generation strategy.",
            "Concept generation requires ESM v15 or later.");
        foreach (var concept in application.Concepts) SemanticGenerationContract.Validate(concept);
        ValidateFeatureVersion(
            UsesGeneratedConceptRules(application),
            version,
            SemanticVersion.V20,
            "An ESM v20 model must use rules on a generated concept.",
            "Rules on generated concepts require ESM v20 or later.");
        foreach (var concept in GeneratedConceptsWithRules(application))
        {
            foreach (var rule in concept.Validations)
            {
                var result = RuleConsistency.Check(concept.Generation, concept.Primitive, rule);
                if (result.Status == RuleConsistencyStatus.Refuted)
                {
                    throw new InvalidSemanticContract($"Generation strategy '{concept.Generation?.Kind ?? "uuid"}' for concept '{concept.Name}' violates a rule; witness {System.Text.Json.JsonSerializer.Serialize(result.Witness)}.");
                }
            }
        }
    }
}
