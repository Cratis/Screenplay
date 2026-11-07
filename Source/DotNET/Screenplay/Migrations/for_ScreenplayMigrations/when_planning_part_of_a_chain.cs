// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

public class when_planning_part_of_a_chain : Specification
{
    MigrationPlan _plan;

    void Because() => _plan = ScreenplayMigrations.Plan([new given.a_step(4, 5, "first"), new given.a_step(5, 6, "second"), new given.a_step(6, 7, "later")], 5, 6);

    [Fact] void should_run_only_the_steps_between_the_versions() => _plan.Steps.Select(step => step.Name.Value).ShouldEqual(["second"]);
}
