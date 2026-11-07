// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

public class when_migrating_layout_navigation_regions : given.a_fixture
{
    void Because() => Migrate("layout-navigation");

    [Fact] void should_write_the_screenplay_6_form() => _result.Migrated.ShouldEqual(_expected);
    [Fact] void should_compile_without_errors_or_warnings() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_need_no_review() => _result.Outcome.ShouldEqual(MigrationOutcome.Migrated);
    [Fact] void should_move_the_buttons_with_their_comments_to_an_actions_outlet() => _result.Migrated.ShouldContain("  outlet topbarActions of actions\n    // Registering a customer is an action, not a destination.\n    default register");
}
