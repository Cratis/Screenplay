// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_move_captures_a_container_reference : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            module Projects
              feature Source
                feature Helper
                feature Moving
                  depends on Helper
                  slice StateChange Register
                    command Register
              feature Destination
                feature Helper
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", ["Source", "Moving"]),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_refuse_capture_instead_of_retargeting() => Assert.True(!_result.Accepted && _result.Conflicts.Single().Message.Contains("capture", StringComparison.OrdinalIgnoreCase), string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_name_both_candidate_addresses() => Assert.True(_result.Conflicts.Single().Message.Contains("Source.Helper", StringComparison.Ordinal) && _result.Conflicts.Single().Message.Contains("Destination.Helper", StringComparison.Ordinal), _result.Conflicts.Single().Message);
}
