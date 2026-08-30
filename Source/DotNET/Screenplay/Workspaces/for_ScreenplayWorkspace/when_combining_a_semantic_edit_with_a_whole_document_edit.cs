// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_combining_a_semantic_edit_with_a_whole_document_edit : given.a_workspace_with_two_described_slices
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(
        new UpdateSliceDescription
        {
            SemanticId = RegisterSliceId,
            ExpectedCurrentDescription = RegisterDescription,
            NewDescription = "Registers a project"
        },
        new ReplaceWorkspaceDocument
        {
            Document = Registration.Id,
            Bytes = Bytes(RegistrationSource)
        }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_multi_owner_semantic_edit() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.MultiOwnerSemanticEdit);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
}
