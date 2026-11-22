// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_finishing_parallel_workflow_branches;

public class and_a_waiting_branch_stops_later_items : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> EachLine\n  foreach EachLine over lines\n    each -> Tag\n    completed -> Charge\n  action Tag invokes TagInvoice\n    next -> Review\n  approval Review by Controller\n  action Charge invokes ChargeFee", "    then invokes TagInvoice\n    then waits at Review", items: 3);

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_stop_after_the_first_suspended_item() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
