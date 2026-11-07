// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

public class when_migrating_a_module_navigation_region_with_buttons_and_no_actions_outlet : given.a_fixture
{
    void Because() => Migrate("module-navigation-buttons-without-target");

    MigrationReview Review => _result.Reviews.Single();

    [Fact] void should_need_review() => _result.Outcome.ShouldEqual(MigrationOutcome.NeedsReview);
    [Fact] void should_keep_the_buttons_out_of_the_navigation_outlet() => _result.Migrated.ShouldContain("  outlet topbar extends\n    item open title \"Open orders\" target Shop.Orders\n  outlet topbarActions extends\n");
    [Fact] void should_report_the_region_line() => Review.Location.Line.ShouldEqual(17);
    [Fact] void should_report_the_file() => Review.Location.Path.ShouldEqual("document.play");
    [Fact] void should_say_no_layout_offers_the_actions_outlet() => Review.Message.ShouldContain("no layout in this document offers 'outlet topbarActions of actions'");
}
