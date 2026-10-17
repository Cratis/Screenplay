// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v13_golden : Specification
{
    [Fact]
    void should_pin_v13_bytes_and_revision()
    {
        var bytes = canonical_serialization_golden_vectors.EsmV13Bytes;
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.SemanticVersion.ShouldEqual(SemanticVersion.V13);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
    }
}
