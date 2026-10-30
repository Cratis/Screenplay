// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_preserving_portable_payloads : given.a_command_handler
{
    [Theory]
    [InlineData("valid")]
    [InlineData("wrong-member")]
    [InlineData("wrong-primitive")]
    [InlineData("unknown-nested-property")]
    [InlineData("duplicate-nested-property")]
    [InlineData("null-item")]
    [InlineData("default-items")]
    void should_validate_nested_collections_and_concept_membership(string shape)
    {
        const string declarations = "concept Status : Enum\n  active\ntype Details\n  status Status\n  note String optional\n";
        Configure(declarations + Source.Replace("label String optional", "label Details[]", StringComparison.Ordinal));
        var type = Plan.Model.Application.Types.Single();
        var member = new SemanticPropertyValue(type.Properties.First().Id, shape switch
        {
            "wrong-member" => SemanticValue.Text("unknown"),
            "wrong-primitive" => SemanticValue.Number(1),
            _ => SemanticValue.Text("active")
        });
        var nested = shape switch
        {
            "unknown-nested-property" => SemanticValue.Composite([member, new(Plan.Commands.Values.Single().Properties.Single().Id, SemanticValue.Text("x"))]),
            "duplicate-nested-property" => SemanticValue.Composite([member, member]),
            _ => SemanticValue.Composite([member])
        };
        var items = shape switch
        {
            "null-item" => SemanticValue.Array([SemanticValue.Null]),
            "default-items" => new SemanticArrayValue(default),
            _ => SemanticValue.Array([nested])
        };
        var first = (SemanticCompositeValue)Payloads[0];
        var contract = Plan.Events[Plan.Commands.Values.Single().Produces[0].EventContract];
        var label = contract.Properties.Single(property => property.Name == "label");
        Answer = SemanticCommandBodyResult.Produced([SemanticValue.Composite([.. first.Properties, new(label.Id, items)]), Payloads[1]]);
        var result = Execute();
        if (shape == "valid")
        {
            result.ShouldBeOfExactType<SemanticAccepted>();
            result.World.Facts[0].Values.Single(value => value.TargetProperty == label.Id).Value.ShouldEqual(items);
        }
        else
        {
            ((SemanticUnsupported)result).Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
            ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
        }
    }
}
