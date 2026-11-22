// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_resolving_workflow_outlets;

public class and_compute_with_a_next_edge_takes_it_without_an_outcome : given.a_join_workflow
{
    void Establish() => BindWorkflow("  trigger Started manual\n    -> Risk\n  compute Risk\n    returns\n      score String\n    ```csharp\n    return new { score = \"low\" };\n    ```\n    next -> Tag\n  action Tag invokes TagInvoice", "    then invokes TagInvoice\n    then completes");

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_continue_on_next_without_an_outcome() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
