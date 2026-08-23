// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_renaming_an_event_with_a_persisted_assignment : Specification
{
    EventContractId _originalId;
    EventContractIdentityAssignment _renamedAssignment;
    EventContractIdentityAssignment _inferredAssignment;

    void Because()
    {
        var oldAddress = Address("ProjectRegistered");
        var renamedAddress = Address("ProjectCreated");
        _originalId = EventContractId.CreateLegacy(oldAddress);
        var catalog = SemanticIdentityCatalog.Create(
            [],
            [],
            [new(renamedAddress, _originalId, EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        _renamedAssignment = catalog.ResolveEventContract(renamedAddress);
        _inferredAssignment = SemanticIdentityCatalog.Empty.ResolveEventContract(renamedAddress);
    }

    [Fact] void should_preserve_the_persisted_contract_identity() => _renamedAssignment.Id.ShouldEqual(_originalId);
    [Fact] void should_keep_the_initial_contract_revision() => _renamedAssignment.Revision.ShouldEqual(EventContractRevision.Initial);
    [Fact] void should_never_infer_rename_continuity() => _inferredAssignment.Id.ShouldNotEqual(_originalId);

    static SemanticAddress Address(string key) => SemanticAddress.Create(
        SemanticKind.EventContract,
        [SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, key)]);
}
