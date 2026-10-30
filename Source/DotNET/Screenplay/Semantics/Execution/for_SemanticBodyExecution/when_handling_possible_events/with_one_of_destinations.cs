// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_one_of_destinations : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Recorded", "A"), Item("Recorded", "B")]);
    void Because() => Result = Execute();

    [Fact] void should_produce_one_fact_per_selected_member() => ((SemanticAccepted)Result).Facts.Select(fact => fact.Destination).ShouldEqual([SemanticValue.Text("A"), SemanticValue.Text("B")]);
    [Fact] void should_use_the_element_type() => ((SemanticAccepted)Result).Facts.All(fact => fact.Context.EventSource.Type.Equals(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))).ShouldBeTrue();
}
