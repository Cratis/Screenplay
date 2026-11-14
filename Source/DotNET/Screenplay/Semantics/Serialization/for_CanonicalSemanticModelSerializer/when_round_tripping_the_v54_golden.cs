// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v54_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV54Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV54()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v54() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V54);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_write_the_collects() => _text.ShouldContain("\"collects\":[");
    [Fact] void should_write_a_list_collect() => _text.ShouldContain("\"cardinality\":\"many\"");
    [Fact] void should_write_a_single_collect() => _text.ShouldContain("\"cardinality\":\"one\"");
    [Fact] void should_write_an_edge_property_member() => _text.ShouldContain("\"kind\":\"edgeProperty\",\"source\":\"since\"");
    [Fact] void should_keep_both_collects() => _model.Application.GraphQueries.Single(query => query.Name == "EmployeesInTeam").Collects.Length.ShouldEqual(2);
    [Fact] void should_leave_v53_unextended() => EsmSchemaV53Support.Supports(LanguageVersion.V54, SemanticVersion.V54).ShouldBeFalse();
}
