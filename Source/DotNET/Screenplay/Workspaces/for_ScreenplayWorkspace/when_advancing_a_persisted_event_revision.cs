// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_advancing_a_persisted_event_revision : Specification
{
    WorkspaceTransactionResult _withoutAdvancement = null!;
    WorkspaceTransactionResult _withAdvancement = null!;
    EventContractId _original;

    void Because()
    {
        const string initial = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n";
        const string evolved = initial + "      event Registered generation 2\n        current String\n";
        var document = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(initial));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var assignment = workspace.IdentityCatalog.EventContracts.Single();
        _original = assignment.Id;
        var request = new WorkspaceTransactionRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = [new ReplaceWorkspaceDocument { Document = document.Id, Bytes = [.. Encoding.UTF8.GetBytes(evolved)] }]
        };
        _withoutAdvancement = workspace.Propose(request);
        _withAdvancement = workspace.Propose(request with
        {
            EventRevisionAdvancements = [new(assignment.Address, new(2))]
        });
    }

    [Fact] void should_refuse_implicit_advancement() => _withoutAdvancement.Success.ShouldBeFalse();
    [Fact] void should_keep_the_persisted_identity() => _withAdvancement.Workspace!.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(_original);
    [Fact] void should_record_the_declared_revision() => _withAdvancement.Workspace!.IdentityCatalog.EventContracts.Single().Revision.Value.ShouldEqual(2u);
    [Fact] void should_compile_after_explicit_advancement() => Assert.True(_withAdvancement.Success, string.Join("; ", _withAdvancement.Conflicts.Select(value => value.Message)));
}
