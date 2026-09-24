// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalVectors.Specs.given;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_v2_golden_bytes : Specification
{
    byte[] _actual = [];
    byte[] _expected = [];

    void Establish() => _expected = canonical_serialization_golden_bytes.SemanticModelV2;

    void Because() => _actual = SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_expected));

    [Fact] void should_preserve_the_exact_v2_bytes() => _actual.SequenceEqual(_expected).ShouldBeTrue();
}
