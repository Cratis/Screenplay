// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_v7_golden_model : Specification
{
    byte[] _expected = [];
    byte[] _written = [];
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        _expected = canonical_serialization_golden_vectors.SemanticModelV7Bytes;
        _model = SemanticModelSerializer.Deserialize(_expected);
    }

    void Because() => _written = SemanticModelSerializer.Serialize(_model);

    [Fact] void should_select_v7() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_match_the_checked_in_bytes() => _written.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_bytes_on_round_trip() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_expected)).SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_revision() => SemanticModelSerializer.Deserialize(_expected).Revision.ShouldEqual(_model.Revision);
}
