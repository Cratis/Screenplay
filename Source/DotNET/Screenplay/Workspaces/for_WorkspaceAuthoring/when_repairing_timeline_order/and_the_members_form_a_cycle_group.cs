// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_the_members_form_a_cycle_group : given.a_timeline
{
    [Fact]
    void should_not_repair_the_group_or_its_suppressed_edges()
    {
        var workspace = Features(("A", ["X"], ["Y"]), ("B", ["Y"], ["X"]));
        var diagnostic = workspace.Compilation.Diagnostics.Single(value => value.Code == DiagnosticCodes.TimelineCycleGroup);
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).ShouldBeEmpty();
        workspace.Compilation.Diagnostics.Where(value => value.Code == DiagnosticCodes.EventFromLaterSlice).ShouldBeEmpty();
    }
}
