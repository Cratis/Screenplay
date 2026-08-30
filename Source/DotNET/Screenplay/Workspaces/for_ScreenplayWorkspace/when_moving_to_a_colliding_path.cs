// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_moving_to_a_colliding_path : given.a_valid_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new MoveWorkspaceDocument
    {
        Document = Registration.Id,
        Path = PortablePlayPath.Parse("APPLICATION.play")
    }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_portable_path_collision() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.PortablePathCollision);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
    [Fact] void should_leave_both_original_paths_unchanged() => Workspace.Documents.Select(document => document.Path.Value).ShouldContainOnly("application.play", "Projects/Registration.play");
}
