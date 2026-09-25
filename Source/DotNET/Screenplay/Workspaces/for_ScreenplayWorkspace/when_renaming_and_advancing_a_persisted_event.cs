// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_renaming_and_advancing_a_persisted_event : Specification
{
    WorkspaceTransactionResult _implicit = null!;
    WorkspaceTransactionResult _explicit = null!;
    WorkspaceTransactionResult _stale = null!;
    EventContractId _original;
    SemanticId _firstProperty;

    void Because()
    {
        const string initial = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n";
        const string evolved = "module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Renamed\n        old String\n      event Renamed generation 2\n        current String\n";
        var document = WorkspaceDocument.Create("events", PortablePlayPath.Parse("events.play"), Encoding.UTF8.GetBytes(initial));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var oldEvent = workspace.IdentityCatalog.EventContracts.Single();
        _original = oldEvent.Id;
        _firstProperty = workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property).Id;
        var target = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(workspace.IdentityCatalog.Application, "Projects", "Registration", "RegisterProject"), "Renamed");
        var request = new WorkspaceTransactionRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Operations = [new ReplaceWorkspaceDocument { Document = document.Id, Bytes = [.. Encoding.UTF8.GetBytes(evolved)] }],
            SemanticRenames = [new(oldEvent.Address, target)],
            EventRenames = [new(oldEvent.Address, target)]
        };
        _implicit = workspace.Propose(request);
        _explicit = workspace.Propose(request with { EventRevisionAdvancements = [new(target, new(2))] });
        _stale = workspace.Propose(request with { ExpectedCatalogRevision = default, EventRevisionAdvancements = [new(target, new(2))] });
    }

    [Fact] void should_refuse_implicit_advancement() => _implicit.Success.ShouldBeFalse();
    [Fact] void should_refuse_a_stale_catalog_revision() => _stale.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_accept_explicit_advancement() => Assert.True(_explicit.Success, string.Join("; ", _explicit.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_preserve_the_contract_identity() => _explicit.Workspace!.IdentityCatalog.EventContracts.Single().Id.ShouldEqual(_original);
    [Fact] void should_carry_the_generation_one_property_identity() => _explicit.Workspace!.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property && assignment.Address.Parts[^3].Key == "1").Id.ShouldEqual(_firstProperty);
}
