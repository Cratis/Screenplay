// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v50_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV50Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV50()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v50() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V50);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV49Support.Supports(LanguageVersion.V50, SemanticVersion.V50).ShouldBeFalse();
    [Fact] void should_write_the_systems() => _text.ShouldContain("\"systems\":[{\"id\":");
    [Fact] void should_write_the_effect_class() => _text.ShouldContain("\"effect\":\"idempotent\"");
    [Fact] void should_default_the_effect_class_to_once() => _text.ShouldContain("\"effect\":\"once\"");
    [Fact] void should_write_the_outcomes() => _text.ShouldContain("\"outcomes\":[\"NoCapacity\"]");
    [Fact] void should_write_a_code_realization() => _text.ShouldContain("\"realization\":{\"kind\":\"code\",\"requirementId\":");
    [Fact] void should_keep_the_caller_identifier() => _text.ShouldContain("\"callerId\":true");
    [Fact] void should_keep_the_screens() => _text.ShouldContain("\"screens\":[{\"id\":");
    [Fact] void should_keep_the_graph_handlers_that_iterate() => _text.ShouldContain("\"forEach\":");
}
