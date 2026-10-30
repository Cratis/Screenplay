// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_a_destination_on_a_scalar_entry : given.a_possible_events_handler
{
    [Theory]
    [InlineData("Placed")]
    [InlineData("Noted")]
    void should_refuse_handler_owned_scalar_destinations(string name)
    {
        var outcome = SemanticHandlerResultValidation.Validate(Plan, Command, Values, SemanticCommandBodyResult.ProducedEvents([Item(name, "B")]));
        outcome.Violation.ItemIndex.ShouldEqual(0);
        outcome.Items.ShouldBeEmpty();
    }
}
