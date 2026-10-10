// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v10_golden : Specification
{
    [Fact]
    void should_pin_v10_bytes_and_revision()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        var model = canonical_serialization_golden_vectors.CreateSemanticModelV10();
        var bytes = SemanticModelSerializer.Serialize(model);
        bytes.SequenceEqual(canonical_serialization_golden_vectors.EsmV10Bytes).ShouldBeTrue();
        var roundTrip = SemanticModelSerializer.Deserialize(bytes);
        roundTrip.Revision.ShouldEqual(model.Revision);
        SemanticModelSerializer.Serialize(roundTrip).SequenceEqual(bytes).ShouldBeTrue();
    }
}
