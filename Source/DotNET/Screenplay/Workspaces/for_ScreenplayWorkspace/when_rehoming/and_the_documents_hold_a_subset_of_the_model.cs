// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.when_rehoming;

public class and_the_documents_hold_a_subset_of_the_model : given.a_workspace_with_assigned_events
{
    ScreenplayWorkspace _rehomed = null!;
    Exception? _whole;

    void Because()
    {
        _rehomed = Original.Rehome([Document("application.play", Concepts)], RehomeScope.Subset);
        _whole = Catch.Exception(() => Original.Rehome([Document("application.play", Concepts)]));
    }

    [Fact] void should_compile() => _rehomed.Compilation.Success.ShouldBeTrue();
    [Fact] void should_keep_the_semantic_ids_of_present_declarations() =>
        _rehomed.IdentityCatalog.Semantics.All(assignment => Original.IdentityCatalog.Semantics.Contains(assignment)).ShouldBeTrue();
    [Fact] void should_keep_assignments_for_the_present_declarations() => _rehomed.IdentityCatalog.Semantics.ShouldNotBeEmpty();
    [Fact] void should_drop_the_assignments_of_absent_declarations() =>
        _rehomed.IdentityCatalog.Semantics.Count().ShouldBeLessThan(Original.IdentityCatalog.Semantics.Count());
    [Fact] void should_drop_the_events_the_subset_does_not_contain() => _rehomed.IdentityCatalog.EventContracts.ShouldBeEmpty();
    [Fact] void should_still_refuse_the_subset_as_a_whole_model() => _whole.ShouldBeOfExactType<InvalidScreenplayWorkspace>();
}
