// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_a_default_destination : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Noted")]);
    void Because() => Result = Execute();

    [Fact] void should_use_the_command_identifier() => ((SemanticAccepted)Result).Facts.Single().Destination.ShouldEqual(SemanticValue.Text("First"));
}
