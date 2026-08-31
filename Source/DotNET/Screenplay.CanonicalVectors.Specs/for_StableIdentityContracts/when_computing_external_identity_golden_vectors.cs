// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_StableIdentityContracts;

public class when_computing_external_identity_golden_vectors : Specification
{
    const string ExpectedSemanticIdentity = "sem1:dfdd420060bb9da261b1b8727727bcba5e7887dc0791413ff5a85efd49497d63";
    const string ExpectedEventContractIdentity = "evt1:05842c3e5d30449034a41ec5b6efa2b8ae1abe841d939ca836a9d822c4a32dc0";
    string _eventContractIdentity = string.Empty;
    string _semanticIdentity = string.Empty;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        _semanticIdentity = SemanticId.Create(SemanticKind.Command, "studio-artifact-42").ToString();
        _eventContractIdentity = EventContractId.Create(application, "studio-artifact-42").ToString();
    }

    [Fact] void should_match_the_external_semantic_identity_vector() => _semanticIdentity.ShouldEqual(ExpectedSemanticIdentity);
    [Fact] void should_match_the_external_event_contract_identity_vector() => _eventContractIdentity.ShouldEqual(ExpectedEventContractIdentity);
}
