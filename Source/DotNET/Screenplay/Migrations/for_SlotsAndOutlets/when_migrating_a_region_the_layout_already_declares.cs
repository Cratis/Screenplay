// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

public class when_migrating_a_region_the_layout_already_declares : given.a_fixture
{
    void Because() => MigrateSource("""
        layout AppShell
          category Shell
          navigation
          content
          outlet sidebar of navigation
            default orders title "Orders" icon receipt target Shop
          navigation sidebar
            item open title "Open orders" target Shop
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
    [Fact] void should_say_to_merge_the_two() => ReviewAt(7).Message.ShouldContain("the layout AppShell already declares outlet 'sidebar' - merge the two");
}
