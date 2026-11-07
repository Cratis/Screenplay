// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Migrations.for_SlotsAndOutlets;

public class when_migrating_a_feature_toolbar_item : given.a_fixture
{
    void Because() => MigrateSource("""
        module Sales
          feature Orders
            toolbar actions
              button refresh title "Refresh" runs refresh
              item cancel action CancelOrder
            slice StateChange CancelOrder
              command CancelOrder
                orderId Uuid identifier
        """);

    [Fact] void should_need_review() => _result.Outcome.ShouldEqual(MigrationOutcome.NeedsReview);
    [Fact] void should_write_the_outlet_change_with_its_buttons() => _result.Migrated.ShouldContain("    outlet actions extends\n      button refresh");
    [Fact] void should_say_the_item_has_no_outlet_form() => ReviewAt(5).Message.ShouldContain("toolbar item 'cancel' (action CancelOrder) has no outlet form - write it as 'button cancel title \"…\" slice <Slice>' or remove it");
}
