// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_renaming_a_query_argument_assignment : Specification
{
    SemanticId _originalId;
    SemanticIdentityAssignment _renamedAssignment;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Projects", "Summaries");
        var query = SemanticAddress.ForQuery(slice, "ProjectById");
        var previousAddress = SemanticAddress.ForQueryArgument(query, "projectId");
        var currentAddress = SemanticAddress.ForQueryArgument(query, "id");
        _originalId = SemanticId.Create(previousAddress);
        var previous = SemanticIdentityCatalog.Create(
            application,
            [],
            [new(previousAddress, _originalId, SemanticIdentityOrigin.LegacyBootstrap)],
            []);
        var plan = SemanticIdentityCatalog.PlanMigration(
            previous,
            previous.Revision,
            [],
            [currentAddress],
            [],
            [],
            [new(previousAddress, currentAddress)],
            []);
        _renamedAssignment = plan.Catalog.Semantics.Single();
    }

    [Fact] void should_preserve_the_query_argument_identity() => _renamedAssignment.Id.ShouldEqual(_originalId);
    [Fact] void should_make_the_query_argument_assignment_persisted() => _renamedAssignment.Origin.ShouldEqual(SemanticIdentityOrigin.Persisted);
    [Fact] void should_use_the_current_query_argument_address() => _renamedAssignment.Address.Name.ShouldEqual("id");
}
#endif
