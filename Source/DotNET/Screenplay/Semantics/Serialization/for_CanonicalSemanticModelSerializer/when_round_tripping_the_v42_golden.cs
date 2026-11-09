// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v42_golden : Specification
{
    byte[] _bytes;
    string _text;
    ExecutableSemanticModel _model;
    SemanticScreen _board;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = canonical_serialization_golden_vectors.EsmV42Bytes;
        _model = SemanticModelSerializer.Deserialize(_bytes);
        _text = Encoding.UTF8.GetString(_bytes);
        _board = _model.Application.Modules.Single(module => module.Name == "Desk").Features[0].Slices.Single(slice => slice.Name == "TicketBoard").Screens[0];
    }

    [Fact] void should_pin_the_golden() => _bytes.ShouldEqual(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV42()));
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_model).ShouldEqual(_bytes);
    [Fact] void should_select_v42() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V42);
    [Fact] void should_leave_earlier_readers_unextended() => EsmSchemaV41Support.Supports(LanguageVersion.V42, SemanticVersion.V42).ShouldBeFalse();
    [Fact] void should_write_the_screens_of_a_slice() => _text.ShouldContain("\"screens\":[{\"id\":");
    [Fact] void should_read_the_screen_title() => _board.Title.ShouldEqual("Tickets");
    [Fact] void should_read_the_fields_the_screen_shows() => _board.Data[0].Fields.Select(field => (field.Property, field.Label)).ShouldContainOnly([("subject", "Subject")]);
    [Fact] void should_read_the_layout_regions() => _board.Layout.Regions.Select(region => region.Kind).ShouldContainOnly(["list"]);
}
