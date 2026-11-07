// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

public class when_migrating_a_template_toolbar_slot_holding_a_component : given.a_fixture
{
    void Because() => Migrate("template-toolbar-slot-with-component");

    [Fact] void should_move_the_buttons_and_leave_the_rest() => _result.Migrated.ShouldEqual(_expected);
    [Fact] void should_need_review() => _result.Outcome.ShouldEqual(MigrationOutcome.NeedsReview);
    [Fact] void should_name_the_slot_line_for_the_component() => _result.Reviews.Single(review => review.Message.Contains("other line", StringComparison.Ordinal)).Location.Line.ShouldEqual(5);
    [Fact] void should_say_what_the_slot_holds() => _result.Reviews.Single(review => review.Location.Line == 5).Message.ShouldContain("holds 3 items and 1 other line");
    [Fact] void should_name_the_item_without_an_outlet_form() => _result.Reviews.Single(review => review.Location.Line == 9).Message.ShouldContain("toolbar item 'cancel' (action CancelOrder)");
    [Fact] void should_name_the_file() => _result.Reviews.All(review => review.Location.Path == "document.play").ShouldBeTrue();
    [Fact] void should_report_that_what_is_left_does_not_compile() => _result.Diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldContain(5);
}
