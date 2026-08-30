// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_replacing_a_document : given.a_valid_workspace
{
    const string Replacement =
        """
        // Shared project concepts
        concept ProjectId : Uuid
        concept ProjectName : String
        """;
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new ReplaceWorkspaceDocument
    {
        Document = Concepts.Id,
        Bytes = Bytes(Replacement)
    }));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_advance_the_workspace_revision() => _result.Workspace!.Revision.ShouldNotEqual(Workspace.Revision);
    [Fact] void should_plan_one_replacement() => _result.WritePlan!.Entries.Select(entry => entry.Kind).ShouldContainOnly(WorkspaceWriteKind.Replaced);
    [Fact] void should_preserve_the_untouched_document_instance() => ReferenceEquals(_result.Workspace!.Documents.Single(document => document.Id == Registration.Id), Registration).ShouldBeTrue();
    [Fact] void should_keep_the_original_workspace_bytes() => Concepts.Text.ShouldEqual(ConceptsSource);
    [Fact] void should_keep_the_exact_replacement_bytes() => _result.Workspace!.Documents.Single(document => document.Id == Concepts.Id).Bytes.SequenceEqual(Bytes(Replacement)).ShouldBeTrue();
    [Fact] void should_keep_the_semantic_compilation_valid() => _result.Workspace!.Compilation.Success.ShouldBeTrue();
}
