// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_adding_and_removing_a_document : given.a_valid_workspace
{
    WorkspaceTransactionResult _added = null!;
    WorkspaceTransactionResult _removed = null!;

    void Because()
    {
        _added = Workspace.Propose(Request(new AddWorkspaceDocument
        {
            StableKey = "notes",
            Path = PortablePlayPath.Parse("Notes.play"),
            Bytes = Bytes("// Workspace notes\n")
        }));
        var addedDocument = _added.Workspace!.Documents.Single(document => document.StableKey == "notes");
        _removed = _added.Workspace.Propose(new WorkspaceTransactionRequest
        {
            ExpectedRevision = _added.Workspace.Revision,
            ExpectedCatalogRevision = _added.Workspace.IdentityCatalog.Revision,
            Operations = [new RemoveWorkspaceDocument { Document = addedDocument.Id }]
        });
    }

    [Fact] void should_add_the_document_atomically() => _added.Success.ShouldBeTrue();
    [Fact] void should_plan_one_addition() => _added.WritePlan!.Entries.Select(entry => entry.Kind).ShouldContainOnly(WorkspaceWriteKind.Added);
    [Fact] void should_remove_the_document_atomically() => _removed.Success.ShouldBeTrue();
    [Fact] void should_plan_one_removal() => _removed.WritePlan!.Entries.Select(entry => entry.Kind).ShouldContainOnly(WorkspaceWriteKind.Removed);
    [Fact] void should_return_to_the_original_exact_workspace_revision() => _removed.Workspace!.Revision.ShouldEqual(Workspace.Revision);
    [Fact] void should_return_to_the_original_catalog_revision() => _removed.Workspace!.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
}
