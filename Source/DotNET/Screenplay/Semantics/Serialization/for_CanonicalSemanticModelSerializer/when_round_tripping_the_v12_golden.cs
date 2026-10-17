// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v12_golden : Specification
{
    [Fact]
    void should_pin_v12_bytes_and_revision()
    {
        var bytes = canonical_serialization_golden_vectors.EsmV12Bytes;
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.SemanticVersion.ShouldEqual(SemanticVersion.V12);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
    }
}
