// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_planning_explicit_retirements : Specification
{
    SemanticIdentityCatalogMigrationPlan _plan = null!;
    Exception _unknownRetirement = null!;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var concept = SemanticAddress.ForConcept(application, "ProjectId");
        var @event = SemanticAddress.ForEventContract(
            SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject"),
            "ProjectRegistered");
        var previous = SemanticIdentityCatalog.Create(
            application,
            [new("application", DocumentId.Create("application"), SemanticIdentityOrigin.Persisted)],
            [
                new(concept, SemanticId.Create(concept), SemanticIdentityOrigin.Persisted),
                new(@event, SemanticId.Create(@event), SemanticIdentityOrigin.Persisted)
            ],
            [new(@event, EventContractId.CreateLegacy(application, @event.Name), EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        _plan = SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [],
            [],
            [],
            [],
            [],
            [],
            ["application"],
            [concept, @event],
            [@event]);
        _unknownRetirement = Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [],
            [],
            [],
            [],
            [],
            [],
            ["unknown"],
            [concept, @event],
            [@event]));
    }

    [Fact] void should_retire_the_document_assignment() => _plan.Catalog.Documents.ShouldBeEmpty();
    [Fact] void should_retire_the_semantic_assignments() => _plan.Catalog.Semantics.ShouldBeEmpty();
    [Fact] void should_retire_the_event_contract_assignment() => _plan.Catalog.EventContracts.ShouldBeEmpty();
    [Fact] void should_reject_an_identity_that_was_never_assigned() => _unknownRetirement.ShouldBeOfExactType<InvalidSemanticContract>();
}
