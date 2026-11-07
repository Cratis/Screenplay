// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

public class when_planning_across_a_gap : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ScreenplayMigrations.Plan([new given.a_step(4, 5, "first"), new given.a_step(6, 7, "later")], 4, 7));

    [Fact] void should_refuse_the_plan() => _error.ShouldBeOfExactType<InvalidMigrationPlan>();
    [Fact] void should_name_the_version_no_step_leaves() => _error.Message.ShouldContain("No migration step goes from Screenplay 5");
}
