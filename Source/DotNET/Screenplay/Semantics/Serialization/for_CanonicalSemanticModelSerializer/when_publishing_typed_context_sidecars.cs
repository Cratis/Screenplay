// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_publishing_typed_context_sidecars : Specification
{
    [Fact] void should_preserve_every_existing_schema_byte_vector_and_semantic_revision()
    {
        foreach (var (model, golden) in new[]
        {
            (canonical_serialization_golden_vectors.CreateSemanticModel(), canonical_serialization_golden_vectors.SemanticModelBytes),
            (canonical_serialization_golden_vectors.CreateSemanticModelV2(), canonical_serialization_golden_vectors.SemanticModelV2Bytes),
            (canonical_serialization_golden_vectors.CreateSemanticModelV3(), canonical_serialization_golden_vectors.SemanticModelV3Bytes),
            (canonical_serialization_golden_vectors.CreateSemanticModelV4(), canonical_serialization_golden_vectors.SemanticModelV4Bytes)
        })
        {
            SemanticModelSerializer.Serialize(model).SequenceEqual(golden).ShouldBeTrue();
            SemanticModelSerializer.Deserialize(golden).Revision.ShouldEqual(model.Revision);
        }
    }
}
