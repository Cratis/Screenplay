// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_planning_an_event_without_a_semantic_address : Specification
{
    Exception _exception;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var eventAddress = SemanticAddress.ForEventContract(
            SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration"),
            "ProjectRegistered");
        var previous = SemanticIdentityCatalog.Empty(application);
        _exception = Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [],
            [],
            [eventAddress],
            [],
            [],
            []));
    }

    [Fact] void should_reject_the_incomplete_identity_set() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
