// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v44_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV44Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV44()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v44() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V44);
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV43Support.Supports(LanguageVersion.V44, SemanticVersion.V44).ShouldBeFalse();
    [Fact] void should_preserve_the_allowlist() => _model.Application.Agents!.Operations.Length.ShouldEqual(2);
    [Fact] void should_retain_the_previous_projection_contract() => System.Text.Encoding.UTF8.GetString(_bytes).ShouldContain("\"operation\":\"setOnce\"");
}
