// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_renaming_external_identity_assignments : Specification
{
    EventContractId _eventContractId;
    SemanticId _commandId;
    SemanticId _eventId;
    SemanticIdentityCatalog _previous = null!;
    SemanticIdentityCatalog _renamed = null!;
    SemanticAddress _renamedCommand = null!;
    SemanticAddress _renamedEvent = null!;

    void Because()
    {
        var application = ApplicationIdentity.Create("studio-application-42");
        var previousSlice = SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject");
        var renamedSlice = SemanticAddress.ForSlice(application, "Projects", "Registration", "CreateProject");
        var previousCommand = SemanticAddress.ForCommand(previousSlice, "RegisterProject");
        var previousEvent = SemanticAddress.ForEventContract(previousSlice, "ProjectRegistered");
        _renamedCommand = SemanticAddress.ForCommand(renamedSlice, "CreateProject");
        _renamedEvent = SemanticAddress.ForEventContract(renamedSlice, "ProjectCreated");
        _commandId = SemanticId.Create(SemanticKind.Command, "studio-command-node-42");
        _eventId = SemanticId.Create(SemanticKind.EventContract, "studio-event-node-42");
        _eventContractId = EventContractId.Create(application, "studio-event-contract-42");
        _previous = SemanticIdentityCatalog.Create(
            application,
            [],
            [
                new(previousCommand, _commandId, SemanticIdentityOrigin.Persisted),
                new(previousEvent, _eventId, SemanticIdentityOrigin.Persisted)
            ],
            [new(previousEvent, _eventContractId, EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        _renamed = SemanticIdentityCatalog.PlanMigration(
            _previous,
            _previous.Revision,
            [],
            [_renamedCommand, _renamedEvent],
            [_renamedEvent],
            [],
            [new(previousCommand, _renamedCommand), new(previousEvent, _renamedEvent)],
            [new(previousEvent, _renamedEvent)]).Catalog;
    }

    [Fact] void should_preserve_the_external_command_identity() => _renamed.ResolveSemantic(_renamedCommand).ShouldEqual(_commandId);
    [Fact] void should_preserve_the_external_event_semantic_identity() => _renamed.ResolveSemantic(_renamedEvent).ShouldEqual(_eventId);
    [Fact] void should_preserve_the_external_event_contract_identity() => _renamed.ResolveEventContract(_renamedEvent).Id.ShouldEqual(_eventContractId);
    [Fact] void should_keep_the_event_contract_revision() => _renamed.ResolveEventContract(_renamedEvent).Revision.ShouldEqual(EventContractRevision.Initial);
    [Fact] void should_keep_every_renamed_assignment_persisted() => _renamed.Semantics.All(assignment => assignment.Origin == SemanticIdentityOrigin.Persisted).ShouldBeTrue();
    [Fact] void should_advance_the_catalog_revision() => _renamed.Revision.ShouldNotEqual(_previous.Revision);
    [Fact] void should_not_rederive_the_command_identity_from_the_new_address() => _renamed.ResolveSemantic(_renamedCommand).ShouldNotEqual(SemanticId.Create(_renamedCommand));
}
