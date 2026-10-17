// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v14_golden : Specification
{
    [Fact]
    void should_preserve_filter_free_v13_bytes_and_revision()
    {
        var bytes = canonical_serialization_golden_vectors.EsmV13Bytes;
        var baseline = SemanticModelSerializer.Deserialize(bytes);
        baseline.SemanticVersion.ShouldEqual(SemanticVersion.V13);
        SemanticModelSerializer.Serialize(baseline).SequenceEqual(bytes).ShouldBeTrue();
    }

    [Fact]
    void should_pin_v14_bytes_and_revision()
    {
        var bytes = canonical_serialization_golden_vectors.EsmV14Bytes;
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.SemanticVersion.ShouldEqual(SemanticVersion.V14);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
    }
}
