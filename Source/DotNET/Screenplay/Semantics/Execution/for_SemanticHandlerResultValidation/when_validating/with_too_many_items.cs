// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_too_many_items : given.a_possible_events_handler
{
    void Establish()
    {
        MaximumItems = 2;
        Answer = SemanticCommandBodyResult.ProducedEvents([Item("Noted"), Item("Noted"), Item("Noted")]);
    }

    void Because() => Validate();

    [Fact] void should_identify_the_first_excess_item() => Outcome.Violation.ItemIndex.ShouldEqual(2);
    [Fact] void should_return_no_partial_batch() => Outcome.Items.ShouldBeEmpty();
}
