// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v40_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV40Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV40()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v40() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V40);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV38Support.Supports(LanguageVersion.V40, SemanticVersion.V40).ShouldBeFalse();
    [Fact] void should_write_the_graph_query() => _text.ShouldContain("\"graphQueries\":[{\"name\":\"MembersOf\",\"graph\":\"Organization\"");
    [Fact] void should_write_the_optional_hop_alias() => _text.ShouldContain("\"alias\":\"link\",\"type\":\"MEMBER_OF\",\"outgoing\":true");
    [Fact] void should_write_the_query_expectation() => _text.ShouldContain("\"thenQueries\":[{\"graphQuery\":\"MembersOf\"");
}
