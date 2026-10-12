// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV13Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v13.json");

    public static ExecutableSemanticModel CreateSemanticModelV13()
    {
        var application = CreateSemanticModelV12().Application;
        SemanticConcept[] concepts =
        [
            new(Id(10300), "HealthInformation", SemanticPrimitiveType.Text, [], []) { Compliance = new(new("health", true), null) },
            new(Id(10301), "ApiKey", SemanticPrimitiveType.Text, [], []) { Compliance = new(null, new("namespace")) },
            new(Id(10302), "PersonalSecret", SemanticPrimitiveType.Text, [], []) { Compliance = new(new(), new()) }
        ];
        application = application with { Concepts = application.Concepts.AddRange(concepts) };

        return ExecutableSemanticModel.Create(LanguageVersion.V13, SemanticVersion.V13, application);
    }
}
#endif
