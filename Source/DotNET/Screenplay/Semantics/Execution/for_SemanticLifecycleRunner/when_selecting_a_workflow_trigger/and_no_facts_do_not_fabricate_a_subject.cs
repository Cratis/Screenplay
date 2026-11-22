// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticLifecycleRunner.when_selecting_a_workflow_trigger;

public class and_no_facts_do_not_fabricate_a_subject : given.a_join_workflow
{
    void Establish()
    {
        BindWorkflow("  trigger Started manual\n    -> Choice\n  decide Choice when status == \"paid\"\n    \"yes\" -> Tag\n    \"no\" -> Charge\n  action Tag invokes TagInvoice\n  action Charge invokes ChargeFee", "    then invokes ChargeFee\n    then completes");
        // An authored fixture normally supplies facts; an empty history is a supported runner input.
        var workflow = Model.Application.Workflows.Single();
        var specification = workflow.Specifications.Single() with { Given = [] };
        Model = ExecutableSemanticModel.CreateValidated(Model.LanguageVersion, Model.SemanticVersion, Model.Application with { Workflows = [workflow with { Specifications = [specification] }] });
    }

    void Because() => Results = SemanticLifecycleRunner.RunAll(Model);

    [Fact] void should_take_the_false_branch_without_a_subject() => Results.Single(result => result.Name == "JoinExpectation").Failures.ShouldBeEmpty();
}
