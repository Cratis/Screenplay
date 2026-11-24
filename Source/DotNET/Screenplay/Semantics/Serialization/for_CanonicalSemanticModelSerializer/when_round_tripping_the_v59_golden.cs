// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v59_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV59Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV59()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v59() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V59);
    [Fact] void should_select_language_v59() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V59);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_write_the_gate_of_the_action() => _text.ShouldContain("\"available\":[{\"kind\":\"logical\"");
    [Fact] void should_write_the_negated_audience_of_the_gate() => _text.ShouldContain("\"audience\":\"Preview\",\"negated\":true");
    [Fact] void should_leave_v58_unextended() => EsmSchemaV58Support.Supports(LanguageVersion.V59, SemanticVersion.V59).ShouldBeFalse();
}
