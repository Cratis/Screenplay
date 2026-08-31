// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_moving_a_document_with_external_identities : given.a_workspace_with_external_identities
{
    WorkspaceTransactionResult _result = null!;

    void Because() => _result = Workspace.Propose(Request(new MoveWorkspaceDocument
    {
        Document = Registration.Id,
        Path = PortablePlayPath.Parse("Projects/Registration/RegisterProject.play")
    }));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_move_the_document() => _result.Workspace.Documents.Single(document => document.Id == Registration.Id).Path.ShouldEqual(PortablePlayPath.Parse("Projects/Registration/RegisterProject.play"));
    [Fact] void should_preserve_every_semantic_assignment() => _result.Workspace.IdentityCatalog.Semantics.SequenceEqual(Workspace.IdentityCatalog.Semantics).ShouldBeTrue();
    [Fact] void should_preserve_every_event_contract_assignment() => _result.Workspace.IdentityCatalog.EventContracts.SequenceEqual(Workspace.IdentityCatalog.EventContracts).ShouldBeTrue();
    [Fact] void should_preserve_the_catalog_revision() => _result.Workspace.IdentityCatalog.Revision.ShouldEqual(Workspace.IdentityCatalog.Revision);
    [Fact] void should_advance_the_workspace_revision() => _result.Workspace.Revision.ShouldNotEqual(Workspace.Revision);
    [Fact] void should_plan_one_move() => _result.WritePlan.Entries.Select(entry => entry.Kind).ShouldContainOnly(WorkspaceWriteKind.Moved);
}
