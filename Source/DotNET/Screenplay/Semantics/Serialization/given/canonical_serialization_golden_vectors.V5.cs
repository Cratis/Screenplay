// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    const string V5Resource = "Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v5.json";

    public static byte[] SemanticModelV5Bytes => ReadResource(V5Resource);

    public static ExecutableSemanticModel CreateSemanticModelV5()
    {
        var v4 = CreateSemanticModelV4();
        var modules = v4.Application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(root => root with
            {
                Features = [.. root.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification =>
                            specification.Name == "creates an entity from existing state"
                                ? specification with
                                {
                                    ThenAbsentReadModels =
                                    [
                                        new(specification.ThenReadModels.Single().ReadModel,
                                            SemanticValue.Text("00000000-0000-0000-0000-000000000999"))
                                    ]
                                }
                                : specification)]
                    })]
                })]
            })]
        }).ToImmutableArray();
        return ExecutableSemanticModel.Create(LanguageVersion.V5, SemanticVersion.V5, v4.Application with { Modules = modules });
    }
}
#endif
