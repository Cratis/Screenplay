// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_creating_duplicate_addresses : Specification
{
    Exception _exception;

    void Because()
    {
        var address = SemanticAddress.Create(
            SemanticKind.Command,
            [SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, "Register")]);
        _exception = Catch.Exception(() => SemanticIdentityCatalog.Create(
            [],
            [
                new(address, SemanticId.Create(address), SemanticIdentityOrigin.Persisted),
                new(address, SemanticId.Parse("sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"), SemanticIdentityOrigin.Persisted)
            ],
            []));
    }

    [Fact] void should_reject_the_catalog() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
