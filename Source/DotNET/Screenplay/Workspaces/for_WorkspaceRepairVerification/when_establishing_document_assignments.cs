// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_establishing_document_assignments : Specification
{
    ScreenplayWorkspace _before;
    SemanticIdentityCatalog _established;

    void Establish()
    {
        var documents = new[] { "root.play", "other.play" }.Select(path => WorkspaceDocument.Create(path, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes("module M"))).ToImmutableArray();
        var application = ApplicationIdentity.Create("Repairs");
        var address = SemanticAddress.ForModule(application, "M");
        var eventAddress = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(application, "M", "F", "S"), "E");
        var catalog = SemanticIdentityCatalog.Create(application,
            [new(documents[0].StableKey, documents[0].Id, SemanticIdentityOrigin.Persisted)],
            [new(address, SemanticId.Create(address), SemanticIdentityOrigin.Persisted)],
            [new(eventAddress, EventContractId.CreateLegacy(application, "E"), EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        _before = ScreenplayWorkspace.CreateValidated("Repairs", documents, catalog, ScreenplayWorkspace.EmptyCompilation());
        _established = SemanticIdentityCatalog.Create(application,
            [.. documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
            catalog.Semantics,
            catalog.EventContracts);
    }

    [Fact] void should_accept_only_missing_document_assignments() => Keeps(_established).ShouldBeTrue();
    [Fact] void should_accept_an_unchanged_catalog() => Keeps(_before.IdentityCatalog).ShouldBeTrue();
    [Fact] void should_refuse_a_changed_existing_document_id() => Keeps(SemanticIdentityCatalog.Create(_established.Application, [.. _established.Documents.Select((assignment, index) => index == 0 ? assignment with { Id = DocumentId.Create("changed") } : assignment)], _established.Semantics, _established.EventContracts)).ShouldBeFalse();
    [Fact] void should_refuse_a_different_new_document_id() => Keeps(SemanticIdentityCatalog.Create(_established.Application, [.. _established.Documents.Select((assignment, index) => index == 1 ? assignment with { Id = DocumentId.Create("changed") } : assignment)], _established.Semantics, _established.EventContracts)).ShouldBeFalse();
    [Fact] void should_refuse_an_unrelated_document_assignment() => Keeps(SemanticIdentityCatalog.Create(_established.Application, [.. _established.Documents, new("extra", DocumentId.Create("extra"), SemanticIdentityOrigin.Persisted)], _established.Semantics, _established.EventContracts)).ShouldBeFalse();
    [Fact] void should_refuse_a_retired_existing_assignment() => Keeps(SemanticIdentityCatalog.Create(_established.Application, _established.Documents, [], _established.EventContracts)).ShouldBeFalse();
    [Fact] void should_refuse_a_changed_semantic_id() => Keeps(SemanticIdentityCatalog.Create(_established.Application, _established.Documents, [.. _established.Semantics.Select(assignment => assignment with { Id = SemanticId.Create(SemanticAddress.ForModule(_established.Application, "Changed")) })], _established.EventContracts)).ShouldBeFalse();
    [Fact] void should_refuse_a_changed_event_contract_id() => Keeps(SemanticIdentityCatalog.Create(_established.Application, _established.Documents, _established.Semantics, [.. _established.EventContracts.Select(assignment => assignment with { Id = EventContractId.CreateLegacy(_established.Application, "Changed") })])).ShouldBeFalse();
    [Fact] void should_refuse_an_advanced_event_revision() => Keeps(SemanticIdentityCatalog.Create(_established.Application, _established.Documents, _established.Semantics, [.. _established.EventContracts.Select(assignment => assignment with { Revision = new EventContractRevision(2) })])).ShouldBeFalse();
    [Fact] void should_refuse_a_changed_origin() => Keeps(SemanticIdentityCatalog.Create(_established.Application, _established.Documents, [.. _established.Semantics.Select(assignment => assignment with { Origin = SemanticIdentityOrigin.LegacyBootstrap })], _established.EventContracts)).ShouldBeFalse();

    bool Keeps(SemanticIdentityCatalog catalog) => WorkspaceRepairVerification.KeepsCatalog(_before, ScreenplayWorkspace.CreateValidated(_before.ApplicationName, _before.Documents, catalog, _before.Compilation));
}
