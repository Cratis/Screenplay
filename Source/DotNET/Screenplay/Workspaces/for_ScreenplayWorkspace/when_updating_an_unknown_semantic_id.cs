// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_an_unknown_semantic_id : given.a_workspace_with_a_described_slice
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new UpdateSliceDescription
    {
        SemanticId = SemanticId.Parse($"sem1:{new string('a', 64)}"),
        ExpectedCurrentDescription = OriginalDescription,
        NewDescription = "Registers a brand new project"
    }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_semantic_id_not_found() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.SemanticIdNotFound);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
}
