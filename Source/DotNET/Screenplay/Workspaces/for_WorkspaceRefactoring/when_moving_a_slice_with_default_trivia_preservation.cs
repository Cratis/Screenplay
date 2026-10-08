// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_a_slice_with_default_trivia_preservation : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", "module Projects\n  feature Source\n    slice StateChange Moving\n      command Save\n  feature Destination")],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", "Source", "Moving"),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination")
    });

    [Fact] void should_refuse_without_canonical_formatting_consent() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_required_formatting_consent() => Assert.Contains("CanonicalizeTouchedDocuments", string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
}
