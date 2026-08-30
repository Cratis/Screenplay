// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_two_slice_descriptions_in_one_document : given.a_workspace_with_two_described_slices
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(
        new UpdateSliceDescription
        {
            SemanticId = RegisterSliceId,
            ExpectedCurrentDescription = RegisterDescription,
            NewDescription = "Registers a project"
        },
        new UpdateSliceDescription
        {
            SemanticId = RenameSliceId,
            ExpectedCurrentDescription = RenameDescription,
            NewDescription = "Renames a project"
        }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_multi_owner_semantic_edit() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.MultiOwnerSemanticEdit);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
    [Fact] void should_leave_the_original_document_untouched() => Workspace.Documents.Single(document => document.Id == Registration.Id).Text.ShouldEqual(RegistrationSource);
}
