// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_resolving_workflow_outlets;

public class and_a_terminal_failure_outlet_records_the_same_node : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> Charge\n  action Charge invokes ChargeFee", "    outcome Charge failed\n    then invokes ChargeFee\n    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_keep_the_terminal_failure_description() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldContainOnly("Expected the run to complete, but the run failed at 'Charge'.");
}
