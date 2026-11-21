// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_running_a_join;

public class with_a_sibling_branch_finishing_before_the_join_is_satisfied : given.a_join_workflow
{
    void Establish() => BindWorkflow(
        """
          trigger Started manual
            -> EachLine
          foreach EachLine over lines
            each -> Joined
            completed -> Done
          join Joined at least 3
          action Done invokes WriteOff
        """,
        "    then invokes WriteOff\n    then waits at Joined");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_finish_the_sibling_but_keep_the_run_waiting() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
