// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_reaching_the_workflow_step_limit;

public class and_foreach_branches_retain_their_own_step_budget : given.a_join_workflow
{
    void Establish()
    {
        const int branchSteps = 6000;
        var waits = string.Join("\n", Enumerable.Range(0, branchSteps).Select(index => $"  wait Delay{index} for 1 days\n    resumed -> {(index + 1 < branchSteps ? $"Delay{index + 1}" : "Joined")}"));
        BindWorkflow($"  trigger Started manual\n    -> EachLine\n  foreach EachLine over lines\n    each -> Delay0\n    completed -> Tag\n{waits}\n  join Joined all\n  action Tag invokes TagInvoice", "    then invokes TagInvoice\n    then completes", items: 2);
    }

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_allow_more_than_ten_thousand_total_branch_steps() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
