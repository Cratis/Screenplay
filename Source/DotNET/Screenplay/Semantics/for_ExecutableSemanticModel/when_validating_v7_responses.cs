// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_v7_responses : a_v7_contract
{
    static SemanticCommand RecordCommand => Command with { Response = new SemanticRecordCommandResponse([new("id", IdentityType, GeneratedId), new("note", NoteType, InputId)]) };
    static SemanticSpecification RecordScenario => Scenario with { ThenReturns = new SemanticRecordSpecificationResponse([new("note", SemanticValue.Null)]) };

    [Fact] void should_accept_a_record_subset_with_null() => Catch.Exception(() => Create(Application(RecordCommand, RecordScenario))).ShouldBeNull();
    [Fact] void should_accept_a_null_scalar() => Catch.Exception(() => Create(Application(Command with { Response = new SemanticScalarCommandResponse(InputId, NoteType) }, Scenario with { ThenReturns = new SemanticScalarSpecificationResponse(SemanticValue.Null) }))).ShouldBeNull();
    [Fact] void should_refuse_a_null_expectation_for_a_required_source() => Refuse(Application(scenario: Scenario with { ThenReturns = new SemanticScalarSpecificationResponse(SemanticValue.Null) }));
    [Fact] void should_refuse_foreign_response_sources() => Refuse(Application(Command with { Response = new SemanticScalarCommandResponse(Id(99), IdentityType) }));
    [Fact]
    void should_require_exact_source_type_and_optionality()
    {
        foreach (var type in new[] { SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid), IdentityType with { IsOptional = true }, NoteType })
        {
            Refuse(Application(Command with { Response = new SemanticScalarCommandResponse(GeneratedId, type) }));
        }

        var application = Application(Command with { Response = new SemanticScalarCommandResponse(GeneratedId, SemanticTypeReference.ForConcept(Id(10))) });
        Refuse(application with { Concepts = application.Concepts.Add(new(Id(10), "OtherIdentity", SemanticPrimitiveType.Uuid, [], [])) });
    }
    [Fact]
    void should_refuse_collection_sources()
    {
        var collection = NoteType with { IsCollection = true };
        var command = Command with { Properties = [Command.Properties[0], Command.Properties[1] with { Type = collection }], Response = new SemanticScalarCommandResponse(InputId, collection) };
        Refuse(Application(command));
    }
    [Fact]
    void should_refuse_empty_duplicate_unnamed_and_null_response_fields()
    {
        foreach (var fields in new[]
        {
            Array.Empty<SemanticCommandResponseField>(),
            [new("id", IdentityType, GeneratedId), new("id", IdentityType, GeneratedId)],
            [new("", IdentityType, GeneratedId)],
            [new(" ", IdentityType, GeneratedId)],
            [null!],
            [new("id", null!, GeneratedId)]
        })
        {
            Refuse(Application(Command with { Response = new SemanticRecordCommandResponse([.. fields]) }, RecordScenario));
        }
    }
    [Fact]
    void should_refuse_empty_unknown_duplicate_mistyped_and_null_expectation_fields()
    {
        foreach (var fields in new[]
        {
            Array.Empty<SemanticSpecificationResponseField>(),
            [new("unknown", GeneratedValue)],
            [new("note", SemanticValue.Null), new("note", SemanticValue.Null)],
            [new("note", SemanticValue.Number(3))],
            [new("note", null!)],
            [null!]
        })
        {
            Refuse(Application(RecordCommand, Scenario with { ThenReturns = new SemanticRecordSpecificationResponse([.. fields]) }));
        }
    }
    [Fact]
    void should_refuse_scalar_record_shape_disagreements()
    {
        Refuse(Application(RecordCommand));
        Refuse(Application(scenario: RecordScenario));
    }
    [Fact] void should_require_a_response_for_a_return_assertion() => Refuse(Application(Command with { Response = null }));
    [Fact] void should_require_a_command_action() => Refuse(Application(scenario: Scenario with { When = null }));
    [Fact]
    void should_refuse_returns_with_denial_or_errors()
    {
        Refuse(Application(scenario: Scenario with { ThenDenied = true }));
        Refuse(Application(scenario: Scenario with { ThenErrors = [new(null, null)] }));
    }
    [Fact] void should_allow_an_absent_return_assertion() => Catch.Exception(() => Create(Application(scenario: Scenario with { ThenReturns = null, ThenErrors = [new(null, null)] }))).ShouldBeNull();
    [Fact]
    void should_admit_ordinary_composites_with_array_members()
    {
        var type = new SemanticCompositeType(Id(10), "Details", [new(Id(11), "Tags", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isCollection: true), false)]);
        var reference = SemanticTypeReference.ForCompositeType(type.Id);
        var property = Command.Properties[1] with { Type = reference };
        var value = SemanticValue.Composite([new(Id(11), SemanticValue.Array([SemanticValue.Text("first"), SemanticValue.Text("second")]))]);
        var application = Application(Command with { Properties = [Command.Properties[0], property], Response = new SemanticScalarCommandResponse(property.Id, reference) },
            Scenario with { When = Scenario.When with { Values = [new(InputId, value)] }, ThenReturns = new SemanticScalarSpecificationResponse(value) });
        Catch.Exception(() => Create(application with { Types = [type] })).ShouldBeNull();
    }
}
