// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_advancing_duplicate_event_addresses : Specification
{
    SemanticIdentityCatalog _previous;
    SemanticAddress _event;
    Exception _exception;

    void Establish()
    {
        var application = ApplicationIdentity.Create("Projects");
        _previous = SemanticIdentityCatalog.Empty(application);
        _event = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(application, "Projects", "Registration", "RegisterProject"), "ProjectRegistered");
    }

    void Because() => _exception = Catch.Exception(() => SemanticIdentityCatalog.PlanEventRevisionAdvancement(
        _previous,
        _previous.Revision,
        [],
        [_event],
        [_event],
        [new(_event, new(0)), new(_event, new(2))]));

    [Fact] void should_reject_the_duplicate_before_the_invalid_revision() => _exception.Message.ShouldEqual($"Duplicate event advancement address '{_event}' is ambiguous.");
    [Fact] void should_report_an_invalid_semantic_contract() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
