// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_resolving_workflow_outlets;

public class and_compute_without_a_next_edge_suspends : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> Risk\n  compute Risk\n    returns\n      score String\n    ```csharp\n    return new { score = \"low\" };\n    ```", "    then waits at Risk");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_suspend_at_the_compute() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
