// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_serializing_possible_events;

public class when_writing_a_possible_event_set : for_SemanticModelBinder.when_binding_possible_events.given.a_possible_events_binder
{
    ExecutableSemanticModel _model;
    byte[] _bytes;
    string _json;

    void Establish()
    {
        _model = BindPossible(Source).Value!.Model;
        _bytes = SemanticModelSerializer.Serialize(_model);
        _json = Encoding.UTF8.GetString(_bytes);
    }

    [Fact] void should_write_possible_events_after_result_version() => _json.ShouldContain("\"resultVersion\":2,\"possibleEvents\":[");
    [Fact] void should_write_one_of_with_its_property() => _json.ShouldContain("{\"kind\":\"oneOf\",\"property\":\"" + ((SemanticOneOfExpression)_model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Handler!.PossibleEvents[1].Destination!).CollectionProperty);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);

    [Fact]
    void should_write_entry_members_in_contract_order()
    {
        var model = given.canonical_serialization_golden_vectors.CreateSemanticModelV35();
        using var document = JsonDocument.Parse(SemanticModelSerializer.Serialize(model));
        var entry = document.RootElement.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices")[0].GetProperty("commands")[0].GetProperty("handler").GetProperty("possibleEvents")[3];
        entry.EnumerateObject().Select(property => property.Name).ShouldEqual(["eventContract", "destination", "tags", "route"]);
    }

    [Fact]
    void should_leave_v1_handlers_byte_identical() => SemanticModelSerializer.Serialize(given.canonical_serialization_golden_vectors.CreateSemanticModelV23()).ShouldEqual(given.canonical_serialization_golden_vectors.EsmV23Bytes);

    [Fact]
    void should_refuse_to_write_one_of_outside_possible_event_destinations()
    {
        var application = _model.Application with
        {
            Modules = [.. _model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Commands = [.. slice.Commands.Select(command => command with { Produces = [command.Handler!.PossibleEvents[1]] })]
                    })]
                })]
            })]
        };
        Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V35, SemanticVersion.V35, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_v2_below_v35()
    {
        var error = Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V34, SemanticVersion.V34, _model.Application));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("require ESM v35 or later");
    }

    [Fact]
    void should_omit_legacy_productions()
    {
        using var document = JsonDocument.Parse(_bytes);
        document.RootElement.GetProperty("application").GetProperty("modules")[0].GetProperty("features")[0].GetProperty("slices")[0].GetProperty("commands")[0].GetProperty("produces").GetArrayLength().ShouldEqual(0);
    }
}
