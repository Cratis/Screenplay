// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_planning_invalid_renames : Specification
{
    const string Hash = "0000000000000000000000000000000000000000000000000000000000000000";
    Exception _staleBaseException;
    Exception _guessedContinuityException;
    Exception _ambiguousRenameException;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var first = Address(application, "ProjectRegistered");
        var second = Address(application, "ProjectRenamed");
        var current = Address(application, "ProjectChanged");
        var previous = SemanticIdentityCatalog.Create(
            application,
            [],
            [],
            [
                new(first, EventContractId.CreateLegacy(application, first.Name), EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap),
                new(second, EventContractId.CreateLegacy(application, second.Name), EventContractRevision.Initial, SemanticIdentityOrigin.LegacyBootstrap)
            ]);
        _staleBaseException = Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
            previous,
            CatalogRevision.Parse($"catrev1:{Hash}"),
            [],
            [],
            [first, second],
            [],
            [],
            []));
        _guessedContinuityException = Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [],
            [],
            [current, second],
            [],
            [],
            []));
        _ambiguousRenameException = Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [],
            [],
            [current],
            [],
            [],
            [new(first, current), new(second, current)]));
    }

    [Fact] void should_reject_a_stale_base_revision() => _staleBaseException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_guessed_continuity() => _guessedContinuityException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_ambiguous_many_to_one_rename() => _ambiguousRenameException.ShouldBeOfExactType<InvalidSemanticContract>();

    static SemanticAddress Address(ApplicationIdentity application, string name) => SemanticAddress.ForEventContract(
        SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration"),
        name);
}
