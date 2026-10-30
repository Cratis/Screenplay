// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_a_destination_outside_the_collection : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Recorded", "C")]);

    void Because() => Validate();

    [Fact] void should_refuse_the_item() => Outcome.Violation.ItemIndex.ShouldEqual(0);
    [Fact] void should_return_no_items() => Outcome.Items.ShouldBeEmpty();
}
