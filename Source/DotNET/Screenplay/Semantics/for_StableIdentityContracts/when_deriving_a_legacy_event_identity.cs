// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_deriving_a_legacy_event_identity : Specification
{
    const string Expected = "evt1:46abf8a1c198cd2ce27642ee744764d0797b844f7bc66fbd3f8c311fbea72f62";
    EventContractId _first;
    EventContractId _otherSlice;
    EventContractId _composed;
    EventContractId _decomposed;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        _first = EventContractId.CreateLegacy(application, "ProjectRegistered");
        _otherSlice = EventContractId.CreateLegacy(application, "ProjectRegistered");
        _composed = EventContractId.CreateLegacy(application, "CaféRegistered");
        _decomposed = EventContractId.CreateLegacy(application, "Cafe\u0301Registered");
    }

    [Fact] void should_match_the_domain_application_and_exact_nfc_name_vector() => _first.ToString().ShouldEqual(Expected);
    [Fact] void should_not_include_slice_path_or_order() => _otherSlice.ShouldEqual(_first);
    [Fact] void should_normalize_the_exact_event_name_to_nfc() => _decomposed.ShouldEqual(_composed);
}
