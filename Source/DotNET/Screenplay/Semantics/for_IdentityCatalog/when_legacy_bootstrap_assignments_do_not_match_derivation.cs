// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_legacy_bootstrap_assignments_do_not_match_derivation : Specification
{
    const string Hash = "0000000000000000000000000000000000000000000000000000000000000000";
    Exception _documentException;
    Exception _semanticException;
    Exception _eventException;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration");
        var command = SemanticAddress.ForCommand(slice, "RegisterProject");
        var eventAddress = SemanticAddress.ForEventContract(slice, "ProjectRegistered");
        _documentException = Catch.Exception(() => SemanticIdentityCatalog.Create(
            application,
            [new("projects-main", DocumentId.Parse($"doc1:{Hash}"), SemanticIdentityOrigin.LegacyBootstrap)],
            [],
            []));
        _semanticException = Catch.Exception(() => SemanticIdentityCatalog.Create(
            application,
            [],
            [new(command, SemanticId.Parse($"sem1:{Hash}"), SemanticIdentityOrigin.LegacyBootstrap)],
            []));
        _eventException = Catch.Exception(() => SemanticIdentityCatalog.Create(
            application,
            [],
            [],
            [new(eventAddress, EventContractId.Parse($"evt1:{Hash}"), EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap)]));
    }

    [Fact] void should_reject_the_document_assignment() => _documentException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_semantic_assignment() => _semanticException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_the_event_assignment() => _eventException.ShouldBeOfExactType<InvalidSemanticContract>();
}
