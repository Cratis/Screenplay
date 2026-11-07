// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_feature_and_module_outlets : given.a_printer
{
    const string Source =
        """
        module Shop
          screen template Page
            category Test
            header
            outlet shell of actions in header
            outlet actions of actions in header
          outlet shell extends
            button help title "Help" slice Shop.Orders.PlaceOrder emphasis quiet
          feature Orders
            outlet actions extends
              button cancel title "Cancel order" slice CancelOrder icon trash
                confirm "Cancel this order?"
              button place title "Place order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
                confirm $strings.orders.confirmPlace
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                produces OrderPlaced
                  orderId = orderId
              event OrderPlaced
                orderId Uuid
            slice StateChange CancelOrder
              command CancelOrder
                orderId Uuid identifier
                produces OrderCancelled
                  orderId = orderId
              event OrderCancelled
                orderId Uuid
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse_to_the_same_diagnostics() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(_roundtrip.Original!.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_feature_outlet() => _roundtrip.Printed.ShouldContain("  feature Orders\n    outlet actions extends\n      button cancel title \"Cancel order\" slice CancelOrder icon trash\n        confirm \"Cancel this order?\"");
    [Fact] void should_print_the_button_confirmation_under_its_button() => _roundtrip.Printed.ShouldContain("button place title \"Place order\" slice Shop.Orders.PlaceOrder emphasis primary icon plus\n        confirm $strings.orders.confirmPlace");
    [Fact] void should_print_the_module_outlet() => _roundtrip.Printed.ShouldContain("  outlet shell extends\n    button help title \"Help\" slice Shop.Orders.PlaceOrder emphasis quiet");
}
