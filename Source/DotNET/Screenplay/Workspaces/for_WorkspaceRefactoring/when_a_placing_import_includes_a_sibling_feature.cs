// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_placing_import_includes_a_sibling_feature : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("root", "application.play", "module Projects\n  import \"features.play\"\nmodule Archive"),
         Document("features", "features.play", "feature Moving\n  slice StateChange Save\n    command Save\nfeature Sibling")],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Moving"),
        NewParent = SemanticAddress.ForModule(Workspace.IdentityCatalog.Application, "Archive"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_refuse_with_the_import_placement_reason() => Assert.True(!_result.Accepted && _result.Conflicts.Single().Message.Contains("also places declarations outside", StringComparison.Ordinal), string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
}
