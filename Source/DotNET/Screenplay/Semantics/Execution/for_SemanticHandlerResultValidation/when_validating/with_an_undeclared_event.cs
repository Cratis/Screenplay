// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_an_undeclared_event : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Placed"), Item("Noted") with { EventContract = Command.Id }]);

    void Because() => Validate();

    [Fact] void should_identify_the_item() => Outcome.Violation.ItemIndex.ShouldEqual(1);
    [Fact] void should_describe_the_index() => Outcome.Violation.Describe().ShouldEqual($"result item 1: event {Command.Id} is not a possible event");
    [Fact] void should_return_no_partial_batch() => Outcome.Items.ShouldBeEmpty();
    [Fact] void should_not_be_unavailable() => Outcome.Unavailable.ShouldBeNull();
}
