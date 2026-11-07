// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

public class when_a_step_goes_backwards : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ScreenplayMigrations.Plan([new given.a_step(6, 5, "back")], 5, 6));

    [Fact] void should_refuse_the_plan() => _error.ShouldBeOfExactType<InvalidMigrationPlan>();
}
