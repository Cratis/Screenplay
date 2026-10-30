// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_an_empty_result : given.a_possible_events_handler
{
    void Because() => Result = Execute();

    [Fact] void should_accept_zero_facts() => ((SemanticAccepted)Result).Facts.ShouldBeEmpty();
    [Fact] void should_leave_world_facts_empty() => Result.World.Facts.ShouldBeEmpty();
    [Fact] void should_leave_read_models_empty() => Result.World.ReadModels.ShouldBeEmpty();
    [Fact] void should_leave_no_pending_children() => Result.World.PendingChildren.ShouldBeEmpty();
    [Fact] void should_execute_the_handler_once() => Provider.Calls.ShouldEqual(1);
}
