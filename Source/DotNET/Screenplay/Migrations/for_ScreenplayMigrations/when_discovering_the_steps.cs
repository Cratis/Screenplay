// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_ScreenplayMigrations;

public class when_discovering_the_steps : Specification
{
    IReadOnlyList<IScreenplayMigration> _steps;
    MigrationPlan _plan;

    void Because()
    {
        _steps = ScreenplayMigrations.Discover();
        _plan = ScreenplayMigrations.Plan(_steps, 5, ScreenplayVersion.Current);
    }

    [Fact] void should_find_the_slots_and_outlets_step() => _steps.OfType<SlotsAndOutlets>().ShouldNotBeEmpty();
    [Fact] void should_find_steps_that_chain_without_overlap() => _steps.Count.ShouldEqual(_plan.Steps.Count);
    [Fact] void should_end_at_the_compilers_version() => _plan.Steps[^1].To.ShouldEqual(ScreenplayVersion.Current);
    [Fact] void should_move_from_five_to_six_with_slots_and_outlets() => _plan.Steps.Select(step => $"{step.From}→{step.To} {step.Name}").ShouldContain("5→6 slots-and-outlets");
}
