// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_no_items : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([]);

    void Because() => Validate();

    [Fact] void should_return_no_items() => Outcome.Items.ShouldBeEmpty();
    [Fact] void should_be_available() => Outcome.Unavailable.ShouldBeNull();
    [Fact] void should_have_no_violation() => Outcome.Violation.ShouldBeNull();
}
