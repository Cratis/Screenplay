// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_IdentityCatalog;

public class when_planning_semantic_renames_with_invalid_endpoints : Specification
{
    Exception _nullPreviousException;
    Exception _nullCurrentException;
    Exception _crossKindException;

    void Because()
    {
        var application = ApplicationIdentity.Create("Projects");
        var slice = SemanticAddress.ForSlice(application, "Projects", "Projects", "Registration");
        var previousAddress = SemanticAddress.ForCommand(slice, "RegisterProject");
        var currentAddress = SemanticAddress.ForCommand(slice, "CreateProject");
        var crossKindAddress = SemanticAddress.ForReadModel(slice, "ProjectSummary");
        var previous = SemanticIdentityCatalog.Create(
            application,
            [],
            [new(previousAddress, SemanticId.Create(previousAddress), SemanticIdentityOrigin.LegacyBootstrap)],
            []);
        _nullPreviousException = Plan(previous, currentAddress, new(null!, currentAddress));
        _nullCurrentException = Plan(previous, currentAddress, new(previousAddress, null!));
        _crossKindException = Plan(previous, crossKindAddress, new(previousAddress, crossKindAddress));
    }

    [Fact] void should_reject_a_null_previous_endpoint_as_an_invalid_contract() => _nullPreviousException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_null_current_endpoint_as_an_invalid_contract() => _nullCurrentException.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_cross_kind_rename_as_an_invalid_contract() => _crossKindException.ShouldBeOfExactType<InvalidSemanticContract>();

    static Exception Plan(SemanticIdentityCatalog previous, SemanticAddress current, SemanticIdentityRename rename) =>
        Catch.Exception(() => SemanticIdentityCatalog.PlanMigration(previous, previous.Revision, [], [current], [], [], [rename], []));
}
