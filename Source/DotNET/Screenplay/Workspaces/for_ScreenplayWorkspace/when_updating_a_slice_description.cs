// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_a_slice_description : given.a_workspace_with_a_described_slice
{
    const string NewDescription = "Registers a brand new project";
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new UpdateSliceDescription
    {
        SemanticId = SliceId,
        ExpectedCurrentDescription = OriginalDescription,
        NewDescription = NewDescription
    }));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_advance_the_workspace_revision() => _result.Workspace!.Revision.ShouldNotEqual(Workspace.Revision);
    [Fact] void should_plan_exactly_one_write() => _result.WritePlan!.Entries.Length.ShouldEqual(1);
    [Fact] void should_plan_a_replacement() => _result.WritePlan!.Entries.Single().Kind.ShouldEqual(WorkspaceWriteKind.Replaced);
    [Fact] void should_replace_the_registration_document() => _result.WritePlan!.Entries.Single().Document.ShouldEqual(Registration.Id);
    [Fact] void should_preserve_the_untouched_concepts_document_instance() => ReferenceEquals(_result.Workspace!.Documents.Single(document => document.Id == Concepts.Id), Concepts).ShouldBeTrue();
    [Fact] void should_only_change_the_description_text() => _result.Workspace!.Documents.Single(document => document.Id == Registration.Id).Text
        .ShouldEqual(RegistrationSource.Replace(OriginalDescription, NewDescription));
    [Fact] void should_keep_the_semantic_compilation_valid() => _result.Workspace!.Compilation.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_slice_identity() => _result.Workspace!.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Id.ShouldEqual(SliceId);
    [Fact] void should_leave_the_original_workspace_untouched() => Workspace.Documents.Single(document => document.Id == Registration.Id).Text.ShouldEqual(RegistrationSource);
}
