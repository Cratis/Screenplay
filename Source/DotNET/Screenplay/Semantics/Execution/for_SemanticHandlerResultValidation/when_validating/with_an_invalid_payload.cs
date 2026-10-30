// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_an_invalid_payload : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Placed"), Item("Noted") with { Payload = SemanticValue.Text("wrong shape") }]);

    void Because() => Validate();

    [Fact] void should_identify_the_item() => Outcome.Violation.ItemIndex.ShouldEqual(1);
    [Fact] void should_return_no_partial_batch() => Outcome.Items.ShouldBeEmpty();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_refuse_incomplete_or_extra_payloads(bool extra)
    {
        var contract = Plan.Events.Values.Single(value => value.Name == "Noted");
        var payload = extra
            ? SemanticValue.Composite([new(contract.Properties[0].Id, SemanticValue.Text("Handled")), new(Command.Id, SemanticValue.Text("extra"))])
            : SemanticValue.Composite([]);
        var outcome = SemanticHandlerResultValidation.Validate(Plan, Command, Values, SemanticCommandBodyResult.ProducedEvents([Item("Noted") with { Payload = payload }]));
        outcome.Violation.ItemIndex.ShouldEqual(0);
        outcome.Items.ShouldBeEmpty();
    }

    [Fact]
    void should_refuse_null_result_items()
    {
        var outcome = SemanticHandlerResultValidation.Validate(Plan, Command, Values, SemanticCommandBodyResult.ProducedEvents([null!]));
        outcome.Violation.ItemIndex.ShouldEqual(0);
        outcome.Items.ShouldBeEmpty();
    }
}
