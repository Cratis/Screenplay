// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_revision_is_stale : given.a_timeline
{
    [Fact]
    void should_refuse_without_a_partial_candidate()
    {
        var workspace = Features(("Consumer", [], ["E"]), ("Producer", ["E"], []));
        var repair = Repair(workspace, "E");
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, new()
        {
            ExpectedRevision = default,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });
        result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
        result.Workspace.ShouldBeNull();
        result.WritePlan.ShouldBeNull();
    }
}
