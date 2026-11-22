// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_resolving_workflow_outlets;

public class and_approval_without_an_outcome_keeps_the_suspension_reason : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> Review\n  approval Review by Controller", "    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_keep_the_exact_suspension_description() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldContainOnly("Expected the run to complete, but the run waits at 'Review'.");
}
