// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_running_a_join;

public class with_repeated_arrivals_after_the_join_completes : given.a_join_workflow
{
    void Establish() => BindWorkflow(
        """
          trigger Started manual
            -> EachLine
          foreach EachLine over lines
            each -> Joined
          join Joined at least 2
            next -> Done
          action Done invokes TagInvoice
        """,
        "    then invokes TagInvoice\n    then completes",
        items: 3);

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_not_reactivate_the_join() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
