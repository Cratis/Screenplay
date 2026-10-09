// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_a_feature_with_unchanged_interaction_targets : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            module Projects
              feature Source
                feature Moving
                  on enter
                    execute Projects.Source.Services.Save
                      on success
                        refresh Projects.Source.Services.Items
                        navigate to Projects.Source.Services.Home
                  slice StateView View
                    screen Details
                slice StateView Services
                  command Save
                  readmodel Item
                    name String
                  query Items => Item[]
                  screen Home
              feature Destination
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

    [Fact] void should_accept_the_feature_to_feature_move() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_report_the_nested_destination_migrations() => _result.MoveReport.IdentityMigrations.All(migration => migration.CurrentAddress.Parts[2].Key == "Destination" && migration.CurrentAddress.Parts[3].Key == "Moving").ShouldBeTrue();
    [Fact] void should_leave_unaffected_operands_unchanged() => _result.MoveReport.ReferenceRepairs.ShouldBeEmpty();
}
