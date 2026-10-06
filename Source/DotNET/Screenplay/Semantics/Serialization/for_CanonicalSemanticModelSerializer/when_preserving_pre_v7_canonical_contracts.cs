// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_preserving_pre_v7_canonical_contracts : Specification
{
    [Fact]
    void should_preserve_all_six_versions_bytes_and_revisions()
    {
        var vectors = new[]
        {
            (canonical_serialization_golden_vectors.SemanticModelBytes, canonical_serialization_golden_vectors.CreateSemanticModel()),
            (canonical_serialization_golden_vectors.SemanticModelV2Bytes, canonical_serialization_golden_vectors.CreateSemanticModelV2()),
            (canonical_serialization_golden_vectors.SemanticModelV3Bytes, canonical_serialization_golden_vectors.CreateSemanticModelV3()),
            (canonical_serialization_golden_vectors.SemanticModelV4Bytes, canonical_serialization_golden_vectors.CreateSemanticModelV4()),
            (canonical_serialization_golden_vectors.SemanticModelV5Bytes, canonical_serialization_golden_vectors.CreateSemanticModelV5()),
            (canonical_serialization_golden_vectors.SemanticModelV6Bytes, canonical_serialization_golden_vectors.CreateSemanticModelV6())
        };
        vectors.Length.ShouldEqual(6);
        foreach (var (expected, model) in vectors)
        {
            SemanticModelSerializer.Serialize(model).SequenceEqual(expected).ShouldBeTrue();
            var roundTrip = SemanticModelSerializer.Deserialize(expected);
            roundTrip.Revision.ShouldEqual(model.Revision);
            SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(expected).ShouldBeTrue();
            var text = Encoding.UTF8.GetString(expected);
            foreach (var member in new[] { "generated", "response", "generatedValues", "thenReturns", "numericMode" })
            {
                text.Contains($"\"{member}\":", StringComparison.Ordinal).ShouldBeFalse();
            }
        }
    }
}
