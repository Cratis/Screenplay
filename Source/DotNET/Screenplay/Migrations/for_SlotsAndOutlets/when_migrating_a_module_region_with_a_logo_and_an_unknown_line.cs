// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

public class when_migrating_a_module_region_with_a_logo_and_an_unknown_line : given.a_fixture
{
    void Because() => MigrateSource("""
        module Shop
          navigation sidebar extends
            logo image "/assets/shop.svg" title "Shop"
            item open title "Open orders" target Shop.Orders
            divider
          feature Orders
            slice StateView OrderList
              readmodel OrderRow
                orderId Uuid
              query AllOrders => OrderRow[]
        """);

    [Fact] void should_need_review() => _result.Outcome.ShouldEqual(MigrationOutcome.NeedsReview);
    [Fact] void should_still_rewrite_the_region() => _result.Migrated.ShouldContain("  outlet sidebar extends\n");
    [Fact] void should_say_a_module_cannot_declare_the_logo() => ReviewAt(3).Message.ShouldContain("a module cannot declare the logo - move it to the layout's 'outlet sidebar of navigation'");
    [Fact] void should_name_the_unknown_line() => ReviewAt(5).Message.ShouldContain("'divider' in the sidebar region is neither an item, a button nor a logo");
    [Fact] void should_name_the_file() => _result.Reviews.All(review => review.Location.Path == "document.play").ShouldBeTrue();
}
