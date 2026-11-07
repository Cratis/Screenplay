// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

/// <summary>
/// No step leaves a version older than the oldest step - a Screenplay 4 model is written in the forms the 5 to 6 step
/// reads - so the refusal says where to start.
/// </summary>
public class when_planning_from_before_the_oldest_step : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ScreenplayMigrations.Plan([new given.a_step(5, 6, "slots-and-outlets")], 4, 6));

    [Fact] void should_refuse_the_plan() => _error.ShouldBeOfExactType<InvalidMigrationPlan>();
    [Fact] void should_name_the_version_no_step_leaves() => _error.Message.ShouldContain("No migration step goes from Screenplay 4");
    [Fact] void should_say_where_the_oldest_step_starts() => _error.Message.ShouldContain("the oldest step starts at Screenplay 5 - a model written for an earlier version migrates from 5");
}
