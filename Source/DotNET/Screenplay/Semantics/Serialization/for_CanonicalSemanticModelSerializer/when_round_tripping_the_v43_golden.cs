// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v43_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV43Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV43()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v43() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V43);
    [Fact] void should_carry_a_revision() => _model.Revision.ShouldNotBeNull();
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV42Support.Supports(LanguageVersion.V43, SemanticVersion.V43).ShouldBeFalse();
    [Fact] void should_write_the_set_once_mapping() => _text.ShouldContain("\"operation\":\"setOnce\"");
    [Fact] void should_write_a_prepending_collection() => _text.ShouldContain("\"ordering\":{\"placement\":\"prepend\"}");
    [Fact] void should_write_a_sorted_collection() => _text.ShouldContain("\"placement\":\"sorted\",\"sortBy\":[");
    [Fact] void should_write_the_sort_direction() => _text.ShouldContain("\"direction\":\"descending\"");
    [Fact] void should_keep_the_screens() => _text.ShouldContain("\"screens\":[{\"id\":");
    [Fact] void should_keep_the_graph_handlers_that_iterate() => _text.ShouldContain("\"forEach\":");
}
