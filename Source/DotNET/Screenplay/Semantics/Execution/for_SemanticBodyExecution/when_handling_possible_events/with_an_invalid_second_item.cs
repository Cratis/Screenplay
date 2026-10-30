// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_an_invalid_second_item : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.ProducedEvents([Item("Placed"), Item("Recorded", "C")]);
    void Because() => Result = Execute();

    [Fact] void should_be_unsupported() => ((SemanticUnsupported)Result).Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
    [Fact] void should_identify_the_item() => ((SemanticUnsupported)Result).Details.ShouldContain("result item 1:");
    [Fact] void should_commit_nothing() => Result.World.ShouldEqual(SemanticWorld.Empty);
}
