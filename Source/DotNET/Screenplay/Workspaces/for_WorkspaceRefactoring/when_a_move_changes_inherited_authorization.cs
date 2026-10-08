// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_move_changes_inherited_authorization : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        Registration = Document("registration", "registration.play", RegistrationSource + "\n  feature Archive\n    authorize CanArchive\n    slice StateChange Old\n      command Old\npolicy CanArchive\n  require role \"Archivist\"");
        Workspace = ScreenplayWorkspace.Create("Projects", [Concepts, Registration], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", "Registration", "RegisterProject"),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Archive"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_report_the_inherited_difference() => Assert.True(_result.Conflicts.Single().Message.Contains("authorize", StringComparison.Ordinal), _result.Conflicts.Single().Message);
    [Fact] void should_not_expose_a_partial_candidate() => _result.Workspace.ShouldBeNull();
}
