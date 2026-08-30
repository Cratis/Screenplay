// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_a_slice_with_a_missing_description : given.a_valid_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because()
    {
        var sliceId = Workspace.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Id;
        _result = Workspace.Propose(Request(new UpdateSliceDescription
        {
            SemanticId = sliceId,
            ExpectedCurrentDescription = "anything",
            NewDescription = "anything else"
        }));
    }

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_unsupported_semantic_field() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedSemanticField);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
}
