// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_finishing_parallel_workflow_branches;

public class and_pending_joins_remain_waiting_after_other_branches_end : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> EachLine\n  foreach EachLine over lines\n    each -> Joined\n    completed -> Tag\n  join Joined at least 3\n  action Tag invokes TagInvoice", "    then invokes TagInvoice\n    then completes", items: 2);

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_keep_the_pending_join_description() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldContainOnly("Expected the run to complete, but the run waits at 'Joined'.");
}
