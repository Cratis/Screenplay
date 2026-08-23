// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_verifying_ambiguous_effective_event_contracts : Specification
{
    Exception _legacyCollisionException;
    Exception _persistedAndLegacyCollisionException;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var registration = SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration");
        var import = SemanticAddress.ForSlice(application, "Projects", "Projects", "Import");
        var first = SemanticAddress.ForEventContract(registration, "ProjectRegistered");
        var sameNameInAnotherSlice = SemanticAddress.ForEventContract(import, "ProjectRegistered");
        _legacyCollisionException = Catch.Exception(() => SemanticIdentityCatalog.Empty(application).VerifyAgainst(
            [],
            [first, sameNameInAnotherSlice],
            [first, sameNameInAnotherSlice]));

        var persisted = SemanticAddress.ForEventContract(registration, "ImportedProjectRegistered");
        var legacy = SemanticAddress.ForEventContract(import, "ProjectImported");
        var legacyId = EventContractId.CreateLegacy(application, legacy.Name);
        var catalog = SemanticIdentityCatalog.Create(
            application,
            [],
            [],
            [new(persisted, legacyId, EventContractRevision.Initial, SemanticIdentityOrigin.Persisted)]);
        _persistedAndLegacyCollisionException = Catch.Exception(() => catalog.VerifyAgainst([], [persisted, legacy], [persisted, legacy]));
    }

    [Fact] void should_reject_same_name_legacy_bootstrap_ambiguity() => _legacyCollisionException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_report_the_first_legacy_address() => _legacyCollisionException.Message.ShouldContain("Registration");
    [Fact] void should_report_the_second_legacy_address() => _legacyCollisionException.Message.ShouldContain("Import");
    [Fact] void should_reject_persisted_and_legacy_identity_ambiguity() => _persistedAndLegacyCollisionException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_report_the_persisted_address() => _persistedAndLegacyCollisionException.Message.ShouldContain("ImportedProjectRegistered");
    [Fact] void should_report_the_legacy_address() => _persistedAndLegacyCollisionException.Message.ShouldContain("ProjectImported");
}
