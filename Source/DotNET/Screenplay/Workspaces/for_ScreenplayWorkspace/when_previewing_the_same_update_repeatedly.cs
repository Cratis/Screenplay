// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_previewing_the_same_update_repeatedly : given.a_workspace_with_a_described_slice
{
    WorkspaceTransactionResult _first = null!;
    WorkspaceTransactionResult _second = null!;

    void Because()
    {
        var request = Request(new UpdateSliceDescription
        {
            SemanticId = SliceId,
            ExpectedCurrentDescription = OriginalDescription,
            NewDescription = "Registers a brand new project"
        });
        _first = Workspace.Propose(request);
        _second = Workspace.Propose(request);
    }

    [Fact] void should_succeed_both_times() => (_first.Success && _second.Success).ShouldBeTrue();
    [Fact] void should_produce_the_same_candidate_revision() => _first.Workspace!.Revision.ShouldEqual(_second.Workspace!.Revision);
    [Fact] void should_produce_the_same_candidate_bytes() => _first.Workspace!.Documents.Single(document => document.Id == Registration.Id).Bytes
        .SequenceEqual(_second.Workspace!.Documents.Single(document => document.Id == Registration.Id).Bytes).ShouldBeTrue();
    [Fact] void should_leave_the_original_workspace_document_untouched() => Workspace.Documents.Single(document => document.Id == Registration.Id).Text.ShouldEqual(RegistrationSource);
}
