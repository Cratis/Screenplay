// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v51_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV51Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV51()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v51() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V51);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_keep_the_systems() => _text.ShouldContain("\"systems\":[{\"id\":");
    [Fact] void should_write_the_invoked_ability() => _text.ShouldContain("\"ability\":\"BookPickup\"");
    [Fact] void should_write_the_ability_inputs() => _text.ShouldContain("\"inputs\":[{\"input\":\"parcelId\"");
    [Fact] void should_write_the_branches() => _text.ShouldContain("\"branches\":[{\"selector\":\"returned\"");
    [Fact] void should_write_the_outcome_branch() => _text.ShouldContain("\"selector\":\"NoCapacity\"");
    [Fact] void should_write_the_returned_value() => _text.ShouldContain("\"kind\":\"invocationValue\"");
    [Fact] void should_write_the_ability_answers() => _text.ShouldContain("\"givenAbilityAnswers\":[{\"system\":");
    [Fact] void should_write_the_expected_ability_invocation() => _text.ShouldContain("\"thenInvocations\":[{\"system\":");
    [Fact] void should_leave_v50_unextended() => EsmSchemaV50Support.Supports(LanguageVersion.V51, SemanticVersion.V51).ShouldBeFalse();
}
