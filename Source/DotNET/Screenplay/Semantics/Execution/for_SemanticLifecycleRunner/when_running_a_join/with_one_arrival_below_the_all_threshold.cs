// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_running_a_join;

public class with_one_arrival_below_the_all_threshold : given.a_join_workflow
{
    void Establish() => BindWorkflow(
        """
          trigger Started manual
            -> Joined
          action Other invokes TagInvoice
            next -> Joined
          join Joined all
        """,
        "    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_not_complete() => Results.Single(result => result.Name == "JoinExpectation").Failures.Single().ShouldEqual("Expected the run to complete, but the run waits at 'Joined'.");
}
