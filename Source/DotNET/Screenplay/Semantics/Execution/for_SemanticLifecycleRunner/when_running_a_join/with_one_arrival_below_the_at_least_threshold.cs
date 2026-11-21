// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_running_a_join;

public class with_one_arrival_below_the_at_least_threshold : given.a_join_workflow
{
    void Establish() => BindWorkflow(
        """
          trigger Started manual
            -> Joined
          join Joined at least 2
        """,
        "    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_not_complete() => Results.Single(result => result.Name == "JoinExpectation").Failures.Single().ShouldEqual("Expected the run to complete, but the run waits at 'Joined'.");
}
