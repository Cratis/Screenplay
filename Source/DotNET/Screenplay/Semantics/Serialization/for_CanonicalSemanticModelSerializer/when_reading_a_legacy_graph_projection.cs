// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_a_legacy_graph_projection : Specification
{
    ExecutableSemanticModel _model;
    byte[] _bytes;

    void Because()
    {
        _bytes = canonical_serialization_golden_vectors.EsmV38Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_not_invent_a_declaring_slice() => _model.Application.GraphProjections.Single().DeclaringSlice.ShouldBeNull();
    [Fact] void should_keep_the_frozen_bytes() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_omit_the_member() => Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model)).Contains("declaringSlice", StringComparison.Ordinal).ShouldBeFalse();
}
