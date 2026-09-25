// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_a_legacy_transition_without_cardinality : Specification
{
    [Fact] void should_require_cardinality_for_v1_through_v3()
    {
        foreach (var bytes in new[] { canonical_serialization_golden_vectors.SemanticModelBytes,
                     canonical_serialization_golden_vectors.SemanticModelV2Bytes,
                     canonical_serialization_golden_vectors.SemanticModelV3Bytes })
        {
            var document = JsonNode.Parse(bytes)!.AsObject();
            var affected = document["application"]!["modules"]![0]!["features"]![0]!["features"]![0]!["slices"]![1]!["projections"]![0]!["transitions"]![0]!["affectedInstance"]!.AsObject();
            affected.Remove("cardinality").ShouldBeTrue();
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(document.ToJsonString())));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.ShouldContain("requires 'cardinality'");
        }
    }
}
#endif
