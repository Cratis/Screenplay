// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_resolving_workflow_outlets;

public class and_the_first_matching_edge_is_taken : given.a_join_workflow
{
    void Establish()
    {
        BindWorkflow("  trigger Started manual\n    -> Tag\n  action Tag invokes TagInvoice\n  action Charge invokes ChargeFee", "    then invokes TagInvoice\n    then completes");
        // The compiler forbids two edges from the same outlet.
        var workflow = Model.Application.Workflows.Single();
        var edge = workflow.Edges.Single();
        workflow = workflow with { Edges = [edge, edge with { To = "Charge" }] };
        Model = ExecutableSemanticModel.CreateValidated(Model.LanguageVersion, Model.SemanticVersion, Model.Application with { Workflows = [workflow] });
    }

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_follow_the_first_duplicate_outlet_only() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
