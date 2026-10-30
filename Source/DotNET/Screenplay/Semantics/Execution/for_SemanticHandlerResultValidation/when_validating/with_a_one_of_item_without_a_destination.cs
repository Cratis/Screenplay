// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_a_one_of_item_without_a_destination : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Recorded")]);

    void Because() => Validate();

    [Fact] void should_require_an_explicit_member() => Outcome.Violation.ItemIndex.ShouldEqual(0);
}
