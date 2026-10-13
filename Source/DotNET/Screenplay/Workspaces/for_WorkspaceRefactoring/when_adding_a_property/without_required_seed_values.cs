// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class without_required_seed_values : given.a_seed_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest("Registered") with { EventNeverPersisted = true });

    [Fact] void should_require_a_fixture_value() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FixtureValueRequired);
    [Fact] void should_list_every_seed_entry_including_empty_entries() => Result.Conflicts.Single().Candidates.Length.ShouldEqual(3);
    [Fact] void should_label_the_seed_candidates_as_fixtures() => Result.Conflicts.Single().Candidates.All(candidate => candidate.Role == "fixture").ShouldBeTrue();
    [Fact] void should_have_no_candidate_workspace() => Result.Workspace.ShouldBeNull();
    [Fact] void should_have_no_write_plan() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_have_no_changes() => Result.PropertyReport.Changed.ShouldBeEmpty();
}
