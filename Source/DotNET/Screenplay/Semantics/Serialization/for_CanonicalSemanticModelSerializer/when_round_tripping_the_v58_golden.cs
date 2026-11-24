// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v58_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV58Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV58()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v58() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V58);
    [Fact] void should_select_language_v58() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V58);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_write_the_negated_audience() => _text.ShouldContain("\"kind\":\"audience\",\"audience\":\"Preview\",\"negated\":true");
    [Fact] void should_keep_the_code_source() => _text.ShouldContain("\"kind\":\"code\",\"requirementId\":");
    [Fact] void should_leave_v57_unextended() => EsmSchemaV57Support.Supports(LanguageVersion.V58, SemanticVersion.V58).ShouldBeFalse();
}
