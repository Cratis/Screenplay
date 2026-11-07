// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

public class when_migrating_a_layout_region_with_a_mode_and_an_unknown_line : given.a_fixture
{
    void Because() => MigrateSource("""
        layout AppShell
          category Shell
          navigation
          content
          navigation sidebar overrides
            item orders title "Orders" icon receipt target Shop
            divider
          arrangement flow
            row
              navigation width 240
              content grow

        module Shop
          feature Orders
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              query AllOrders => OrderRow[]
        """);

    [Fact] void should_need_review() => _result.Outcome.ShouldEqual(MigrationOutcome.NeedsReview);
    [Fact] void should_declare_the_outlet() => _result.Migrated.ShouldContain("  outlet sidebar of navigation\n    default orders");
    [Fact] void should_say_the_mode_was_dropped() => _result.Reviews.Single(review => review.Message.Contains("mode")).Message.ShouldContain("a layout AppShell declares its outlets - the mode 'override' was dropped");
    [Fact] void should_locate_the_mode_on_the_region() => _result.Reviews.Single(review => review.Message.Contains("mode")).Location.Line.ShouldEqual(5);
    [Fact] void should_name_the_unknown_line() => ReviewAt(7).Message.ShouldContain("'divider' in the sidebar region is neither an item, a button nor a logo");
}
