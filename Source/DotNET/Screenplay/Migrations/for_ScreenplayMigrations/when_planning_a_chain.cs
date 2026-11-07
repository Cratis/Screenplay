// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

public class when_planning_a_chain : Specification
{
    MigrationPlan _plan;

    void Because() => _plan = ScreenplayMigrations.Plan([new given.a_step(6, 7, "later"), new given.a_step(4, 5, "first"), new given.a_step(5, 6, "second")], 4, 7);

    [Fact] void should_run_every_step_between_the_versions_in_order() => _plan.Steps.Select(step => step.Name.Value).ShouldEqual(["first", "second", "later"]);
    [Fact] void should_describe_the_steps() => _plan.Description.ShouldEqual("4→5 first, 5→6 second, 6→7 later");
}
