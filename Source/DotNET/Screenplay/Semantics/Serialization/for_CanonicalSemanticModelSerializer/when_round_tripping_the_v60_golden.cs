// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v60_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV60Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV60()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v60() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V60);
    [Fact] void should_select_language_v60() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V60);
    [Fact] void should_name_the_declaring_slice() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"declaringSlice\":");
    [Fact] void should_keep_the_negated_audience() => Encoding.UTF8.GetString(_bytes).ShouldContain("\"kind\":\"audience\",\"audience\":\"Preview\",\"negated\":true");
    [Fact] void should_keep_screen_element_gates() => SemanticModelValidator.UsesElementGates(_model.Application).ShouldBeTrue();
    [Fact] void should_keep_graph_collects() => _model.Application.GraphQueries.Single(query => query.Name == "EmployeesInTeam").Collects.Length.ShouldEqual(2);
    [Fact] void should_leave_v58_unextended() => EsmSchemaV58Support.Supports(LanguageVersion.V60, SemanticVersion.V60).ShouldBeFalse();
    [Fact] void should_leave_v59_unextended() => EsmSchemaV59Support.Supports(LanguageVersion.V60, SemanticVersion.V60).ShouldBeFalse();
}
