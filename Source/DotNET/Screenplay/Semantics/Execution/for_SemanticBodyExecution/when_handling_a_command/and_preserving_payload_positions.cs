// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_preserving_payload_positions : given.a_command_handler
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_keep_every_declared_slot_including_empty_payloads(bool emptyEvent)
    {
        if (emptyEvent)
        {
            Configure(Source.Replace("event Recorded\n        name String", "event Recorded", StringComparison.Ordinal));
            Answer = SemanticCommandBodyResult.Produced([Payloads[0], SemanticValue.Composite([])]);
            ((SemanticAccepted)Execute()).Facts[1].Values.ShouldBeEmpty();
        }
        else
        {
            Configure(Source.Replace("produces Placed\n          for name\n          tag production", "produces Recorded\n          for name", StringComparison.Ordinal));
            var property = Plan.Events[Plan.Commands.Values.Single().Produces[0].EventContract].Properties.Single();
            Answer = SemanticCommandBodyResult.Produced([
                SemanticValue.Composite([new(property.Id, SemanticValue.Text("First payload"))]),
                SemanticValue.Composite([new(property.Id, SemanticValue.Text("Second payload"))])]);
            var result = (SemanticAccepted)Execute();
            result.Facts.Length.ShouldEqual(2);
            result.Facts[0].EventContract.ShouldEqual(result.Facts[1].EventContract);
            result.Facts.Select(fact => fact.Values.Single().Value).ShouldEqual([SemanticValue.Text("First payload"), SemanticValue.Text("Second payload")]);
        }
    }
}
