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
            (canonical_serialization_golden_vectors.SemanticModelBytes, SemanticVersion.V1),
            (canonical_serialization_golden_vectors.SemanticModelV2Bytes, SemanticVersion.V2),
            (canonical_serialization_golden_vectors.SemanticModelV3Bytes, SemanticVersion.V3),
            (canonical_serialization_golden_vectors.SemanticModelV4Bytes, SemanticVersion.V4),
            (canonical_serialization_golden_vectors.SemanticModelV5Bytes, SemanticVersion.V5),
            (canonical_serialization_golden_vectors.SemanticModelV6Bytes, SemanticVersion.V6)
        };
        vectors.Length.ShouldEqual(6);
        foreach (var (expected, version) in vectors)
        {
            var roundTrip = SemanticModelSerializer.Deserialize(expected);
            roundTrip.SemanticVersion.ShouldEqual(version);
            SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(expected).ShouldBeTrue();
            var text = Encoding.UTF8.GetString(expected);
            foreach (var member in new[] { "generated", "response", "generatedValues", "thenReturns", "numericMode" })
            {
                text.Contains($"\"{member}\":", StringComparison.Ordinal).ShouldBeFalse();
            }
        }
    }
}
