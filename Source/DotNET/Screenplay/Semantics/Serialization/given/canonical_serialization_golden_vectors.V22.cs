// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV22Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v22.json");

    public static ExecutableSemanticModel CreateSemanticModelV22() =>
        ExecutableSemanticModel.Create(LanguageVersion.V22, SemanticVersion.V22,
            CreateSemanticModelV21().Application with { ReactionProductionMode = SemanticReactionProductionMode.Atomic }, 2026);
}
#endif
