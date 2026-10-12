// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class without_values_for_case_examples : given.a_case_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(CaseRequest());

    [Fact] void should_require_an_explicit_literal_for_examples_and_steps() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FixtureValueRequired);
    [Fact] void should_list_all_four_occurrences() => Result.Conflicts.Single().Candidates.Length.ShouldEqual(4);
    [Fact] void should_not_offer_partial_writes() => Result.WritePlan.ShouldBeNull();
}
