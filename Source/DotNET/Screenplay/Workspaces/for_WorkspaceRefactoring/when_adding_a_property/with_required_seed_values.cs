// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_required_seed_values : given.a_seed_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest("Registered") with { EventNeverPersisted = true, FixtureValue = "\"Normal\"" });

    [Fact] void should_accept() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_fill_every_seed_entry_including_empty_entries() => RegisteredSeeds.All(seed => seed.Properties.Any(property => property.Property == "priority" && property.Source is LiteralExpressionSyntax literal && Equals(literal.Value, "Normal"))).ShouldBeTrue();
    [Fact] void should_report_every_added_fixture() => Result.PropertyReport.Changed.Count(change => change.Change == "fixtureAdded" && change.Role == "fixture").ShouldEqual(3);
    [Fact] void should_leave_unrelated_seed_assignments() => Seeds.Single(seed => seed.Event == "Other").Properties.Select(property => property.Property).ShouldContainOnly("name");
    [Fact] void should_leave_event_source_ids() => Candidate.Contains("for \"name\"", StringComparison.Ordinal).ShouldBeTrue();
}
