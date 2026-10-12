// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_a_seed_value : given.a_seed_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Registered", "remove") with { EventNeverPersisted = true });

    [Fact] void should_accept() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_remove_seed_assignments() => RegisteredSeeds.SelectMany(seed => seed.Properties).ShouldBeEmpty();
    [Fact] void should_report_removed_seed_fixtures() => Result.PropertyReport.Changed.Count(change => change.Change == "fixtureRemoved" && change.Role == "fixture").ShouldEqual(2);
    [Fact] void should_not_weaken_assertions() => Result.PropertyReport.Changed.Any(change => change.WeakensAssertion).ShouldBeFalse();
    [Fact] void should_leave_unrelated_seed_assignments() => Seeds.Single(seed => seed.Event == "Other").Properties.Single().Property.ShouldEqual("name");
    [Fact] void should_leave_event_source_ids() => Candidate.Contains("for \"name\"", StringComparison.Ordinal).ShouldBeTrue();
}
