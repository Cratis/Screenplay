// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_running_a_join;

public class with_the_all_threshold_satisfied : given.a_join_workflow
{
    void Establish() => BindWorkflow(
        """
          trigger Started manual
            -> EachLine
          foreach EachLine over lines
            each -> Joined
          join Joined all
            next -> Done
          action Done invokes TagInvoice
        """,
        "    then invokes TagInvoice\n    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_complete() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
