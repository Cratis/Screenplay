// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_creating_an_event_assignment_with_an_unknown_origin : Specification
{
    ApplicationIdentity _application;
    SemanticAddress _event;
    Exception _exception;

    void Establish()
    {
        _application = ApplicationIdentity.Create("Projects");
        _event = SemanticAddress.ForEventContract(SemanticAddress.ForSlice(_application, "Projects", "Registration", "RegisterProject"), "ProjectRegistered");
    }

    void Because() => _exception = Catch.Exception(() => SemanticIdentityCatalog.Create(
        _application,
        [],
        [],
        [new(_event, EventContractId.CreateLegacy(_application, "DifferentIdentity"), new(1), (SemanticIdentityOrigin)99)]));

    [Fact] void should_reject_the_unknown_origin() => _exception.Message.ShouldEqual($"Event assignment '{_event}' has an unknown origin.");
    [Fact] void should_report_an_invalid_semantic_contract() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
