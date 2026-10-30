// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_a_routed_item : given.a_possible_events_handler
{
    void Establish()
    {
        Configure("eventsource Store\n  identifier String\n  stream Main\n" + Source.Replace("produces Placed\n", "produces Placed\n                  stream Store.Main\n", StringComparison.Ordinal));
        Answer = SemanticCommandBodyResult.ProducedEvents([Item("Placed")]);
    }

    void Because() => Result = Execute();

    [Fact] void should_use_the_declared_route() => ((SemanticAccepted)Result).Facts.Single().Route.ShouldEqual(new SemanticEventRoute("Store", "Main", null));
    [Fact] void should_use_the_resolved_destination() => ((SemanticAccepted)Result).Facts.Single().Destination.ShouldEqual(SemanticValue.Text("First"));
}
