// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_reversed_event_selection : given.a_property_chain_workspace
{
    WorkspaceAuthoringResult _forward;

    void Because()
    {
        _forward = Workspace.ProposeProperty(Request() with { NewGeneration = true, AlsoRename = [Select("Registered"), Select("Audited", 2)] });
        Result = Workspace.ProposeProperty(Request() with { NewGeneration = true, AlsoRename = [Select("Audited", 2), Select("Registered")] });
    }

    [Fact] void should_have_the_same_ordered_report() => Result.PropertyReport.Generations.ToArray().ShouldEqual(_forward.PropertyReport.Generations.ToArray());
    [Fact] void should_have_the_same_exact_candidate_source() => Candidate.ShouldEqual(_forward.Workspace.Documents.Single().Text);
    [Fact] void should_have_the_same_catalog_revision() => Result.Workspace.IdentityCatalog.Revision.ShouldEqual(_forward.Workspace.IdentityCatalog.Revision);
}
