// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v57_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV57Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV57()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v57() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V57);
    [Fact] void should_select_language_v57() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V57);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_write_the_code_source() => _text.ShouldContain("\"kind\":\"code\",\"requirementId\":");
    [Fact] void should_keep_graph_collects() => _model.Application.GraphQueries.Single(query => query.Name == "EmployeesInTeam").Collects.Length.ShouldEqual(2);
    [Fact] void should_keep_expected_tags() => _text.ShouldContain("\"expectedTags\":[\"audit\"]");
    [Fact] void should_keep_free_screens() => _model.Application.Screens.Select(screen => screen.Name).ShouldContainOnly(["ParcelBoard", "ParcelDetails"]);
    [Fact] void should_leave_v56_unextended() => EsmSchemaV56Support.Supports(LanguageVersion.V57, SemanticVersion.V57).ShouldBeFalse();
}
