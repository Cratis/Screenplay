// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v15_golden : Specification
{
    [Fact]
    void should_preserve_generation_free_v14_bytes_and_revision()
    {
        var bytes = canonical_serialization_golden_vectors.EsmV14Bytes;
        var baseline = SemanticModelSerializer.Deserialize(bytes);
        baseline.SemanticVersion.ShouldEqual(SemanticVersion.V14);
        SemanticModelSerializer.Serialize(baseline).SequenceEqual(bytes).ShouldBeTrue();
    }

    [Fact]
    void should_pin_v15_bytes_and_revision()
    {
        var bytes = canonical_serialization_golden_vectors.EsmV15Bytes;
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.SemanticVersion.ShouldEqual(SemanticVersion.V15);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
    }
}
