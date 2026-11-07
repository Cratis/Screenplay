// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

public class when_planning_with_overlapping_steps : Specification
{
    Exception _error;

    // The overlap is outside the requested versions; it is refused all the same.
    void Because() => _error = Catch.Exception(() => ScreenplayMigrations.Plan([new given.a_step(4, 5, "first"), new given.a_step(5, 7, "wide"), new given.a_step(6, 7, "narrow")], 4, 5));

    [Fact] void should_refuse_the_plan() => _error.ShouldBeOfExactType<InvalidMigrationPlan>();
    [Fact] void should_name_both_steps() => _error.Message.ShouldContain("'wide' (5→7) and 'narrow' (6→7) overlap");
}
