// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateGenerationVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            application.Concepts.Any(concept => concept.Generation is not null),
            version,
            SemanticVersion.V15,
            "An ESM v15 model must declare a non-default generation strategy.",
            "Concept generation requires ESM v15 or later.");
        foreach (var concept in application.Concepts) SemanticGenerationContract.Validate(concept);
    }
}
