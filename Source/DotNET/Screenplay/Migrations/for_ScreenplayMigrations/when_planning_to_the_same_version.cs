// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

public class when_planning_to_the_same_version : Specification
{
    MigrationPlan _plan;

    void Because() => _plan = ScreenplayMigrations.Plan([new given.a_step(5, 6, "step")], 6, 6);

    [Fact] void should_run_no_steps() => _plan.Steps.ShouldBeEmpty();
}
