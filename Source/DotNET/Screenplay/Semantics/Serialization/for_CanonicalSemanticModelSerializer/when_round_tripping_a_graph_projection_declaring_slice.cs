// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_a_graph_projection_declaring_slice : for_SemanticModelBinder.given.a_graph_projection_source
{
    ExecutableSemanticModel _model;
    ExecutableSemanticModel _read;
    byte[] _bytes;

    void Establish() => _model = Compile(Source);

    void Because()
    {
        _bytes = SemanticModelSerializer.Serialize(_model);
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_keep_the_declaring_slice() => _read.Application.GraphProjections.Single().DeclaringSlice.ShouldEqual(_model.Application.GraphProjections.Single().DeclaringSlice);
    [Fact] void should_keep_the_revision() => _read.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_keep_the_canonical_bytes() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_write_scope_after_edges() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"edges\":[],\"declaringSlice\":");
}
