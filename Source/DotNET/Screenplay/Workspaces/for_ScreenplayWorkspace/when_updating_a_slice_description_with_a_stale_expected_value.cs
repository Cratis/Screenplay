// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_a_slice_description_with_a_stale_expected_value : given.a_workspace_with_a_described_slice
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new UpdateSliceDescription
    {
        SemanticId = SliceId,
        ExpectedCurrentDescription = "This is not the current description",
        NewDescription = "Registers a brand new project"
    }));

    [Fact] void should_reject_the_complete_transaction() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_a_semantic_field_value_drift() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.SemanticFieldValueDrift);
    [Fact] void should_return_no_candidate_workspace() => _result.Workspace.ShouldBeNull();
    [Fact] void should_leave_the_original_document_untouched() => Workspace.Documents.Single(document => document.Id == Registration.Id).Text.ShouldEqual(RegistrationSource);
}
