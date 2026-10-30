// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_the_answer_is_invalid : given.a_command_handler
{
    [Theory]
    [InlineData("default")]
    [InlineData("null-result")]
    [InlineData("null-slot")]
    [InlineData("scalar-slot")]
    [InlineData("diagnostic")]
    [InlineData("empty-diagnostic")]
    [InlineData("empty")]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("reordered")]
    [InlineData("wrong-slot-properties")]
    [InlineData("default-properties")]
    [InlineData("null-property")]
    [InlineData("null-value")]
    [InlineData("duplicate-property")]
    [InlineData("unknown-property")]
    [InlineData("missing-required")]
    [InlineData("wrong-type")]
    [InlineData("null-required")]
    [InlineData("later-slot")]
    void should_refuse_the_entire_result_without_facts_or_state(string defect)
    {
        var first = (SemanticCompositeValue)Payloads[0];
        var property = first.Properties.Single();
        Answer = defect switch
        {
            "default" => new(),
            "null-result" => null!,
            "null-slot" => SemanticCommandBodyResult.Produced([null!, Payloads[1]]),
            "scalar-slot" => SemanticCommandBodyResult.Produced([SemanticValue.Text("not a payload"), Payloads[1]]),
            "diagnostic" => new(Payloads, "private\n" + new string('x', 600)),
            "empty-diagnostic" => new(Payloads, string.Empty),
            "empty" => SemanticCommandBodyResult.Produced([]),
            "missing" => SemanticCommandBodyResult.Produced([first]),
            "extra" => SemanticCommandBodyResult.Produced([.. Payloads, first]),
            "reordered" => SemanticCommandBodyResult.Produced([Payloads[1], first]),
            "wrong-slot-properties" => SemanticCommandBodyResult.Produced([Payloads[1], Payloads[1]]),
            "default-properties" => SemanticCommandBodyResult.Produced([first with { Properties = default }, Payloads[1]]),
            "null-property" => SemanticCommandBodyResult.Produced([SemanticValue.Composite([null!]), Payloads[1]]),
            "null-value" => SemanticCommandBodyResult.Produced([SemanticValue.Composite([property with { Value = null! }]), Payloads[1]]),
            "duplicate-property" => SemanticCommandBodyResult.Produced([SemanticValue.Composite([property, property]), Payloads[1]]),
            "unknown-property" => SemanticCommandBodyResult.Produced([SemanticValue.Composite([property, new(Plan.Commands.Values.Single().Properties.Single().Id, property.Value)]), Payloads[1]]),
            "missing-required" => SemanticCommandBodyResult.Produced([SemanticValue.Composite([]), Payloads[1]]),
            "null-required" => SemanticCommandBodyResult.Produced([SemanticValue.Composite([property with { Value = SemanticValue.Null }]), Payloads[1]]),
            "later-slot" => SemanticCommandBodyResult.Produced([first, SemanticValue.Composite([])]),
            _ => SemanticCommandBodyResult.Produced([SemanticValue.Composite([property with { Value = SemanticValue.Number(7) }]), Payloads[1]])
        };
        var result = (SemanticUnsupported)Execute();
        result.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        result.Details.ShouldContain("Place");
        result.Details.ShouldContain(Bodies.Single().Requirement.RequirementId);
        result.Details.Contains('\n').ShouldBeFalse();
        result.Details.Length.ShouldBeLessThan(800);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
        result.World.Facts.ShouldBeEmpty();
        result.World.ReadModels.ShouldBeEmpty();
    }
}
