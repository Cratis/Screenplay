// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_a_command_payload_is_invalid : given.a_reaction_effect
{
    [Theory]
    [InlineData("target")]
    [InlineData("shape")]
    [InlineData("unknown")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("type")]
    void should_validate_the_whole_batch_before_invoking(string kind)
    {
        var command = Plan.Commands.Values.Single(command => command.Name == "Record");
        var property = command.Properties.Single();
        var valid = SemanticValue.Composite([new(property.Id, SemanticValue.Text("First"))]);
        var invalid = InvalidPayload(kind, property, valid);
        Answer = SemanticReactionBodyResult.Invoked([new(command.Id, valid), new(kind == "target" ? default : command.Id, invalid)]);
        Fire();
        Failure.ShouldBeOfExactType<SemanticUnsupported>();
        Loop.Facts.ShouldBeEmpty();
        Loop.Invocations.ShouldBeEmpty();
    }

    static SemanticValue InvalidPayload(string kind, SemanticProperty property, SemanticValue valid) => kind switch
    {
        "shape" => SemanticValue.Text("not a composite"),
        "unknown" => SemanticValue.Composite([new(default, SemanticValue.Text("unknown"))]),
        "duplicate" => SemanticValue.Composite([new(property.Id, SemanticValue.Text("one")), new(property.Id, SemanticValue.Text("two"))]),
        "missing" => SemanticValue.Composite([]),
        "type" => SemanticValue.Composite([new(property.Id, SemanticValue.Number(42))]),
        _ => valid
    };

    [Fact]
    void should_not_allow_supplied_generated_properties()
    {
        var command = Plan.Commands.Values.Single(command => command.Name == "Record");
        var generated = command with { Properties = [command.Properties.Single() with { IsGenerated = true }] };
        var payload = SemanticValue.Composite([new(command.Properties.Single().Id, SemanticValue.Text("First"))]);
        var exception = Catch.Exception(() => SemanticCommandPayloads.ValidateCommand(new(Plan.Model.Application.Concepts.ToDictionary(concept => concept.Id), Plan.Model.Application.Types.ToDictionary(type => type.Id)), generated, payload, "reaction body"));
        exception.ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
