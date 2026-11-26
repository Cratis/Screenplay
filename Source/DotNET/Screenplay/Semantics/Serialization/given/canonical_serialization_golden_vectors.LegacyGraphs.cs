// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    static ExecutableSemanticModel WithoutGraphProjectionDeclaringSlices(ExecutableSemanticModel model, LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        ExecutableSemanticModel.Create(languageVersion, semanticVersion, model.Application with
        {
            GraphProjections = [.. model.Application.GraphProjections.Select(projection => projection with { DeclaringSlice = null })]
        });
}
#endif
