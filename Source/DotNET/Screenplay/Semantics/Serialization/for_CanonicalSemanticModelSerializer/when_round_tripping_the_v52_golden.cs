// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v52_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV52Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV52()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v52() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V52);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_write_the_extensions_the_screen_needs() => _text.ShouldContain("\"extensions\":[{\"system\":");
    [Fact] void should_write_the_placed_component() => _text.ShouldContain("\"component\":\"Badge\"");
    [Fact] void should_write_the_signal_handler() => _text.ShouldContain("\"handles\":[{\"signal\":\"tapped\"");
    [Fact] void should_leave_v51_unextended() => EsmSchemaV51Support.Supports(LanguageVersion.V52, SemanticVersion.V52).ShouldBeFalse();
}
