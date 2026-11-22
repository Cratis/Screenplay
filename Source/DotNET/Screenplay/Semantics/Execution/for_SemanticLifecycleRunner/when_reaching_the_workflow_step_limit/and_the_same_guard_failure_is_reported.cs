// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_reaching_the_workflow_step_limit;

public class and_the_same_guard_failure_is_reported : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> Even\n  wait Even for 1 days\n    resumed -> Odd\n  wait Odd for 1 days\n    resumed -> Even", "    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_keep_the_node_after_exactly_ten_thousand_steps() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldContainOnly("Expected the run to complete, but the run failed at 'Odd'.");
}
