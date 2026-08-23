// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_creating_ambiguous_assignments : Specification
{
    Exception _exception;

    void Because()
    {
        var id = SemanticId.Parse("sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        var first = Address("First");
        var second = Address("Second");
        _exception = Catch.Exception(() => SemanticIdentityCatalog.Create(
            [],
            [
                new(first, id, SemanticIdentityOrigin.Persisted),
                new(second, id, SemanticIdentityOrigin.Persisted)
            ],
            []));
    }

    [Fact] void should_reject_the_catalog() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();

    static SemanticAddress Address(string key) => SemanticAddress.Create(
        SemanticKind.Command,
        [SemanticAddressPart.Create(SemanticAddressPartKind.Declaration, key)]);
}
