// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_replacing_a_document_with_identical_bytes : given.a_valid_workspace
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new ReplaceWorkspaceDocument
    {
        Document = Concepts.Id,
        Bytes = Concepts.Bytes
    }));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_same_workspace_revision() => _result.Workspace!.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_keep_the_same_catalog_revision() => _result.Workspace!.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
    [Fact] void should_plan_no_writes() => _result.WritePlan!.Entries.ShouldBeEmpty();
    [Fact] void should_preserve_both_document_instances() => _result.Workspace!.Documents.All(document => Workspace.Documents.Any(original => ReferenceEquals(original, document))).ShouldBeTrue();
}
