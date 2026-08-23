// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_resolving_persisted_assignments : Specification
{
    SemanticAddress _address;
    SemanticId _assignedId;
    SemanticId _resolvedId;
    ApplicationIdentity _application;

    void Establish()
    {
        _application = ApplicationIdentity.Create("Projects");
        _address = SemanticAddress.ForCommand(
            SemanticAddress.ForSlice(_application, "Projects", "Projects", "Registration"),
            "RenamedCommand");
        _assignedId = SemanticId.Parse("sem1:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
    }

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Create(
            _application,
            [],
            [new(_address, _assignedId, SemanticIdentityOrigin.Persisted)],
            []);
        _resolvedId = catalog.ResolveSemantic(_address);
    }

    [Fact] void should_treat_persisted_assignment_as_authoritative() => _resolvedId.ShouldEqual(_assignedId);
    [Fact] void should_not_replace_assignment_with_provisional_identity() => _resolvedId.ShouldNotEqual(SemanticId.Create(_address));
}
