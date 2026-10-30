// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_valid_items : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Placed"), Item("Recorded", "B"), Item("Recorded", "B"), Item("Noted")]);

    void Because() => Validate();

    [Fact] void should_preserve_order_and_duplicates() => Outcome.Items.Select(item => item.Entry.EventContract).ShouldEqual(Answer.Items.Select(item => item.EventContract));
    [Fact] void should_resolve_declared_entries() => Outcome.Items.All(item => Command.Handler.PossibleEvents.Contains(item.Entry)).ShouldBeTrue();
    [Fact] void should_resolve_destinations() => Outcome.Items.Select(item => item.Destination).ShouldEqual([SemanticValue.Text("First"), SemanticValue.Text("B"), SemanticValue.Text("B"), null]);
    [Fact] void should_have_no_violation() => Outcome.Violation.ShouldBeNull();
    [Fact] void should_be_available() => Outcome.Unavailable.ShouldBeNull();
}
