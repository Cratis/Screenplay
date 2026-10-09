// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_a_feature_with_qualified_references : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var other = Document("other", "other.play", """
            module Archive
              feature Consumer
                depends on Projects.Registration
                slice StateView Check
                  readmodel Result
                    name ProjectName
                  specification Recorded
                    given Projects.Registration.RegisterProject.ProjectRegistered
                      projectId = "b0f2e9b0-1b9a-4e54-9b0a-222222222222"
                      name = "A project"
                    when append Projects.Registration.RegisterProject.ProjectRegistered
                      projectId = "b0f2e9b0-1b9a-4e54-9b0a-222222222222"
                      name = "A project"
                    then readmodel Result
                      name = "A project"
            """);
        Workspace = ScreenplayWorkspace.Create("Projects", [Concepts, Registration, other], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Registration"),
        NewParent = SemanticAddress.ForModule(Workspace.IdentityCatalog.Application, "Archive"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_accept() => _result.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_repair_the_container_dependency() => _result.MoveReport.ReferenceRepairs.Any(repair => repair.Previous == "Projects.Registration" && repair.Current == "Archive.Registration").ShouldBeTrue();
    [Fact] void should_repair_the_qualified_event_references() => _result.MoveReport.ReferenceRepairs.Count(repair => repair.Current == "Archive.Registration.RegisterProject.ProjectRegistered").ShouldEqual(2);
}
