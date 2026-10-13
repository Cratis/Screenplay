// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_optional_seed_values : given.a_seed_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest("Registered") with { EventNeverPersisted = true, Optional = true, FixtureValue = "\"Normal\"" });

    [Fact] void should_accept_without_needing_seed_values() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_not_add_optional_seed_values() => RegisteredSeeds.SelectMany(seed => seed.Properties).Select(property => property.Property).ShouldContainOnly("name", "name");
    [Fact] void should_not_report_fixture_changes() => Result.PropertyReport.Changed.Where(change => change.Role == "fixture").ShouldBeEmpty();
}
