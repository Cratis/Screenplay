// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_creating_ambiguous_assignments : Specification
{
    Exception _exception;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var id = SemanticId.Parse("sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration");
        var first = SemanticAddress.ForCommand(slice, "First");
        var second = SemanticAddress.ForCommand(slice, "Second");
        _exception = Catch.Exception(() => SemanticIdentityCatalog.Create(
            application,
            [],
            [
                new(first, id, SemanticIdentityOrigin.Persisted),
                new(second, id, SemanticIdentityOrigin.Persisted)
            ],
            []));
    }

    [Fact] void should_reject_the_catalog() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
