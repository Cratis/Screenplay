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
        var application = ApplicationIdentity.Create("Projects");
        var oldAddress = Address(application, "ProjectRegistered");
        var renamedAddress = Address(application, "ProjectCreated");
        _originalId = EventContractId.CreateLegacy(application, oldAddress.Name);
        var previous = SemanticIdentityCatalog.Create(
            application,
            [],
            [],
            [new(oldAddress, _originalId, EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap)]);
        var plan = SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [],
            [],
            [renamedAddress],
            [],
            [],
            [new(oldAddress, renamedAddress)]);
        _renamedAssignment = plan.Catalog.ResolveEventContract(renamedAddress);
        _inferredAssignment = SemanticIdentityCatalog.Empty(application).ResolveEventContract(renamedAddress);
    }

    [Fact] void should_preserve_the_persisted_contract_identity() => _renamedAssignment.Id.ShouldEqual(_originalId);
    [Fact] void should_make_the_migrated_assignment_persisted() => _renamedAssignment.Origin.ShouldEqual(SemanticIdentityOrigin.Persisted);
    [Fact] void should_keep_the_initial_contract_revision() => _renamedAssignment.Revision.ShouldEqual(EventContractRevision.Initial);
    [Fact] void should_never_infer_rename_continuity() => _inferredAssignment.Id.ShouldNotEqual(_originalId);

    static SemanticAddress Address(ApplicationIdentity application, string key) => SemanticAddress.ForEventContract(
        SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration"),
        key);
}
