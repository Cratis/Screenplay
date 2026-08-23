// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_verifying_a_stale_assignment : Specification
{
    Exception _exception;

    void Because()
    {
        var staleAddress = Address("RemovedCommand");
        var catalog = SemanticIdentityCatalog.Create(
            [],
            [new(staleAddress, SemanticId.Create(staleAddress), SemanticIdentityOrigin.Persisted)],
            []);
        _exception = Catch.Exception(() => catalog.VerifyAgainst([], [], []));
    }

    [Fact] void should_reject_the_stale_assignment() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();

    static SemanticAddress Address(string key) => SemanticAddress.Create(
        SemanticKind.Command,
        [SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, key)]);
}
