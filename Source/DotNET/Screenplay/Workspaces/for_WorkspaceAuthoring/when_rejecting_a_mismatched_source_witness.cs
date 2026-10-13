// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_a_mismatched_source_witness : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeAuthoring(Authoring() with
    {
        Formatting = WorkspaceAuthoringFormatting.PreserveExactSource,
        Documents = [new ReplaceWorkspaceSyntaxDocument(Concepts.Id, Syntax(ConceptsSource)) { SourceWitness = ConceptsSource + "\nconcept Extra : String\n" }]
    });

    [Fact] void should_reject_source_that_differs_from_the_intended_tree() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_report_structural_fidelity_failure() => _result.Conflicts.Single().Message.ShouldContain("intended typed AST");
    [Fact] void should_never_expose_a_write_plan() => _result.WritePlan.ShouldBeNull();
}
