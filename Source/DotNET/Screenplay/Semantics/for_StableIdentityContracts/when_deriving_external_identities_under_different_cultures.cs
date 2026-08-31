// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.for_StableIdentityContracts;

public class when_deriving_external_identities_under_different_cultures : Specification
{
    CultureInfo _originalCulture;
    EventContractId _frenchEventContract;
    EventContractId _turkishEventContract;
    SemanticId _frenchSemantic;
    SemanticId _turkishSemantic;

    void Establish() => _originalCulture = CultureInfo.CurrentCulture;

    void Because()
    {
        var application = ApplicationIdentity.Create("stable-application");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        _frenchSemantic = SemanticId.Create(SemanticKind.Command, "external-identity-I");
        _frenchEventContract = EventContractId.Create(application, "external-identity-I");
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
        _turkishSemantic = SemanticId.Create(SemanticKind.Command, "external-identity-I");
        _turkishEventContract = EventContractId.Create(application, "external-identity-I");
    }

    [Fact] void should_derive_the_same_semantic_identity() => _turkishSemantic.ShouldEqual(_frenchSemantic);
    [Fact] void should_derive_the_same_event_contract_identity() => _turkishEventContract.ShouldEqual(_frenchEventContract);

    void Destroy() => CultureInfo.CurrentCulture = _originalCulture;
}
