// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_canonicalizing_v7_response_identity : a_v7_contract
{
    static SemanticCommand RecordCommand => Command with { Response = new SemanticRecordCommandResponse([new("id", IdentityType, GeneratedId), new("note", NoteType, InputId)]) };
    static SemanticSpecification RecordScenario => Scenario with { ThenReturns = new SemanticRecordSpecificationResponse([new("note", SemanticValue.Null)]) };

    [Fact]
    void should_keep_property_identity_when_generation_is_added()
    {
        var input = Command.Properties[0] with { IsGenerated = false };
        (input with { IsGenerated = true }).Id.ShouldEqual(input.Id);
    }
    [Fact]
    void should_keep_response_sources_and_names_when_a_source_is_renamed()
    {
        var command = RecordCommand with { Properties = [Command.Properties[0] with { Name = "NewId" }, Command.Properties[1]] };
        var roundTrip = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(Create(Application(command, RecordScenario))));
        var response = (SemanticRecordCommandResponse)roundTrip.Application.Modules[0].Features[0].Slices[0].Commands[0].Response;
        response.Fields[0].Source.ShouldEqual(GeneratedId);
        response.Fields[0].Name.ShouldEqual("id");
    }
    [Fact]
    void should_preserve_revision_when_declarations_and_fixtures_are_reordered()
    {
        var token = new SemanticProperty(Id(10), "Token", IdentityType, false) { IsGenerated = true };
        var command = Command with { Properties = Command.Properties.Add(token) };
        var when = Scenario.When with { GeneratedValues = [new(token.Id, GeneratedValue), new(GeneratedId, GeneratedValue)] };
        var original = Create(Application(command, Scenario with { When = when }));
        var reordered = Create(Application(command with { Properties = [.. command.Properties.Reverse()] }, Scenario with { When = when with { GeneratedValues = [.. when.GeneratedValues.Reverse()] } }));
        reordered.Revision.ShouldEqual(original.Revision);
        SemanticModelSerializer.Serialize(reordered).SequenceEqual(SemanticModelSerializer.Serialize(original)).ShouldBeTrue();
        var read = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(original));
        read.Application.Modules[0].Features[0].Slices[0].Specifications[0].When.GeneratedValues.Select(value => value.TargetProperty).ShouldEqual([GeneratedId, token.Id]);
    }
    [Fact]
    void should_include_response_field_order_and_name_in_the_revision()
    {
        var original = Create(Application(RecordCommand, RecordScenario));
        var response = (SemanticRecordCommandResponse)RecordCommand.Response;
        Create(Application(RecordCommand with { Response = response with { Fields = [.. response.Fields.Reverse()] } }, RecordScenario)).Revision.ShouldNotEqual(original.Revision);
        Create(Application(RecordCommand with { Response = response with { Fields = [response.Fields[0] with { Name = "renamedId" }, response.Fields[1]] } }, RecordScenario)).Revision.ShouldNotEqual(original.Revision);
    }
    [Fact]
    void should_include_response_mapping_and_type_in_the_revision()
    {
        var requiredNote = NoteType with { IsOptional = false };
        var alternative = new SemanticProperty(Id(10), "OtherNote", requiredNote, false);
        var command = Command with { Properties = [Command.Properties[0], Command.Properties[1] with { Type = requiredNote }, alternative], Response = new SemanticScalarCommandResponse(InputId, requiredNote) };
        var scenario = Scenario with { When = Scenario.When with { Values = [new(InputId, SemanticValue.Text("note")), new(alternative.Id, SemanticValue.Text("other"))] }, ThenReturns = new SemanticScalarSpecificationResponse(SemanticValue.Text("note")) };
        var original = Create(Application(command, scenario));
        Create(Application(command with { Response = new SemanticScalarCommandResponse(alternative.Id, requiredNote) }, scenario)).Revision.ShouldNotEqual(original.Revision);
        var optional = command with { Properties = [command.Properties[0], command.Properties[1] with { Type = NoteType }, alternative], Response = new SemanticScalarCommandResponse(InputId, NoteType) };
        Create(Application(optional, scenario)).Revision.ShouldNotEqual(original.Revision);
    }
}
