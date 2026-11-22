// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_resolving_workflow_outlets;

public class and_a_duration_wait_continues_immediately : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> Delay\n  wait Delay for 1 days\n    resumed -> Tag\n  action Tag invokes TagInvoice", "    then invokes TagInvoice\n    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_resume_without_an_outcome_or_real_delay() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
