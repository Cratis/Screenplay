// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order;

public class and_neither_move_is_safe : given.a_timeline
{
    [Fact]
    void should_not_offer_a_repair()
    {
        var workspace = Features(("Consumer", ["Y"], ["E"]), ("Anchor", ["X"], []), ("Dependent", [], ["Y"]), ("Producer", ["E"], ["X"]));
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, Finding(workspace, "E")).ShouldBeEmpty();
    }
}
