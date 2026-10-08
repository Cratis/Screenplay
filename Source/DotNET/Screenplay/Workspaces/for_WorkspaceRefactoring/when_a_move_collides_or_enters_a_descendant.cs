// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_move_collides_or_enters_a_descendant : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _collision = null!;
    WorkspaceAuthoringResult _descendant = null!;
    WorkspaceAuthoringResult _kind = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", "module Projects\n  feature Source\n    feature Nested\n    slice StateChange Register\n      command Register\n  feature Destination\n    slice StateChange Register\n      command Other")],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because()
    {
        var application = Workspace.IdentityCatalog.Application;
        var request = new WorkspaceMoveRequest
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = SemanticAddress.ForSlice(application, "Projects", "Source", "Register"),
            NewParent = SemanticAddress.ForFeature(application, "Projects", "Destination"),
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        };
        _collision = Workspace.ProposeMove(request);
        _descendant = Workspace.ProposeMove(request with { Target = SemanticAddress.ForFeature(application, "Projects", "Source"), NewParent = SemanticAddress.ForFeature(application, "Projects", ["Source", "Nested"]) });
        _kind = Workspace.ProposeMove(request with { NewParent = SemanticAddress.ForModule(application, "Projects") });
    }

    [Fact] void should_report_the_colliding_address() => _collision.Conflicts.Single().Message.Contains("Slice:Projects.Destination.Register", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_refuse_a_descendant_destination() => _descendant.Conflicts.Single().Message.Contains("descendant", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_refuse_a_disallowed_destination_kind() => _kind.Accepted.ShouldBeFalse();
}
