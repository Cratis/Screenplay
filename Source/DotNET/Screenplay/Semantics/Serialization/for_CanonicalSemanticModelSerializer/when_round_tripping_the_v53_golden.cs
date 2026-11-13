// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v53_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV53Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV53()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v53() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V53);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_write_the_audiences() => _text.ShouldContain("\"audiences\":[{\"name\":\"Plus\"},{\"name\":\"Preview\"}]");
    [Fact] void should_write_the_reusable_availabilities() => _text.ShouldContain("\"availabilities\":[");
    [Fact] void should_write_the_gates() => _text.ShouldContain("\"available\":[");
    [Fact] void should_write_the_provided_values() => _text.ShouldContain("\"provides\":[");
    [Fact] void should_write_the_provided_query_source() => _text.ShouldContain("\"kind\":\"query\"");
    [Fact] void should_leave_v51_unextended() => EsmSchemaV52Support.Supports(LanguageVersion.V53, SemanticVersion.V53).ShouldBeFalse();
}
