// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_selecting_a_workflow_trigger;

public class and_manual_start_selects_the_first_manual_or_schedule_trigger : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger First every 1 days\n    -> Tag\n  trigger Second manual\n    -> Charge\n  action Tag invokes TagInvoice\n  action Charge invokes ChargeFee", "    then invokes TagInvoice\n    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_take_only_the_first_start_trigger() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
